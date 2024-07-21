using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUIManager : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject MainMenu;
    [SerializeField] private GameObject MenuScreen;

    private void Awake()
    {
        MainMenu.SetActive(false);
        MenuScreen.SetActive(true);
    }


   

   

}
