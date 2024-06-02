using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


namespace FPS_personal_project
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class InitializeMainCameraSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<MainCameraTag>();
        }

        protected override void OnUpdate()
        {
            Enabled = false;
            var mainCameraEntity = SystemAPI.GetSingletonEntity<MainCameraTag>();
            EntityManager.SetComponentData(mainCameraEntity, new MainCamera { Value = Camera.main });
        }
    }
}

