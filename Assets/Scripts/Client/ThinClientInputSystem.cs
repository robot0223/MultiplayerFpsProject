using Unity.Entities;
using Unity.NetCode;

namespace TMG.NFE_Tutorial
{
    [WorldSystemFilter(WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct ThinClientInputSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (moveTargetPosition, inputProperties) in SystemAPI.Query<RefRW<ChampMoveTargetPosition>, 
                         RefRW<ThinClientInputProperties>>())
            {
                inputProperties.ValueRW.Timer -= deltaTime;

                if (inputProperties.ValueRO.Timer > 0f) continue;

                var randomPosition = inputProperties.ValueRW.Random.NextFloat3(inputProperties.ValueRO.MinPosition,
                    inputProperties.ValueRO.MaxPosition);
                moveTargetPosition.ValueRW.Value = randomPosition;

                inputProperties.ValueRW.Timer =
                    inputProperties.ValueRW.Random.NextFloat(inputProperties.ValueRO.MinTimer,
                        inputProperties.ValueRO.MaxTimer);
            }
        }
    }
}