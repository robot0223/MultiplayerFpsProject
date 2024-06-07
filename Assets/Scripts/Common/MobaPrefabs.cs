using Unity.Entities;
using UnityEngine;

namespace FPS_personal_project
{
    public struct MobaPrefabs : IComponentData
    {
        public Entity Champion;
        public Entity Minion;
        public Entity GameOverEntity;
        public Entity RespawnEntity;
    }

    public class UIPrefabs : IComponentData
    {
        public GameObject HealthBar;
        public GameObject SkillShot;
    }
}