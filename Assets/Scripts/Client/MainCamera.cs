using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


namespace FPS_personal_project
{
    public class MainCamera : IComponentData
    {
        public Camera Value;
    }

    public struct MainCameraTag : IComponentData { }
}

