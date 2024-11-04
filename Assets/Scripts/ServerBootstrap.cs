using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerBootstrap : MonoBehaviour
{
#if UNITY_SERVER
 private void Awake()
    {
        Debug.LogError("ServerBootstrapCode at work. changing scene to 00_main");
        SceneManager.LoadScene("Level_00_Main");

    }
#endif

}
