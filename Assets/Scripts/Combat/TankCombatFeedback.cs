using UnityEngine;

namespace Tanks.Combat
{
    [RequireComponent(typeof(Health))]
    public class TankCombatFeedback : MonoBehaviour
    {
        [SerializeField] private float flashDuration = 0.12f;
        [SerializeField] private float scalePunch = 0.12f;
        [SerializeField] private float scaleRecoverSpeed = 8f;

        private Health health;
        private Renderer[] cachedRenderers;
        private Color[] baseColors;
        private Vector3 baseScale;
        private float flashTimer;
        private float punchAmount;
        private bool deathEffectPlayed;

        private void Awake()
        {
            health = GetComponent<Health>();
            cachedRenderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[cachedRenderers.Length];
            baseScale = transform.localScale;

            for (int index = 0; index < cachedRenderers.Length; index++)
            {
                baseColors[index] = cachedRenderers[index].material.color;
            }
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void Update()
        {
            if (flashTimer > 0f)
            {
                flashTimer = Mathf.Max(0f, flashTimer - Time.deltaTime);
            }

            punchAmount = Mathf.MoveTowards(punchAmount, 0f, scaleRecoverSpeed * Time.deltaTime);
            transform.localScale = baseScale * (1f + punchAmount);

            float flashStrength = flashDuration > 0f ? flashTimer / flashDuration : 0f;
            Color flashColor = CombatVisualPalette.GetDamageFlashColor(health.Team);

            for (int index = 0; index < cachedRenderers.Length; index++)
            {
                cachedRenderers[index].material.color = Color.Lerp(baseColors[index], flashColor, flashStrength);
            }
        }

        private void OnDamaged(Health damagedHealth)
        {
            flashTimer = flashDuration;
            punchAmount = Mathf.Max(punchAmount, scalePunch);

            SimpleLifetimeEffect.SpawnSphere(
                transform.position + Vector3.up * 1.2f,
                CombatVisualPalette.GetDamageFlashColor(damagedHealth.Team),
                0.15f,
                Vector3.one * 0.3f,
                Vector3.one * 0.7f,
                Vector3.up * 1f);
        }

        private void OnDied(Health deadHealth)
        {
            if (deathEffectPlayed)
            {
                return;
            }

            deathEffectPlayed = true;
            Color deathColor = CombatVisualPalette.GetDeathColor(deadHealth.Team);

            SimpleLifetimeEffect.SpawnSphere(
                transform.position + Vector3.up * 1f,
                deathColor,
                0.35f,
                Vector3.one * 0.8f,
                Vector3.one * 2.6f,
                Vector3.up * 1.8f);

            SimpleLifetimeEffect.SpawnCube(
                transform.position + Vector3.up * 0.9f,
                Color.white,
                0.18f,
                Vector3.one * 0.5f,
                Vector3.one * 1.5f,
                Vector3.up * 0.5f);
        }
    }
}
