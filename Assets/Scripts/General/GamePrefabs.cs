using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace FPS_personal_project
{
    public struct GamePrefabs : IComponentData
    {
        public Entity Character;
        //public Entity Minion;
        public Entity GameOverEntity;
        public Entity RespawnEntity;
    }
    
    public class UIPrefabs : IComponentData
    {
        public GameObject HealthBar;
        public GameObject SkillShot;
    }
}
