using Unity.Entities;

namespace TMG.NFE_Tutorial
{
    public struct MinionSpawnProperties : IComponentData
    {
        public float TimeBetweenWaves;
        public float TimeBetweenMinions;
        public int CountToSpawnInWave;
    }

    public struct MinionSpawnTimers : IComponentData
    {
        public float TimeToNextWave;
        public float TimeToNextMinion;
        public int CountSpawnedInWave;
    }

    public struct MinionPathContainers : IComponentData
    {
        public Entity TopLane;
        public Entity MidLane;
        public Entity BotLane;
    }
}