using Fusion;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

namespace FPS_personal_project
{
    public enum EWeaponType
    {
        None = -1,
        Terraformer_GooGun,
        Robot_BarrelMachineGun,
        Terraformer_Sniper,
    }

    /// <summary>
    /// Main script that handles all the shooting. Weapon fires hitscan projectiles that are synchronized
    /// over the networked through projectile data buffer (_projectileData array). Check Projectiles Essentials
    /// sample where the basic projectile concepts and their implementation in Fusion is explained in detail.
    /// </summary>
    public class Weapon : NetworkBehaviour
    {
        public EWeaponType Type;

        [Header("Fire Setup")]
        public bool IsAutomatic = true;
        public float Damage = 10f;
        public float FireRate = 100;
        public bool isProjectile = false;
        public NetworkObject ProjectilePrefab;
        [Range(1, 20)]
        public int ProjectilesPerShot = 1;
        public float Dispersion = 0f;
        public LayerMask HitMask;
        public float MaxHitDistance = 100;
        public bool InfinitHitDistance = false;

        [Header("Ammo")]
        public int MaxClipAmmo = 12;
        //public int StartAmmo = 25;
        public float ReloadTime = 2f;

        [Header("Visuals")]
        public Sprite Icon;
        public string Name;
        public Animator Animator;
        public Animator WeaponAnimator;
        [FormerlySerializedAs("WeaponVisual")]
        //public GameObject FirstPersonVisual;
        // public GameObject ThirdPersonVisual;

        [Header("Fire Effect")]
        [FormerlySerializedAs("MuzzleTransform")]
        public Transform FirstPersonMuzzleTransform;
        public Transform ThirdPersonMuzzleTransform;
        public VisualEffect FirstPersonMuzzleEffect;
        public VisualEffect ThirdPersonMuzzleEffect;
        public ProjectileVisual ProjectileVisualPrefab;
        public HitscanEffectTypeDefinition HitscanEffect;
        public SpatialEffectTypeDefinition ImpactEffect;

        [Header("Sounds")]
        public AudioSource FireSound;
        public AudioSource ReloadingSound;
        public AudioSource EmptyClipSound;

        //public bool HasAmmo => ClipAmmo > 0 || RemainingAmmo > 0;

        /*[Networked]
        public NetworkBool IsCollected { get; set; }*/
        [Networked]
        public NetworkBool IsReloading { get; set; }
        [Networked]
        public int ClipAmmo { get; set; }
        /*[Networked]
        public int RemainingAmmo { get; set; }*/

        [Networked]
        private int _fireCount { get; set; }
        [Networked]
        private TickTimer _fireCooldown { get; set; }
        [Networked, Capacity(32)]
        private NetworkArray<ProjectileData> _projectileData { get; }

        private int _fireTicks;
        private int _visibleFireCount;
        private bool _reloadingVisible;
        private VisualEffect _muzzleEffectInstance;
        private SceneObjects _sceneObjects;


        private void Awake()
        {
            MaxHitDistance = InfinitHitDistance ? float.PositiveInfinity : MaxHitDistance;
        }
        public void Fire(Vector3 firePosition, Vector3 fireDirection, bool justPressed)
        {

            /*if (IsCollected == false)
                return;*/
            if (justPressed == false && IsAutomatic == false)
                return;
            if (IsReloading)
                return;
            if (_fireCooldown.ExpiredOrNotRunning(Runner) == false)
                return;

            if (ClipAmmo <= 0)
            {
                PlayEmptyClipSound(justPressed);
                return;
            }

            // Random needs to be initialized with same seed on both input and
            // state authority to ensure the projectiles are fired in the same direction on both.
            Random.InitState(Runner.Tick * unchecked((int)Object.Id.Raw));

            for (int i = 0; i < ProjectilesPerShot; i++)
            {
                var projectileDirection = fireDirection;

                if (Dispersion > 0f)
                {
                    // We use unit sphere on purpose -> non-uniform distribution (more projectiles in the center).
                    var dispersionRotation = Quaternion.Euler(Random.insideUnitSphere * Dispersion);
                    projectileDirection = dispersionRotation * fireDirection;
                }

                FireProjectile(firePosition, projectileDirection);
            }

            _fireCooldown = TickTimer.CreateFromTicks(Runner, _fireTicks);
            ClipAmmo--;
        }

        public void Reload()
        {
            /* if (IsCollected == false)
                 return;*/
            if (ClipAmmo >= MaxClipAmmo)
                return;
            /* if (RemainingAmmo <= 0)
                 return;*/
            if (IsReloading)
                return;
            if (_fireCooldown.ExpiredOrNotRunning(Runner) == false)
                return; // Fire finishing.

            IsReloading = true;
            _fireCooldown = TickTimer.CreateFromSeconds(Runner, ReloadTime);
        }

        /*public void AddAmmo(int amount)
        {
            RemainingAmmo += amount;
        }*/

        /* public void ToggleVisibility(bool isVisible)
         {
             FirstPersonVisual.SetActive(isVisible);
             ThirdPersonVisual.SetActive(isVisible);

             if (_muzzleEffectInstance != null)
             {
                 _muzzleEffectInstance.SetActive(false);
             }
         }*/

        public float GetReloadProgress()
        {
            if (IsReloading == false)
                return 1f;

            return 1f - _fireCooldown.RemainingTime(Runner).GetValueOrDefault() / ReloadTime;
        }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                //ClipAmmo = Mathf.Clamp(StartAmmo, 0, MaxClipAmmo);
                //RemainingAmmo = StartAmmo - ClipAmmo;
                ClipAmmo = MaxClipAmmo;

            }

            _visibleFireCount = _fireCount;

            float fireTime = 60f / FireRate;
            _fireTicks = Mathf.CeilToInt(fireTime / Runner.DeltaTime);

            /* _muzzleEffectInstance = Instantiate(MuzzleEffectPrefab, HasInputAuthority ? FirstPersonMuzzleTransform : ThirdPersonMuzzleTransform);
             _muzzleEffectInstance.SetActive(false);*/

            _sceneObjects = Runner.GetSingleton<SceneObjects>();
        }

        public override void FixedUpdateNetwork()
        {
            /* if (IsCollected == false)
                 return;*/

            if (ClipAmmo == 0)
            {
                // Try auto-reload.
                Reload();
            }

            if (IsReloading && _fireCooldown.ExpiredOrNotRunning(Runner))
            {
                // Reloading finished.
                IsReloading = false;

                int reloadAmmo = MaxClipAmmo - ClipAmmo;
                // reloadAmmo = Mathf.Min(reloadAmmo, RemainingAmmo);

                ClipAmmo += reloadAmmo;
                // RemainingAmmo -= reloadAmmo;

                // Add small prepare time after reload.
                _fireCooldown = TickTimer.CreateFromSeconds(Runner, 0.25f);
            }
        }

        public override void Render()
        {
            if (_visibleFireCount < _fireCount)
            {
                PlayFireEffect();
            }

            // Prepare projectile visuals for all projectiles that were not displayed yet.
            for (int i = _visibleFireCount; i < _fireCount; i++)
            {
                var data = _projectileData[i % _projectileData.Length];
                var muzzleTransform = HasInputAuthority ? FirstPersonMuzzleTransform : ThirdPersonMuzzleTransform;

                //var projectileVisual = Instantiate(ProjectileVisualPrefab, muzzleTransform.position, muzzleTransform.rotation);
                //projectileVisual.SetHit(data.HitPosition, data.HitNormal, data.ShowHitEffect
                //Debug.LogWarning(Runner.GetSingleton<SceneObjects>().EffectModuleClient == null);
                _sceneObjects.EffectModuleClient.vfxManagerInScene.GetComponent<HitscanEffectSystems>().Request(HitscanEffect, muzzleTransform.position, data.HitPosition);
                _sceneObjects.EffectModuleClient.vfxManagerInScene.GetComponent<SpatialEffectSystems>().Request(ImpactEffect, data.HitPosition, Quaternion.FromToRotation(Vector3.up, data.HitNormal));
            }

            _visibleFireCount = _fireCount;

            if (_reloadingVisible != IsReloading)
            {


                if (IsReloading)
                {
                    ReloadingSound.Play();
                    SetAnimationTrigger("TriggerAction_Reloading");
                }

                _reloadingVisible = IsReloading;
            }
        }

        private void FireProjectile(Vector3 firePosition, Vector3 fireDirection)
        {
            if (!isProjectile)
            {


                var projectileData = new ProjectileData();
                var hitOptions = HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority;


                // Whole projectile path and effects are immediately processed (= hitscan projectile).
                if (Runner.LagCompensation.Raycast(firePosition, fireDirection, MaxHitDistance,
                       player: Object.InputAuthority, out var hit, HitMask, hitOptions))
                {

                    projectileData.HitPosition = hit.Point;
                    projectileData.HitNormal = hit.Normal;

                    if (hit.Hitbox != null)
                    {
                        ApplyDamage(hit.Hitbox, hit.Point, fireDirection);

                    }
                    else
                    {
                        //Hit effect is shown only when player hits solid object.
                        projectileData.ShowHitEffect = true;

                    }
                }
                _projectileData.Set(_fireCount % _projectileData.Length, projectileData);
            }
            else//projectile is not finished....
            {
                Runner.Spawn(ProjectilePrefab, transform.position + Vector3.forward,
                    Quaternion.LookRotation(Vector3.forward), Object.InputAuthority);
            }
           

            _fireCount++;
        }

        private void PlayFireEffect()
        {
            if (FireSound != null)
            {
                FireSound.PlayOneShot(FireSound.clip);
            }


            FirstPersonMuzzleEffect.Play();

            ThirdPersonMuzzleEffect.Play();
            //_muzzleEffectInstance.SetActive(true);

            SetAnimationTrigger("TriggerAction_PrimaryFire");

            GetComponentInParent<Player>().PlayFireEffect();
        }

        private void ApplyDamage(Hitbox enemyHitbox, Vector3 position, Vector3 direction)
        {
            var enemyHealth = enemyHitbox.Root.GetComponent<Health>();
            if (enemyHealth == null || enemyHealth.IsAlive == false)
                return;

            float damageMultiplier = enemyHitbox is BodyHitbox bodyHitbox ? bodyHitbox.DamageMultiplier : 1f;
            bool isCriticalHit = damageMultiplier > 1f;

            float damage = Damage * damageMultiplier;
            /*if (_sceneObjects.Gameplay.DoubleDamageActive)
            {
                damage *= 2f;
            }*/

            if (enemyHealth.ApplyDamage(Object.InputAuthority, damage, position, direction, Type, isCriticalHit) == false)
                return;

            if (HasInputAuthority && Runner.IsForward)
            {
                // For local player show UI hit effect.
                _sceneObjects.GameUI.PlayerView.Crosshair.ShowHit(enemyHealth.IsAlive == false, isCriticalHit);
            }
        }

        private void PlayEmptyClipSound(bool fireJustPressed)
        {
            // For automatic weapons we want to play empty clip sound once after last fire.
            bool firstEmptyShot = _fireCooldown.TargetTick.GetValueOrDefault() == Runner.Tick - 1;

            if (fireJustPressed == false && firstEmptyShot == false)
                return;

            if (EmptyClipSound == null || EmptyClipSound.isPlaying)
                return;

            if (Runner.IsForward && HasInputAuthority)
            {
                EmptyClipSound.Play();
            }
        }

        private void SetAnimationTrigger(string trigger)
        {
            Animator.SetTrigger(trigger);
            WeaponAnimator.SetTrigger(trigger);
        }

        /// <summary>
        /// Structure representing single projectile shot.
        /// </summary>
        private struct ProjectileData : INetworkStruct
        {
            public Vector3 HitPosition;
            public Vector3 HitNormal;
            public NetworkBool ShowHitEffect;
        }
    }
}
