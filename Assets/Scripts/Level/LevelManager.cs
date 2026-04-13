using System;
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
        private bool isAdvancingLevel;
        private bool objectiveComplete;
        private bool isRestarting;
        private float nextLevelLoadTime;
        private GameObject currentPlayerTank;
        private PrototypeLevelBuilder levelBuilder;
        private GameManager gameManager;
        private ObjectiveNode objectiveNode;
        private LevelExitGate exitGate;
        private readonly System.Collections.Generic.List<GameObject> spawnedEnemies = new();

        public string ObjectiveText => objectiveComplete ? "Reach the Exit Gate" : "Destroy the Command Node";
        public string ExitStatusText => objectiveComplete ? "Exit: Unlocked" : "Exit: Locked";

        public string ObjectiveProgressText
        {
            get
            {
                if (objectiveComplete)
                {
                    return "Command Node: Destroyed";
                }

                if (objectiveNode == null)
                {
                    return "Command Node: Offline";
                }

                return $"Command Node HP: {objectiveNode.CurrentHealth:0}/{objectiveNode.MaxHealth:0}";
            }
        }

        private void Start()
        {
            if (!isInitialized)
            {
                InitializeRun();
            }
        }

        private void Update()
        {
            if (gameManager == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (gameManager.CurrentState == GameState.Defeat &&
                keyboard != null &&
                keyboard.rKey.wasPressedThisFrame)
            {
                RestartRun();
            }

            if (isAdvancingLevel && Time.time >= nextLevelLoadTime)
            {
                isAdvancingLevel = false;
                gameManager.AdvanceToNextLevel();
                LoadCurrentLevel();
            }
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged -= OnGameStateChanged;
            }

            UnsubscribeMissionObjects();
        }

        public void InitializeRun()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;
            gameManager = GameManager.Instance != null ? GameManager.Instance : gameObject.AddComponent<GameManager>();
            gameManager.StateChanged += OnGameStateChanged;
            gameManager.ResetRun();

            EnsureLevelBuilder();
            EnsureHud();
            LoadCurrentLevel();
        }

        public void RestartRun()
        {
            if (isRestarting)
            {
                return;
            }

            isRestarting = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void LoadCurrentLevel()
        {
            EnsureLevelBuilder();
            ClearCurrentLevel();

            objectiveComplete = false;
            gameManager.ClearEnemies();
            levelBuilder.ConfigureForLevel(gameManager.CurrentLevel);
            levelBuilder.BuildLevel();

            currentPlayerTank = CreatePlayer(levelBuilder.PlayerSpawnPoint);
            ConfigureCamera(currentPlayerTank.transform);
            CreateMissionObjects();

            for (int index = 0; index < levelBuilder.EnemySpawnPoints.Count; index++)
            {
                CreateEnemy(levelBuilder.EnemySpawnPoints[index], currentPlayerTank.transform, index);
            }

            gameManager.BeginGameplay();
        }

        private void EnsureLevelBuilder()
        {
            levelBuilder = GetComponentInChildren<PrototypeLevelBuilder>();
            if (levelBuilder == null)
            {
                GameObject levelRoot = new("PrototypeLevel");
                levelRoot.transform.SetParent(transform, false);
                levelBuilder = levelRoot.AddComponent<PrototypeLevelBuilder>();
            }
        }

        private GameObject CreatePlayer(Vector3 spawnPosition)
        {
            GameObject playerTank = TankPrototypeFactory.CreateTank(
                "PlayerTank",
                new Color(0.18f, 0.6f, 0.22f),
                spawnPosition,
                Quaternion.identity,
                new Color(0.82f, 1f, 0.82f));
            playerTank.transform.SetParent(transform, true);

            Health health = playerTank.GetComponent<Health>();
            health.Configure(gameManager.Progression.PlayerMaxHealth, Team.Player, false);

            TankWeapon weapon = playerTank.GetComponent<TankWeapon>();
            weapon.Configure(
                gameManager.Progression.PlayerWeaponCooldown,
                gameManager.Progression.PlayerProjectileSpeed,
                1f,
                gameManager.Progression.PlayerProjectileLifetime,
                gameManager.Progression.PlayerProjectileRadius);
            weapon.ConfigureProjectileStyle(ProjectileStyle.Player);

            PlayerTankController controller = playerTank.AddComponent<PlayerTankController>();
            controller.Configure(8f, 140f);
            playerTank.AddComponent<PlayerAimIndicator>();

            gameManager.RegisterPlayer(health);
            return playerTank;
        }

        private void CreateEnemy(Vector3 spawnPosition, Transform playerTransform, int enemyIndex)
        {
            EnemyVariant variant = EnemyVariantLibrary.PickVariant(gameManager.CurrentLevel, enemyIndex);
            EnemyArchetype archetype = EnemyVariantLibrary.CreateArchetype(variant, gameManager.CurrentLevel);

            Vector3 lookDirection = playerTransform.position - spawnPosition;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude < 0.001f)
            {
                lookDirection = Vector3.forward;
            }

            GameObject enemyTank = TankPrototypeFactory.CreateTank(
                $"{archetype.DisplayName}_{enemyIndex + 1}",
                archetype.Color,
                spawnPosition,
                Quaternion.LookRotation(lookDirection.normalized, Vector3.up),
                archetype.AccentColor,
                archetype.HullScale,
                archetype.TurretScale,
                archetype.BarrelLengthScale,
                archetype.BarrelThicknessScale,
                variant,
                enemyIndex);

            enemyTank.transform.SetParent(transform, true);
            spawnedEnemies.Add(enemyTank);

            Health health = enemyTank.GetComponent<Health>();
            health.Configure(archetype.MaxHealth, Team.Enemy, true);

            TankWeapon weapon = enemyTank.GetComponent<TankWeapon>();
            weapon.Configure(
                archetype.WeaponCooldown,
                archetype.ProjectileSpeed,
                archetype.ProjectileDamage,
                archetype.ProjectileLifetime,
                archetype.ProjectileRadius);
            weapon.ConfigureProjectileStyle(GetProjectileStyle(archetype.Variant));

            TankTurretAim turretAim = enemyTank.GetComponent<TankTurretAim>();
            if (turretAim != null)
            {
                turretAim.Configure(archetype.TurretTurnSpeed);
            }

            EnemyTankAI enemyAI = enemyTank.AddComponent<EnemyTankAI>();
            enemyAI.Configure(playerTransform, archetype);

            EnemyHealthBar healthBar = enemyTank.AddComponent<EnemyHealthBar>();
            healthBar.Configure(archetype.DisplayName, archetype.AccentColor, 2.6f * archetype.HullScale + 0.35f * archetype.TurretScale);

            gameManager.RegisterEnemy(health);
        }

        private void CreateMissionObjects()
        {
            objectiveNode = CreateObjectiveNode(levelBuilder.ObjectivePoint);
            objectiveNode.ObjectiveDestroyed += OnObjectiveDestroyed;

            exitGate = CreateExitGate(levelBuilder.ExitPoint);
            exitGate.ExitReached += OnExitReached;
            exitGate.SetUnlocked(false);
        }

        private ObjectiveNode CreateObjectiveNode(Vector3 position)
        {
            GameObject root = new("CommandNode");
            root.transform.SetParent(transform, true);
            root.transform.position = position;

            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Pedestal",
                root.transform,
                new Vector3(0f, 0.45f, 0f),
                new Vector3(1.6f, 0.45f, 1.6f),
                new Color(0.3f, 0.22f, 0.2f));

            CreatePrimitive(
                PrimitiveType.Cube,
                "BaseRing",
                root.transform,
                new Vector3(0f, 1.05f, 0f),
                new Vector3(1.9f, 0.22f, 1.9f),
                new Color(0.52f, 0.24f, 0.22f));

            Transform corePivot = new GameObject("CorePivot").transform;
            corePivot.SetParent(root.transform, false);
            corePivot.localPosition = new Vector3(0f, 1.6f, 0f);

            CreatePrimitive(
                PrimitiveType.Cube,
                "Core",
                corePivot,
                Vector3.zero,
                new Vector3(0.9f, 0.9f, 0.9f),
                new Color(0.98f, 0.48f, 0.34f));

            CreatePrimitive(
                PrimitiveType.Cylinder,
                "AntennaNorth",
                root.transform,
                new Vector3(0f, 1.3f, 0.95f),
                new Vector3(0.12f, 0.6f, 0.12f),
                new Color(0.78f, 0.62f, 0.56f));

            CreatePrimitive(
                PrimitiveType.Cylinder,
                "AntennaSouth",
                root.transform,
                new Vector3(0f, 1.3f, -0.95f),
                new Vector3(0.12f, 0.6f, 0.12f),
                new Color(0.78f, 0.62f, 0.56f));

            CreatePrimitive(
                PrimitiveType.Cube,
                "SideArmLeft",
                root.transform,
                new Vector3(-0.95f, 1.2f, 0f),
                new Vector3(0.28f, 0.55f, 0.9f),
                new Color(0.66f, 0.32f, 0.28f));

            CreatePrimitive(
                PrimitiveType.Cube,
                "SideArmRight",
                root.transform,
                new Vector3(0.95f, 1.2f, 0f),
                new Vector3(0.28f, 0.55f, 0.9f),
                new Color(0.66f, 0.32f, 0.28f));

            Health health = root.AddComponent<Health>();
            health.Configure(4f + Mathf.Floor((gameManager.CurrentLevel - 1) * 0.5f), Team.Enemy, true);
            root.AddComponent<TankCombatFeedback>();

            ObjectiveNode node = root.AddComponent<ObjectiveNode>();
            node.SetAnimatedCore(corePivot);
            return node;
        }

        private LevelExitGate CreateExitGate(Vector3 position)
        {
            GameObject root = new("ExitGate");
            root.transform.SetParent(transform, true);
            root.transform.position = position;

            Renderer[] indicators =
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "GatePad",
                    root.transform,
                    new Vector3(0f, 0.08f, 0f),
                    new Vector3(3.6f, 0.16f, 3.6f),
                    new Color(0.18f, 0.28f, 0.3f)).GetComponent<Renderer>(),
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "LeftPillar",
                    root.transform,
                    new Vector3(-1.35f, 1.2f, 0f),
                    new Vector3(0.22f, 1.2f, 0.22f),
                    new Color(0.42f, 0.56f, 0.6f)).GetComponent<Renderer>(),
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "RightPillar",
                    root.transform,
                    new Vector3(1.35f, 1.2f, 0f),
                    new Vector3(0.22f, 1.2f, 0.22f),
                    new Color(0.42f, 0.56f, 0.6f)).GetComponent<Renderer>(),
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "TopBeam",
                    root.transform,
                    new Vector3(0f, 2.5f, 0f),
                    new Vector3(3.1f, 0.24f, 0.36f),
                    new Color(0.42f, 0.56f, 0.6f)).GetComponent<Renderer>()
            };

            GameObject barrier = CreatePrimitive(
                PrimitiveType.Cube,
                "Barrier",
                root.transform,
                new Vector3(0f, 1.1f, 0f),
                new Vector3(2.2f, 1.9f, 0.32f),
                new Color(0.9f, 0.26f, 0.22f));
            barrier.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            LevelExitGate gate = root.AddComponent<LevelExitGate>();
            gate.Configure(barrier.GetComponent<Collider>(), barrier.GetComponent<Renderer>(), indicators);
            return gate;
        }

        private static GameObject CreatePrimitive(
            PrimitiveType primitiveType,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;
            CombatVisualPalette.ApplyRuntimeMaterial(primitive.GetComponent<Renderer>(), color);
            return primitive;
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

            mainCamera.orthographic = false;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 350f;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.11f, 0.13f, 0.16f);
            mainCamera.fieldOfView = Application.isMobilePlatform ? 62f : 60f;

            CameraFollow follow = mainCamera.GetComponent<CameraFollow>();
            if (follow == null)
            {
                follow = mainCamera.gameObject.AddComponent<CameraFollow>();
            }

            follow.SetTarget(playerTransform);
        }

        private void EnsureHud()
        {
            if (FindAnyObjectByType<GameHud>() != null)
            {
                return;
            }

            GameObject hudObject = new("GameHud");
            hudObject.transform.SetParent(transform, false);
            hudObject.AddComponent<GameHud>();
        }

        private void ClearCurrentLevel()
        {
            UnsubscribeMissionObjects();
            gameManager.RegisterPlayer(null);

            if (currentPlayerTank != null)
            {
                Destroy(currentPlayerTank);
                currentPlayerTank = null;
            }

            if (objectiveNode != null)
            {
                Destroy(objectiveNode.gameObject);
                objectiveNode = null;
            }

            if (exitGate != null)
            {
                Destroy(exitGate.gameObject);
                exitGate = null;
            }

            for (int index = 0; index < spawnedEnemies.Count; index++)
            {
                if (spawnedEnemies[index] != null)
                {
                    Destroy(spawnedEnemies[index]);
                }
            }

            spawnedEnemies.Clear();
        }

        private void UnsubscribeMissionObjects()
        {
            if (objectiveNode != null)
            {
                objectiveNode.ObjectiveDestroyed -= OnObjectiveDestroyed;
            }

            if (exitGate != null)
            {
                exitGate.ExitReached -= OnExitReached;
            }
        }

        private void OnObjectiveDestroyed(ObjectiveNode destroyedNode)
        {
            objectiveComplete = true;

            if (exitGate != null)
            {
                exitGate.SetUnlocked(true);
            }

            if (gameManager != null && gameManager.CurrentState == GameState.Playing)
            {
                gameManager.ShowMessage("Command Node Destroyed\nExit Gate Unlocked", 2.5f);
            }
        }

        private void OnExitReached(LevelExitGate gate)
        {
            if (!objectiveComplete || gameManager == null || gameManager.CurrentState != GameState.Playing)
            {
                return;
            }

            gameManager.CompleteLevel();
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.Victory)
            {
                isAdvancingLevel = true;
                nextLevelLoadTime = Time.time + 2.2f;
                gameManager.ShowMessage($"Level {gameManager.CurrentLevel} Complete!\nAdvancing...", 2f);
                return;
            }

            if (newState == GameState.Defeat)
            {
                isAdvancingLevel = false;
                gameManager.ShowMessage("Defeat\nPress R to restart", 60f);
            }
        }

        private static ProjectileStyle GetProjectileStyle(EnemyVariant variant)
        {
            return variant switch
            {
                EnemyVariant.Raider => ProjectileStyle.RaiderEnemy,
                EnemyVariant.Bulwark => ProjectileStyle.BulwarkEnemy,
                _ => ProjectileStyle.BasicEnemy
            };
        }
    }

    internal class ObjectiveNode : MonoBehaviour
    {
        private Health health;
        private Transform animatedCore;
        private Vector3 animatedCoreBasePosition;

        public event Action<ObjectiveNode> ObjectiveDestroyed;

        public float CurrentHealth => health != null ? health.CurrentHealth : 0f;
        public float MaxHealth => health != null ? health.MaxHealth : 0f;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        private void Update()
        {
            if (animatedCore == null)
            {
                return;
            }

            animatedCore.localPosition = animatedCoreBasePosition + Vector3.up * (Mathf.Sin(Time.time * 3.2f) * 0.12f);
            animatedCore.Rotate(Vector3.up, 70f * Time.deltaTime, Space.Self);
        }

        public void SetAnimatedCore(Transform core)
        {
            animatedCore = core;
            animatedCoreBasePosition = core.localPosition;
        }

        private void OnDied(Health deadHealth)
        {
            ObjectiveDestroyed?.Invoke(this);
        }
    }

    internal class LevelExitGate : MonoBehaviour
    {
        private Collider barrierCollider;
        private Renderer barrierRenderer;
        private Renderer[] indicatorRenderers;
        private BoxCollider triggerZone;
        private bool isUnlocked;
        private bool hasTriggered;

        public event Action<LevelExitGate> ExitReached;

        private void Awake()
        {
            triggerZone = gameObject.AddComponent<BoxCollider>();
            triggerZone.isTrigger = true;
            triggerZone.center = new Vector3(0f, 1.2f, 0f);
            triggerZone.size = new Vector3(2.8f, 2.5f, 2.8f);
        }

        public void Configure(Collider barrier, Renderer barrierVisual, Renderer[] indicators)
        {
            barrierCollider = barrier;
            barrierRenderer = barrierVisual;
            indicatorRenderers = indicators;
        }

        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
            hasTriggered = false;

            if (barrierCollider != null)
            {
                barrierCollider.enabled = !unlocked;
            }

            if (barrierRenderer != null)
            {
                barrierRenderer.enabled = !unlocked;
            }

            Color frameColor = unlocked ? new Color(0.42f, 0.96f, 0.9f) : new Color(0.72f, 0.32f, 0.26f);
            if (indicatorRenderers == null)
            {
                return;
            }

            for (int index = 0; index < indicatorRenderers.Length; index++)
            {
                CombatVisualPalette.SetRuntimeMaterialColor(indicatorRenderers[index], frameColor);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryTriggerExit(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryTriggerExit(other);
        }

        private void TryTriggerExit(Collider other)
        {
            if (!isUnlocked || hasTriggered)
            {
                return;
            }

            Health health = other.GetComponentInParent<Health>();
            if (health == null || health.Team != Team.Player)
            {
                return;
            }

            hasTriggered = true;
            ExitReached?.Invoke(this);
        }
    }
}
