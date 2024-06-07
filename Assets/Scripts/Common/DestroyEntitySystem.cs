using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace FPS_personal_project
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
    public partial struct DestroyEntitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RespawnEntityTag>();
            state.RequireForUpdate<MobaPrefabs>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NetworkTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();

            if (!networkTime.IsFirstTimeFullyPredictingTick) return;
            var currentTick = networkTime.ServerTick;
            
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (transform, entity) in SystemAPI.Query<RefRW<LocalTransform>>()
                         .WithAll<DestroyEntityTag, Simulate>().WithEntityAccess())
            {
                if (state.World.IsServer())
                {
                    if (SystemAPI.HasComponent<GameOverOnDestroyTag>(entity))
                    {
                        var gameOverPrefab = SystemAPI.GetSingleton<MobaPrefabs>().GameOverEntity;
                        var gameOverEntity = ecb.Instantiate(gameOverPrefab);

                        var losing = SystemAPI.GetComponent<MobaTeam>(entity).Value;
                        var winning = losing == TeamType.Blue ? TeamType.Red : TeamType.Blue;
                        Debug.Log($"{winning.ToString()} Team Won!!");

                        ecb.SetComponent(gameOverEntity, new WinningTeam { Value = winning });
                    }

                    if (SystemAPI.HasComponent<ChampTag>(entity))
                    {
                        var networkEntity = SystemAPI.GetComponent<NetworkEntityReference>(entity).Value;
                        var respawnEntity = SystemAPI.GetSingletonEntity<RespawnEntityTag>();
                        var respawnTickCount = SystemAPI.GetComponent<RespawnTickCount>(respawnEntity).Value;

                        var respawnTick = currentTick;
                        respawnTick.Add(respawnTickCount);

                        ecb.AppendToBuffer(respawnEntity, new RespawnBufferElement
                        {
                            NetworkEntity = networkEntity,
                            RespawnTick = respawnTick,
                            NetworkId = SystemAPI.GetComponent<NetworkId>(networkEntity).Value
                        });
                    }
                    
                    ecb.DestroyEntity(entity);
                }
                else
                {
                    transform.ValueRW.Position = new float3(1000f, 1000f, 1000f);
                }
            }
        }
    }
}