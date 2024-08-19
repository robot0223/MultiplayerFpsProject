using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPS_personal_project
{
    public class HitscanEffectSystems : MonoBehaviour
    {
        struct HitscanEffectReques
        {
            public HitscanEffectTypeDefinition effectDef;
            public Vector3 startPos;
            public Vector3 endPos;
        }

        List<HitscanEffectReques> m_requests = new List<HitscanEffectReques>(32);

        public void Request(HitscanEffectTypeDefinition effectDef, Vector3 startPos, Vector3 endPos)
        {
            Debug.LogWarning("Requesting effect");
            m_requests.Add(new HitscanEffectReques
            {
                effectDef = effectDef,
                startPos = startPos,
                endPos = endPos,
            });
        }

        private void Update()
        {
            for (int nRequest = 0; nRequest < m_requests.Count; nRequest++)
            {
                var request = m_requests[nRequest];

                if (request.effectDef.effect != null)
                {
                    var vfxSystem = this.gameObject.GetComponent<VFXSystem>();

                    vfxSystem.SpawnLineEffect(request.effectDef.effect, request.startPos, request.endPos);
                }
            }
            m_requests.Clear();
        }

    }
}
