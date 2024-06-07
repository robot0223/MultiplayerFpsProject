using Unity.Entities;
using UnityEngine;

namespace TMG.NFE_Tutorial
{
    public class MainCamera : IComponentData
    {
        public Camera Value;
    }

    public struct MainCameraTag : IComponentData {}
}