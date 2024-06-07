using System.Collections;
using UnityEngine;

namespace TMG.NFE_Tutorial
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SkillShotVisualDelay : MonoBehaviour
    {
        [SerializeField] private int _delayFrameCount = 1;
        
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.enabled = false;
        }

        private IEnumerator Start()
        {
            for (var i = 0; i < _delayFrameCount; i++)
            {
                yield return null;
            }

            _spriteRenderer.enabled = true;
        }
    }
}