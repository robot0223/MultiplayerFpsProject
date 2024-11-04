using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EInputButton
{
    Jump,
    Fire,
    Reload
}

public struct NetworkedInput : INetworkInput
{
    public NetworkButtons Buttons;
    public Vector2 MoveDirection;
   // public Vector2 Direction;
    public Vector2 LookRotationDelta;
    public Vector2 LookDelta;
}