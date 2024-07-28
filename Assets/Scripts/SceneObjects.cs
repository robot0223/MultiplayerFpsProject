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

            private GamePlay _gameplay;
    }
}

