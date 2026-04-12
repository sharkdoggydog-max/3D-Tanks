using Tanks.Combat;
using Tanks.Core;
using Tanks.Enemy;
using Tanks.Player;
using Tanks.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Tanks.Level
{
    public class LevelManager : MonoBehaviour
    {
        private bool isInitialized;
        private PrototypeLevelBuilder levelBuilder;

        private void Start()
        {
            if (!isInitialized)
            {
                InitializePrototype();
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if ((GameManager.Instance.CurrentState == GameState.Victory || GameManager.Instance.CurrentState == GameState.Defeat) &&
                keyboard != null &&
                keyboard.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        public void InitializePrototype()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;

            GameManager gameManager = GameManager.Instance != null ? GameManager.Instance : gameObject.AddComponent<GameManager>();
            gameManager.ClearEnemies();

            levelBuilder = GetComponentInChildren<PrototypeLevelBuilder>();
            if (levelBuilder == null)
            {
                GameObject levelRoot = new("PrototypeLevel");
                levelRoot.transform.SetParent(transform, false);
                levelBuilder = levelRoot.AddComponent<PrototypeLevelBuilder>();
            }

            levelBuilder.BuildLevel();

            GameObject playerTank = CreatePlayer(levelBuilder.PlayerSpawnPoint);
            ConfigureCamera(playerTank.transform);
            EnsureHud();

            for (int index = 0; index < levelBuilder.EnemySpawnPoints.Count; index++)
            {
                CreateEnemy(levelBuilder.EnemySpawnPoints[index], playerTank.transform, index);
            }

            gameManager.BeginGameplay();
        }

        private GameObject CreatePlayer(Vector3 spawnPosition)
        {
            GameObject playerTank = TankPrototypeFactory.CreateTank("PlayerTank", new Color(0.18f, 0.6f, 0.22f), spawnPosition, Quaternion.identity);
            playerTank.transform.SetParent(transform, true);

            Health health = playerTank.GetComponent<Health>();
            health.Configure(5f, Team.Player, false);

            TankWeapon weapon = playerTank.GetComponent<TankWeapon>();
            weapon.Configure(0.3f, 28f, 1f, 2.4f, 0.3f);

            PlayerTankController controller = playerTank.AddComponent<PlayerTankController>();
            controller.Configure(8f, 140f);

            GameManager.Instance.RegisterPlayer(health);
            return playerTank;
        }

        private void CreateEnemy(Vector3 spawnPosition, Transform playerTransform, int enemyIndex)
        {
            Vector3 lookDirection = playerTransform.position - spawnPosition;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude < 0.001f)
            {
                lookDirection = Vector3.forward;
            }

            GameObject enemyTank = TankPrototypeFactory.CreateTank(
                $"EnemyTank_{enemyIndex + 1}",
                new Color(0.72f, 0.2f, 0.16f),
                spawnPosition,
                Quaternion.LookRotation(lookDirection.normalized, Vector3.up));

            enemyTank.transform.SetParent(transform, true);

            Health health = enemyTank.GetComponent<Health>();
            health.Configure(3f, Team.Enemy, true);

            TankWeapon weapon = enemyTank.GetComponent<TankWeapon>();
            weapon.Configure(0.85f, 19f, 1f, 3f, 0.24f);

            EnemyTankAI enemyAI = enemyTank.AddComponent<EnemyTankAI>();
            enemyAI.Configure(playerTransform, 4f, 110f, 14f, 9f);

            GameManager.Instance.RegisterEnemy(health);
        }

        private void ConfigureCamera(Transform playerTransform)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObject = new("Main Camera");
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            CameraFollow follow = mainCamera.GetComponent<CameraFollow>();
            if (follow == null)
            {
                follow = mainCamera.gameObject.AddComponent<CameraFollow>();
            }

            follow.SetTarget(playerTransform);
        }

        private void EnsureHud()
        {
            if (FindFirstObjectByType<GameHud>() != null)
            {
                return;
            }

            GameObject hudObject = new("GameHud");
            hudObject.transform.SetParent(transform, false);
            hudObject.AddComponent<GameHud>();
        }
    }
}
