using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FPS_personal_project
{


    public class UIWeapons : MonoBehaviour
    {
        public Image WeaponIcon;
        //public Image WeaponIconShadow;
       // public TextMeshProUGUI WeaponName;
        public TextMeshProUGUI ClipAmmo;
        public TextMeshProUGUI RemainingAmmo;
       //public Image AmmoProgress;
        public GameObject NoAmmoGroup;
      // public CanvasGroup[] WeaponThumbnails;

        private Weapon _weapon;
        private int _lastClipAmmo;
        private int _lastRemainingAmmo;

        public void UpdateWeapons(Weapons weapons)
        {
            SetWeapon(weapons.CurrentWeapon);

            // Update weapon thumbnails.
            var weapon = weapons.CurrentWeapon;
            //WeaponThumbnails[(int)weapon.Type].alpha =  weapon.HasAmmo ? 1f : 0.2f;
            

            if (_weapon == null)
                return;

            UpdateAmmoProgress();

            // Modify UI text only when value changed.
            if (_weapon.ClipAmmo == _lastClipAmmo && _weapon.MaxClipAmmo == _lastRemainingAmmo)
                return;

            ClipAmmo.text = _weapon.ClipAmmo.ToString();
            RemainingAmmo.text = _weapon.MaxClipAmmo < 1000 ? _weapon.MaxClipAmmo.ToString() : "-";

            NoAmmoGroup.SetActive(_weapon.ClipAmmo == 0 && _weapon.MaxClipAmmo == 0);

            _lastClipAmmo = _weapon.ClipAmmo;
            _lastRemainingAmmo = _weapon.MaxClipAmmo;
        }

        private void SetWeapon(Weapon weapon)
        {
            if (weapon == _weapon)
                return;

            _weapon = weapon;

            if (weapon == null)
                return;

            WeaponIcon.sprite = weapon.Icon;
            //WeaponIconShadow.sprite = weapon.Icon;
           // WeaponName.text = weapon.Name;
        }

        
        private void UpdateAmmoProgress()
        {
            if (_weapon.IsReloading)
            {
                // During reloading the ammo progress bar slowly fills.
                //AmmoProgress.fillAmount = _weapon.GetReloadProgress();
            }
            else
            {
                //AmmoProgress.fillAmount = _weapon.ClipAmmo / (float)_weapon.MaxClipAmmo;
            }
        }
    }
}
