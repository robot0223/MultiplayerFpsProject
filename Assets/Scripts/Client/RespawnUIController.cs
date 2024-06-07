using TMPro;
using Unity.Entities;
using UnityEngine;

namespace TMG.NFE_Tutorial
{
    public class RespawnUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _respawnPanel;
        [SerializeField] private TextMeshProUGUI _respawnCountdownText;

        private void OnEnable()
        {
            _respawnPanel.SetActive(false);
            
            if (World.DefaultGameObjectInjectionWorld == null) return;
            var respawnSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<RespawnChampSystem>();
            if (respawnSystem != null)
            {
                respawnSystem.OnUpdateRespawnCountdown += UpdateRespawnCountdownText;
                respawnSystem.OnRespawn += CloseRespawnPanel;
            }
        }
        
        private void OnDisable()
        {
            if (World.DefaultGameObjectInjectionWorld == null) return;
            var respawnSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<RespawnChampSystem>();
            if (respawnSystem != null)
            {
                respawnSystem.OnUpdateRespawnCountdown -= UpdateRespawnCountdownText;
                respawnSystem.OnRespawn -= CloseRespawnPanel;
            }
        }
        
        private void UpdateRespawnCountdownText(int countdownTime)
        {
            if(!_respawnPanel.activeSelf) _respawnPanel.SetActive(true);
            
            _respawnCountdownText.text = countdownTime.ToString();
        }

        private void CloseRespawnPanel()
        {
            _respawnPanel.SetActive(false);
        }
    }
}