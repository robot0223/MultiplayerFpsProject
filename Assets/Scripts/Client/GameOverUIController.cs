using TMPro;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TMG.NFE_Tutorial
{
    public class GameOverUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TextMeshProUGUI _gameOverText;
        [SerializeField] private Button _returnToMainButton;
        [SerializeField] private Button _rageQuitButton;

        private EntityQuery _networkConnectionQuery;
        
        private void OnEnable()
        {
            _returnToMainButton.onClick.AddListener(ReturnToMain);
            _rageQuitButton.onClick.AddListener(RageQuit);
            if (World.DefaultGameObjectInjectionWorld == null) return;
            _networkConnectionQuery =
                World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamConnection));
            var gameOverSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<GameOverSystem>();
            gameOverSystem.OnGameOver += ShowGameOverUI;
        }

        private void ReturnToMain()
        {
            if (_networkConnectionQuery.TryGetSingletonEntity<NetworkStreamConnection>(out var networkConnectionEntity))
            {
                World.DefaultGameObjectInjectionWorld.EntityManager.AddComponent<NetworkStreamRequestDisconnect>(
                    networkConnectionEntity);
            }

            World.DisposeAllWorlds();
            
            SceneManager.LoadScene(0);
        }

        private void RageQuit()
        {
            Application.Quit();
        }

        private void OnDisable()
        {
            if (World.DefaultGameObjectInjectionWorld == null) return;
            var gameOverSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<GameOverSystem>();
            gameOverSystem.OnGameOver -= ShowGameOverUI;
        }

        private void ShowGameOverUI(TeamType winningTeam)
        {
            _gameOverPanel.SetActive(true);
            _gameOverText.text = $"{winningTeam.ToString()} Team Wins!";
        }
    }
}