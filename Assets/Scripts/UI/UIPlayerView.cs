using FPS_personal_project;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FPS_personal_project
{


    public class UIPlayerView : MonoBehaviour
    {
        //public TextMeshProUGUI Nickname;
        public UIHealth Health;
        public UIWeapons Weapons;
        public UICrosshair Crosshair;

        public void UpdatePlayer(Player player, PlayerData playerData)
        {
            //Nickname.text = playerData.Nickname;

            Health.UpdateHealth(player.Health);
            Weapons.UpdateWeapons(player.Weapons);

            Crosshair.gameObject.SetActive(player.Health.IsAlive);
        }
    }

}
