using System;
using Unity.Entities;

namespace TMG.NFE_Tutorial
{
    public partial class GameOverSystem : SystemBase
    {
        public Action<TeamType> OnGameOver;
        
        protected override void OnCreate()
        {
            RequireForUpdate<GameOverTag>();
            RequireForUpdate<GamePlayingTag>();
        }
        
        protected override void OnUpdate()
        {
            var gameOverEntity = SystemAPI.GetSingletonEntity<GameOverTag>();
            var winningTeam = SystemAPI.GetComponent<WinningTeam>(gameOverEntity).Value;
            OnGameOver?.Invoke(winningTeam);
            
            var gamePlayingEntity = SystemAPI.GetSingletonEntity<GamePlayingTag>();
            EntityManager.DestroyEntity(gamePlayingEntity);

            Enabled = false;
        }
    }
}