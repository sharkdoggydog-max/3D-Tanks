using System.Collections.Generic;
using Tanks.Core;
using UnityEngine;

namespace Tanks.Combat
{
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        private const bool EnableDebugLogging = false;

        [SerializeField] private float radius = 0.25f;

        private float damage = 1f;
        private float lifetime = 4f;
        private float splashRadius;
        private float splashDamageMultiplier;
        private ProjectileStyle projectileStyle = ProjectileStyle.Player;
        private Team owningTeam = Team.Neutral;
        private GameObject owner;
        private Rigidbody projectileRigidbody;
        private SphereCollider projectileCollider;
        private Renderer projectileRenderer;
        private bool hasImpacted;
        private bool isInitialized;
        private float trailTimer;

        public static Projectile Spawn(
            Vector3 position,
            Quaternion rotation,
            Vector3 direction,
            float speed,
            float damageAmount,
            float maxLifetime,
            float projectileRadius,
            float splashDamageRadius,
            float splashMultiplier,
            ProjectileStyle style,
            Team team,
            GameObject ownerObject)
        {
            GameObject projectileObject = RuntimePrimitiveVisuals.CreatePrimitive(
                PrimitiveType.Sphere,
                "Projectile",
                null,
                position,
                Vector3.one,
                CombatVisualPalette.GetProjectileColor(style, team),
                RuntimeMaterialKind.Opaque);
            projectileObject.transform.SetPositionAndRotation(position, rotation);

            Projectile projectile = projectileObject.AddComponent<Projectile>();
            projectile.Initialize(direction, speed, damageAmount, maxLifetime, projectileRadius, splashDamageRadius, splashMultiplier, style, team, ownerObject);
            return projectile;
        }

        private void Awake()
        {
            projectileCollider = GetComponent<SphereCollider>();
            projectileRigidbody = GetComponent<Rigidbody>();
            projectileRenderer = GetComponent<Renderer>();

            projectileCollider.isTrigger = true;
            projectileCollider.radius = 0.5f;

            ApplySize();

            projectileRigidbody.useGravity = false;
            projectileRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            projectileRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            projectileRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        }

        public void Initialize(
            Vector3 direction,
            float speed,
            float damageAmount,
            float maxLifetime,
            float projectileRadius,
            float splashDamageRadius,
            float splashMultiplier,
            ProjectileStyle style,
            Team team,
            GameObject ownerObject)
        {
            radius = Mathf.Max(0.15f, projectileRadius);
            damage = damageAmount;
            lifetime = maxLifetime;
            splashRadius = Mathf.Max(0f, splashDamageRadius);
            splashDamageMultiplier = Mathf.Clamp01(splashMultiplier);
            projectileStyle = style;
            owningTeam = team;
            owner = ownerObject;
            hasImpacted = false;
            isInitialized = true;
            trailTimer = 0f;

            ApplySize();
            ApplyVisuals();
            IgnoreOwnerCollisions();
            projectileRigidbody.linearVelocity = direction.normalized * speed;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (!isInitialized || hasImpacted)
            {
                return;
            }

            trailTimer -= Time.deltaTime;
            if (trailTimer > 0f)
            {
                return;
            }

            SpawnTrailEffect();
            trailTimer = GetTrailInterval();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isInitialized || hasImpacted || other.transform.root.gameObject == owner)
            {
                return;
            }

            if (other.isTrigger && other.GetComponentInParent<Health>() == null)
            {
                return;
            }

            HandleImpact(other);
        }

        private void HandleImpact(Collider other)
        {
            Health targetHealth = other.GetComponentInParent<Health>();
            if (targetHealth != null && targetHealth.Team == owningTeam)
            {
                Log($"Ignored friendly hit on {targetHealth.name}.");
                return;
            }

            hasImpacted = true;
            Vector3 impactPoint = other.ClosestPoint(transform.position);

            if (targetHealth != null)
            {
                Log($"Hit {targetHealth.name} for {damage:0.##} damage.");
                SpawnImpactEffect(impactPoint, targetHealth.Team, true);
                targetHealth.ApplyDamage(damage, owner);
                ApplySplashDamage(impactPoint, targetHealth);
            }
            else
            {
                Log($"Hit world collider {other.name}.");
                SpawnImpactEffect(impactPoint, Team.Neutral, false);
                ApplySplashDamage(impactPoint, null);
            }

            Destroy(gameObject);
        }

        private void IgnoreOwnerCollisions()
        {
            if (owner == null)
            {
                return;
            }

            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            Collider[] projectileColliders = GetComponentsInChildren<Collider>();

            for (int ownerIndex = 0; ownerIndex < ownerColliders.Length; ownerIndex++)
            {
                for (int projectileIndex = 0; projectileIndex < projectileColliders.Length; projectileIndex++)
                {
                    Physics.IgnoreCollision(ownerColliders[ownerIndex], projectileColliders[projectileIndex]);
                }
            }
        }

        private void ApplySize()
        {
            transform.localScale = Vector3.one * (radius * 2f);
        }

        private void ApplyVisuals()
        {
            if (projectileRenderer == null)
            {
                return;
            }

            RuntimePrimitiveVisuals.SetColor(projectileRenderer, CombatVisualPalette.GetProjectileColor(projectileStyle, owningTeam));
        }

        private void SpawnImpactEffect(Vector3 position, Team impactedTeam, bool directHit)
        {
            Color impactColor = CombatVisualPalette.GetImpactColor(projectileStyle, impactedTeam);
            float styleSizeScale = GetImpactSizeScale();
            float styleLifeScale = GetImpactLifeScale();
            float life = (directHit ? 0.14f : 0.1f) * styleLifeScale;
            float size = (directHit ? radius * 3.4f : radius * 2.2f) * styleSizeScale;

            SimpleLifetimeEffect.SpawnSphere(
                position,
                impactColor,
                life,
                Vector3.one * size * 0.45f,
                Vector3.one * size,
                Vector3.up * 0.45f);

            SimpleLifetimeEffect.SpawnCube(
                position + Vector3.up * 0.05f,
                Color.white,
                life * 0.8f,
                Vector3.one * size * 0.22f,
                Vector3.one * size * 0.55f,
                Vector3.up * 0.2f);

            if (projectileStyle == ProjectileStyle.BulwarkEnemy)
            {
                SimpleLifetimeEffect.SpawnCube(
                    position,
                    impactColor,
                    life * 1.1f,
                    Vector3.one * size * 0.32f,
                    new Vector3(size * 1.4f, size * 0.4f, size * 1.4f),
                    Vector3.up * 0.1f);
            }
            else if (projectileStyle == ProjectileStyle.ArtilleryEnemy || splashRadius > radius + 0.15f)
            {
                float splashSize = Mathf.Max(size * 1.2f, splashRadius * 0.9f);
                SimpleLifetimeEffect.SpawnCube(
                    position + Vector3.up * 0.02f,
                    impactColor,
                    life * 1.25f,
                    new Vector3(splashSize * 0.75f, splashSize * 0.08f, splashSize * 0.75f),
                    new Vector3(splashSize * 1.45f, splashSize * 0.04f, splashSize * 1.45f),
                    Vector3.zero);
            }
        }

        private void SpawnTrailEffect()
        {
            Vector3 velocity = projectileRigidbody != null ? projectileRigidbody.linearVelocity : transform.forward * 12f;
            Vector3 direction = velocity.sqrMagnitude > 0.001f ? velocity.normalized : transform.forward;
            Vector3 trailPosition = transform.position - direction * Mathf.Max(radius * 0.4f, 0.08f);
            Color trailColor = CombatVisualPalette.GetProjectileColor(projectileStyle, owningTeam);
            float speed = velocity.magnitude;

            switch (projectileStyle)
            {
                case ProjectileStyle.RaiderEnemy:
                    SimpleLifetimeEffect.SpawnCube(
                        trailPosition,
                        trailColor,
                        0.08f,
                        new Vector3(radius * 0.75f, radius * 0.38f, radius * 1.9f),
                        new Vector3(radius * 0.35f, radius * 0.18f, radius * 0.8f),
                        direction * (-speed * 0.025f));
                    break;

                case ProjectileStyle.BulwarkEnemy:
                    SimpleLifetimeEffect.SpawnSphere(
                        trailPosition,
                        trailColor,
                        0.18f,
                        Vector3.one * (radius * 1.35f),
                        Vector3.one * (radius * 0.6f),
                        direction * (-speed * 0.018f));
                    break;

                case ProjectileStyle.ArtilleryEnemy:
                    SimpleLifetimeEffect.SpawnCube(
                        trailPosition,
                        trailColor,
                        0.16f,
                        new Vector3(radius * 0.9f, radius * 0.65f, radius * 2.1f),
                        new Vector3(radius * 0.45f, radius * 0.3f, radius * 0.9f),
                        direction * (-speed * 0.015f));
                    break;

                case ProjectileStyle.StrikerEnemy:
                    SimpleLifetimeEffect.SpawnCube(
                        trailPosition,
                        trailColor,
                        0.08f,
                        new Vector3(radius * 0.85f, radius * 0.4f, radius * 1.55f),
                        new Vector3(radius * 0.35f, radius * 0.2f, radius * 0.62f),
                        direction * (-speed * 0.026f));
                    break;

                case ProjectileStyle.ScoutEnemy:
                    SimpleLifetimeEffect.SpawnSphere(
                        trailPosition,
                        trailColor,
                        0.07f,
                        Vector3.one * (radius * 0.72f),
                        Vector3.one * (radius * 0.28f),
                        direction * (-speed * 0.03f));
                    break;

                case ProjectileStyle.BasicEnemy:
                    SimpleLifetimeEffect.SpawnSphere(
                        trailPosition,
                        trailColor,
                        0.11f,
                        Vector3.one * (radius * 0.95f),
                        Vector3.one * (radius * 0.45f),
                        direction * (-speed * 0.02f));
                    break;

                default:
                    SimpleLifetimeEffect.SpawnSphere(
                        trailPosition,
                        trailColor,
                        0.09f,
                        Vector3.one * (radius * 0.85f),
                        Vector3.one * (radius * 0.35f),
                        direction * (-speed * 0.02f));
                    break;
            }
        }

        private float GetTrailInterval()
        {
            return projectileStyle switch
            {
                ProjectileStyle.RaiderEnemy => 0.028f,
                ProjectileStyle.BulwarkEnemy => 0.05f,
                ProjectileStyle.ArtilleryEnemy => 0.06f,
                ProjectileStyle.StrikerEnemy => 0.024f,
                ProjectileStyle.ScoutEnemy => 0.022f,
                ProjectileStyle.BasicEnemy => 0.04f,
                _ => 0.035f
            };
        }

        private float GetImpactSizeScale()
        {
            return projectileStyle switch
            {
                ProjectileStyle.RaiderEnemy => 0.88f,
                ProjectileStyle.BulwarkEnemy => 1.45f,
                ProjectileStyle.ArtilleryEnemy => 1.75f,
                ProjectileStyle.StrikerEnemy => 1.08f,
                ProjectileStyle.ScoutEnemy => 0.82f,
                ProjectileStyle.BasicEnemy => 1.05f,
                _ => 1f
            };
        }

        private float GetImpactLifeScale()
        {
            return projectileStyle switch
            {
                ProjectileStyle.RaiderEnemy => 0.85f,
                ProjectileStyle.BulwarkEnemy => 1.25f,
                ProjectileStyle.ArtilleryEnemy => 1.45f,
                ProjectileStyle.StrikerEnemy => 0.95f,
                ProjectileStyle.ScoutEnemy => 0.8f,
                ProjectileStyle.BasicEnemy => 1.05f,
                _ => 1f
            };
        }

        private void ApplySplashDamage(Vector3 impactPoint, Health primaryTarget)
        {
            if (splashRadius <= radius + 0.05f || splashDamageMultiplier <= 0.01f)
            {
                return;
            }

            Collider[] hits = Physics.OverlapSphere(impactPoint, splashRadius);
            if (hits == null || hits.Length == 0)
            {
                return;
            }

            HashSet<Health> damagedTargets = new HashSet<Health>();

            for (int index = 0; index < hits.Length; index++)
            {
                Health nearbyHealth = hits[index].GetComponentInParent<Health>();
                if (nearbyHealth == null ||
                    nearbyHealth == primaryTarget ||
                    nearbyHealth.Team == owningTeam ||
                    !damagedTargets.Add(nearbyHealth))
                {
                    continue;
                }

                float distance = Vector3.Distance(impactPoint, hits[index].ClosestPoint(impactPoint));
                float normalizedDistance = Mathf.Clamp01(distance / splashRadius);
                float falloff = Mathf.Lerp(1f, 0.35f, normalizedDistance);
                float splashDamage = damage * splashDamageMultiplier * falloff;

                if (splashDamage <= 0.05f)
                {
                    continue;
                }

                nearbyHealth.ApplyDamage(splashDamage, owner);
            }
        }

        private static void Log(string message)
        {
            if (!EnableDebugLogging)
            {
                return;
            }

            Debug.Log($"[Projectile] {message}");
        }
    }
}
