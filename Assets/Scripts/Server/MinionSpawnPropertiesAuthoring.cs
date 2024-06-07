using Unity.Entities;
using UnityEngine;

namespace TMG.NFE_Tutorial
{
    public class MinionSpawnPropertiesAuthoring : MonoBehaviour
    {
        public float TimeBetweenWaves;
        public float TimeBetweenMinions;
        public int CountToSpawnInWave;

        public class MinionSpawnPropertiesBaker : Baker<MinionSpawnPropertiesAuthoring>
        {
            public override void Bake(MinionSpawnPropertiesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MinionSpawnProperties
                {
                    TimeBetweenWaves = authoring.TimeBetweenWaves,
                    TimeBetweenMinions = authoring.TimeBetweenMinions,
                    CountToSpawnInWave = authoring.CountToSpawnInWave
                });
                AddComponent(entity, new MinionSpawnTimers
                {
                    TimeToNextWave = authoring.TimeBetweenWaves,
                    TimeToNextMinion = 0f,
                    CountSpawnedInWave = 0
                });
            }
        }
    }
}