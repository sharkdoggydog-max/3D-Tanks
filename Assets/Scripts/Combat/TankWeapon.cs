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
        [SerializeField] private float splashRadius;
        [SerializeField] private float splashDamageMultiplier = 0.65f;
        [SerializeField] private int burstCount = 1;
        [SerializeField] private float burstInterval = 0.08f;
        [SerializeField] private float projectileSpawnOffset = 0.45f;
        [SerializeField] private ProjectileStyle projectileStyle = ProjectileStyle.Player;

        private float nextFireTime;
        private int pendingBurstShots;
        private float nextBurstShotTime;
        private Health ownerHealth;

        private void Awake()
        {
            ownerHealth = GetComponent<Health>();
        }

        private void Update()
        {
            if (pendingBurstShots <= 0 || Time.time < nextBurstShotTime)
            {
                return;
            }

            if (!CanFire())
            {
                pendingBurstShots = 0;
                return;
            }

            FireSingleShot();
            pendingBurstShots--;
            nextBurstShotTime = Time.time + burstInterval;
        }

        public void Configure(
            Transform muzzle,
            float cooldownSeconds,
            float speed,
            float damage,
            float lifetime,
            float radius = 0.28f,
            float splashRadiusValue = 0f,
            float splashDamageMultiplierValue = 0.65f,
            int burstShotCount = 1,
            float burstShotInterval = 0.08f)
        {
            muzzlePoint = muzzle;
            cooldown = cooldownSeconds;
            projectileSpeed = speed;
            projectileDamage = damage;
            projectileLifetime = lifetime;
            projectileRadius = radius;
            splashRadius = splashRadiusValue;
            splashDamageMultiplier = splashDamageMultiplierValue;
            burstCount = Mathf.Max(1, burstShotCount);
            burstInterval = Mathf.Max(0.03f, burstShotInterval);
        }

        public void Configure(
            float cooldownSeconds,
            float speed,
            float damage,
            float lifetime,
            float radius,
            float splashRadiusValue = 0f,
            float splashDamageMultiplierValue = 0.65f,
            int burstShotCount = 1,
            float burstShotInterval = 0.08f)
        {
            cooldown = cooldownSeconds;
            projectileSpeed = speed;
            projectileDamage = damage;
            projectileLifetime = lifetime;
            projectileRadius = radius;
            splashRadius = splashRadiusValue;
            splashDamageMultiplier = splashDamageMultiplierValue;
            burstCount = Mathf.Max(1, burstShotCount);
            burstInterval = Mathf.Max(0.03f, burstShotInterval);
        }

        public void ConfigureProjectileStyle(ProjectileStyle style)
        {
            projectileStyle = style;
        }

        public bool TryFire()
        {
            if (pendingBurstShots > 0 || Time.time < nextFireTime || !CanFire())
            {
                return false;
            }

            FireSingleShot();
            pendingBurstShots = burstCount - 1;
            nextBurstShotTime = Time.time + burstInterval;
            nextFireTime = Time.time + cooldown + Mathf.Max(0f, burstInterval * (burstCount - 1));
            return true;
        }

        private bool CanFire()
        {
            if (ownerHealth == null)
            {
                ownerHealth = GetComponent<Health>();
            }

            return ownerHealth == null || ownerHealth.IsAlive;
        }

        private void FireSingleShot()
        {
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
                splashRadius,
                splashDamageMultiplier,
                projectileStyle,
                ownerTeam,
                gameObject);

            SpawnMuzzleFlash(firePoint.position, fireDirection, ownerTeam);
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
            else if (projectileStyle == ProjectileStyle.ArtilleryEnemy)
            {
                SimpleLifetimeEffect.SpawnSphere(
                    position + direction * 0.22f,
                    flashColor,
                    flashLife * 1.2f,
                    Vector3.one * (projectileRadius * 2.4f),
                    Vector3.one * (projectileRadius * 0.7f),
                    direction * 0.9f);
            }
            else if (projectileStyle == ProjectileStyle.StrikerEnemy)
            {
                SimpleLifetimeEffect.SpawnCube(
                    position + direction * 0.12f,
                    flashColor,
                    flashLife * 0.9f,
                    new Vector3(projectileRadius * 2.1f, projectileRadius * 0.95f, projectileRadius * 1.7f),
                    Vector3.one * (projectileRadius * 0.4f),
                    direction * 1.6f);
            }
        }

        private float GetMuzzleFlashScale()
        {
            return projectileStyle switch
            {
                ProjectileStyle.RaiderEnemy => 0.82f,
                ProjectileStyle.BulwarkEnemy => 1.75f,
                ProjectileStyle.ArtilleryEnemy => 1.95f,
                ProjectileStyle.StrikerEnemy => 1.15f,
                ProjectileStyle.ScoutEnemy => 0.7f,
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
                ProjectileStyle.ArtilleryEnemy => 0.14f,
                ProjectileStyle.StrikerEnemy => 0.07f,
                ProjectileStyle.ScoutEnemy => 0.05f,
                ProjectileStyle.BasicEnemy => 0.09f,
                _ => 0.08f
            };
        }
    }
}
