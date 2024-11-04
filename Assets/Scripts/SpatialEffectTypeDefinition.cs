using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace FPS_personal_project
{

    [CreateAssetMenu(fileName = "SpatialEffectTypeDefinition", menuName = "FPS_personal_project/Effect/SpatialEffectTypeDefinition")]
    public class SpatialEffectTypeDefinition : ScriptableObject
    {
        [Header("Visual Effect")]
        [Tooltip("Impact Effect template used by VFXImpactManager")]
        public VisualEffectAsset effect;

        public AudioClip sound;

        [Serializable]
        public class ShockwaveSettings
        {
            public bool enabled;
            public float force = 7;
            public float radius = 5;
            public float upwardsModifier = 0.0f;
            public ForceMode mode = ForceMode.Impulse;
        }

        public ShockwaveSettings shockwave;
    }

}
