using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FPS_personal_project
{
    public class GameUI : MonoBehaviour
    {
        public GamePlay GamePlay;
        [HideInInspector]
        public NetworkRunner Runner;

        public UIPlayerView PlayerView;
        public UIGamePlayView GameplayView;
       // public UIGameOverView GameOverView;
        public GameObject ScoreboardView;
        public GameObject MenuView;
        public UISettingsView SettingsView;
        public GameObject DisconnectedView;

        public void OnRunnerShutdown(NetworkRunner runner, ShutdownReason reason)
        {
            /*if (GameOverView.gameObject.activeSelf)
                return; // Regular shutdown - GameOver already active*/

            ScoreboardView.SetActive(false);
            SettingsView.gameObject.SetActive(false);
            MenuView.gameObject.SetActive(false);

            DisconnectedView.SetActive(true);
        }

        public void GoToMenu()
        {
            if (Runner != null)
            {
                Runner.Shutdown();
            }

            SceneManager.LoadScene("Level_Menu_Background");
        }

        private void Awake()
        {
            PlayerView.gameObject.SetActive(false);
            MenuView.SetActive(false);
            SettingsView.gameObject.SetActive(false);
            DisconnectedView.SetActive(false);

            SettingsView.LoadSettings();

            // Make sure the cursor starts unlocked
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }


        private void Update()
        {
            if (Application.isBatchMode == true)
                return;
            if (GamePlay.Object == null || GamePlay.Object.IsValid == false)
                return;

            Runner = GamePlay.Runner;

            var keyboard = Keyboard.current;
            bool gameplayActive = GamePlay.State < EGamePlayState.Finished;

            ScoreboardView.SetActive(gameplayActive && keyboard != null && keyboard.tabKey.isPressed);

            if (gameplayActive && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                MenuView.SetActive(!MenuView.activeSelf);
            }

            GameplayView.gameObject.SetActive(gameplayActive);
            //GameOverView.gameObject.SetActive(gameplayActive == false);

            var playerObject = Runner.GetPlayerObject(Runner.LocalPlayer);
            if (playerObject != null)
            {
                var player = playerObject.GetComponent<Player>();
                var playerData = GamePlay.AllPlayerData.Get(Runner.LocalPlayer);

                PlayerView.UpdatePlayer(player, playerData);
                PlayerView.gameObject.SetActive(gameplayActive);
            }
            else
            {
                PlayerView.gameObject.SetActive(false);
            }
        }

    }


}


