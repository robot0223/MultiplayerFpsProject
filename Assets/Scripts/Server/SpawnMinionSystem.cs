using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace FPS_personal_project
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct SpawnMinionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamePlayingTag>();
            state.RequireForUpdate<MinionPathContainers>();
            state.RequireForUpdate<MobaPrefabs>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            foreach (var minionSpawnAspect in SystemAPI.Query<MinionSpawnAspect>())
            {
                minionSpawnAspect.DecrementTimers(deltaTime);
                if (minionSpawnAspect.ShouldSpawn)
                {
                    SpawnOnEachLane(ref state);
                    minionSpawnAspect.CountSpawnedInWave++;
                    if (minionSpawnAspect.IsWaveSpawned)
                    {
                        minionSpawnAspect.ResetMinionTimer();
                        minionSpawnAspect.ResetWaveTimer();
                        minionSpawnAspect.ResetSpawnCounter();
                    }
                    else
                    {
                        minionSpawnAspect.ResetMinionTimer();
                    }
                }
            }
        }

        private void SpawnOnEachLane(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var minionPrefab = SystemAPI.GetSingleton<MobaPrefabs>().Minion;
            var pathContainers = SystemAPI.GetSingleton<MinionPathContainers>();

            var topLane = SystemAPI.GetBuffer<MinionPathPosition>(pathContainers.TopLane);
            SpawnOnLane(ecb, minionPrefab, topLane);
            
            var midLane = SystemAPI.GetBuffer<MinionPathPosition>(pathContainers.MidLane);
            SpawnOnLane(ecb, minionPrefab, midLane);
            
            var botLane = SystemAPI.GetBuffer<MinionPathPosition>(pathContainers.BotLane);
            SpawnOnLane(ecb, minionPrefab, botLane);
        }

        private void SpawnOnLane(EntityCommandBuffer ecb, Entity minionPrefab, DynamicBuffer<MinionPathPosition> curLane)
        {
            var newBlueMinion = ecb.Instantiate(minionPrefab);
            for (var i = 0; i < curLane.Length; i++)
            {
                ecb.AppendToBuffer(newBlueMinion, curLane[i]);
            }

            var blueSpawnTransform = LocalTransform.FromPosition(curLane[0].Value);
            ecb.SetComponent(newBlueMinion, blueSpawnTransform);
            ecb.SetComponent(newBlueMinion, new MobaTeam { Value = TeamType.Blue });
            
            var newRedMinion = ecb.Instantiate(minionPrefab);
            for (var i = curLane.Length - 1; i >= 0; i--)
            {
                ecb.AppendToBuffer(newRedMinion, curLane[i]);
            }

            var redSpawnTransform = LocalTransform.FromPosition(curLane[^1].Value);
            ecb.SetComponent(newRedMinion, redSpawnTransform);
            ecb.SetComponent(newRedMinion, new MobaTeam { Value = TeamType.Red });
        }
    }
}