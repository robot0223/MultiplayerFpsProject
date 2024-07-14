using Fusion;
using System.Net.Http.Headers;
using Unity.VisualScripting;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{

    [SerializeField] private Transform ThirdPersonVisual;
   

    public static NetworkPlayer Local { get; set; }
    
    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            Local = this;
            Debug.Log("Spawned Local Player");
            foreach (Transform trans in ThirdPersonVisual.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = 31;
            }
            Camera[] Cameras = Camera.allCameras;

            

            foreach (Camera cam in Cameras)
            {
                if (cam.gameObject.tag == "PlayerCamera")
                {
                    cam.enabled = true;
                    continue;
                }

                cam.enabled = false;
            }


        }
        else
        {
            Debug.Log("Spawned Remote Player");
           
        }
        Runner.SetPlayerObject(Object.InputAuthority, Object);

        transform.name = $"P_{Object.Id}";
    }

    public void PlayerLeft(PlayerRef player)
    {
        if(player == Object.InputAuthority)
        {
            Runner.Despawn(Object);
        }
    }

}
