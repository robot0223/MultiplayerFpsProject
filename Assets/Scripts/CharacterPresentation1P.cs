using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterPresentation1P : MonoBehaviour
{
    [SerializeField] private GameObject Weapon1P;
    [SerializeField] private GameObject ArmAttachBone;
    [SerializeField] private GameObject WeaponAttachBone;

    private Camera PlayerCam;

    private void Start()
    {
        Camera[] Cameras = Camera.allCameras;
        foreach (Camera cam in Cameras)
        {
            if (cam.tag == "PlayerCamera")
                PlayerCam = cam;
        }
    }

    private void Update()
    {
        
        AttachWeapon1P();
    }

    private void AttachWeapon1P()
    {
        Weapon1P.transform.position = ArmAttachBone.transform.position;
        Weapon1P.transform.rotation = ArmAttachBone.transform.rotation;
    }
}
