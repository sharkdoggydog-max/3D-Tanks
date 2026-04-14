using System;
using System.Collections.Generic;
using Tanks.Combat;
using UnityEngine;

namespace Tanks.Core
{
    public enum GameState
    {
        Booting = 0,
        MainMenu = 1,
        Playing = 2,
        Victory = 3,
        Defeat = 4
    }

    public class GameManager : MonoBehaviour
    {
        private readonly List<Health> enemies = new();
        private readonly RunProgression progression = new();
        private Health playerHealth;
        private string activeMessage;
        private float messageExpiresAt;

        public static GameManager Instance { get; private set; }

        public event Action<GameState> StateChanged;

        public GameState CurrentState { get; private set; } = GameState.Booting;
        public Health PlayerHealth => playerHealth;
        public int EnemyCount => enemies.Count;
        public int CurrentLevel => progression.CurrentLevel;
        public RunProgression Progression => progression;
        public string ActiveMessage => Time.time < messageExpiresAt ? activeMessage : string.Empty;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterPlayer(Health health)
        {
            if (playerHealth == health)
            {
                return;
            }

            if (playerHealth != null)
            {
                playerHealth.Died -= OnPlayerDied;
            }

            playerHealth = health;

            if (playerHealth != null)
            {
                playerHealth.Died += OnPlayerDied;
            }
        }

        public void ResetRun()
        {
            SetState(GameState.Booting);
            progression.Reset();
            ShowMessage($"Level {CurrentLevel}\n{progression.LastUpgradeSummary}", 2.5f);
        }

        public void EnterMainMenu()
        {
            ClearEnemies();
            RegisterPlayer(null);
            ClearMessage();
            SetState(GameState.MainMenu);
        }

        public void AdvanceToNextLevel()
        {
            progression.AdvanceToNextLevel();
            ShowMessage($"Level {CurrentLevel}\n{progression.LastUpgradeSummary}", 2.75f);
        }

        public void ShowMessage(string message, float duration)
        {
            activeMessage = message;
            messageExpiresAt = Time.time + duration;
        }

        public void ClearMessage()
        {
            activeMessage = string.Empty;
            messageExpiresAt = 0f;
        }

        public void ClearEnemies()
        {
            for (int index = 0; index < enemies.Count; index++)
            {
                if (enemies[index] != null)
                {
                    enemies[index].Died -= OnEnemyDied;
                }
            }

            enemies.Clear();
        }

        public void RegisterEnemy(Health enemyHealth)
        {
            if (enemyHealth == null || enemies.Contains(enemyHealth))
            {
                return;
            }

            enemies.Add(enemyHealth);
            enemyHealth.Died += OnEnemyDied;
        }

        public void BeginGameplay()
        {
            SetState(GameState.Playing);
        }

        public void CompleteLevel()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            SetState(GameState.Victory);
        }

        private void OnPlayerDied(Health deadPlayer)
        {
            SetState(GameState.Defeat);
        }

        private void OnEnemyDied(Health deadEnemy)
        {
            deadEnemy.Died -= OnEnemyDied;
            enemies.Remove(deadEnemy);
        }

        private void SetState(GameState newState)
        {
            if (CurrentState == newState)
            {
                return;
            }

            CurrentState = newState;
            StateChanged?.Invoke(CurrentState);
        }
    }
}
