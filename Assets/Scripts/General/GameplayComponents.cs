using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace FPS_personal_project
{
    public struct GamePlayingTag : IComponentData { }

    public struct GameStartTick : IComponentData
    {
        public NetworkTick Value;
    }

    public struct GameOverTag : IComponentData { }

    public struct WinningTeam : IComponentData
    {
        [GhostField] public TeamType Value;
    }
}