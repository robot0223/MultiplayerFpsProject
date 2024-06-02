using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace FPS_personal_project
{

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct InitializeLocalCharSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (transform, entity) in SystemAPI.Query<LocalTransform>().WithAll<GhostOwnerIsLocal>()
                         .WithNone<OwnerCharTag>().WithEntityAccess())
            {
                ecb.AddComponent<OwnerCharTag>(entity);
                ecb.SetComponent(entity, new CharMoveTargetPosition { Value = transform.Position });
            }

            ecb.Playback(state.EntityManager);
        }
    }
}