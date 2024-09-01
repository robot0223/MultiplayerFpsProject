using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerBootstrap : MonoBehaviour
{
#if UNITY_SERVER
 private void OnEnable()
    {
        SceneManager.LoadScene("Level_00_Main");
    }
#endif

}
