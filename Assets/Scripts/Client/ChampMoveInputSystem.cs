using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FPS_personal_project
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial class ChampMoveInputSystem : SystemBase
    {
        //private MobaInputActions _inputActions;
        private CollisionFilter _selectionFilter;

        protected override void OnCreate()
        {
            RequireForUpdate<GamePlayingTag>();
           // _inputActions = new MobaInputActions();
            _selectionFilter = new CollisionFilter
            {
                BelongsTo = 1 << 5, // Raycasts
                CollidesWith = 1 << 0 // GroundPlane
            };
            RequireForUpdate<OwnerChampTag>();
        }
        
        protected override void OnStartRunning()
        {
          //  _inputActions.Enable();
           // _inputActions.GameplayMap.SelectMovePosition.performed += OnSelectMovePosition;
        }

        protected override void OnStopRunning()
        {
          //  _inputActions.GameplayMap.SelectMovePosition.performed -= OnSelectMovePosition;
          //  _inputActions.Disable();
        }

        private void OnSelectMovePosition(InputAction.CallbackContext obj)
        {
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            var cameraEntity = SystemAPI.GetSingletonEntity<MainCameraTag>();
            var mainCamera = EntityManager.GetComponentObject<MainCamera>(cameraEntity).Value;
            
            var mousePosition = Input.mousePosition;
            mousePosition.z = 100f;
            var worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
            
            var selectionInput = new RaycastInput
            {
                Start = mainCamera.transform.position,
                End = worldPosition,
                Filter = _selectionFilter
            };

            if (collisionWorld.CastRay(selectionInput, out var closestHit))
            {
                var champEntity = SystemAPI.GetSingletonEntity<OwnerChampTag>();
                EntityManager.SetComponentData(champEntity, new ChampMoveTargetPosition
                {
                    Value = closestHit.Position
                });
            }
        }

        protected override void OnUpdate()
        {
            
        }
    }
}