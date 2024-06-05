using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace FPS_personal_project
{
    public struct RespawnEntityTag : IComponentData { }

    public struct RespawnBufferElement : IBufferElementData
    {
        [GhostField] public NetworkTick RespawnTick;
        [GhostField] public Entity NetworkEntity;
        [GhostField] public int NetworkId;
    }

    public struct RespawnTickCount : IComponentData
    {
        public uint Value;
    }

    public struct PlayerSpawnInfo : IComponentData
    {
        public TeamType GameTeam;
        public float3 SpawnPosition;
    }

    public struct NetworkEntityReference : IComponentData
    {
        public Entity Value;
    }
}