using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace TMG.NFE_Tutorial
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct InitializeLocalChampSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (transform, entity) in SystemAPI.Query<LocalTransform>().WithAll<GhostOwnerIsLocal>()
                         .WithNone<OwnerChampTag>().WithEntityAccess())
            {
                ecb.AddComponent<OwnerChampTag>(entity);
                ecb.SetComponent(entity, new ChampMoveTargetPosition { Value = transform.Position });
            }
            
            ecb.Playback(state.EntityManager);
        }
    }
}