using FPS_personal_project;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class DamageArea : NetworkBehaviour
{
    public Collider damageArea;
    public float enterDamage;
    public float continuousDamage;
    public float damageDuration;
    public bool instantKill;

    private Transform _transform;

    private void Awake()
    {
        _transform = this.gameObject.transform;
    }
    public override void FixedUpdateNetwork()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.LogWarning(other.ToString());
        Health health = other.gameObject.GetComponentInParent<Health>();
        if (instantKill)
            enterDamage = health.CurrentHealth;
        other.gameObject.GetComponentInParent<Health>().ApplyDamage(Runner.LocalPlayer, enterDamage,
            _transform.position, Vector3.zero, EWeaponType.None, false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.LogWarning(collision.ToString());
        Health health = collision.gameObject.GetComponentInParent<Health>();
        if (instantKill)
            enterDamage = health.CurrentHealth;
        collision.gameObject.GetComponentInParent<Health>().ApplyDamage(Runner.LocalPlayer, enterDamage,
            _transform.position, Vector3.zero, EWeaponType.None, false);

    }

}
