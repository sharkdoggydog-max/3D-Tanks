using UnityEngine;

namespace Tanks.Combat
{
    public class SimpleLifetimeEffect : MonoBehaviour
    {
        private Renderer effectRenderer;
        private Color baseColor;
        private Vector3 startScale = Vector3.one;
        private Vector3 endScale = Vector3.one;
        private Vector3 velocity;
        private float duration = 0.2f;
        private float elapsed;

        public static void SpawnSphere(
            Vector3 position,
            Color color,
            float life,
            Vector3 start,
            Vector3 end,
            Vector3 drift)
        {
            SpawnPrimitive(PrimitiveType.Sphere, position, color, life, start, end, drift);
        }

        public static void SpawnCube(
            Vector3 position,
            Color color,
            float life,
            Vector3 start,
            Vector3 end,
            Vector3 drift)
        {
            SpawnPrimitive(PrimitiveType.Cube, position, color, life, start, end, drift);
        }

        public void Configure(Color color, float life, Vector3 start, Vector3 end, Vector3 drift)
        {
            effectRenderer = GetComponent<Renderer>();
            baseColor = color;
            duration = Mathf.Max(0.01f, life);
            startScale = start;
            endScale = end;
            velocity = drift;

            transform.localScale = startScale;
            CombatVisualPalette.ApplyRuntimeMaterial(effectRenderer, color, transparent: true);
            ApplyColor(color, 0.9f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            transform.position += velocity * Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            ApplyColor(baseColor, Mathf.Lerp(0.9f, 0f, t));

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private static void SpawnPrimitive(
            PrimitiveType primitiveType,
            Vector3 position,
            Color color,
            float life,
            Vector3 start,
            Vector3 end,
            Vector3 drift)
        {
            GameObject effectObject = GameObject.CreatePrimitive(primitiveType);
            effectObject.name = $"{primitiveType}Effect";
            effectObject.transform.position = position;

            Collider collider = effectObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }

            SimpleLifetimeEffect effect = effectObject.AddComponent<SimpleLifetimeEffect>();
            effect.Configure(color, life, start, end, drift);
        }

        private void ApplyColor(Color color, float alpha)
        {
            if (effectRenderer == null)
            {
                return;
            }

            Color tintedColor = color;
            tintedColor.a = alpha;
            CombatVisualPalette.SetRuntimeMaterialColor(effectRenderer, tintedColor);
        }
    }
}
