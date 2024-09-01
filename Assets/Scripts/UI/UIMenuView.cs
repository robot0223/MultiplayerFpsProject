
using UnityEngine;
using UnityEngine.UI;

namespace FPS_personal_project
{
	public class UIMenuView : MonoBehaviour
	{
		public Button LeaveMatchButton;

		private GameUI _gameUI;
		private MainMenuUI _mainMenuUI;
		private GameObject _managerUI;
		
		private CursorLockMode _previousLockState;
		private bool _previousCursorVisibility;
		private bool _useGameUI;
		// Called from button OnClick event.
		public void ResumeGame()
		{
			gameObject.SetActive(false);
		}

		// Called from button OnClick event.
		public void OpenSettings()
		{
			if(_useGameUI)
				_gameUI.SettingsView.gameObject.SetActive(true);
			else
				_mainMenuUI.SettingsView.gameObject.SetActive(true);
		}

		// Called from button OnClick event.
		public void LeaveGame()
		{
			// Clear previous cursor state so it does not get locked when unloading scene.
			_previousLockState = CursorLockMode.None;
			_previousCursorVisibility = true;
			if(_useGameUI)
				_gameUI.GoToMenu();
			
			
		}

		private  void GetUIManager(GameObject managerObject, bool useA)
		{ 
			if(useA)
				_gameUI = managerObject.GetComponent<GameUI>();
			else
				_mainMenuUI = managerObject.GetComponent<MainMenuUI>();
		}

		private void Awake()
		{
			_managerUI = GetComponentInParent<GameUI>() ? GetComponentInParent<GameUI>().gameObject : GetComponentInParent<MainMenuUI>().gameObject;
			_useGameUI = GetComponentInParent<GameUI>();
			GetUIManager(_managerUI, _useGameUI);
			LeaveMatchButton.gameObject.SetActive(_useGameUI);
		}

		private void OnEnable()
		{
			
			_previousLockState = Cursor.lockState;
			_previousCursorVisibility = Cursor.visible;

			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}

		private void OnDisable()
		{
			Cursor.lockState = _previousLockState;
			Cursor.visible = _previousCursorVisibility;
		}
	}
}
