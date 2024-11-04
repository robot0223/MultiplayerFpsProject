using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileBullet : NetworkBehaviour
{
    public int Damage = 10;
    public float Speed = 10;
    public float MaxDistance;
    public bool InfinitDistance;

    public override void FixedUpdateNetwork()
    {
        transform.position += Speed * transform.forward * Runner.DeltaTime;
        
    }
}
