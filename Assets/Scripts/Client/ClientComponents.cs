using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace FPS_personal_project
{
    public struct ClientTeamRequest : IComponentData
    {
        public TeamType Value;
    }

}
