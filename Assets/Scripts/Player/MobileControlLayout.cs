using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks.Player
{
    public static class MobileControlLayout
    {
        public static bool ShouldUseTouchControls()
        {
            return Application.isMobilePlatform && Touchscreen.current != null;
        }

        public static Rect GetMovementZone()
        {
            return new Rect(0f, 0f, Screen.width * 0.44f, Screen.height);
        }

        public static Rect GetAimZone()
        {
            float left = Screen.width * 0.44f;
            return new Rect(left, 0f, Screen.width - left, Screen.height);
        }

        public static Vector2 GetAimZoneCenter()
        {
            Rect aimZone = GetAimZone();
            return aimZone.center;
        }

        public static Vector2 GetNormalizedAimInput(Vector2 screenPosition)
        {
            Rect aimZone = GetAimZone();
            Vector2 zoneCenter = aimZone.center;
            Vector2 halfExtents = new Vector2(aimZone.width * 0.5f, aimZone.height * 0.5f);

            float normalizedX = halfExtents.x > 0.001f ? (screenPosition.x - zoneCenter.x) / halfExtents.x : 0f;
            float normalizedY = halfExtents.y > 0.001f ? (screenPosition.y - zoneCenter.y) / halfExtents.y : 0f;
            return Vector2.ClampMagnitude(new Vector2(normalizedX, normalizedY), 1f);
        }

        public static Rect GetFireButtonRect()
        {
            float size = GetButtonSize();
            float margin = GetOuterMargin();
            return new Rect(
                Screen.width - size - margin - GetHorizontalInset(),
                margin + GetVerticalInset(),
                size,
                size);
        }

        public static Rect GetRestartButtonRect()
        {
            float width = Mathf.Clamp(Screen.width * 0.34f, 200f, 320f);
            float height = 72f;
            return new Rect((Screen.width - width) * 0.5f, GetOuterMargin() + 8f, width, height);
        }

        public static Vector2 GetMoveStickCenter()
        {
            float radius = GetStickRadius();
            float margin = GetOuterMargin();
            return new Vector2(
                margin + radius + GetHorizontalInset(),
                margin + radius + GetVerticalInset());
        }

        public static float GetStickRadius()
        {
            return Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * 0.12f, 52f, 92f);
        }

        public static float GetKnobRadius()
        {
            return GetStickRadius() * 0.46f;
        }

        public static float GetButtonSize()
        {
            return Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * 0.16f, 72f, 122f);
        }

        public static float GetOuterMargin()
        {
            return Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * 0.045f, 18f, 34f);
        }

        public static float GetHorizontalInset()
        {
            return Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * 0.145f, 56f, 86f);
        }

        public static float GetVerticalInset()
        {
            return Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * 0.07f, 24f, 42f);
        }

        public static Vector2 ToGuiPosition(Vector2 screenPosition)
        {
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        }

        public static Rect ToGuiRect(Rect screenRect)
        {
            Vector2 guiPosition = ToGuiPosition(new Vector2(screenRect.xMin, screenRect.yMax));
            return new Rect(guiPosition.x, guiPosition.y, screenRect.width, screenRect.height);
        }
    }
}
