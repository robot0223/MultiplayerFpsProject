using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace FPS_personal_project
{

public class GamePrefabsAuthoring : MonoBehaviour
{
        [Header("Entities")]
        public GameObject Character;
        //public GameObject Minion;
        public GameObject GameOverEntity;
        public GameObject RespawnEntity;

        [Header("GameObjects")]
        public GameObject HealthBarPrefab;
        public GameObject SkillShotAimPrefab;

        public class GamePrefabsBaker : Baker<GamePrefabsAuthoring>
        {
            public override void Bake(GamePrefabsAuthoring authoring)
            {
                var prefabContainerEntity = GetEntity(TransformUsageFlags.None);
                AddComponent(prefabContainerEntity, new GamePrefabs
                {
                    Character = GetEntity(authoring.Character, TransformUsageFlags.Dynamic),
                    //Minion = GetEntity(authoring.Minion, TransformUsageFlags.Dynamic),
                    GameOverEntity = GetEntity(authoring.GameOverEntity, TransformUsageFlags.None),
                    RespawnEntity = GetEntity(authoring.RespawnEntity, TransformUsageFlags.None)
                });

                AddComponentObject(prefabContainerEntity, new UIPrefabs
                {
                    HealthBar = authoring.HealthBarPrefab,
                    SkillShot = authoring.SkillShotAimPrefab
                });
            }
        }

    }

}