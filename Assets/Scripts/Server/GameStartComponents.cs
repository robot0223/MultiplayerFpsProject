using Unity.Entities;
using Unity.Mathematics;

namespace TMG.NFE_Tutorial
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