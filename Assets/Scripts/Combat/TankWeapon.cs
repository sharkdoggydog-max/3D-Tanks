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
        [SerializeField] private ProjectileStyle projectileStyle = ProjectileStyle.Player;

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

        public void ConfigureProjectileStyle(ProjectileStyle style)
        {
            projectileStyle = style;
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
                projectileStyle,
                ownerTeam,
                gameObject);

            SpawnMuzzleFlash(firePoint.position, fireDirection, ownerTeam);
            nextFireTime = Time.time + cooldown;
            return true;
        }

        private void SpawnMuzzleFlash(Vector3 position, Vector3 direction, Team team)
        {
            float flashScale = GetMuzzleFlashScale();
            float flashLife = GetMuzzleFlashLifetime();
            Color flashColor = CombatVisualPalette.GetMuzzleFlashColor(projectileStyle, team);

            SimpleLifetimeEffect.SpawnSphere(
                position + direction * 0.15f,
                flashColor,
                flashLife,
                Vector3.one * (projectileRadius * 1.8f * flashScale),
                Vector3.one * (projectileRadius * 0.4f * flashScale),
                direction * (2.4f + flashScale));

            if (projectileStyle == ProjectileStyle.BulwarkEnemy)
            {
                SimpleLifetimeEffect.SpawnCube(
                    position + direction * 0.1f,
                    flashColor,
                    flashLife * 1.15f,
                    new Vector3(projectileRadius * 2.8f, projectileRadius * 1.4f, projectileRadius * 2f),
                    Vector3.one * (projectileRadius * 0.5f),
                    direction * 1.2f);
            }
        }

        private float GetMuzzleFlashScale()
        {
            return projectileStyle switch
            {
                ProjectileStyle.RaiderEnemy => 0.82f,
                ProjectileStyle.BulwarkEnemy => 1.75f,
                ProjectileStyle.BasicEnemy => 1.1f,
                _ => 1f
            };
        }

        private float GetMuzzleFlashLifetime()
        {
            return projectileStyle switch
            {
                ProjectileStyle.RaiderEnemy => 0.06f,
                ProjectileStyle.BulwarkEnemy => 0.12f,
                ProjectileStyle.BasicEnemy => 0.09f,
                _ => 0.08f
            };
        }
    }
}
