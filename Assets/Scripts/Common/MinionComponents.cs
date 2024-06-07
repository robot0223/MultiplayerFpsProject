using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace FPS_personal_project
{
    public struct MinionTag : IComponentData {}
    public struct NewMinionTag : IComponentData {}

    public struct MinionPathPosition : IBufferElementData
    {
        [GhostField(Quantization = 0)] public float3 Value;
    }
    
    public struct MinionPathIndex : IComponentData
    {
        [GhostField] public byte Value;
    }
}