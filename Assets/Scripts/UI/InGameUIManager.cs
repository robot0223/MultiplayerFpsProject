using Fusion;
using TMPro;
using UnityEngine;

namespace FPS_personal_project
{

   
    public class InGameUIManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI HealthText;
        NetworkRunner runner;
        GameObject[] allPlayers;
        Player localPlayer;

        private void Update()
        {
            runner = (GameObject.FindGameObjectWithTag("NetworkRunner")).GetComponent<NetworkRunner>();
            if(runner != null)
            {
                allPlayers = GameObject.FindGameObjectsWithTag("Player");
                foreach(var player in allPlayers)
                {
                    if (player.GetComponent<NetworkBehaviour>().HasInputAuthority)
                        localPlayer = player.GetComponent<Player>();
                }
                Debug.LogWarning("local Player" +localPlayer != null);
            }
        }






    }
}
