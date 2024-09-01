using FPS_personal_project;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace FPS_personal_project
{

 
    public class MainMenuUI : MonoBehaviour
    {
        public UIMainMenuView MainMenuView;
        public GameObject MenuView;
        public UISettingsView SettingsView;
        public UILoadingScreen LoadingScreen;

        private void Awake()
        {


            MainMenuView.gameObject.SetActive(true);
            MenuView.SetActive(false);
            SettingsView.gameObject.SetActive(false);

            SettingsView.LoadSettings();

            //Make sure the Cursor is not locked and shown.
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void Update()
        {
            if (Application.isBatchMode)
                return;

            var keyboard = Keyboard.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                MenuView.SetActive(!MenuView.activeSelf);
        }

    }
}
