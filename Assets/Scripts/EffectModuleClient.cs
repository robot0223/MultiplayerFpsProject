using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FPS_personal_project
{


    public class EffectModuleClient : MonoBehaviour
    {
        public GameObject vfxManager;
        
       // public HitscanEffectTypeDefinition testEffect;
        public GameObject vfxManagerInScene;
        private void Start()
        {
           
                vfxManagerInScene = Instantiate(vfxManager);
            
        }

        private void Update()
        {

           /* Keyboard keyboard = Keyboard.current;

            if(keyboard.zKey.wasPressedThisFrame)
            {
                Debug.LogWarning("zkey pressed");
                _vfxManagerInScene.GetComponent<HitscanEffectSystems>().Request(testEffect, this.gameObject.transform.position, this.gameObject.transform.position + new Vector3(100, 100, 100));
            }*/
        }
    }
}
