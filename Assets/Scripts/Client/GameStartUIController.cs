using System.Collections;
using TMPro;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TMG.NFE_Tutorial
{
    public class GameStartUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _beginGamePanel;
        [SerializeField] private GameObject _confirmQuitPanel;
        [SerializeField] private GameObject _countdownPanel;
        
        [SerializeField] private Button _quitWaitingButton;
        [SerializeField] private Button _confirmQuitButton;
        [SerializeField] private Button _cancelQuitButton;
        
        [SerializeField] private TextMeshProUGUI _waitingText;
        [SerializeField] private TextMeshProUGUI _countdownText;
        
        private EntityQuery _networkConnectionQuery;
        private EntityManager _entityManager;
        
        private void OnEnable()
        {
            _beginGamePanel.SetActive(true);
            
            _quitWaitingButton.onClick.AddListener(AttemptQuitWaiting);
            _confirmQuitButton.onClick.AddListener(ConfirmQuit);
            _cancelQuitButton.onClick.AddListener(CancelQuit);

            if (World.DefaultGameObjectInjectionWorld == null) return;
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _networkConnectionQuery = _entityManager.CreateEntityQuery(typeof(NetworkStreamConnection));
            
            var startGameSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ClientStartGameSystem>();
            if (startGameSystem != null)
            {
                startGameSystem.OnUpdatePlayersRemainingToStart += UpdatePlayerRemainingText;
                startGameSystem.OnStartGameCountdown += BeginCountdown;
            }

            var countdownSystem = World.DefaultGameObjectInjectionWorld
                .GetExistingSystemManaged<CountdownToGameStartSystem>();
            if (countdownSystem != null)
            {
                countdownSystem.OnUpdateCountdownText += UpdateCountdownText;
                countdownSystem.OnCountdownEnd += EndCountdown;
            }
        }
        
        private void OnDisable()
        {
            _quitWaitingButton.onClick.RemoveAllListeners();
            _confirmQuitButton.onClick.RemoveAllListeners();
            _cancelQuitButton.onClick.RemoveAllListeners();

            if (World.DefaultGameObjectInjectionWorld == null) return;
            var startGameSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ClientStartGameSystem>();
            if (startGameSystem != null)
            {
                startGameSystem.OnUpdatePlayersRemainingToStart -= UpdatePlayerRemainingText;
                startGameSystem.OnStartGameCountdown -= BeginCountdown;
            }
            
            var countdownSystem = World.DefaultGameObjectInjectionWorld
                .GetExistingSystemManaged<CountdownToGameStartSystem>();
            if (countdownSystem != null)
            {
                countdownSystem.OnUpdateCountdownText -= UpdateCountdownText;
                countdownSystem.OnCountdownEnd -= EndCountdown;
            }
        }
        
        private void UpdatePlayerRemainingText(int playersRemainingToStart)
        {
            var playersText = playersRemainingToStart == 1 ? "player" : "players";
            _waitingText.text = $"Waiting for {playersRemainingToStart.ToString()} more {playersText} to join...";
        }

        private void UpdateCountdownText(int countdownTime)
        {
            _countdownText.text = countdownTime.ToString();
        }
        
        private void AttemptQuitWaiting()
        {
            _beginGamePanel.SetActive(false);
            _confirmQuitPanel.SetActive(true);
        }

        private void ConfirmQuit()
        {
            StartCoroutine(DisconnectDelay());
        }

        IEnumerator DisconnectDelay()
        {
            yield return new WaitForSeconds(1f);
            if (_networkConnectionQuery.TryGetSingletonEntity<NetworkStreamConnection>(out var networkConnectionEntity))
            {
                World.DefaultGameObjectInjectionWorld.EntityManager.AddComponent<NetworkStreamRequestDisconnect>(
                    networkConnectionEntity);
            }
            World.DisposeAllWorlds();
            SceneManager.LoadScene(0);
        }
        
        private void CancelQuit()
        {
            _confirmQuitPanel.SetActive(false);
            _beginGamePanel.SetActive(true);
        }

        private void BeginCountdown()
        {
            _beginGamePanel.SetActive(false);
            _confirmQuitPanel.SetActive(false);
            _countdownPanel.SetActive(true);
        }

        private void EndCountdown()
        {
            _countdownPanel.SetActive(false);
        }
    }
}