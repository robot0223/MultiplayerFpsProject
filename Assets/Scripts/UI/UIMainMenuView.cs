using FPS_personal_project;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIMainMenuView : MonoBehaviour
{
    private MainMenuUI _mainMenuUI;
    private UILoadingScreen _loadingScreen;
    public UIMatchmakingScreen MatchMakingScreen;
    List<AsyncOperation> scenesToLoad = new List<AsyncOperation>();

    private void OnEnable()
    {
        _mainMenuUI = GetComponentInParent<MainMenuUI>();
        _loadingScreen = _mainMenuUI.LoadingScreen;
    }

    private void Update()
    {
        MatchMakingScreen.gameObject.SetActive (MatchmakerClient.IsMatchmaking);
    }

    /* public void OnClickPlay()
     {
        _loadingScreen.gameObject.SetActive(true);

         scenesToLoad.Add(SceneManager.LoadSceneAsync("Level_00_Main",LoadSceneMode.Single));
         StartCoroutine(UpdateLoadingScreen());

     }

     IEnumerator UpdateLoadingScreen()
     {
         float totalProgress = 0f;
         for(int i = 0;i<scenesToLoad.Count;i++)
         {
             while (!scenesToLoad[i].isDone)
             {
                 totalProgress += scenesToLoad[i].progress;
                 _loadingScreen.loadingImage.fillAmount = totalProgress/scenesToLoad.Count;
                 yield return null;
             }
         }
     }*/


}
