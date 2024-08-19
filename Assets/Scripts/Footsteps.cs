using Fusion;
using Fusion.Addons.KCC;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Footsteps : NetworkBehaviour
{
    public KCC KCC;
    public AudioClip[] FootstepClips;
    public AudioClip[] LandClips;
    public AudioSource FootstepsSource;
    public float FootstepDuration;


    [NonSerialized] public bool onFootDown;
    [NonSerialized] public bool onLand;

    private float _footstepCooldown;
    private bool _wasGrounded;

    

  

     public override void Spawned()
    {
        _wasGrounded = true;
        if(HasInputAuthority)
        {
            FootstepsSource.volume -= 0.1f;
        }
    }

    public override void FixedUpdateNetwork()
    {
        

        
        if (!KCC.FixedData.IsGrounded)
            return;

        if (KCC.FixedData.IsGrounded != _wasGrounded)
        {
            PlayClipSound(LandClips);
            _wasGrounded = KCC.FixedData.IsGrounded;
        }

        _footstepCooldown -= Time.deltaTime;

        if (_footstepCooldown <= 0f)
        {
            PlayClipSound(FootstepClips);
        }
    }

    private void PlayClipSound(AudioClip[] audios)
    {
        var clip = FootstepClips[UnityEngine.Random.Range(0, audios.Length)];
        FootstepsSource.PlayOneShot(clip);
        if(audios == FootstepClips)
            _footstepCooldown = FootstepDuration;
    }


}
