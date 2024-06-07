using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace FPS_personal_project
{
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    public partial struct DamageOnTriggerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var damageOnTriggerJob = new DamageOnTriggerJob
            {
                DamageOnTriggerLookup = SystemAPI.GetComponentLookup<DamageOnTrigger>(true),
                TeamLookup = SystemAPI.GetComponentLookup<MobaTeam>(true),
                AlreadyDamagedLookup = SystemAPI.GetBufferLookup<AlreadyDamagedEntity>(true),
                DamageBufferLookup = SystemAPI.GetBufferLookup<DamageBufferElement>(true),
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged)
            };
            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = damageOnTriggerJob.Schedule(simulationSingleton, state.Dependency);
        }
    }
    
    public struct DamageOnTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<DamageOnTrigger> DamageOnTriggerLookup;
        [ReadOnly] public ComponentLookup<MobaTeam> TeamLookup;
        [ReadOnly] public BufferLookup<AlreadyDamagedEntity> AlreadyDamagedLookup;
        [ReadOnly] public BufferLookup<DamageBufferElement> DamageBufferLookup;

        public EntityCommandBuffer ECB;
        
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity damageDealingEntity;
            Entity damageReceivingEntity;
            
            if (DamageBufferLookup.HasBuffer(triggerEvent.EntityA) &&
                DamageOnTriggerLookup.HasComponent(triggerEvent.EntityB))
            {
                damageReceivingEntity = triggerEvent.EntityA;
                damageDealingEntity = triggerEvent.EntityB;
            }
            else if (DamageOnTriggerLookup.HasComponent(triggerEvent.EntityA) &&
                     DamageBufferLookup.HasBuffer(triggerEvent.EntityB))
            {
                damageDealingEntity = triggerEvent.EntityA;
                damageReceivingEntity = triggerEvent.EntityB;
            }
            else
            {
                return;
            }
            
            // Don't apply damage multiple times
            var alreadyDamagedBuffer = AlreadyDamagedLookup[damageDealingEntity];
            foreach (var alreadyDamagedEntity in alreadyDamagedBuffer)
            {
                if (alreadyDamagedEntity.Value.Equals(damageReceivingEntity)) return;
            }
            
            // Ignore friendly fire
            if (TeamLookup.TryGetComponent(damageDealingEntity, out var damageDealingTeam) &&
                TeamLookup.TryGetComponent(damageReceivingEntity, out var damageReceivingTeam))
            {
                if (damageDealingTeam.Value == damageReceivingTeam.Value) return;
            }
            
            var damageOnTrigger = DamageOnTriggerLookup[damageDealingEntity];
            ECB.AppendToBuffer(damageReceivingEntity, new DamageBufferElement { Value = damageOnTrigger.Value });
            ECB.AppendToBuffer(damageDealingEntity, new AlreadyDamagedEntity { Value = damageReceivingEntity });
        }
    }
}