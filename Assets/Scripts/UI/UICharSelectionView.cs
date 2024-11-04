using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace FPS_personal_project
{


    public class UICharSelectionView : MonoBehaviour
    {
        public GameUI GameUI;
        
        public TextMeshProUGUI time;
       
        private void Start()
        {
            GameUI = GetComponentInParent<GameUI>();
          
           
        }
        private void Update()
        {
            if (GameUI.Runner == null)
                return;

            var gamePlay = GameUI.GamePlay;

            if (gamePlay == null || gamePlay.Object.IsValid == false)
                return;

            int remainingTime = (int)gamePlay.CharSelectionTime.RemainingTime(GameUI.Runner).GetValueOrDefault();

            int minute = remainingTime / 60;
            int second = remainingTime % 60;
            time.text = $"{ minute}:{  second }";
        }


    }
}