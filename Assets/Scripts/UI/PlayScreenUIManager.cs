using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayScreenUIManager : MonoBehaviour
{
    [SerializeField] private GameObject PlayScreen;

    public void OnClickPlayButton()
    {
        SceneManager.LoadSceneAsync("TestArena", LoadSceneMode.Additive);
    }
}
