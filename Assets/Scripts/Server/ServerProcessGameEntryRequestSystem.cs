using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;


namespace FPS_personal_project
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerProcessGameEntryRequestSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<GameStartProperties>();
            state.RequireForUpdate<GamePrefabs>();
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<GameTeamRequest, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        public void OnUpdate(ref SystemState state)
        {
            Debug.Log("We are updating!!");
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var characterPrefab = SystemAPI.GetSingleton<GamePrefabs>().Character;

            var gamePropertyEntity = SystemAPI.GetSingletonEntity<GameStartProperties>();
            var gameStartProperties = SystemAPI.GetComponent<GameStartProperties>(gamePropertyEntity);
            var teamPlayerCounter = SystemAPI.GetComponent<TeamPlayerCounter>(gamePropertyEntity);
            var spawnOffsets = SystemAPI.GetBuffer<SpawnOffset>(gamePropertyEntity);


            foreach (var (teamRequest, requestSource, requestEntity) in
                     SystemAPI.Query<GameTeamRequest, ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                ecb.DestroyEntity(requestEntity);
                ecb.AddComponent<NetworkStreamInGame>(requestSource.SourceConnection);

                var requestedTeamType = teamRequest.Value;

                if (requestedTeamType == TeamType.AutoAssign)
                {
                    if (teamPlayerCounter.BlueTeamPlayers > teamPlayerCounter.RedTeamPlayers)
                    {
                        requestedTeamType = TeamType.B;
                    }
                    else if (teamPlayerCounter.BlueTeamPlayers <= teamPlayerCounter.RedTeamPlayers)
                    {
                        requestedTeamType = TeamType.A;
                    }
                }

                var clientId = SystemAPI.GetComponent<NetworkId>(requestSource.SourceConnection).Value;
                float3 spawnPosition;

                switch (requestedTeamType)
                {
                    case TeamType.A:
                        if (teamPlayerCounter.BlueTeamPlayers >= gameStartProperties.MaxPlayersPerTeam)
                        {
                            Debug.Log($"Blue Team is full. Client ID: {clientId} is spectating the game");
                            continue;
                        }
                        spawnPosition = new float3(-50f, 1f, -50f);
                        spawnPosition += spawnOffsets[teamPlayerCounter.BlueTeamPlayers].Value;
                        teamPlayerCounter.BlueTeamPlayers++;
                        break;

                    case TeamType.B:
                        if (teamPlayerCounter.RedTeamPlayers >= gameStartProperties.MaxPlayersPerTeam)
                        {
                            Debug.Log($"Red Team is full. Client ID: {clientId} is spectating the game");
                            continue;
                        }
                        spawnPosition = new float3(50f, 1f, 50f);
                        spawnPosition += spawnOffsets[teamPlayerCounter.RedTeamPlayers].Value;
                        teamPlayerCounter.RedTeamPlayers++;
                        break;

                    default:
                        continue;
                }

                Debug.Log($"Server is assigning Client ID: {clientId} to the {requestedTeamType.ToString()} team.");

                var newChar = ecb.Instantiate(characterPrefab);
                ecb.SetName(newChar, "Character");

                var newTransform = LocalTransform.FromPosition(spawnPosition);
                ecb.SetComponent(newChar, newTransform);
                ecb.SetComponent(newChar, new GhostOwner { NetworkId = clientId });
                ecb.SetComponent(newChar, new GameTeam { Value = requestedTeamType });

                ecb.AppendToBuffer(requestSource.SourceConnection, new LinkedEntityGroup { Value = newChar });

                ecb.SetComponent(newChar, new NetworkEntityReference { Value = requestSource.SourceConnection });

                ecb.AddComponent(requestSource.SourceConnection, new PlayerSpawnInfo
                {
                    GameTeam = requestedTeamType,
                    SpawnPosition = spawnPosition
                });

                ecb.SetComponent(requestSource.SourceConnection, new CommandTarget { targetEntity = newChar });

                var playersRemainingToStart = gameStartProperties.MinPlayersToStartGame - teamPlayerCounter.TotalPlayers;

                var gameStartRpc = ecb.CreateEntity();
                if (playersRemainingToStart <= 0 && !SystemAPI.HasSingleton<GamePlayingTag>())
                {
                    var simulationTickRate = NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;
                    var ticksUntilStart = (uint)(simulationTickRate * gameStartProperties.CountdownTime);
                    var gameStartTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
                    gameStartTick.Add(ticksUntilStart);

                    ecb.AddComponent(gameStartRpc, new GameStartTickRpc
                    {
                        Value = gameStartTick
                    });

                    var gameStartEntity = ecb.CreateEntity();
                    ecb.AddComponent(gameStartEntity, new GameStartTick
                    {
                        Value = gameStartTick
                    });
                }
                else
                {
                    ecb.AddComponent(gameStartRpc, new PlayersRemainingToStart { Value = playersRemainingToStart });
                }
                ecb.AddComponent<SendRpcCommandRequest>(gameStartRpc);
            }

            ecb.Playback(state.EntityManager);
            SystemAPI.SetSingleton(teamPlayerCounter);
        }
    }
}

