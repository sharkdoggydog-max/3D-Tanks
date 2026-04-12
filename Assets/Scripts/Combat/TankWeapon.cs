using UnityEngine;

namespace Tanks.Combat
{
    public class TankWeapon : MonoBehaviour
    {
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private float cooldown = 0.4f;
        [SerializeField] private float projectileSpeed = 18f;
        [SerializeField] private float projectileDamage = 1f;
        [SerializeField] private float projectileLifetime = 4f;
        [SerializeField] private float projectileRadius = 0.28f;
        [SerializeField] private float projectileSpawnOffset = 0.45f;

        private float nextFireTime;
        private Health ownerHealth;

        private void Awake()
        {
            ownerHealth = GetComponent<Health>();
        }

        public void Configure(Transform muzzle, float cooldownSeconds, float speed, float damage, float lifetime, float radius = 0.28f)
        {
            muzzlePoint = muzzle;
            cooldown = cooldownSeconds;
            projectileSpeed = speed;
            projectileDamage = damage;
            projectileLifetime = lifetime;
            projectileRadius = radius;
        }

        public void Configure(float cooldownSeconds, float speed, float damage, float lifetime, float radius)
        {
            cooldown = cooldownSeconds;
            projectileSpeed = speed;
            projectileDamage = damage;
            projectileLifetime = lifetime;
            projectileRadius = radius;
        }

        public bool TryFire()
        {
            if (ownerHealth == null)
            {
                ownerHealth = GetComponent<Health>();
            }

            if (Time.time < nextFireTime)
            {
                return false;
            }

            if (ownerHealth != null && !ownerHealth.IsAlive)
            {
                return false;
            }

            Transform firePoint = muzzlePoint != null ? muzzlePoint : transform;
            Team ownerTeam = ownerHealth != null ? ownerHealth.Team : Team.Neutral;
            Vector3 fireDirection = Vector3.ProjectOnPlane(firePoint.forward, Vector3.up).normalized;
            if (fireDirection.sqrMagnitude < 0.001f)
            {
                fireDirection = transform.forward;
            }

            Vector3 spawnPosition = firePoint.position + fireDirection * projectileSpawnOffset;

            Projectile.Spawn(
                spawnPosition,
                Quaternion.LookRotation(fireDirection, Vector3.up),
                fireDirection,
                projectileSpeed,
                projectileDamage,
                projectileLifetime,
                projectileRadius,
                ownerTeam,
                gameObject);

            SpawnMuzzleFlash(firePoint.position, fireDirection, ownerTeam);
            nextFireTime = Time.time + cooldown;
            return true;
        }

        private void SpawnMuzzleFlash(Vector3 position, Vector3 direction, Team team)
        {
            SimpleLifetimeEffect.SpawnSphere(
                position + direction * 0.15f,
                CombatVisualPalette.GetMuzzleFlashColor(team),
                0.08f,
                Vector3.one * (projectileRadius * 1.8f),
                Vector3.one * (projectileRadius * 0.4f),
                direction * 3f);
        }
    }
}
