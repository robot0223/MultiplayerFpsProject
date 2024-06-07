using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace FPS_personal_project
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct BeginSkillShotSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            var netTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!netTime.IsFirstTimeFullyPredictingTick) return;
            var currentTick = netTime.ServerTick;
            var isServer = state.WorldUnmanaged.IsServer();

            foreach (var skillShot in SystemAPI.Query<SkillShotAspect>().WithAll<Simulate>().WithNone<AimSkillShotTag>())
            {
                var isOnCooldown = true;
                
                for (var i = 0u; i < netTime.SimulationStepBatchSize; i++)
                {
                    var testTick = currentTick;
                    testTick.Subtract(i);

                    if (!skillShot.CooldownTargetTicks.GetDataAtTick(currentTick, out var curTargetTicks))
                    {
                        curTargetTicks.SkillShotAbility = NetworkTick.Invalid;
                    }

                    if (curTargetTicks.SkillShotAbility == NetworkTick.Invalid ||
                        !curTargetTicks.SkillShotAbility.IsNewerThan(currentTick))
                    {
                        isOnCooldown = false;
                        break;
                    }
                }

                if (isOnCooldown) continue;

                if (!skillShot.BeginAttack) continue;
                ecb.AddComponent<AimSkillShotTag>(skillShot.ChampionEntity);

                if (isServer || !SystemAPI.HasComponent<OwnerChampTag>(skillShot.ChampionEntity)) continue;
                var skillShotUIPrefab = SystemAPI.ManagedAPI.GetSingleton<UIPrefabs>().SkillShot;
                var newSkillShotUI =
                    Object.Instantiate(skillShotUIPrefab, skillShot.AttackPosition, Quaternion.identity);
                ecb.AddComponent(skillShot.ChampionEntity, new SkillShotUIReference { Value = newSkillShotUI });
            }

            foreach (var skillShot in SystemAPI.Query<SkillShotAspect>().WithAll<AimSkillShotTag, Simulate>())
            {
                if (!skillShot.ConfirmAttack) continue;
                var skillShotAbility = ecb.Instantiate(skillShot.AbilityPrefab);
                
                var newPosition = skillShot.SpawnPosition;
                ecb.SetComponent(skillShotAbility, newPosition);
                ecb.SetComponent(skillShotAbility, skillShot.Team);
                ecb.RemoveComponent<AimSkillShotTag>(skillShot.ChampionEntity);
                
                if (isServer) continue;
                skillShot.CooldownTargetTicks.GetDataAtTick(currentTick, out var curTargetTicks);
                
                var newCooldownTargetTick = currentTick;
                newCooldownTargetTick.Add(skillShot.CooldownTicks);
                curTargetTicks.SkillShotAbility = newCooldownTargetTick;
                    
                var nextTick = currentTick;
                nextTick.Add(1u);
                curTargetTicks.Tick = nextTick;

                skillShot.CooldownTargetTicks.AddCommandData(curTargetTicks);
            }

            // Cleanup UI after user casts spell
            foreach (var (abilityInput, skillShotUIReference, entity) in SystemAPI
                         .Query<AbilityInput, SkillShotUIReference>().WithAll<Simulate>().WithEntityAccess())
            {
                if (!abilityInput.ConfirmSkillShotAbility.IsSet) continue;
                Object.Destroy(skillShotUIReference.Value);
                ecb.RemoveComponent<SkillShotUIReference>(entity);
            }

            // Cleanup UI if player entity gets destroyed
            foreach (var (skillShotUIReference, entity) in SystemAPI.Query<SkillShotUIReference>()
                         .WithAll<Simulate>().WithNone<LocalTransform>().WithEntityAccess())
            {
                Object.Destroy(skillShotUIReference.Value);
                ecb.RemoveComponent<SkillShotUIReference>(entity);
            }
            
            ecb.Playback(state.EntityManager);
        }
    }
}