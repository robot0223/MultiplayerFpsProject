using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace FPS_personal_project
{


    [CreateAssetMenu(fileName = "HitscanEffectTypeDefinition", menuName = "FPS_personal_project/Effect/HitscanEffectTypeDefinition")]
    public class HitscanEffectTypeDefinition : ScriptableObject
    {
        public VisualEffectAsset effect;
    }

}
