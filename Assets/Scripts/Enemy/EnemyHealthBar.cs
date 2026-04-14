using Tanks.Combat;
using Tanks.Core;
using UnityEngine;

namespace Tanks.Enemy
{
    [RequireComponent(typeof(Health))]
    public class EnemyHealthBar : MonoBehaviour
    {
        private static GUIStyle labelStyle;

        private Health health;
        private Transform barRoot;
        private Transform fillBar;
        private Camera mainCamera;
        private float heightOffset = 2.7f;
        private string labelText;

        public void Configure(string label, Color fillColor, float offset)
        {
            labelText = label;
            heightOffset = offset;

            barRoot = new GameObject("HealthBar").transform;
            barRoot.SetParent(transform, false);
            barRoot.localPosition = new Vector3(0f, heightOffset, 0f);

            Transform background = CreateBarPart("Background", barRoot, new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0.12f, 0.12f), new Color(0.08f, 0.08f, 0.08f, 0.95f));
            fillBar = CreateBarPart("Fill", barRoot, new Vector3(0f, 0f, -0.01f), new Vector3(1.36f, 0.08f, 0.08f), fillColor);
            background.localRotation = Quaternion.identity;
            fillBar.localRotation = Quaternion.identity;
        }

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void LateUpdate()
        {
            if (barRoot == null)
            {
                return;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null)
            {
                barRoot.rotation = mainCamera.transform.rotation;
            }

            if (fillBar != null)
            {
                float ratio = health.MaxHealth > 0f ? health.CurrentHealth / health.MaxHealth : 0f;
                fillBar.localScale = new Vector3(1.36f * Mathf.Clamp01(ratio), 0.08f, 0.08f);
                fillBar.localPosition = new Vector3((-1.36f + fillBar.localScale.x) * 0.5f, 0f, -0.01f);
            }
        }

        private void OnGUI()
        {
            if (mainCamera == null || string.IsNullOrEmpty(labelText))
            {
                return;
            }

            Vector3 screenPoint = mainCamera.WorldToScreenPoint(transform.position + Vector3.up * (heightOffset + 0.32f));
            if (screenPoint.z <= 0f)
            {
                return;
            }

            EnsureLabelStyle();
            Rect labelRect = new Rect(screenPoint.x - 50f, Screen.height - screenPoint.y - 12f, 100f, 22f);
            GUI.Label(labelRect, labelText, labelStyle);
        }

        private static Transform CreateBarPart(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject part = RuntimePrimitiveVisuals.CreatePrimitive(
                PrimitiveType.Cube,
                name,
                parent,
                localPosition,
                localScale,
                color,
                RuntimeMaterialKind.Transparent,
                keepCollider: false);
            return part.transform;
        }

        private static void EnsureLabelStyle()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
    }
}
