using Tanks.Level;
using UnityEngine;

namespace Tanks.Core
{
    public static class PrototypeBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
            if (gameManager == null)
            {
                GameObject root = new("GameRoot");
                gameManager = root.AddComponent<GameManager>();
                root.AddComponent<LevelManager>();
                return;
            }

            if (Object.FindAnyObjectByType<LevelManager>() == null)
            {
                gameManager.gameObject.AddComponent<LevelManager>();
            }
        }
    }
}
