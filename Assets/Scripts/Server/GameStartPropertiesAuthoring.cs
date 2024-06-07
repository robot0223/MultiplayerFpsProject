using Unity.Entities;
using UnityEngine;

namespace TMG.NFE_Tutorial
{
    public class GameStartPropertiesAuthoring : MonoBehaviour
    {
        public int MaxPlayersPerTeam;
        public int MinPlayersToStartGame;
        public int CountdownTime;
        public Vector3[] SpawnOffsets;
        
        public class GameStartPropertiesBaker : Baker<GameStartPropertiesAuthoring>
        {
            public override void Bake(GameStartPropertiesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new GameStartProperties
                {
                    MaxPlayersPerTeam = authoring.MaxPlayersPerTeam,
                    MinPlayersToStartGame = authoring.MinPlayersToStartGame,
                    CountdownTime = authoring.CountdownTime
                });
                
                AddComponent<TeamPlayerCounter>(entity);

                var spawnOffsets = AddBuffer<SpawnOffset>(entity);
                foreach (var spawnOffset in authoring.SpawnOffsets)
                {
                    spawnOffsets.Add(new SpawnOffset { Value = spawnOffset });
                }
            }
        }
    }
}