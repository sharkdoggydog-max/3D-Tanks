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
        private Team owningTeam = Team.Neutral;
        private GameObject owner;
        private Rigidbody projectileRigidbody;
        private SphereCollider projectileCollider;
        private Renderer projectileRenderer;
        private bool hasImpacted;
        private bool isInitialized;

        public static Projectile Spawn(
            Vector3 position,
            Quaternion rotation,
            Vector3 direction,
            float speed,
            float damageAmount,
            float maxLifetime,
            float projectileRadius,
            Team team,
            GameObject ownerObject)
        {
            GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "Projectile";
            projectileObject.transform.SetPositionAndRotation(position, rotation);

            Projectile projectile = projectileObject.AddComponent<Projectile>();
            projectile.Initialize(direction, speed, damageAmount, maxLifetime, projectileRadius, team, ownerObject);
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
            Team team,
            GameObject ownerObject)
        {
            radius = Mathf.Max(0.15f, projectileRadius);
            damage = damageAmount;
            lifetime = maxLifetime;
            owningTeam = team;
            owner = ownerObject;
            hasImpacted = false;
            isInitialized = true;

            ApplySize();
            ApplyVisuals();
            IgnoreOwnerCollisions();
            projectileRigidbody.linearVelocity = direction.normalized * speed;
            Destroy(gameObject, lifetime);
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

            if (targetHealth != null)
            {
                Log($"Hit {targetHealth.name} for {damage:0.##} damage.");
                targetHealth.ApplyDamage(damage, owner);
            }
            else
            {
                Log($"Hit world collider {other.name}.");
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

            projectileRenderer.material.color = CombatVisualPalette.GetProjectileColor(owningTeam);
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
