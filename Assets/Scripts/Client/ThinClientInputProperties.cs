using Unity.Entities;
using Unity.Mathematics;

namespace TMG.NFE_Tutorial
{
    public struct ThinClientInputProperties : IComponentData
    {
        public Random Random;
        public float Timer;
        public float MinTimer;
        public float MaxTimer;
        public float3 MinPosition;
        public float3 MaxPosition;
    }
}