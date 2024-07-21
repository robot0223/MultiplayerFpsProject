using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuScreenUIManager : MonoBehaviour
{
    [SerializeField] private GameObject MenuScreen;
    [SerializeField] private GameObject PlayScreen;
    [SerializeField] private GameObject SettingsScreen;

    public void OnClickMenuPlayButton()
    {
        MenuScreen.SetActive(false);
        PlayScreen.SetActive(true);
    }

}
