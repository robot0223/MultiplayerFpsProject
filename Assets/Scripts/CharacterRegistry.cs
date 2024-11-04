using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPS_personal_project
{

    [CreateAssetMenu(fileName = "CharacterRegistry", menuName = "FPS_personal_project/Game/CharacterRegistry")]
    public class CharacterRegistry : ScriptableObject
    {
        public enum CharacterType
        {
            Terraformer = 0,
            Robot = 1
        }
        public Player[] Characters;
        public CharacterType[] CharacterTypes;
        public Sprite[] CharaccterIcons;

    }

}
