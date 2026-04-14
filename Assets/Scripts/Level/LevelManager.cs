using System;
using Tanks.Combat;
using Tanks.Core;
using Tanks.Enemy;
using Tanks.Player;
using Tanks.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks.Level
{
    public class LevelManager : MonoBehaviour
    {
        private bool isInitialized;
        private bool isAdvancingLevel;
        private bool objectiveComplete;
        private float nextLevelLoadTime;
        private GameObject currentPlayerTank;
        private PrototypeLevelBuilder levelBuilder;
        private GameManager gameManager;
        private ObjectiveNode objectiveNode;
        private LevelExitGate exitGate;
        private readonly System.Collections.Generic.List<GameObject> spawnedEnemies = new();

        public string ObjectiveText => objectiveComplete ? "Reach the Exit Gate" : "Destroy the Command Node";
        public string ExitStatusText => objectiveComplete ? "Exit: Unlocked" : "Exit: Locked";
        public EnemyVariant SelectedPlayerTank => gameManager != null ? gameManager.Progression.SelectedPlayerTank : EnemyVariant.Basic;

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
                RestartCurrentRun();
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

            EnsureLevelBuilder();
            EnsureHud();
            ShowMainMenu();
        }

        public void StartSelectedTankRun(EnemyVariant tankType)
        {
            if (!isInitialized)
            {
                InitializeRun();
            }

            if (gameManager == null)
            {
                gameManager = GameManager.Instance != null ? GameManager.Instance : GetComponent<GameManager>();
            }

            if (gameManager == null)
            {
                Debug.LogWarning("[LevelManager] Unable to start run because GameManager was not found.");
                return;
            }

            isAdvancingLevel = false;
            gameManager.Progression.SelectPlayerTank(tankType);
            gameManager.ResetRun();
            LoadCurrentLevel();
        }

        public void RestartCurrentRun()
        {
            isAdvancingLevel = false;

            if (!isInitialized)
            {
                InitializeRun();
                return;
            }

            if (gameManager == null)
            {
                gameManager = GameManager.Instance != null ? GameManager.Instance : GetComponent<GameManager>();
            }

            if (gameManager == null)
            {
                Debug.LogWarning("[LevelManager] Restart requested without an active GameManager. Reinitializing run.");
                isInitialized = false;
                InitializeRun();
                return;
            }

            gameManager.ResetRun();
            LoadCurrentLevel();
        }

        public void ReturnToMainMenu()
        {
            isAdvancingLevel = false;

            if (!isInitialized)
            {
                InitializeRun();
                return;
            }

            if (gameManager == null)
            {
                gameManager = GameManager.Instance != null ? GameManager.Instance : GetComponent<GameManager>();
            }

            if (gameManager == null)
            {
                Debug.LogWarning("[LevelManager] Return to menu requested without an active GameManager. Reinitializing run.");
                isInitialized = false;
                InitializeRun();
                return;
            }

            gameManager.ResetRun();
            ShowMainMenu();
        }

        private void ShowMainMenu()
        {
            isAdvancingLevel = false;
            objectiveComplete = false;
            ClearCurrentLevel();

            if (gameManager != null)
            {
                gameManager.ClearEnemies();
                gameManager.EnterMainMenu();
            }
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
            PlayerTankPreset preset = GetPlayerTankPreset();
            GameObject playerTank = TankPrototypeFactory.CreateTank(
                $"Player{preset.DisplayName}",
                preset.BodyColor,
                spawnPosition,
                Quaternion.identity,
                preset.AccentColor,
                preset.HullScale,
                preset.TurretScale,
                preset.BarrelLengthScale,
                preset.BarrelThicknessScale,
                preset.VisualVariant);
            playerTank.transform.SetParent(transform, true);

            Health health = playerTank.GetComponent<Health>();
            health.Configure(
                gameManager.Progression.GetPlayerMaxHealth(preset.BaseMaxHealth, MobileControlLayout.ShouldUseTouchControls()),
                Team.Player,
                false);

            TankWeapon weapon = playerTank.GetComponent<TankWeapon>();
            weapon.Configure(
                gameManager.Progression.PlayerWeaponCooldown * preset.WeaponCooldownMultiplier,
                gameManager.Progression.PlayerProjectileSpeed * preset.ProjectileSpeedMultiplier,
                preset.ProjectileDamage,
                gameManager.Progression.PlayerProjectileLifetime * preset.ProjectileLifetimeMultiplier,
                Mathf.Max(0.18f, gameManager.Progression.PlayerProjectileRadius + preset.ProjectileRadiusOffset),
                preset.SplashRadius,
                preset.SplashDamageMultiplier,
                preset.BurstCount,
                preset.BurstInterval);
            weapon.ConfigureProjectileStyle(ProjectileStyle.Player);

            PlayerTankController controller = playerTank.AddComponent<PlayerTankController>();
            controller.Configure(preset.MoveSpeed, preset.TurnSpeed);
            playerTank.AddComponent<PlayerAimIndicator>();

            TankTurretAim turretAim = playerTank.GetComponent<TankTurretAim>();
            if (turretAim != null)
            {
                turretAim.Configure(preset.TurretTurnSpeed);
            }

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
                archetype.ProjectileRadius,
                archetype.SplashRadius,
                archetype.SplashDamageMultiplier,
                archetype.BurstCount,
                archetype.BurstInterval);
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
            return RuntimePrimitiveVisuals.CreatePrimitive(
                primitiveType,
                name,
                parent,
                localPosition,
                localScale,
                color);
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
            if (gameManager != null)
            {
                gameManager.RegisterPlayer(null);
            }

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
                gameManager.ShowMessage(
                    MobileControlLayout.ShouldUseTouchControls() ? "Defeat\nTap Restart" : "Defeat\nPress R to restart",
                    60f);
            }
        }

        private static ProjectileStyle GetProjectileStyle(EnemyVariant variant)
        {
            return variant switch
            {
                EnemyVariant.Raider => ProjectileStyle.RaiderEnemy,
                EnemyVariant.Bulwark => ProjectileStyle.BulwarkEnemy,
                EnemyVariant.Artillery => ProjectileStyle.ArtilleryEnemy,
                EnemyVariant.Striker => ProjectileStyle.StrikerEnemy,
                EnemyVariant.Scout => ProjectileStyle.ScoutEnemy,
                _ => ProjectileStyle.BasicEnemy
            };
        }

        private PlayerTankPreset GetPlayerTankPreset()
        {
            return SelectedPlayerTank switch
            {
                EnemyVariant.Raider => new PlayerTankPreset(
                    EnemyVariant.Raider,
                    "Raider",
                    new Color(0.14f, 0.55f, 0.46f),
                    new Color(0.82f, 1f, 0.96f),
                    0.9f,
                    0.84f,
                    1.18f,
                    0.82f,
                    4f,
                    10.2f,
                    192f,
                    320f,
                    0.72f,
                    1.18f,
                    0.72f,
                    -0.08f,
                    0.88f,
                    0f,
                    0f,
                    1,
                    0f),

                EnemyVariant.Bulwark => new PlayerTankPreset(
                    EnemyVariant.Bulwark,
                    "Bulwark",
                    new Color(0.24f, 0.42f, 0.28f),
                    new Color(0.9f, 0.97f, 1f),
                    1.12f,
                    1.14f,
                    0.9f,
                    1.28f,
                    8f,
                    5.8f,
                    96f,
                    145f,
                    1.28f,
                    0.9f,
                    1.75f,
                    0.12f,
                    1.18f,
                    0f,
                    0f,
                    1,
                    0f),

                EnemyVariant.Artillery => new PlayerTankPreset(
                    EnemyVariant.Artillery,
                    "Artillery",
                    new Color(0.38f, 0.46f, 0.28f),
                    new Color(1f, 0.82f, 0.48f),
                    1f,
                    0.88f,
                    1.72f,
                    1.08f,
                    4f,
                    5.4f,
                    94f,
                    126f,
                    1.86f,
                    0.82f,
                    1.55f,
                    0.12f,
                    1.45f,
                    2.2f,
                    0.6f,
                    1,
                    0f),

                EnemyVariant.Striker => new PlayerTankPreset(
                    EnemyVariant.Striker,
                    "Striker",
                    new Color(0.54f, 0.18f, 0.14f),
                    new Color(1f, 0.7f, 0.4f),
                    1.02f,
                    0.92f,
                    0.92f,
                    1.22f,
                    6f,
                    8.6f,
                    158f,
                    228f,
                    1.12f,
                    1f,
                    0.78f,
                    0.02f,
                    0.95f,
                    0f,
                    0f,
                    2,
                    0.1f),

                EnemyVariant.Scout => new PlayerTankPreset(
                    EnemyVariant.Scout,
                    "Scout",
                    new Color(0.18f, 0.34f, 0.46f),
                    new Color(0.7f, 1f, 0.96f),
                    0.76f,
                    0.68f,
                    1.34f,
                    0.64f,
                    3.5f,
                    11.4f,
                    228f,
                    340f,
                    0.68f,
                    1.08f,
                    0.52f,
                    -0.1f,
                    0.96f,
                    0f,
                    0f,
                    1,
                    0f),

                _ => new PlayerTankPreset(
                    EnemyVariant.Basic,
                    "Basic",
                    new Color(0.18f, 0.6f, 0.22f),
                    new Color(0.82f, 1f, 0.82f),
                    1f,
                    1f,
                    1f,
                    1f,
                    5f,
                    7.8f,
                    140f,
                    220f,
                    1f,
                    1f,
                    1f,
                    0f,
                    1f,
                    0f,
                    0f,
                    1,
                    0f)
            };
        }

        private readonly struct PlayerTankPreset
        {
            public PlayerTankPreset(
                EnemyVariant visualVariant,
                string displayName,
                Color bodyColor,
                Color accentColor,
                float hullScale,
                float turretScale,
                float barrelLengthScale,
                float barrelThicknessScale,
                float baseMaxHealth,
                float moveSpeed,
                float turnSpeed,
                float turretTurnSpeed,
                float weaponCooldownMultiplier,
                float projectileSpeedMultiplier,
                float projectileDamage,
                float projectileRadiusOffset,
                float projectileLifetimeMultiplier,
                float splashRadius,
                float splashDamageMultiplier,
                int burstCount,
                float burstInterval)
            {
                VisualVariant = visualVariant;
                DisplayName = displayName;
                BodyColor = bodyColor;
                AccentColor = accentColor;
                HullScale = hullScale;
                TurretScale = turretScale;
                BarrelLengthScale = barrelLengthScale;
                BarrelThicknessScale = barrelThicknessScale;
                BaseMaxHealth = baseMaxHealth;
                MoveSpeed = moveSpeed;
                TurnSpeed = turnSpeed;
                TurretTurnSpeed = turretTurnSpeed;
                WeaponCooldownMultiplier = weaponCooldownMultiplier;
                ProjectileSpeedMultiplier = projectileSpeedMultiplier;
                ProjectileDamage = projectileDamage;
                ProjectileRadiusOffset = projectileRadiusOffset;
                ProjectileLifetimeMultiplier = projectileLifetimeMultiplier;
                SplashRadius = splashRadius;
                SplashDamageMultiplier = splashDamageMultiplier;
                BurstCount = burstCount;
                BurstInterval = burstInterval;
            }

            public EnemyVariant VisualVariant { get; }
            public string DisplayName { get; }
            public Color BodyColor { get; }
            public Color AccentColor { get; }
            public float HullScale { get; }
            public float TurretScale { get; }
            public float BarrelLengthScale { get; }
            public float BarrelThicknessScale { get; }
            public float BaseMaxHealth { get; }
            public float MoveSpeed { get; }
            public float TurnSpeed { get; }
            public float TurretTurnSpeed { get; }
            public float WeaponCooldownMultiplier { get; }
            public float ProjectileSpeedMultiplier { get; }
            public float ProjectileDamage { get; }
            public float ProjectileRadiusOffset { get; }
            public float ProjectileLifetimeMultiplier { get; }
            public float SplashRadius { get; }
            public float SplashDamageMultiplier { get; }
            public int BurstCount { get; }
            public float BurstInterval { get; }
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
                RuntimePrimitiveVisuals.SetColor(indicatorRenderers[index], frameColor);
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
