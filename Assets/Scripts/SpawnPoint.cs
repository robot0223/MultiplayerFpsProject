using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPS_personal_project
{
    /// <summary>
    /// component for spawnPoint lookup
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}