using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace FPS_personal_project
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct CharMoveSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamePlayingTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, movePosition, moveSpeed) in SystemAPI
                         .Query<RefRW<LocalTransform>, CharMoveTargetPosition, CharacterMoveSpeed>()
                         .WithAll<Simulate>())
            {
                var moveTarget = movePosition.Value;
                moveTarget.y = transform.ValueRO.Position.y;

                if (math.distancesq(transform.ValueRO.Position, moveTarget) < 0.001f) continue;
                var moveDirection = math.normalize(moveTarget - transform.ValueRO.Position);
                var moveVector = moveDirection * moveSpeed.Value * deltaTime;
                transform.ValueRW.Position += moveVector;
                transform.ValueRW.Rotation = quaternion.LookRotationSafe(moveDirection, math.up());
            }
        }
    }
}