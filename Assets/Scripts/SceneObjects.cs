using Fusion;

namespace FPS_personal_project
{
    /// <summary>
    /// Singleton on Runner used to obtain scene object references using lazy getters.
    /// </summary>
    public class SceneObjects : SimulationBehaviour
    {
        // Use Runner.GetSingleton<SceneObjects>() to get SceneObjects instance.
        public GamePlay Gameplay
        {
            get
            {
                if (_gameplay == null && Runner != null && Runner.SceneManager != null && Runner.SceneManager.MainRunnerScene.IsValid())
                {
                    var gameplays = Runner.SceneManager.MainRunnerScene.GetComponents<GamePlay>(true);
                    if (gameplays.Length > 0)
                    {
                        _gameplay = gameplays[0];
                    }
                }

                return _gameplay;
            }
        }

        public GameUI GameUI
        {
            get
            {
                if (_gameUI == null && Runner != null && Runner.SceneManager != null && Runner.SceneManager.MainRunnerScene.IsValid())
                {
                    var gameUIs = Runner.SceneManager.MainRunnerScene.GetComponents<GameUI>(true);
                    if (gameUIs.Length > 0)
                    {
                        _gameUI = gameUIs[0];
                    }
                }

                return _gameUI;
            }
        }
        
        public EffectModuleClient EffectModuleClient
        {
            get
            {
                if (_effectModuleClient == null && Runner != null && Runner.SceneManager != null && Runner.SceneManager.MainRunnerScene.IsValid())
                {
                    var effectModules = Runner.SceneManager.MainRunnerScene.GetComponents<EffectModuleClient>(true);
                    if (effectModules.Length > 0)
                    {
                        _effectModuleClient = effectModules[0];
                    }
                }

                return _effectModuleClient;
            }
        }





        private EffectModuleClient _effectModuleClient;
        private GamePlay _gameplay;
        private GameUI _gameUI;
    }
}

