using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace FPS_personal_project
{
    public struct CharTag : IComponentData { }

    public struct NewCharTag : IComponentData { }

    public struct OwnerCharTag : IComponentData { }

    public struct GameTeam : IComponentData
    {
        [GhostField] public TeamType Value;
    }

    public struct CharacterMoveSpeed : IComponentData
    {
        public float Value;
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct CharMoveTargetPosition : IInputComponentData
    {
        [GhostField(Quantization = 0)] public float3 Value;
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct AbilityInput : IInputComponentData
    {
        [GhostField] public InputEvent AoeAbility;
        [GhostField] public InputEvent SkillShotAbility;
        [GhostField] public InputEvent ConfirmSkillShotAbility;
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct AimInput : IInputComponentData
    {
        [GhostField(Quantization = 0)] public float3 Value;
    }
}
