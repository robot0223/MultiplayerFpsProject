using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace TMG.NFE_Tutorial
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial class RespawnChampSystem : SystemBase
    {
        public Action<int> OnUpdateRespawnCountdown;
        public Action OnRespawn;

        protected override void OnCreate()
        {
            RequireForUpdate<NetworkTime>();
            RequireForUpdate<MobaPrefabs>();
        }
        
        protected override void OnStartRunning()
        {
            if (SystemAPI.HasSingleton<RespawnEntityTag>()) return;
            var respawnPrefab = SystemAPI.GetSingleton<MobaPrefabs>().RespawnEntity;
            EntityManager.Instantiate(respawnPrefab);
        }

        protected override void OnUpdate()
        {
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.IsFirstTimeFullyPredictingTick) return;
            var currentTick = networkTime.ServerTick;
            
            var isServer = World.IsServer();
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var respawnBuffer in SystemAPI.Query<DynamicBuffer<RespawnBufferElement>>()
                         .WithAll<RespawnTickCount, Simulate>())
            {
                var respawnsToCleanup = new NativeList<int>(Allocator.Temp);
                
                for (var i = 0; i < respawnBuffer.Length; i++)
                {
                    var curRespawn = respawnBuffer[i];

                    if (currentTick.Equals(curRespawn.RespawnTick) || currentTick.IsNewerThan(curRespawn.RespawnTick))
                    {
                        if (isServer)
                        {
                            var networkId = SystemAPI.GetComponent<NetworkId>(curRespawn.NetworkEntity).Value;
                            var playerSpawnInfo = SystemAPI.GetComponent<PlayerSpawnInfo>(curRespawn.NetworkEntity);

                            var championPrefab = SystemAPI.GetSingleton<MobaPrefabs>().Champion;
                            var newChamp = ecb.Instantiate(championPrefab);

                            ecb.SetComponent(newChamp, new GhostOwner { NetworkId = networkId });
                            ecb.SetComponent(newChamp, new MobaTeam { Value = playerSpawnInfo.MobaTeam });
                            ecb.SetComponent(newChamp, LocalTransform.FromPosition(playerSpawnInfo.SpawnPosition));
                            ecb.SetComponent(newChamp, new ChampMoveTargetPosition
                            {
                                Value = playerSpawnInfo.SpawnPosition
                            });
                            ecb.AppendToBuffer(curRespawn.NetworkEntity, new LinkedEntityGroup { Value = newChamp });
                            ecb.SetComponent(newChamp, new NetworkEntityReference { Value = curRespawn.NetworkEntity });

                            respawnsToCleanup.Add(i);
                        }
                        else
                        {
                            OnRespawn?.Invoke();
                        }
                    }
                    else if (!isServer)
                    {
                        if (SystemAPI.TryGetSingleton<NetworkId>(out var networkId))
                        {
                            if (networkId.Value == curRespawn.NetworkId)
                            {
                                var ticksToRespawn = curRespawn.RespawnTick.TickIndexForValidTick - currentTick.TickIndexForValidTick;
                                var simulationTickRate = NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;
                                var secondsToStart = (int)math.ceil((float)ticksToRespawn / simulationTickRate);
                                OnUpdateRespawnCountdown?.Invoke(secondsToStart);
                            }
                        }
                    }
                }

                foreach (var respawnIndex in respawnsToCleanup)
                {
                    respawnBuffer.RemoveAt(respawnIndex);
                }
            }
            
            ecb.Playback(EntityManager);
        }
    }
}