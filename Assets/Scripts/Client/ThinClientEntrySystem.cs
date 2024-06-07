using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace TMG.NFE_Tutorial
{
    [WorldSystemFilter(WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ThinClientEntrySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var thinClientDummy = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponent<ChampMoveTargetPosition>(thinClientDummy);
            state.EntityManager.AddBuffer<InputBufferData<ChampMoveTargetPosition>>(thinClientDummy);

            var connectionEntity = SystemAPI.GetSingletonEntity<NetworkId>();
            SystemAPI.SetComponent(connectionEntity, new CommandTarget { targetEntity = thinClientDummy });
            var connectionId = SystemAPI.GetSingleton<NetworkId>().Value;
            state.EntityManager.AddComponentData(thinClientDummy, new GhostOwner { NetworkId = connectionId });

            var thinClientRequestEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(thinClientRequestEntity, new ClientTeamRequest
            {
                Value = TeamType.AutoAssign
            });

            state.EntityManager.AddComponentData(thinClientDummy, new ThinClientInputProperties
            {
                Random = Random.CreateFromIndex((uint)connectionId),
                Timer = 0f,
                MinTimer = 1f,
                MaxTimer = 10f,
                MinPosition = new float3(-50f, 0f, -50f),
                MaxPosition = new float3(50f, 0f, 50f)
            });
        }
    }
}