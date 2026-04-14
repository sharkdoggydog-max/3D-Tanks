using Tanks.Combat;
using Tanks.Core;
using Tanks.Enemy;
using Tanks.Level;
using Tanks.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks.UI
{
    public class GameHud : MonoBehaviour
    {
        private const string BrandTextureResourcePath = "3DTanksBranding";

        private GUIStyle panelStyle;
        private GUIStyle textStyle;
        private GUIStyle titleStyle;
        private GUIStyle overlayStyle;
        private GUIStyle menuTagStyle;
        private GUIStyle menuTitleStyle;
        private GUIStyle menuTitleShadowStyle;
        private GUIStyle menuSubtitleStyle;
        private GUIStyle menuCardTitleStyle;
        private GUIStyle menuCardBodyStyle;
        private GUIStyle menuCardStatStyle;
        private GUIStyle menuSelectedStyle;
        private GUIStyle menuButtonStyle;
        private Health observedPlayerHealth;
        private LevelManager levelManager;
        private PlayerTankController playerController;
        private float hitFlashTimer;
        private Texture2D solidTexture;
        private Texture2D menuBrandTexture;

        private void Update()
        {
            if (levelManager == null)
            {
                levelManager = FindAnyObjectByType<LevelManager>();
            }

            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerTankController>();
            }

            SyncPlayerSubscription();
            hitFlashTimer = Mathf.Max(0f, hitFlashTimer - Time.deltaTime * 2.4f);
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (GameManager.Instance == null)
            {
                DrawBootPanel("Booting prototype...");
                return;
            }

            switch (GameManager.Instance.CurrentState)
            {
                case GameState.MainMenu:
                    DrawMainMenu();
                    return;
                case GameState.Booting:
                    DrawBootPanel("Preparing battlefield...");
                    return;
            }

            DrawGameplayPanel();

            if (!string.IsNullOrEmpty(GameManager.Instance.ActiveMessage))
            {
                GUI.Label(
                    new Rect((Screen.width - 520f) * 0.5f, 24f, 520f, 90f),
                    GameManager.Instance.ActiveMessage,
                    overlayStyle);
            }

            DrawPlayerScreenFeedback();
            DrawCrosshair();
            DrawMobileControls();
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

            menuTagStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.94f, 0.84f, 0.42f, 1f) }
            };

            menuTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 68,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.94f, 0.97f, 1f, 1f) }
            };

            menuTitleShadowStyle = new GUIStyle(menuTitleStyle)
            {
                normal = { textColor = new Color(0f, 0f, 0f, 0.5f) }
            };

            menuSubtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                wordWrap = true,
                normal = { textColor = new Color(0.78f, 0.86f, 0.92f, 1f) }
            };

            menuCardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            menuCardBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.88f, 0.92f, 0.95f, 1f) }
            };

            menuCardStatStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = new Color(0.98f, 0.88f, 0.56f, 1f) }
            };

            menuSelectedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.98f, 1f, 1f) }
            };

            menuButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                fixedHeight = 0f
            };

            solidTexture = Texture2D.whiteTexture;
        }

        private void DrawBootPanel(string message)
        {
            Rect panelRect = new Rect((Screen.width - 360f) * 0.5f, (Screen.height - 120f) * 0.5f, 360f, 120f);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(panelRect, message, overlayStyle);
        }

        private void DrawGameplayPanel()
        {
            GUILayout.BeginArea(new Rect(16f, 16f, 360f, 220f), panelStyle);

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
            if (MobileControlLayout.ShouldUseTouchControls())
            {
                GUILayout.Label("Move: Left joystick", textStyle);
                GUILayout.Label("Aim: Right touch area", textStyle);
                GUILayout.Label("Fire: Fire button", textStyle);
            }
            else
            {
                GUILayout.Label("Move: W/S or Up/Down", textStyle);
                GUILayout.Label("Turn: A/D or Left/Right", textStyle);
                GUILayout.Label("Fire: Space or Left Mouse", textStyle);
            }

            if (GameManager.Instance.CurrentState == GameState.Defeat)
            {
                GUILayout.Space(10f);
                GUILayout.Label(
                    MobileControlLayout.ShouldUseTouchControls() ? "Tap Restart to try again" : "Press R to restart",
                    textStyle);
            }

            GUILayout.EndArea();
        }

        private void DrawMainMenu()
        {
            DrawMenuBackdrop();

            float panelWidth = Mathf.Min(Screen.width - 48f, 980f);
            bool compactLayout = panelWidth < 760f;
            float headerHeight = compactLayout ? 330f : 380f;
            float cardHeight = compactLayout ? 148f : 198f;
            float cardGap = 16f;
            float cardsHeight = compactLayout ? cardHeight * 3f + cardGap * 2f : cardHeight;
            float buttonHeight = 68f;
            float panelHeight = headerHeight + cardsHeight + buttonHeight + 98f;
            Rect panelRect = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                Mathf.Max(24f, (Screen.height - panelHeight) * 0.5f),
                panelWidth,
                panelHeight);

            DrawPanel(panelRect, new Color(0.08f, 0.1f, 0.13f, 0.94f), new Color(0.35f, 0.52f, 0.58f, 0.8f), 2f);
            DrawMenuTitle(panelRect);

            Rect cardsArea = new Rect(panelRect.x + 26f, panelRect.y + headerHeight, panelRect.width - 52f, cardsHeight);
            EnemyVariant selectedTank = levelManager != null ? levelManager.SelectedPlayerTank : GameManager.Instance.Progression.SelectedPlayerTank;
            DrawTankSelection(cardsArea, compactLayout, ref selectedTank);

            Rect buttonRect = new Rect(panelRect.x + 26f, panelRect.yMax - buttonHeight - 24f, panelRect.width - 52f, buttonHeight);
            bool canStart = levelManager != null;
            GUI.enabled = canStart;
            if (GUI.Button(buttonRect, $"Start Run: {GetTankDisplayName(selectedTank)}", menuButtonStyle))
            {
                levelManager.StartSelectedTankRun(selectedTank);
            }

            GUI.enabled = true;

            if (!canStart)
            {
                GUI.Label(new Rect(buttonRect.x, buttonRect.y - 30f, buttonRect.width, 24f), "Waiting for level systems...", menuSubtitleStyle);
            }
        }

        private void DrawMenuBackdrop()
        {
            DrawOverlay(new Color(0.03f, 0.04f, 0.06f, 1f));
            DrawPanel(new Rect(0f, 0f, Screen.width, Screen.height * 0.24f), new Color(0.12f, 0.07f, 0.04f, 0.78f), Color.clear, 0f);
            DrawPanel(new Rect(0f, Screen.height * 0.66f, Screen.width, Screen.height * 0.34f), new Color(0.06f, 0.09f, 0.11f, 0.9f), Color.clear, 0f);
            DrawPanel(new Rect(Screen.width * 0.08f, Screen.height * 0.14f, Screen.width * 0.24f, 12f), new Color(0.96f, 0.52f, 0.14f, 0.84f), Color.clear, 0f);
            DrawPanel(new Rect(Screen.width * 0.66f, Screen.height * 0.78f, Screen.width * 0.2f, 12f), new Color(1f, 0.84f, 0.24f, 0.66f), Color.clear, 0f);
        }

        private void DrawMenuTitle(Rect panelRect)
        {
            Rect tagRect = new Rect(panelRect.x + 40f, panelRect.y + 18f, panelRect.width - 80f, 24f);
            Rect logoRect = new Rect(panelRect.x + 52f, panelRect.y + 44f, panelRect.width - 104f, Mathf.Min(250f, panelRect.width * 0.34f));
            Rect subtitleRect = new Rect(panelRect.x + 70f, logoRect.yMax + 12f, panelRect.width - 140f, 44f);
            Texture2D brandTexture = GetMenuBrandTexture();

            GUI.Label(tagRect, "ARMORED ASSAULT PROTOTYPE", menuTagStyle);
            if (brandTexture != null)
            {
                GUI.DrawTexture(logoRect, brandTexture, ScaleMode.ScaleToFit, true);
            }
            else
            {
                DrawShadowedLabel(logoRect, "3D TANKS");
            }

            GUI.Label(subtitleRect, "Choose your tank frame, then punch through the enemy command chain.", menuSubtitleStyle);
        }

        private void DrawTankSelection(Rect areaRect, bool compactLayout, ref EnemyVariant selectedTank)
        {
            Rect[] cardRects = new Rect[3];
            float gap = 16f;
            float cardWidth = compactLayout ? areaRect.width : (areaRect.width - gap * 2f) / 3f;
            float cardHeight = compactLayout ? (areaRect.height - gap * 2f) / 3f : areaRect.height;

            for (int index = 0; index < cardRects.Length; index++)
            {
                if (compactLayout)
                {
                    cardRects[index] = new Rect(areaRect.x, areaRect.y + index * (cardHeight + gap), areaRect.width, cardHeight);
                }
                else
                {
                    cardRects[index] = new Rect(areaRect.x + index * (cardWidth + gap), areaRect.y, cardWidth, cardHeight);
                }
            }

            selectedTank = DrawTankCard(cardRects[0], EnemyVariant.Basic, selectedTank);
            selectedTank = DrawTankCard(cardRects[1], EnemyVariant.Raider, selectedTank);
            selectedTank = DrawTankCard(cardRects[2], EnemyVariant.Bulwark, selectedTank);

            GameManager.Instance.Progression.SelectPlayerTank(selectedTank);
        }

        private EnemyVariant DrawTankCard(Rect rect, EnemyVariant tankType, EnemyVariant selectedTank)
        {
            bool isSelected = selectedTank == tankType;
            Color fillColor = isSelected
                ? new Color(0.16f, 0.24f, 0.28f, 0.98f)
                : new Color(0.11f, 0.14f, 0.18f, 0.94f);
            Color borderColor = isSelected ? GetTankAccentColor(tankType) : new Color(0.25f, 0.31f, 0.35f, 0.85f);
            DrawPanel(rect, fillColor, borderColor, isSelected ? 3f : 1.5f);

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                selectedTank = tankType;
            }

            Rect accentRect = new Rect(rect.x, rect.y, rect.width, 8f);
            DrawPanel(accentRect, GetTankAccentColor(tankType), Color.clear, 0f);

            float contentX = rect.x + 16f;
            float contentWidth = rect.width - 32f;
            float titleY = rect.y + 18f;
            float descriptionY = rect.y + 50f;
            float descriptionHeight = Mathf.Max(48f, rect.height - 116f);
            float statsY = rect.yMax - 50f;

            GUI.Label(new Rect(contentX, titleY, contentWidth - 74f, 24f), GetTankDisplayName(tankType), menuCardTitleStyle);
            GUI.Label(new Rect(contentX, descriptionY, contentWidth, descriptionHeight), GetTankDescription(tankType), menuCardBodyStyle);
            GUI.Label(new Rect(contentX, statsY, contentWidth, 36f), GetTankStatsLine(tankType), menuCardStatStyle);

            if (isSelected)
            {
                GUI.Label(new Rect(rect.x + rect.width - 106f, rect.y + 18f, 90f, 20f), "SELECTED", menuSelectedStyle);
            }

            return selectedTank;
        }

        private static string GetTankDisplayName(EnemyVariant tankType)
        {
            return tankType switch
            {
                EnemyVariant.Raider => "Raider",
                EnemyVariant.Bulwark => "Bulwark",
                _ => "Basic"
            };
        }

        private static string GetTankDescription(EnemyVariant tankType)
        {
            return tankType switch
            {
                EnemyVariant.Raider => "Fast flanker. Sprints hard, pivots quickly, and throws rapid but lighter rounds.",
                EnemyVariant.Bulwark => "Heavy bruiser. Slower to line up, but shrugs off damage and hits much harder.",
                _ => "Balanced line tank. Steady health, handling, and shell power with no major weakness."
            };
        }

        private static string GetTankStatsLine(EnemyVariant tankType)
        {
            return tankType switch
            {
                EnemyVariant.Raider => "Speed +++  Turn +++\nFire Rate ++  HP --  Damage -",
                EnemyVariant.Bulwark => "HP +++  Damage +++\nSpeed -  Turn -",
                _ => "Even speed, armor, and damage\nNo major weakness"
            };
        }

        private static Color GetTankAccentColor(EnemyVariant tankType)
        {
            return tankType switch
            {
                EnemyVariant.Raider => new Color(0.27f, 0.84f, 0.78f, 1f),
                EnemyVariant.Bulwark => new Color(0.62f, 0.8f, 0.94f, 1f),
                _ => new Color(0.56f, 0.94f, 0.48f, 1f)
            };
        }

        private void DrawShadowedLabel(Rect rect, string text)
        {
            Rect shadowRect = new Rect(rect.x + 3f, rect.y + 3f, rect.width, rect.height);
            GUI.Label(shadowRect, text, menuTitleShadowStyle);
            GUI.Label(rect, text, menuTitleStyle);
        }

        private Texture2D GetMenuBrandTexture()
        {
            if (menuBrandTexture == null)
            {
                menuBrandTexture = Resources.Load<Texture2D>(BrandTextureResourcePath);
            }

            return menuBrandTexture;
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

        private void DrawPanel(Rect rect, Color fillColor, Color borderColor, float borderThickness)
        {
            if (solidTexture == null)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = fillColor;
            GUI.DrawTexture(rect, solidTexture);

            if (borderThickness > 0.01f && borderColor.a > 0.001f)
            {
                GUI.color = borderColor;
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, borderThickness), solidTexture);
                GUI.DrawTexture(new Rect(rect.x, rect.yMax - borderThickness, rect.width, borderThickness), solidTexture);
                GUI.DrawTexture(new Rect(rect.x, rect.y, borderThickness, rect.height), solidTexture);
                GUI.DrawTexture(new Rect(rect.xMax - borderThickness, rect.y, borderThickness, rect.height), solidTexture);
            }

            GUI.color = previousColor;
        }

        private void DrawCrosshair()
        {
            if (MobileControlLayout.ShouldUseTouchControls() ||
                GameManager.Instance == null ||
                GameManager.Instance.CurrentState != GameState.Playing)
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

        private void DrawMobileControls()
        {
            if (!MobileControlLayout.ShouldUseTouchControls() || solidTexture == null || GameManager.Instance == null)
            {
                return;
            }

            if (GameManager.Instance.CurrentState == GameState.Playing)
            {
                DrawMoveStick();
                DrawAimArea();
                DrawFireButton();
            }

            if (GameManager.Instance.CurrentState == GameState.Defeat)
            {
                DrawRestartButton();
            }
        }

        private void DrawMoveStick()
        {
            float stickRadius = MobileControlLayout.GetStickRadius();
            float knobRadius = MobileControlLayout.GetKnobRadius();
            Vector2 guiCenter = MobileControlLayout.ToGuiPosition(playerController != null
                ? playerController.MoveStickVisualCenter
                : MobileControlLayout.GetMoveStickCenter());
            Vector2 guiKnobCenter = guiCenter;

            if (playerController != null)
            {
                Vector2 knobScreenCenter = playerController.MoveStickVisualCenter + playerController.MoveStickVisualOffset;
                guiKnobCenter = MobileControlLayout.ToGuiPosition(knobScreenCenter);
            }

            DrawFilledCircle(guiCenter, stickRadius, new Color(0.08f, 0.08f, 0.08f, 0.32f));
            DrawFilledCircle(guiCenter, stickRadius * 0.78f, new Color(0.95f, 0.95f, 0.95f, 0.08f));
            DrawFilledCircle(guiKnobCenter, knobRadius, new Color(0.92f, 0.98f, 1f, 0.45f));
            GUI.Label(new Rect(guiCenter.x - 44f, guiCenter.y - stickRadius - 28f, 88f, 24f), "MOVE", textStyle);
        }

        private void DrawAimArea()
        {
            Rect guiAimRect = MobileControlLayout.ToGuiRect(MobileControlLayout.GetAimZone());
            Rect fireRect = MobileControlLayout.ToGuiRect(MobileControlLayout.GetFireButtonRect());
            guiAimRect.yMin += 12f;
            guiAimRect.height -= fireRect.height + 36f;

            Color previousColor = GUI.color;
            GUI.color = new Color(0.82f, 0.9f, 1f, 0.05f);
            GUI.DrawTexture(guiAimRect, solidTexture);
            GUI.color = previousColor;

            GUI.Label(new Rect(guiAimRect.x + 18f, guiAimRect.yMax - 34f, 140f, 24f), "AIM", textStyle);

            if (playerController != null && playerController.HasAimTouch)
            {
                DrawFilledCircle(playerController.AimTouchGuiPosition, 18f, new Color(1f, 0.96f, 0.62f, 0.42f));
                DrawFilledCircle(playerController.AimTouchGuiPosition, 7f, new Color(1f, 1f, 1f, 0.72f));
            }
        }

        private void DrawFireButton()
        {
            Rect fireRect = MobileControlLayout.ToGuiRect(MobileControlLayout.GetFireButtonRect());
            Vector2 center = fireRect.center;
            float radius = fireRect.width * 0.5f;
            Color fillColor = playerController != null && playerController.IsFireTouchHeld
                ? new Color(1f, 0.44f, 0.28f, 0.82f)
                : new Color(1f, 0.44f, 0.28f, 0.46f);

            DrawFilledCircle(center, radius, fillColor);
            DrawFilledCircle(center, radius * 0.72f, new Color(1f, 1f, 1f, 0.1f));
            GUI.Label(new Rect(fireRect.x, fireRect.y + fireRect.height * 0.34f, fireRect.width, 28f), "FIRE", overlayStyle);
        }

        private void DrawRestartButton()
        {
            if (levelManager == null)
            {
                levelManager = FindAnyObjectByType<LevelManager>();
            }

            if (GameManager.Instance.CurrentState != GameState.Defeat || levelManager == null)
            {
                return;
            }

            Rect restartRect = MobileControlLayout.ToGuiRect(MobileControlLayout.GetRestartButtonRect());
            GUI.Box(
                new Rect(restartRect.x - 10f, restartRect.y - 12f, restartRect.width + 20f, restartRect.height + 24f),
                GUIContent.none,
                panelStyle);
            GUI.Label(
                new Rect(restartRect.x - 40f, restartRect.y - 42f, restartRect.width + 80f, 26f),
                "Tap Restart to try again",
                overlayStyle);

            if (GUI.Button(restartRect, "Restart"))
            {
                levelManager.RestartCurrentRun();
            }
        }

        private void DrawFilledCircle(Vector2 center, float radius, Color color)
        {
            if (solidTexture == null)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), solidTexture);
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
