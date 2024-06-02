#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class LoadFirstSceneSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
        if (SceneManager.GetActiveScene() == SceneManager.GetSceneByBuildIndex(0)) return;
        SceneManager.LoadScene(0);
    }

    protected override void OnUpdate()
    {

    }
}
#endif