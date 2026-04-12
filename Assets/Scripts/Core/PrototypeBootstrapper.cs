using Tanks.Level;
using UnityEngine;

namespace Tanks.Core
{
    public static class PrototypeBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                GameObject root = new("GameRoot");
                gameManager = root.AddComponent<GameManager>();
                root.AddComponent<LevelManager>();
                return;
            }

            if (Object.FindFirstObjectByType<LevelManager>() == null)
            {
                gameManager.gameObject.AddComponent<LevelManager>();
            }
        }
    }
}
