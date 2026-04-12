using System;
using UnityEngine;

namespace Tanks.Combat
{
    public class Health : MonoBehaviour
    {
        private const bool EnableDebugLogging = false;

        [SerializeField] private float maxHealth = 5f;
        [SerializeField] private Team team = Team.Neutral;
        [SerializeField] private bool destroyOnDeath = true;

        private float currentHealth;

        public event Action<Health> Died;
        public event Action<Health> Damaged;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public Team Team => team;
        public bool IsAlive => currentHealth > 0f;

        private void Awake()
        {
            ResetHealth();
        }

        public void Configure(float maxHealthValue, Team assignedTeam, bool shouldDestroyOnDeath)
        {
            maxHealth = Mathf.Max(1f, maxHealthValue);
            team = assignedTeam;
            destroyOnDeath = shouldDestroyOnDeath;
            ResetHealth();
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
        }

        public void ApplyDamage(float damage, GameObject instigator = null)
        {
            if (!IsAlive || damage <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            Log($"{name} took {damage:0.##} damage from {(instigator != null ? instigator.name : "unknown")} and is now at {currentHealth:0.##}/{maxHealth:0.##}.");
            Damaged?.Invoke(this);

            if (currentHealth > 0f)
            {
                return;
            }

            Log($"{name} died.");
            Died?.Invoke(this);

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }

        private void Log(string message)
        {
            if (!EnableDebugLogging)
            {
                return;
            }

            Debug.Log($"[Health] {message}");
        }
    }
}
