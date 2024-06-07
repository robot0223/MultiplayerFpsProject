using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

namespace FPS_personal_project
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct HealthBarSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<UIPrefabs>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Spawn health bars for entities that require them
            foreach (var (transform, healthBarOffset, maxHitPoints, entity) in SystemAPI
                         .Query<LocalTransform, HealthBarOffset, MaxHitPoints>().WithNone<HealthBarUIReference>()
                         .WithEntityAccess())
            {
                var healthBarPrefab = SystemAPI.ManagedAPI.GetSingleton<UIPrefabs>().HealthBar;
                var spawnPosition = transform.Position + healthBarOffset.Value;
                var newHealthBar = Object.Instantiate(healthBarPrefab, spawnPosition, Quaternion.identity);
                SetHealthBar(newHealthBar, maxHitPoints.Value, maxHitPoints.Value);
                ecb.AddComponent(entity, new HealthBarUIReference { Value = newHealthBar });
            }
            
            // Update position and values of health bar
            foreach (var (transform, healthBarOffset, currentHitPoints, maxHitPoints, healthBarUI) in SystemAPI
                         .Query<LocalTransform, HealthBarOffset, CurrentHitPoints, MaxHitPoints, HealthBarUIReference>())
            {
                var healthBarPosition = transform.Position + healthBarOffset.Value;
                healthBarUI.Value.transform.position = healthBarPosition;
                SetHealthBar(healthBarUI.Value, currentHitPoints.Value, maxHitPoints.Value);
            }
            
            // Cleanup health bar once associated entity is destroyed
            foreach (var (healthBarUI, entity) in SystemAPI.Query<HealthBarUIReference>().WithNone<LocalTransform>()
                         .WithEntityAccess())
            {
                Object.Destroy(healthBarUI.Value);
                ecb.RemoveComponent<HealthBarUIReference>(entity);
            }
        }
        
        private void SetHealthBar(GameObject healthBarCanvasObject, int curHitPoints, int maxHitPoints)
        {
            var healthBarSlider = healthBarCanvasObject.GetComponentInChildren<Slider>();
            healthBarSlider.minValue = 0;
            healthBarSlider.maxValue = maxHitPoints;
            healthBarSlider.value = curHitPoints;
        }
    }
}