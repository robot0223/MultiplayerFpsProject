using Unity.Entities;
using Unity.Mathematics;

namespace FPS_personal_project
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