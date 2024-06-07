using Unity.Entities;
using UnityEngine;

namespace TMG.NFE_Tutorial
{
    public class GameOverOnDestroyAuthoring : MonoBehaviour
    {
        public class GameOverOnDestroyBaker : Baker<GameOverOnDestroyAuthoring>
        {
            public override void Bake(GameOverOnDestroyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<GameOverOnDestroyTag>(entity);
            }
        }
    }
}