using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FPS_personal_project
{
    public struct GameStartProperties : IComponentData
    {
        public int MaxPlayersPerTeam;
        public int MinPlayersToStartGame;
        public int CountdownTime;
    }

    public struct TeamPlayerCounter : IComponentData
    {
        public int BlueTeamPlayers;
        public int RedTeamPlayers;

        public int TotalPlayers => BlueTeamPlayers + RedTeamPlayers;
    }

    public struct SpawnOffset : IBufferElementData
    {
        public float3 Value;
    }
}
