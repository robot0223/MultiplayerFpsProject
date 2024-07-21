using FPS_personal_project;
using Fusion;
using UnityEngine;

namespace FPS_personal_project
{
    /// <summary>
    /// Weapons component hold references to all player weapons
    /// and allows for weapon actions such as Fire or Reload.
    /// </summary>
    public class Weapons : NetworkBehaviour
    {
        //public Animator Animator;
        public Transform FireTransform;
       

        

        [Networked]
        public Weapon CurrentWeapon { get; set; }
       

       
        [Networked]
        private Weapon _pendingWeapon { get; set; }

        private Weapon _visibleWeapon;

        public void Fire(bool justPressed)
        {           
            CurrentWeapon.Fire(FireTransform.position, FireTransform.forward, justPressed);
        }

        public void Reload()
        {
            

            CurrentWeapon.Reload();
        }

      

       /* public Weapon GetWeapon(EWeaponType weaponType)
        {
            for (int i = 0; i < AllWeapons.Length; ++i)
            {
                if (AllWeapons[i].Type == weaponType)
                    return AllWeapons[i];
            }

            return default;
        }*/

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
               // CurrentWeapon = AllWeapons[0];
                CurrentWeapon.IsCollected = true;
            }
        }

        public override void FixedUpdateNetwork()
        {
            TryActivatePendingWeapon();
        }

        public override void Render()
        {
            if (_visibleWeapon == CurrentWeapon)
                return;

            //int currentWeaponID = -1;

           /* // Update weapon visibility
            for (int i = 0; i < AllWeapons.Length; i++)
            {
                var weapon = AllWeapons[i];
                if (weapon == CurrentWeapon)
                {
                    currentWeaponID = i;
                    weapon.ToggleVisibility(true);
                }
                else
                {
                    weapon.ToggleVisibility(false);
                }
            }*/

            _visibleWeapon = CurrentWeapon;

           // Animator.SetFloat("WeaponID", currentWeaponID);
        }

        private void Awake()
        {
            // All weapons are already present inside Player prefab.
            // This is the simplest solution when only few weapons are available in the game.
           // AllWeapons = GetComponentsInChildren<Weapon>();
        }

        private void TryActivatePendingWeapon()
        {
            
            //_pendingWeapon = GetWeapon();
            /*CurrentWeapon = _pendingWeapon;
            _pendingWeapon = null;*/

            // Make the weapon immediately active.
            CurrentWeapon.gameObject.SetActive(true);

            if (HasInputAuthority && Runner.IsForward)
            {
                //CurrentWeapon.Animator.SetTrigger("Show");
            }
        }
    }
}
