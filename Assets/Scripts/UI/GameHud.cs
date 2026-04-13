using Tanks.Combat;
using Tanks.Core;
using Tanks.Level;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks.UI
{
    public class GameHud : MonoBehaviour
    {
        private GUIStyle panelStyle;
        private GUIStyle textStyle;
        private GUIStyle titleStyle;
        private GUIStyle overlayStyle;
        private Health observedPlayerHealth;
        private LevelManager levelManager;
        private float hitFlashTimer;
        private Texture2D solidTexture;

        private void Update()
        {
            if (levelManager == null)
            {
                levelManager = FindAnyObjectByType<LevelManager>();
            }

            SyncPlayerSubscription();
            hitFlashTimer = Mathf.Max(0f, hitFlashTimer - Time.deltaTime * 2.4f);
        }

        private void OnGUI()
        {
            EnsureStyles();

            GUILayout.BeginArea(new Rect(16f, 16f, 360f, 220f), panelStyle);

            if (GameManager.Instance == null)
            {
                GUILayout.Label("Booting prototype...", titleStyle);
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label($"State: {GameManager.Instance.CurrentState}", titleStyle);
            GUILayout.Label($"Level: {GameManager.Instance.CurrentLevel}", textStyle);

            if (GameManager.Instance.PlayerHealth != null)
            {
                GUILayout.Label(
                    $"Player HP: {GameManager.Instance.PlayerHealth.CurrentHealth:0}/{GameManager.Instance.PlayerHealth.MaxHealth:0}",
                    textStyle);
            }

            GUILayout.Label($"Enemies Left: {GameManager.Instance.EnemyCount}", textStyle);
            GUILayout.Label($"Upgrades: {GameManager.Instance.Progression.UpgradeStatus}", textStyle);
            GUILayout.Space(6f);

            if (levelManager != null)
            {
                GUILayout.Label($"Objective: {levelManager.ObjectiveText}", textStyle);
                GUILayout.Label(levelManager.ObjectiveProgressText, textStyle);
                GUILayout.Label(levelManager.ExitStatusText, textStyle);
            }

            GUILayout.Space(10f);
            GUILayout.Label("Move: W/S or Up/Down", textStyle);
            GUILayout.Label("Turn: A/D or Left/Right", textStyle);
            GUILayout.Label("Fire: Space or Left Mouse", textStyle);

            if (GameManager.Instance.CurrentState == GameState.Defeat)
            {
                GUILayout.Space(10f);
                GUILayout.Label("Press R to restart", textStyle);
            }

            GUILayout.EndArea();

            if (!string.IsNullOrEmpty(GameManager.Instance.ActiveMessage))
            {
                GUI.Label(
                    new Rect((Screen.width - 520f) * 0.5f, 24f, 520f, 90f),
                    GameManager.Instance.ActiveMessage,
                    overlayStyle);
            }

            DrawPlayerScreenFeedback();
            DrawCrosshair();
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 12, 12)
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            };

            overlayStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            solidTexture = Texture2D.whiteTexture;
        }

        private void SyncPlayerSubscription()
        {
            Health playerHealth = GameManager.Instance != null ? GameManager.Instance.PlayerHealth : null;
            if (observedPlayerHealth == playerHealth)
            {
                return;
            }

            if (observedPlayerHealth != null)
            {
                observedPlayerHealth.Damaged -= OnPlayerDamaged;
            }

            observedPlayerHealth = playerHealth;

            if (observedPlayerHealth != null)
            {
                observedPlayerHealth.Damaged += OnPlayerDamaged;
            }
        }

        private void OnPlayerDamaged(Health damagedHealth)
        {
            hitFlashTimer = 1f;
        }

        private void DrawPlayerScreenFeedback()
        {
            if (solidTexture == null || GameManager.Instance?.PlayerHealth == null)
            {
                return;
            }

            float hpRatio = GameManager.Instance.PlayerHealth.MaxHealth > 0f
                ? GameManager.Instance.PlayerHealth.CurrentHealth / GameManager.Instance.PlayerHealth.MaxHealth
                : 0f;

            if (hpRatio < 0.35f)
            {
                float pulse = 0.18f + Mathf.Sin(Time.time * 7f) * 0.06f;
                DrawOverlay(new Color(0.85f, 0.05f, 0.05f, pulse * (1f - hpRatio)));

                if (GameManager.Instance.CurrentState == GameState.Playing)
                {
                    GUI.Label(new Rect(0f, Screen.height - 72f, Screen.width, 28f), "LOW HEALTH", overlayStyle);
                }
            }

            if (hitFlashTimer > 0f)
            {
                DrawOverlay(new Color(1f, 0.2f, 0.15f, 0.12f * hitFlashTimer));
            }
        }

        private void DrawOverlay(Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), solidTexture);
            GUI.color = previousColor;
        }

        private void DrawCrosshair()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || solidTexture == null)
            {
                return;
            }

            Vector2 mousePosition = mouse.position.ReadValue();
            float screenY = Screen.height - mousePosition.y;
            DrawCrosshairRect(new Rect(mousePosition.x - 10f, screenY - 1f, 20f, 2f));
            DrawCrosshairRect(new Rect(mousePosition.x - 1f, screenY - 10f, 2f, 20f));
            DrawCrosshairRect(new Rect(mousePosition.x - 2f, screenY - 2f, 4f, 4f));
        }

        private void DrawCrosshairRect(Rect rect)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 0.96f, 0.62f, 0.95f);
            GUI.DrawTexture(rect, solidTexture);
            GUI.color = previousColor;
        }

        private void OnDisable()
        {
            if (observedPlayerHealth != null)
            {
                observedPlayerHealth.Damaged -= OnPlayerDamaged;
            }
        }
    }
}
