using System.Collections;
using System.Collections.Generic;
using Unity.NetCode;
using UnityEngine;

namespace FPS_personal_project
{
    public struct GameTeamRequest : IRpcCommand
    {
        public TeamType Value;
    }

    public struct PlayersRemainingToStart : IRpcCommand
    {
        public int Value;
    }

    public struct GameStartTickRpc : IRpcCommand
    {
        public NetworkTick Value;
    }
}