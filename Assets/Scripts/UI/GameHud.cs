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
        private GUIStyle menuListEntryStyle;
        private GUIStyle menuListSelectedEntryStyle;
        private GUIStyle menuCardRoleStyle;
        private GUIStyle menuSelectedStyle;
        private GUIStyle menuDetailTitleStyle;
        private GUIStyle menuDetailBodyStyle;
        private GUIStyle menuChipStyle;
        private GUIStyle menuButtonStyle;
        private GUIStyle hudButtonStyle;
        private GUIStyle hudMenuButtonStyle;
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
            DrawGameplayMainMenuButton();

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

            menuListEntryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                clipping = TextClipping.Clip,
                padding = new RectOffset(20, 14, 0, 0),
                normal = { textColor = new Color(0.94f, 0.97f, 1f, 1f) }
            };

            menuListEntryStyle.hover.textColor = menuListEntryStyle.normal.textColor;
            menuListEntryStyle.active.textColor = menuListEntryStyle.normal.textColor;
            menuListEntryStyle.focused.textColor = menuListEntryStyle.normal.textColor;
            menuListEntryStyle.onNormal.textColor = menuListEntryStyle.normal.textColor;
            menuListEntryStyle.onHover.textColor = menuListEntryStyle.normal.textColor;
            menuListEntryStyle.onActive.textColor = menuListEntryStyle.normal.textColor;
            menuListEntryStyle.onFocused.textColor = menuListEntryStyle.normal.textColor;

            menuListSelectedEntryStyle = new GUIStyle(menuListEntryStyle)
            {
                normal = { textColor = new Color(1f, 1f, 1f, 1f) }
            };

            menuListSelectedEntryStyle.hover.textColor = menuListSelectedEntryStyle.normal.textColor;
            menuListSelectedEntryStyle.active.textColor = menuListSelectedEntryStyle.normal.textColor;
            menuListSelectedEntryStyle.focused.textColor = menuListSelectedEntryStyle.normal.textColor;
            menuListSelectedEntryStyle.onNormal.textColor = menuListSelectedEntryStyle.normal.textColor;
            menuListSelectedEntryStyle.onHover.textColor = menuListSelectedEntryStyle.normal.textColor;
            menuListSelectedEntryStyle.onActive.textColor = menuListSelectedEntryStyle.normal.textColor;
            menuListSelectedEntryStyle.onFocused.textColor = menuListSelectedEntryStyle.normal.textColor;

            menuCardRoleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.UpperLeft,
                clipping = TextClipping.Clip,
                normal = { textColor = new Color(0.88f, 0.92f, 0.95f, 1f) }
            };

            menuDetailTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            menuDetailBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.84f, 0.91f, 0.95f, 1f) }
            };

            menuChipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(8, 8, 2, 2),
                normal = { textColor = new Color(0.98f, 0.9f, 0.62f, 1f) }
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

            hudButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 0f,
                padding = new RectOffset(10, 10, 6, 6)
            };

            hudMenuButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 0f,
                padding = new RectOffset(12, 12, 7, 7)
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
            Rect safeRect = GetGuiSafeArea();
            float areaWidth = Mathf.Min(360f, Mathf.Max(280f, safeRect.width - 32f));
            float areaHeight = levelManager != null ? 296f : 268f;
            GUILayout.BeginArea(new Rect(safeRect.x + 16f, safeRect.y + 16f, areaWidth, areaHeight), panelStyle);

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

            Rect safeRect = GetGuiSafeArea();
            float outerMargin = Mathf.Clamp(Mathf.Min(safeRect.width, safeRect.height) * 0.035f, 16f, 30f);
            float maxPanelWidth = Mathf.Max(0f, safeRect.width - outerMargin * 2f);
            bool stackedLayout = safeRect.width < 920f || safeRect.height > safeRect.width * 1.05f;
            EnemyVariant[] selectableTanks = GetSelectableTankTypes();
            float panelWidth = Mathf.Min(maxPanelWidth, stackedLayout ? 780f : 1120f);
            float referenceHeight = stackedLayout ? 900f : 760f;
            float widthScale = Mathf.Clamp(panelWidth / 1120f, 0.68f, 1f);
            float heightScale = Mathf.Clamp((safeRect.height - outerMargin * 2f) / referenceHeight, 0.68f, 1f);
            float layoutScale = Mathf.Min(widthScale, heightScale);

            ApplyResponsiveMenuStyles(layoutScale, stackedLayout);

            float horizontalPadding = Mathf.Clamp(18f + panelWidth * 0.012f, 18f, 30f);
            float topPadding = Mathf.Lerp(18f, 28f, layoutScale);
            float bottomPadding = Mathf.Lerp(18f, 26f, layoutScale);
            float titleGap = Mathf.Lerp(6f, 12f, layoutScale);
            float sectionGap = Mathf.Lerp(14f, 20f, layoutScale);
            float panelGap = Mathf.Lerp(12f, 18f, layoutScale);
            float buttonHeight = Mathf.Lerp(54f, 68f, layoutScale);
            float waitingLabelHeight = levelManager == null ? Mathf.Lerp(18f, 24f, layoutScale) : 0f;

            Rect tagRect = default;
            Rect logoRect = new Rect();
            Rect subtitleRect = new Rect();

            float tagWidth = panelWidth - horizontalPadding * 2f;
            float tagHeight = menuTagStyle.CalcHeight(new GUIContent("ARMORED ASSAULT PROTOTYPE"), tagWidth);
            float logoWidth = panelWidth - horizontalPadding * 2.4f;
            float logoHeight = Mathf.Clamp(panelWidth * (stackedLayout ? 0.18f : 0.16f), 96f, stackedLayout ? 156f : 190f);
            float subtitleWidth = panelWidth - horizontalPadding * 2.8f;
            float subtitleHeight = menuSubtitleStyle.CalcHeight(
                new GUIContent("Choose your tank frame, then punch through the enemy command chain."),
                subtitleWidth);
            float headerHeight = topPadding + tagHeight + titleGap + logoHeight + titleGap + subtitleHeight;

            float maxPanelHeight = safeRect.height - outerMargin * 2f;
            float contentHeight = Mathf.Max(0f, maxPanelHeight - headerHeight - sectionGap - waitingLabelHeight - buttonHeight - sectionGap - bottomPadding);
            float detailHeight;
            float listHeight;

            if (stackedLayout)
            {
                float preferredListHeight = Mathf.Lerp(220f, 276f, layoutScale);
                float splitSpace = Mathf.Max(0f, contentHeight - panelGap);
                float preferredListRatio = splitSpace > 0.01f ? preferredListHeight / splitSpace : 0.5f;
                preferredListRatio = Mathf.Clamp(preferredListRatio, 0.42f, 0.56f);
                listHeight = splitSpace * preferredListRatio;
                detailHeight = splitSpace - listHeight;
            }
            else
            {
                listHeight = contentHeight;
                detailHeight = contentHeight;
            }

            float panelHeight = headerHeight + sectionGap + contentHeight + sectionGap + waitingLabelHeight + buttonHeight + bottomPadding;
            float panelX = safeRect.x + (safeRect.width - panelWidth) * 0.5f;
            float panelY = safeRect.y + outerMargin + (maxPanelHeight - panelHeight) * 0.35f;
            Rect panelRect = new Rect(panelX, panelY, panelWidth, panelHeight);

            tagRect = new Rect(panelRect.x + horizontalPadding, panelRect.y + topPadding, tagWidth, tagHeight);
            logoRect = new Rect(
                panelRect.x + (panelRect.width - logoWidth) * 0.5f,
                tagRect.yMax + titleGap,
                logoWidth,
                logoHeight);
            subtitleRect = new Rect(
                panelRect.x + (panelRect.width - subtitleWidth) * 0.5f,
                logoRect.yMax + titleGap,
                subtitleWidth,
                subtitleHeight);

            DrawPanel(panelRect, new Color(0.08f, 0.1f, 0.13f, 0.94f), new Color(0.35f, 0.52f, 0.58f, 0.8f), 2f);
            DrawMenuTitle(tagRect, logoRect, subtitleRect);

            EnemyVariant selectedTank = levelManager != null ? levelManager.SelectedPlayerTank : GameManager.Instance.Progression.SelectedPlayerTank;
            Rect contentRect = new Rect(
                panelRect.x + horizontalPadding,
                subtitleRect.yMax + sectionGap,
                panelRect.width - horizontalPadding * 2f,
                contentHeight);

            Rect listRect;
            Rect detailRect;
            Rect buttonRect;

            if (stackedLayout)
            {
                listRect = new Rect(contentRect.x, contentRect.y, contentRect.width, listHeight);
                detailRect = new Rect(contentRect.x, listRect.yMax + panelGap, contentRect.width, detailHeight);
                buttonRect = new Rect(
                    contentRect.x,
                    detailRect.yMax + sectionGap + waitingLabelHeight,
                    contentRect.width,
                    buttonHeight);
            }
            else
            {
                float listWidth = Mathf.Clamp(contentRect.width * 0.36f, 340f, 420f);
                float detailWidth = contentRect.width - listWidth - panelGap;
                listRect = new Rect(contentRect.x, contentRect.y, listWidth, contentRect.height);
                detailRect = new Rect(listRect.xMax + panelGap, contentRect.y, detailWidth, contentRect.height);
                buttonRect = new Rect(
                    detailRect.x,
                    detailRect.yMax + sectionGap + waitingLabelHeight,
                    detailRect.width,
                    buttonHeight);
            }

            DrawTankSelection(listRect, stackedLayout, selectableTanks, ref selectedTank);
            DrawSelectedTankDetail(detailRect, selectedTank, stackedLayout);
            bool canStart = levelManager != null;
            GUI.enabled = canStart;
            if (GUI.Button(buttonRect, $"Start Run: {GetTankDisplayName(selectedTank)}", menuButtonStyle))
            {
                levelManager.StartSelectedTankRun(selectedTank);
            }

            GUI.enabled = true;

            if (!canStart)
            {
                GUI.Label(
                    new Rect(buttonRect.x, buttonRect.y - waitingLabelHeight - 4f, buttonRect.width, waitingLabelHeight),
                    "Waiting for level systems...",
                    menuSubtitleStyle);
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

        private void DrawMenuTitle(Rect tagRect, Rect logoRect, Rect subtitleRect)
        {
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

        private void DrawTankSelection(Rect areaRect, bool stackedLayout, EnemyVariant[] tankTypes, ref EnemyVariant selectedTank)
        {
            if (tankTypes == null || tankTypes.Length == 0)
            {
                return;
            }

            DrawPanel(areaRect, new Color(0.09f, 0.12f, 0.16f, 0.94f), new Color(0.24f, 0.33f, 0.4f, 0.9f), 1.5f);

            float panelPadding = 18f;
            float headerHeight = 20f;
            float gap = 12f;
            GUI.Label(new Rect(areaRect.x + panelPadding, areaRect.y + panelPadding, areaRect.width - panelPadding * 2f, headerHeight), "Select Tank", titleStyle);

            Rect listRect = new Rect(
                areaRect.x + panelPadding,
                areaRect.y + panelPadding + headerHeight + gap,
                areaRect.width - panelPadding * 2f,
                areaRect.height - panelPadding * 2f - headerHeight - gap);
            int columnCount = stackedLayout ? 2 : 1;
            int rowCount = Mathf.CeilToInt(tankTypes.Length / (float)columnCount);
            float entryGap = stackedLayout ? 12f : 10f;
            float cardWidth = (listRect.width - entryGap * Mathf.Max(0, columnCount - 1)) / columnCount;
            float cardHeight = (listRect.height - entryGap * Mathf.Max(0, rowCount - 1)) / rowCount;

            for (int index = 0; index < tankTypes.Length; index++)
            {
                int column = index % columnCount;
                int row = index / columnCount;
                Rect cardRect = new Rect(
                    listRect.x + column * (cardWidth + entryGap),
                    listRect.y + row * (cardHeight + entryGap),
                    cardWidth,
                    cardHeight);
                selectedTank = DrawTankListEntry(cardRect, tankTypes[index], selectedTank);
            }

            GameManager.Instance.Progression.SelectPlayerTank(selectedTank);
        }

        private EnemyVariant DrawTankListEntry(Rect rect, EnemyVariant tankType, EnemyVariant selectedTank)
        {
            bool isSelected = selectedTank == tankType;
            Color fillColor = isSelected
                ? new Color(0.2f, 0.28f, 0.35f, 1f)
                : new Color(0.11f, 0.14f, 0.18f, 0.94f);
            Color borderColor = isSelected ? GetTankAccentColor(tankType) : new Color(0.25f, 0.31f, 0.35f, 0.85f);
            DrawPanel(rect, fillColor, borderColor, isSelected ? 3f : 1.5f);

            float accentInset = 10f;
            float accentWidth = isSelected ? 6f : 3f;
            Rect accentRect = new Rect(rect.x + accentInset, rect.y + 8f, accentWidth, rect.height - 16f);
            DrawPanel(accentRect, GetTankAccentColor(tankType), Color.clear, 0f);

            if (isSelected)
            {
                DrawPanel(
                    new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f),
                    new Color(1f, 1f, 1f, 0.05f),
                    Color.clear,
                    0f);
            }

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                selectedTank = tankType;
            }

            float labelLeft = accentRect.xMax + 14f;
            Rect labelRect = new Rect(labelLeft, rect.y, rect.xMax - labelLeft - 16f, rect.height);
            GUI.Label(labelRect, GetTankDisplayName(tankType), isSelected ? menuListSelectedEntryStyle : menuListEntryStyle);

            return selectedTank;
        }

        private void DrawSelectedTankDetail(Rect rect, EnemyVariant selectedTank, bool stackedLayout)
        {
            DrawPanel(rect, new Color(0.09f, 0.12f, 0.16f, 0.94f), new Color(0.24f, 0.33f, 0.4f, 0.9f), 1.5f);

            float horizontalPadding = 16f;
            float topPadding = 14f;
            float contentWidth = rect.width - horizontalPadding * 2f;
            float titleHeight = 26f;
            float roleHeight = 20f;
            float chipHeight = 22f;
            float chipGap = 6f;
            string[] tags = GetTankTags(selectedTank);
            int chipColumns = stackedLayout ? 2 : 4;
            int chipRows = Mathf.CeilToInt(tags.Length / (float)chipColumns);
            float chipWidth = (contentWidth - chipGap * Mathf.Max(0, chipColumns - 1)) / chipColumns;

            GUI.Label(new Rect(rect.x + horizontalPadding, rect.y + topPadding, contentWidth, 18f), "Selected Tank", menuTagStyle);
            float contentTop = rect.y + topPadding + 22f;
            float chipY = contentTop + titleHeight + roleHeight + 10f;

            GUI.Label(new Rect(rect.x + horizontalPadding, contentTop, contentWidth, titleHeight), GetTankDisplayName(selectedTank), menuDetailTitleStyle);
            GUI.Label(new Rect(rect.x + horizontalPadding, contentTop + titleHeight, contentWidth, roleHeight), GetTankRoleLine(selectedTank), menuCardRoleStyle);

            for (int index = 0; index < tags.Length; index++)
            {
                int column = index % chipColumns;
                int row = index / chipColumns;
                Rect chipRect = new Rect(
                    rect.x + horizontalPadding + column * (chipWidth + chipGap),
                    chipY + row * (chipHeight + chipGap),
                    chipWidth,
                    chipHeight);
                DrawPanel(chipRect, new Color(1f, 1f, 1f, 0.06f), new Color(1f, 1f, 1f, 0.08f), 1f);
                GUI.Label(chipRect, tags[index], menuChipStyle);
            }

            float bodyY = chipY + chipRows * chipHeight + Mathf.Max(0, chipRows - 1) * chipGap + 12f;
            float bodyHeight = Mathf.Max(20f, rect.yMax - bodyY - 12f);
            GUI.Label(
                new Rect(rect.x + horizontalPadding, bodyY, contentWidth, bodyHeight),
                GetTankDetailDescription(selectedTank),
                menuDetailBodyStyle);
        }

        private void ApplyResponsiveMenuStyles(float layoutScale, bool stackedLayout)
        {
            menuTagStyle.fontSize = Mathf.RoundToInt(Mathf.Lerp(14f, 18f, layoutScale));
            menuTitleStyle.fontSize = Mathf.RoundToInt(Mathf.Lerp(48f, 68f, layoutScale));
            menuTitleShadowStyle.fontSize = menuTitleStyle.fontSize;
            menuSubtitleStyle.fontSize = Mathf.RoundToInt(Mathf.Lerp(12f, 15f, layoutScale));
            menuCardTitleStyle.fontSize = Mathf.RoundToInt(Mathf.Lerp(stackedLayout ? 15f : 16f, 20f, layoutScale));
            menuListEntryStyle.fontSize = Mathf.RoundToInt(Mathf.Lerp(16f, 20f, layoutScale));
            menuListSelectedEntryStyle.fontSize = menuListEntryStyle.fontSize;
            menuCardRoleStyle.fontSize = Mathf.RoundToInt(Mathf.Lerp(11f, 13f, layoutScale));
            menuSelectedStyle.fontSize = Mathf.RoundToInt(Mathf.Lerp(10f, 12f, layoutScale));
            menuDetailTitleStyle.fontSize = Mathf.RoundToInt(Mathf.Lerp(16f, 18f, layoutScale));
            menuDetailBodyStyle.fontSize = Mathf.RoundToInt(Mathf.Lerp(12f, 13f, layoutScale));
            menuChipStyle.fontSize = Mathf.RoundToInt(Mathf.Lerp(10f, 11f, layoutScale));
            menuButtonStyle.fontSize = Mathf.RoundToInt(Mathf.Lerp(16f, 19f, layoutScale));
            hudButtonStyle.fontSize = Mathf.RoundToInt(Mathf.Lerp(13f, 14f, layoutScale));
            hudMenuButtonStyle.fontSize = Mathf.RoundToInt(Mathf.Lerp(13f, 15f, layoutScale));

            int buttonPadding = Mathf.RoundToInt(Mathf.Lerp(10f, 14f, layoutScale));
            menuButtonStyle.padding = new RectOffset(buttonPadding, buttonPadding, buttonPadding, buttonPadding);
        }

        private void DrawGameplayMainMenuButton()
        {
            if (levelManager == null || GameManager.Instance == null)
            {
                return;
            }

            if (GameManager.Instance.CurrentState != GameState.Playing &&
                GameManager.Instance.CurrentState != GameState.Defeat &&
                GameManager.Instance.CurrentState != GameState.Victory)
            {
                return;
            }

            Rect safeRect = GetGuiSafeArea();
            float width = MobileControlLayout.ShouldUseTouchControls()
                ? Mathf.Clamp(safeRect.width * 0.28f, 128f, 176f)
                : 156f;
            float height = MobileControlLayout.ShouldUseTouchControls() ? 42f : 38f;
            float margin = 16f;
            Rect buttonRect = new Rect(
                safeRect.xMax - width - margin,
                safeRect.y + margin,
                width,
                height);

            if (GUI.Button(buttonRect, "Main Menu", hudMenuButtonStyle))
            {
                levelManager.ReturnToMainMenu();
            }
        }

        private static Rect GetGuiSafeArea()
        {
            Rect safeArea = Screen.safeArea;
            return new Rect(safeArea.x, Screen.height - safeArea.yMax, safeArea.width, safeArea.height);
        }

        private static string GetTankDisplayName(EnemyVariant tankType)
        {
            return tankType switch
            {
                EnemyVariant.Artillery => "Artillery",
                EnemyVariant.Raider => "Raider",
                EnemyVariant.Striker => "Striker",
                EnemyVariant.Scout => "Scout",
                EnemyVariant.Bulwark => "Bulwark",
                _ => "Basic"
            };
        }

        private static string GetTankRoleLine(EnemyVariant tankType)
        {
            return tankType switch
            {
                EnemyVariant.Artillery => "Backline siege gun",
                EnemyVariant.Raider => "Fast flank attacker",
                EnemyVariant.Striker => "Close assault bruiser",
                EnemyVariant.Scout => "Recon skirmisher",
                EnemyVariant.Bulwark => "Heavy line breaker",
                _ => "Balanced line tank"
            };
        }

        private static string GetTankDetailDescription(EnemyVariant tankType)
        {
            return tankType switch
            {
                EnemyVariant.Artillery => "Slow and vulnerable, but its heavy blast rounds punish clustered targets and dominate open lanes if left alone.",
                EnemyVariant.Raider => "Fast and responsive with lighter firepower, built to flank, reposition, and keep pressure from the edges of a fight.",
                EnemyVariant.Striker => "Built for direct engagements, using compact armor and burst fire to win short-range pushes before enemies can disengage.",
                EnemyVariant.Scout => "Extremely agile and hard to pin down, trading durability for speed and constant harassment from awkward angles.",
                EnemyVariant.Bulwark => "A durable front-line tank that absorbs pressure, holds space, and answers with slower but harder hits.",
                _ => "A steady all-rounder with balanced armor, mobility, and shell performance."
            };
        }

        private static string[] GetTankTags(EnemyVariant tankType)
        {
            return tankType switch
            {
                EnemyVariant.Artillery => new[] { "Long Range", "Splash", "Slow", "Fragile" },
                EnemyVariant.Raider => new[] { "Fast", "Agile", "Flanker", "Rapid Fire" },
                EnemyVariant.Striker => new[] { "Burst", "Pressure", "Close Range", "Medium HP" },
                EnemyVariant.Scout => new[] { "Very Fast", "Recon", "Evasive", "Low Damage" },
                EnemyVariant.Bulwark => new[] { "High HP", "Heavy Hit", "Slow", "Frontline" },
                _ => new[] { "Balanced", "Reliable", "Mid Range", "Steady" }
            };
        }

        private static Color GetTankAccentColor(EnemyVariant tankType)
        {
            return tankType switch
            {
                EnemyVariant.Artillery => new Color(1f, 0.78f, 0.4f, 1f),
                EnemyVariant.Raider => new Color(0.27f, 0.84f, 0.78f, 1f),
                EnemyVariant.Striker => new Color(1f, 0.64f, 0.4f, 1f),
                EnemyVariant.Scout => new Color(0.66f, 1f, 0.94f, 1f),
                EnemyVariant.Bulwark => new Color(0.62f, 0.8f, 0.94f, 1f),
                _ => new Color(0.56f, 0.94f, 0.48f, 1f)
            };
        }

        private static EnemyVariant[] GetSelectableTankTypes()
        {
            return new[]
            {
                EnemyVariant.Basic,
                EnemyVariant.Raider,
                EnemyVariant.Bulwark,
                EnemyVariant.Scout,
                EnemyVariant.Striker,
                EnemyVariant.Artillery
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
