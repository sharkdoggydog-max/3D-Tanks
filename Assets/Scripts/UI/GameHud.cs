using Tanks.Combat;
using Tanks.Core;
using Tanks.Level;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Tanks.UI
{
    public class GameHud : MonoBehaviour
    {
        private GUIStyle panelStyle;
        private GUIStyle textStyle;
        private GUIStyle titleStyle;
        private GUIStyle overlayStyle;
        private GUIStyle mobileButtonStyle;
        private Health observedPlayerHealth;
        private LevelManager levelManager;
        private MobileTouchControls mobileControls;
        private float hitFlashTimer;
        private Texture2D solidTexture;
        private bool stylesBuiltForMobile;
        private int styleShortSideBucket;
        private int restartTouchId = -1;

        private void Awake()
        {
            mobileControls = GetComponent<MobileTouchControls>();
            if (mobileControls == null)
            {
                mobileControls = gameObject.AddComponent<MobileTouchControls>();
            }
        }

        private void Update()
        {
            if (levelManager == null)
            {
                levelManager = FindAnyObjectByType<LevelManager>();
            }

            if (mobileControls == null)
            {
                mobileControls = GetComponent<MobileTouchControls>();
            }

            SyncPlayerSubscription();
            hitFlashTimer = Mathf.Max(0f, hitFlashTimer - Time.deltaTime * 2.4f);
            HandleMobileRestartInput();
        }

        private void OnGUI()
        {
            bool isMobile = IsMobileLayout();
            EnsureStyles(isMobile);

            Rect panelRect = GetHudPanelRect(isMobile);
            GUILayout.BeginArea(panelRect, panelStyle);

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
            GUILayout.Space(isMobile ? 4f : 6f);

            if (levelManager != null)
            {
                GUILayout.Label($"Objective: {levelManager.ObjectiveText}", textStyle);
                GUILayout.Label(levelManager.ObjectiveProgressText, textStyle);
                GUILayout.Label(levelManager.ExitStatusText, textStyle);
            }

            GUILayout.Space(isMobile ? 8f : 10f);
            if (isMobile)
            {
                GUILayout.Label("Touch: Left stick move | Right drag aim | FIRE shoot", textStyle);
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
                if (isMobile)
                {
                    GUILayout.Label("Tap Restart to try again", textStyle);
                }
                else
                {
                    GUILayout.Label("Press R to restart", textStyle);
                }
            }

            GUILayout.EndArea();

            if (!string.IsNullOrEmpty(GameManager.Instance.ActiveMessage))
            {
                Rect messageRect = GetMessageRect(isMobile);
                GUI.Label(messageRect, GameManager.Instance.ActiveMessage, overlayStyle);
            }

            DrawPlayerScreenFeedback();
            DrawCrosshair(isMobile);

            if (GameManager.Instance.CurrentState == GameState.Defeat && isMobile)
            {
                DrawMobileRestartButton();
            }
        }

        private void EnsureStyles(bool isMobile)
        {
            int shortSideBucket = Mathf.RoundToInt(Mathf.Min(Screen.width, Screen.height) / 24f);
            if (panelStyle != null && stylesBuiltForMobile == isMobile && styleShortSideBucket == shortSideBucket)
            {
                return;
            }

            stylesBuiltForMobile = isMobile;
            styleShortSideBucket = shortSideBucket;

            int titleSize = isMobile ? Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.027f), 18, 28) : 18;
            int textSize = isMobile ? Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.02f), 13, 21) : 13;
            int overlaySize = isMobile ? Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.032f), 22, 34) : 22;
            int buttonSize = isMobile ? Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.024f), 16, 24) : 16;

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 12, 12)
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = titleSize,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = textSize,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            overlayStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = overlaySize,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            mobileButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = buttonSize,
                fontStyle = FontStyle.Bold
            };

            solidTexture = Texture2D.whiteTexture;
        }

        private Rect GetHudPanelRect(bool isMobile)
        {
            if (!isMobile)
            {
                return new Rect(16f, 16f, 360f, 260f);
            }

            float width = Mathf.Clamp(Screen.width * 0.56f, 320f, 520f);
            float height = Mathf.Clamp(Screen.height * 0.34f, 220f, 340f);
            float margin = Mathf.Clamp(Screen.width * 0.025f, 12f, 24f);
            return new Rect(margin, margin, width, height);
        }

        private Rect GetMessageRect(bool isMobile)
        {
            if (!isMobile)
            {
                return new Rect((Screen.width - 520f) * 0.5f, 24f, 520f, 90f);
            }

            float width = Mathf.Clamp(Screen.width * 0.8f, 360f, 760f);
            float height = Mathf.Clamp(Screen.height * 0.12f, 90f, 140f);
            return new Rect((Screen.width - width) * 0.5f, Screen.height * 0.035f, width, height);
        }

        private bool IsMobileLayout()
        {
            return mobileControls != null && mobileControls.UseMobileControls;
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

        private void DrawCrosshair(bool isMobile)
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing || solidTexture == null)
            {
                return;
            }

            Vector2 screenPosition;
            float halfSize;

            if (isMobile)
            {
                if (mobileControls == null || !mobileControls.HasAimScreenPosition)
                {
                    return;
                }

                screenPosition = mobileControls.AimScreenPosition;
                halfSize = 14f;
            }
            else
            {
                Mouse mouse = Mouse.current;
                if (mouse == null)
                {
                    return;
                }

                screenPosition = mouse.position.ReadValue();
                halfSize = 10f;
            }

            float screenY = Screen.height - screenPosition.y;
            DrawCrosshairRect(new Rect(screenPosition.x - halfSize, screenY - 1f, halfSize * 2f, 2f));
            DrawCrosshairRect(new Rect(screenPosition.x - 1f, screenY - halfSize, 2f, halfSize * 2f));
            DrawCrosshairRect(new Rect(screenPosition.x - 2f, screenY - 2f, 4f, 4f));
        }

        private void DrawCrosshairRect(Rect rect)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 0.96f, 0.62f, 0.95f);
            GUI.DrawTexture(rect, solidTexture);
            GUI.color = previousColor;
        }

        private void DrawMobileRestartButton()
        {
            Rect buttonRect = GetMobileRestartButtonRect();
            if (GUI.Button(buttonRect, "Restart", mobileButtonStyle))
            {
                RestartGameplay();
            }
        }

        private void HandleMobileRestartInput()
        {
            if (!IsMobileLayout() ||
                GameManager.Instance == null ||
                GameManager.Instance.CurrentState != GameState.Defeat ||
                levelManager == null ||
                Touchscreen.current == null)
            {
                restartTouchId = -1;
                return;
            }

            Rect buttonRect = GetMobileRestartButtonRect();

            for (int index = 0; index < Touchscreen.current.touches.Count; index++)
            {
                var touch = Touchscreen.current.touches[index];
                int touchId = touch.touchId.ReadValue();
                UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
                Vector2 guiPosition = ScreenToGuiPosition(touch.position.ReadValue());

                if (phase == UnityEngine.InputSystem.TouchPhase.Began && buttonRect.Contains(guiPosition))
                {
                    restartTouchId = touchId;
                    continue;
                }

                if (touchId != restartTouchId)
                {
                    continue;
                }

                if (phase == UnityEngine.InputSystem.TouchPhase.Ended)
                {
                    restartTouchId = -1;
                    if (buttonRect.Contains(guiPosition))
                    {
                        RestartGameplay();
                    }
                }
                else if (phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    restartTouchId = -1;
                }
            }
        }

        private Rect GetMobileRestartButtonRect()
        {
            float width = Mathf.Clamp(Screen.width * 0.28f, 170f, 260f);
            float height = Mathf.Clamp(Screen.height * 0.09f, 56f, 82f);
            return new Rect((Screen.width - width) * 0.5f, Screen.height - height - 24f, width, height);
        }

        private static Vector2 ScreenToGuiPosition(Vector2 screenPosition)
        {
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        }

        private void RestartGameplay()
        {
            if (levelManager != null)
            {
                levelManager.RestartRun();
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnDisable()
        {
            if (observedPlayerHealth != null)
            {
                observedPlayerHealth.Damaged -= OnPlayerDamaged;
            }
        }
    }

    public class MobileTouchControls : MonoBehaviour
    {
        [SerializeField] private bool enableInEditorSimulation;
        [SerializeField] private float joystickRadius = 92f;
        [SerializeField] private float fireButtonRadius = 78f;
        [SerializeField] private float sideMargin = 34f;
        [SerializeField] private float bottomMargin = 32f;

        private Texture2D circleTexture;
        private GUIStyle fireButtonLabelStyle;
        private int movementTouchId = -1;
        private int aimTouchId = -1;
        private int fireTouchId = -1;
        private Vector2 movementOrigin;
        private Vector2 movementPosition;
        private Vector2 movementVector;
        private Vector2 aimScreenPosition;
        private bool hasAimScreenPosition;
        private bool fireHeld;

        public bool UseMobileControls => Touchscreen.current != null && (Application.isMobilePlatform || enableInEditorSimulation);
        public Vector2 MovementVector => movementVector;
        public bool FireHeld => fireHeld;
        public bool HasAimScreenPosition => hasAimScreenPosition;
        public Vector2 AimScreenPosition => aimScreenPosition;

        private void Update()
        {
            if (!UseMobileControls)
            {
                ResetState();
                return;
            }

            ProcessTouches();
        }

        private void OnGUI()
        {
            if (!UseMobileControls)
            {
                return;
            }

            EnsureVisuals();

            Color previousColor = GUI.color;

            Vector2 joystickCenter = movementTouchId >= 0 ? movementOrigin : GetDefaultJoystickCenter();
            Vector2 stickOffset = movementTouchId >= 0 ? Vector2.ClampMagnitude(movementPosition - movementOrigin, joystickRadius) : Vector2.zero;
            DrawCircle(joystickCenter, joystickRadius * 2f, new Color(0.08f, 0.1f, 0.12f, 0.32f));
            DrawCircle(joystickCenter, joystickRadius * 1.25f, new Color(1f, 1f, 1f, 0.08f));
            DrawCircle(joystickCenter + stickOffset, joystickRadius * 0.82f, new Color(0.96f, 0.96f, 0.96f, 0.32f));

            Vector2 fireCenter = GetFireButtonCenter();
            DrawCircle(fireCenter, fireButtonRadius * 2f, fireHeld ? new Color(0.95f, 0.42f, 0.28f, 0.64f) : new Color(0.25f, 0.1f, 0.08f, 0.46f));
            DrawCircle(fireCenter, fireButtonRadius * 1.4f, fireHeld ? new Color(1f, 0.85f, 0.64f, 0.7f) : new Color(1f, 0.92f, 0.75f, 0.18f));

            Rect fireLabelRect = new Rect(
                fireCenter.x - fireButtonRadius,
                Screen.height - fireCenter.y - fireButtonRadius * 0.6f,
                fireButtonRadius * 2f,
                fireButtonRadius * 1.2f);
            GUI.Label(fireLabelRect, "FIRE", fireButtonLabelStyle);

            if (hasAimScreenPosition)
            {
                DrawCircle(aimScreenPosition, 32f, new Color(1f, 0.95f, 0.62f, 0.2f));
                DrawCircle(aimScreenPosition, 12f, new Color(1f, 0.95f, 0.62f, 0.38f));
            }

            GUI.color = previousColor;
        }

        public bool TryGetAimPoint(Transform ownerTransform, out Vector3 aimPoint)
        {
            if (!UseMobileControls || !hasAimScreenPosition)
            {
                aimPoint = default;
                return false;
            }

            Camera gameplayCamera = Camera.main;
            if (gameplayCamera == null || ownerTransform == null)
            {
                aimPoint = default;
                return false;
            }

            Ray aimRay = gameplayCamera.ScreenPointToRay(aimScreenPosition);
            Plane aimPlane = new(Vector3.up, ownerTransform.position + Vector3.up * 0.8f);
            if (aimPlane.Raycast(aimRay, out float enterDistance))
            {
                aimPoint = aimRay.GetPoint(enterDistance);
                return true;
            }

            aimPoint = default;
            return false;
        }

        private void ProcessTouches()
        {
            bool movementSeen = false;
            bool aimSeen = false;
            bool fireSeen = false;
            fireHeld = false;

            for (int index = 0; index < Touchscreen.current.touches.Count; index++)
            {
                var touch = Touchscreen.current.touches[index];
                if (!touch.press.isPressed)
                {
                    continue;
                }

                int touchId = touch.touchId.ReadValue();
                Vector2 position = touch.position.ReadValue();

                if (touchId == movementTouchId)
                {
                    movementSeen = true;
                    movementPosition = position;
                    movementVector = Vector2.ClampMagnitude((movementPosition - movementOrigin) / joystickRadius, 1f);
                    continue;
                }

                if (touchId == aimTouchId)
                {
                    aimSeen = true;
                    aimScreenPosition = position;
                    hasAimScreenPosition = true;
                    continue;
                }

                if (touchId == fireTouchId)
                {
                    fireSeen = true;
                    fireHeld = true;
                    continue;
                }

                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    AssignTouch(touchId, position);

                    if (touchId == movementTouchId)
                    {
                        movementSeen = true;
                        movementPosition = position;
                        movementVector = Vector2.zero;
                    }
                    else if (touchId == aimTouchId)
                    {
                        aimSeen = true;
                        aimScreenPosition = position;
                        hasAimScreenPosition = true;
                    }
                    else if (touchId == fireTouchId)
                    {
                        fireSeen = true;
                        fireHeld = true;
                    }
                }
            }

            if (!movementSeen)
            {
                movementTouchId = -1;
                movementVector = Vector2.zero;
            }

            if (!aimSeen)
            {
                aimTouchId = -1;
                hasAimScreenPosition = false;
            }

            if (!fireSeen)
            {
                fireTouchId = -1;
                fireHeld = false;
            }
        }

        private void AssignTouch(int touchId, Vector2 position)
        {
            if (fireTouchId < 0 && IsInsideCircle(position, GetFireButtonCenter(), fireButtonRadius * 1.15f))
            {
                fireTouchId = touchId;
                return;
            }

            if (movementTouchId < 0 && IsMovementZone(position))
            {
                movementTouchId = touchId;
                movementOrigin = ClampJoystickCenter(position);
                movementPosition = movementOrigin;
                return;
            }

            if (aimTouchId < 0 && IsAimZone(position))
            {
                aimTouchId = touchId;
                aimScreenPosition = position;
                hasAimScreenPosition = true;
            }
        }

        private bool IsMovementZone(Vector2 screenPosition)
        {
            return screenPosition.x <= Screen.width * 0.45f && screenPosition.y <= Screen.height * 0.82f;
        }

        private bool IsAimZone(Vector2 screenPosition)
        {
            return screenPosition.x >= Screen.width * 0.35f && !IsInsideCircle(screenPosition, GetFireButtonCenter(), fireButtonRadius * 1.35f);
        }

        private Vector2 ClampJoystickCenter(Vector2 requestedPosition)
        {
            float minX = sideMargin + joystickRadius;
            float maxX = Screen.width * 0.4f;
            float minY = bottomMargin + joystickRadius;
            float maxY = Screen.height * 0.65f;

            return new Vector2(
                Mathf.Clamp(requestedPosition.x, minX, maxX),
                Mathf.Clamp(requestedPosition.y, minY, maxY));
        }

        private Vector2 GetDefaultJoystickCenter()
        {
            return new Vector2(sideMargin + joystickRadius, bottomMargin + joystickRadius);
        }

        private Vector2 GetFireButtonCenter()
        {
            return new Vector2(Screen.width - sideMargin - fireButtonRadius, bottomMargin + fireButtonRadius);
        }

        private static bool IsInsideCircle(Vector2 position, Vector2 center, float radius)
        {
            return (position - center).sqrMagnitude <= radius * radius;
        }

        private void EnsureVisuals()
        {
            if (circleTexture == null)
            {
                circleTexture = BuildCircleTexture(96);
            }

            if (fireButtonLabelStyle == null)
            {
                fireButtonLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.022f), 14, 24),
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }
        }

        private void DrawCircle(Vector2 screenPosition, float size, Color color)
        {
            if (circleTexture == null)
            {
                return;
            }

            Rect rect = new Rect(
                screenPosition.x - size * 0.5f,
                Screen.height - screenPosition.y - size * 0.5f,
                size,
                size);

            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, circleTexture);
            GUI.color = previousColor;
        }

        private static Texture2D BuildCircleTexture(int size)
        {
            Texture2D texture = new(size, size, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;
            float softEdge = size * 0.08f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - Mathf.InverseLerp(radius - softEdge, radius, distance));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private void ResetState()
        {
            movementTouchId = -1;
            aimTouchId = -1;
            fireTouchId = -1;
            movementVector = Vector2.zero;
            hasAimScreenPosition = false;
            fireHeld = false;
        }
    }
}
