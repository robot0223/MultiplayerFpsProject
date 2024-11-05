using FPS_personal_project;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FPS_personal_project
{


    public class UIGameOverView : MonoBehaviour
    {
        public TextMeshProUGUI Winner;
        public GameObject VictoryGroup;
        public GameObject DefeatGroup;

        private GameUI _gameUI;
        private EGamePlayState _lastState;

        public void GoToMenu()
        {
            _gameUI.GoToMenu();
        }

        private void Awake()
        {
            _gameUI = GetComponentInParent<GameUI>();
        }

        private void Update()
        {
            if (_gameUI.Runner == null)
                return;

            // Unlock cursor.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_gameUI.GamePlay.Object == null || _gameUI.GamePlay.Object.IsValid == false)
                return;

            if (_lastState == _gameUI.GamePlay.State)
                return;

           

            _lastState = _gameUI.GamePlay.State;

            bool localPlayerIsWinner = false;
            Winner.text = string.Empty;

            foreach (var playerPair in _gameUI.GamePlay.AllPlayerData)
            {
                if (playerPair.Value.StatisticPosition != 1)
                    continue;

                Winner.text = $"Winner is {playerPair.Value.Nickname}";
                localPlayerIsWinner = playerPair.Key == _gameUI.Runner.LocalPlayer;
            }

            VictoryGroup.SetActive(localPlayerIsWinner);
            DefeatGroup.SetActive(localPlayerIsWinner == false);
        }
    }
}
