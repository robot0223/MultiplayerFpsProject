using FPS_personal_project;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharSelectBtn : MonoBehaviour
{
    public CharacterRegistry.CharacterType BtnCharacterType;
    private UICharSelectionView _charSelectionView;
    private void Start()
    {
        _charSelectionView = GetComponentInParent<UICharSelectionView>();
    }
    public void OnSelect()
    {
        Debug.Log(this.gameObject.name);
        _charSelectionView.GameUI.GamePlay.LocalPlayerCharacterType = this.gameObject.GetComponent<CharSelectBtn>().BtnCharacterType;
        _charSelectionView.GameUI.GamePlay.SetChar = true;
    }
}
