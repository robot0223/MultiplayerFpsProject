using Unity.Entities;
using UnityEngine;

namespace FPS_personal_project
{
    public class MainCameraAuthoring : MonoBehaviour
    {
        public class MainCameraBaker : Baker<MainCameraAuthoring>
        {
            public override void Bake(MainCameraAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponentObject(entity, new MainCamera());
                AddComponent<MainCameraTag>(entity);
            }
        }
    }
}