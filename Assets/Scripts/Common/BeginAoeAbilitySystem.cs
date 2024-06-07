using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace TMG.NFE_Tutorial
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct BeginAoeAbilitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.IsFirstTimeFullyPredictingTick) return;
            var currentTick = networkTime.ServerTick;

            foreach (var aoe in SystemAPI.Query<AoeAspect>().WithAll<Simulate>())
            {
                var isOnCooldown = true;
                var curTargetTicks = new AbilityCooldownTargetTicks();

                for (var i = 0u; i < networkTime.SimulationStepBatchSize; i++)
                {
                    var testTick = currentTick;
                    testTick.Subtract(i);

                    if (!aoe.CooldownTargetTicks.GetDataAtTick(testTick, out curTargetTicks))
                    {
                        curTargetTicks.AoeAbility = NetworkTick.Invalid;
                    }

                    if (curTargetTicks.AoeAbility == NetworkTick.Invalid ||
                        !curTargetTicks.AoeAbility.IsNewerThan(currentTick))
                    {
                        isOnCooldown = false;
                        break;
                    }
                }
                
                if (isOnCooldown) continue;
                
                if (aoe.ShouldAttack)
                {
                    var newAoeAbility = ecb.Instantiate(aoe.AbilityPrefab);
                    var abilityTransform = LocalTransform.FromPosition(aoe.AttackPosition);
                    ecb.SetComponent(newAoeAbility, abilityTransform);
                    ecb.SetComponent(newAoeAbility, aoe.Team);
                    
                    if(state.WorldUnmanaged.IsServer()) continue;
                    var newCooldownTargetTick = currentTick;
                    newCooldownTargetTick.Add(aoe.CooldownTicks);
                    curTargetTicks.AoeAbility = newCooldownTargetTick;

                    var nextTick = currentTick;
                    nextTick.Add(1u);
                    curTargetTicks.Tick = nextTick;

                    aoe.CooldownTargetTicks.AddCommandData(curTargetTicks);
                }
            }
        }
    }
}