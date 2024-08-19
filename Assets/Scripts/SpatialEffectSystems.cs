using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace FPS_personal_project
{


    public class SpatialEffectSystems : MonoBehaviour
    {
        struct SpatialEffectRequest
        {
            public SpatialEffectTypeDefinition effectDef;
            public float3 position;
            public quaternion rotation;
        }

        public GameObject vfxSoundSource;
       
        List<SpatialEffectRequest> m_requests = new List<SpatialEffectRequest>(32);

        public void Request(SpatialEffectTypeDefinition effectDef, float3 position, quaternion rotation)
        {
            m_requests.Add(new SpatialEffectRequest
            {
                effectDef = effectDef,
                position = position,
                rotation = rotation,
            });
        }

        private void Update()
        {
             GameObject[] soundSources = new GameObject[m_requests.Count];

            for (int nRequest = 0; nRequest < m_requests.Count; nRequest++)
            {
                var request = m_requests[nRequest];

                if (request.effectDef.effect != null)
                {
                    var normal = math.mul(request.rotation, new float3(0, 0, 1));

                    var vfxSystem = this.gameObject.GetComponent<VFXSystem>();
                    vfxSystem.SpawnPointEffect(request.effectDef.effect, request.position, normal);
                }

                if (request.effectDef.sound != null)
                {
                    soundSources[nRequest] = Instantiate(vfxSoundSource);
                    soundSources[nRequest].transform.position = request.position;
                    soundSources[nRequest].GetComponent<AudioSource>().PlayOneShot(request.effectDef.sound);
                    
                }
                    
                    

                if (request.effectDef.shockwave.enabled)
                {
                    var layer = LayerMask.NameToLayer("Debris");
                    var mask = 1 << layer;
                    var explosionCenter = request.position + (float3)UnityEngine.Random.insideUnitSphere * 0.2f;
                    var colliders = Physics.OverlapSphere(request.position, request.effectDef.shockwave.radius, mask);
                    for (var i = 0; i < colliders.Length; i++)
                    {
                        var rigidBody = colliders[i].gameObject.GetComponent<Rigidbody>();
                        if (rigidBody != null)
                        {
                            rigidBody.AddExplosionForce(request.effectDef.shockwave.force, explosionCenter,
                                request.effectDef.shockwave.radius, request.effectDef.shockwave.upwardsModifier,
                                request.effectDef.shockwave.mode);
                        }
                    }
                }
            }

            //TODO:This does not work(does not work means destroys everything too quickly). use IEnumerator.
            if (soundSources.Length == null)
                return;
            for(int i = 0; i < soundSources.Length; i++)
            {
                if (soundSources[i] != null)
                    Destroy(soundSources[i]);
            }
            
            m_requests.Clear();
        }
    }

}
