using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace FPS_personal_project
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct MoveMinionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamePlayingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, pathPositions, pathIndex, moveSpeed) in SystemAPI.Query<RefRW<LocalTransform>, 
                         DynamicBuffer<MinionPathPosition>, RefRW<MinionPathIndex>, CharacterMoveSpeed>()
                         .WithAll<Simulate>())
            {
                var curTargetPosition = pathPositions[pathIndex.ValueRO.Value].Value;
                if (math.distance(curTargetPosition, transform.ValueRO.Position) <= 1.5)
                {
                    if(pathIndex.ValueRO.Value >= pathPositions.Length - 1) continue;
                    pathIndex.ValueRW.Value++;
                    curTargetPosition = pathPositions[pathIndex.ValueRO.Value].Value;
                }

                curTargetPosition.y = transform.ValueRO.Position.y;
                var curHeading = math.normalizesafe(curTargetPosition - transform.ValueRO.Position);
                transform.ValueRW.Position += curHeading * moveSpeed.Value * deltaTime;
                transform.ValueRW.Rotation = quaternion.LookRotationSafe(curHeading, math.up());
            }
        }
    }
}