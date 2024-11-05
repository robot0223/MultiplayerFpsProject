using FPS_personal_project;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIDisconnectedView : MonoBehaviour
{
    // Called from button OnClick event.
    public void GoToMenu()
    {
        var gameUI = GetComponentInParent<GameUI>(true);
        gameUI.GoToMenu();
    }



    private void Update()
    {
        // Make sure the cursor stays unlocked.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
