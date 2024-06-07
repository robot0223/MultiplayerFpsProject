using Unity.Entities;

namespace FPS_personal_project
{
    public readonly partial struct MinionSpawnAspect : IAspect
    {
        private readonly RefRW<MinionSpawnTimers> _minionSpawnTimers;
        private readonly RefRO<MinionSpawnProperties> _minionSpawnProperties;

        public int CountSpawnedInWave
        {
            get => _minionSpawnTimers.ValueRO.CountSpawnedInWave;
            set => _minionSpawnTimers.ValueRW.CountSpawnedInWave = value;
        }

        private float TimeToNextMinion
        {
            get => _minionSpawnTimers.ValueRO.TimeToNextMinion;
            set => _minionSpawnTimers.ValueRW.TimeToNextMinion = value;
        }

        private float TimeToNextWave
        {
            get => _minionSpawnTimers.ValueRO.TimeToNextWave;
            set => _minionSpawnTimers.ValueRW.TimeToNextWave = value;
        }

        private int CountToSpawnInWave => _minionSpawnProperties.ValueRO.CountToSpawnInWave;
        private float TimeBetweenMinions => _minionSpawnProperties.ValueRO.TimeBetweenMinions;
        private float TimeBetweenWaves => _minionSpawnProperties.ValueRO.TimeBetweenWaves;

        public bool ShouldSpawn => TimeToNextWave <= 0f && TimeToNextMinion <= 0f;
        public bool IsWaveSpawned => CountSpawnedInWave >= CountToSpawnInWave;

        public void DecrementTimers(float deltaTime)
        {
            if (TimeToNextWave >= 0f)
            {
                TimeToNextWave -= deltaTime;
                return;
            }

            if (TimeToNextMinion >= 0f)
            {
                TimeToNextMinion -= deltaTime;
            }
        }

        public void ResetWaveTimer()
        {
            TimeToNextWave = TimeBetweenWaves;
        }

        public void ResetMinionTimer()
        {
            TimeToNextMinion = TimeBetweenMinions;
        }

        public void ResetSpawnCounter()
        {
            CountSpawnedInWave = 0;
        }
    }
}