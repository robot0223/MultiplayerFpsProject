using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace FPS_personal_project
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(CalculateFrameDamageSystem))]
    public partial struct ApplyDamageSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<GamePlayingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (currentHitPoints, damageThisTickBuffer, entity) in SystemAPI
                         .Query<RefRW<CurrentHitPoints>, DynamicBuffer<DamageThisTick>>().WithAll<Simulate>()
                         .WithEntityAccess())
            {
                if(!damageThisTickBuffer.GetDataAtTick(currentTick, out var damageThisTick)) continue;
                if (damageThisTick.Tick != currentTick) continue;
                currentHitPoints.ValueRW.Value -= damageThisTick.Value;

                if (currentHitPoints.ValueRO.Value <= 0)
                {
                    ecb.AddComponent<DestroyEntityTag>(entity);
                }
            }
            
            ecb.Playback(state.EntityManager);
        }
    }
}