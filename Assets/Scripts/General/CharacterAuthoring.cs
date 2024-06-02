using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace FPS_personal_project
{

public class CharacterAuthoring : MonoBehaviour
{
        public float MoveSpeed;

        public class CharacterComponentsBaker : Baker<CharacterAuthoring>
        {
            public override void Bake(CharacterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<CharTag>(entity);
                AddComponent<NewCharTag>(entity);
                AddComponent<GameTeam>(entity);
               // AddComponent<URPMaterialPropertyBaseColor>(entity);
                AddComponent<CharMoveTargetPosition>(entity);
                AddComponent(entity, new CharacterMoveSpeed { Value = authoring.MoveSpeed });
                AddComponent<AbilityInput>(entity);
                AddComponent<AimInput>(entity);
              //  AddComponent<NetworkEntityReference>(entity);
            }
        }
    }

}
