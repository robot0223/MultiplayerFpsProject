using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace TMG.NFE_Tutorial
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct AimSkillShotSystem : ISystem
    {
        private CollisionFilter _selectionFilter;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MainCameraTag>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            _selectionFilter = new CollisionFilter
            {
                BelongsTo = 1 << 5, // Raycasts
                CollidesWith = 1 << 0 // GroundPlane
            };
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (aimInput, transform, skillShotUIReference) in SystemAPI
                         .Query<RefRW<AimInput>, LocalTransform, SkillShotUIReference>()
                         .WithAll<AimSkillShotTag, OwnerChampTag>())
            {
                skillShotUIReference.Value.transform.position = transform.Position;
                
                var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
                var cameraEntity = SystemAPI.GetSingletonEntity<MainCameraTag>();
                var mainCamera = state.EntityManager.GetComponentObject<MainCamera>(cameraEntity).Value;
                
                var mousePosition = Input.mousePosition;
                mousePosition.z = 1000f;
                var worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);

                var selectionInput = new RaycastInput
                {
                    Start = mainCamera.transform.position,
                    End = worldPosition,
                    Filter = _selectionFilter
                };
                
                if (collisionWorld.CastRay(selectionInput, out var closestHit))
                {
                    var directionToTarget = closestHit.Position - transform.Position;
                    directionToTarget.y = transform.Position.y;
                    directionToTarget = math.normalize(directionToTarget);
                    aimInput.ValueRW.Value = directionToTarget;
                    
                    var angleRag = math.atan2(directionToTarget.z, directionToTarget.x);
                    var angleDeg = math.degrees(angleRag);
                    skillShotUIReference.Value.transform.rotation = Quaternion.Euler(0f, -angleDeg, 0f);
                }
            }
        }
    }
}