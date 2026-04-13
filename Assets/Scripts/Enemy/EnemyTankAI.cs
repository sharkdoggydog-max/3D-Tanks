using Tanks.Combat;
using Tanks.Core;
using UnityEngine;

namespace Tanks.Enemy
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(TankWeapon))]
    [RequireComponent(typeof(Health))]
    public class EnemyTankAI : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float turnSpeed = 110f;
        [SerializeField] private float attackRange = 14f;
        [SerializeField] private float holdDistance = 9f;
        [SerializeField] private float retreatDistance = 5.5f;
        [SerializeField] private float obstacleCheckDistance = 4f;
        [SerializeField] private float aimAngle = 10f;
        [SerializeField] private float aimConfirmTime = 0.18f;
        [SerializeField] private float orbitStrength = 0.3f;
        [SerializeField] private float holdMoveThrottle = 0.2f;
        [SerializeField] private float repositionThrottle = 0.65f;
        [SerializeField] private float advanceThrottle = 1f;

        private Rigidbody tankRigidbody;
        private TankWeapon tankWeapon;
        private TankTurretAim turretAim;
        private Health health;
        private Transform target;
        private Vector3 lastPosition;
        private float stuckTimer;
        private float avoidTimer;
        private int avoidTurnDirection = 1;
        private float aimTimer;
        private int orbitDirection = 1;
        private float orbitTimer;

        private void Awake()
        {
            tankRigidbody = GetComponent<Rigidbody>();
            tankWeapon = GetComponent<TankWeapon>();
            turretAim = GetComponent<TankTurretAim>();
            health = GetComponent<Health>();
            lastPosition = transform.position;
            orbitDirection = Random.value < 0.5f ? -1 : 1;
        }

        private void FixedUpdate()
        {
            if (!CanAct() || target == null)
            {
                tankRigidbody.linearVelocity = Vector3.zero;
                tankRigidbody.angularVelocity = Vector3.zero;
                aimTimer = 0f;
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.01f)
            {
                return;
            }

            float targetDistance = toTarget.magnitude;
            Vector3 targetDirection = toTarget / targetDistance;
            bool hasLineOfSight = HasLineOfSight(targetDistance);

            UpdateOrbitDirection(targetDistance, hasLineOfSight);
            MovementCommand movement = BuildMovementCommand(targetDirection, targetDistance, hasLineOfSight);
            float turnAmount = Vector3.SignedAngle(transform.forward, movement.FacingDirection, Vector3.up);
            float clampedTurn = Mathf.Clamp(turnAmount, -turnSpeed * Time.fixedDeltaTime, turnSpeed * Time.fixedDeltaTime);
            Quaternion turnStep = Quaternion.Euler(0f, clampedTurn, 0f);
            tankRigidbody.MoveRotation(tankRigidbody.rotation * turnStep);

            if (Mathf.Abs(turnAmount) < 65f && Mathf.Abs(movement.Throttle) > 0.01f)
            {
                float throttleScale = Mathf.InverseLerp(80f, 0f, Mathf.Abs(turnAmount));
                Vector3 moveStep = transform.forward * (movement.Throttle * moveSpeed * throttleScale * Time.fixedDeltaTime);
                tankRigidbody.MovePosition(tankRigidbody.position + moveStep);
            }

            UpdateStuckState(movement);
        }

        private void Update()
        {
            if (!CanAct() || target == null)
            {
                aimTimer = 0f;
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float distanceToTarget = toTarget.magnitude;
            Vector3 targetPoint = target.position + Vector3.up * 0.9f;

            if (turretAim != null)
            {
                turretAim.AimAtWorldPoint(targetPoint);
            }

            if (distanceToTarget > attackRange || distanceToTarget < retreatDistance * 0.8f)
            {
                aimTimer = 0f;
                return;
            }

            float facingAngle = turretAim != null ? turretAim.GetAngleToWorldPoint(targetPoint) : Vector3.Angle(transform.forward, toTarget.normalized);
            if (facingAngle > aimAngle || !HasLineOfSight(distanceToTarget))
            {
                aimTimer = 0f;
                return;
            }

            aimTimer += Time.deltaTime;
            if (aimTimer >= aimConfirmTime)
            {
                tankWeapon.TryFire();
            }
        }

        public void Configure(Transform targetTransform, EnemyArchetype archetype)
        {
            target = targetTransform;
            moveSpeed = archetype.MoveSpeed;
            turnSpeed = archetype.TurnSpeed;
            attackRange = archetype.AttackRange;
            holdDistance = archetype.HoldDistance;
            retreatDistance = archetype.RetreatDistance;
            aimAngle = archetype.AimTolerance;
            aimConfirmTime = archetype.AimConfirmTime;
            orbitStrength = archetype.OrbitStrength;
            holdMoveThrottle = archetype.HoldMoveThrottle;
            repositionThrottle = archetype.RepositionThrottle;
            advanceThrottle = archetype.AdvanceThrottle;
        }

        public void SetTarget(Transform targetTransform)
        {
            target = targetTransform;
        }

        private MovementCommand BuildMovementCommand(Vector3 targetDirection, float distanceToTarget, bool hasLineOfSight)
        {
            if (avoidTimer > 0f)
            {
                avoidTimer -= Time.fixedDeltaTime;
                Vector3 avoidDirection = Quaternion.Euler(0f, 45f * avoidTurnDirection, 0f) * transform.forward;
                return new MovementCommand(1f, avoidDirection.normalized);
            }

            if (distanceToTarget < retreatDistance)
            {
                return new MovementCommand(-0.65f, targetDirection);
            }

            if (distanceToTarget > holdDistance)
            {
                Vector3 approachDirection = ChooseMovementDirection(GetApproachDirection(targetDirection, hasLineOfSight));
                return new MovementCommand(advanceThrottle, approachDirection);
            }

            if (!hasLineOfSight)
            {
                Vector3 repositionDirection = ChooseMovementDirection(GetApproachDirection(targetDirection, false));
                return new MovementCommand(repositionThrottle, repositionDirection);
            }

            if (holdMoveThrottle > 0.01f)
            {
                return new MovementCommand(holdMoveThrottle, GetOrbitDirection(targetDirection));
            }

            return new MovementCommand(0f, targetDirection);
        }

        private void UpdateOrbitDirection(float distanceToTarget, bool hasLineOfSight)
        {
            if (!hasLineOfSight || distanceToTarget > attackRange || orbitStrength < 0.05f)
            {
                orbitTimer = 0f;
                return;
            }

            orbitTimer += Time.fixedDeltaTime;
            float orbitSwitchInterval = Mathf.Lerp(2.4f, 0.85f, Mathf.Clamp01(orbitStrength));
            if (orbitTimer >= orbitSwitchInterval)
            {
                orbitTimer = 0f;
                orbitDirection *= -1;
            }
        }

        private Vector3 GetApproachDirection(Vector3 targetDirection, bool hasLineOfSight)
        {
            Vector3 orbitOffset = Vector3.Cross(Vector3.up, targetDirection) * orbitDirection;
            float flankWeight = hasLineOfSight ? orbitStrength * 0.55f : orbitStrength * 0.8f;
            Vector3 desiredDirection = targetDirection + orbitOffset * flankWeight;
            return desiredDirection.sqrMagnitude > 0.001f ? desiredDirection.normalized : targetDirection;
        }

        private Vector3 GetOrbitDirection(Vector3 targetDirection)
        {
            Vector3 orbitOffset = Vector3.Cross(Vector3.up, targetDirection) * orbitDirection;
            Vector3 desiredDirection = targetDirection + orbitOffset * orbitStrength;
            return desiredDirection.sqrMagnitude > 0.001f ? desiredDirection.normalized : targetDirection;
        }

        private Vector3 ChooseMovementDirection(Vector3 targetDirection)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 0.75f;
            Vector3[] probeDirections =
            {
                targetDirection,
                (targetDirection + transform.right * 0.8f).normalized,
                (targetDirection - transform.right * 0.8f).normalized,
                (transform.forward + transform.right * 0.9f).normalized,
                (transform.forward - transform.right * 0.9f).normalized
            };

            float bestScore = float.NegativeInfinity;
            Vector3 bestDirection = targetDirection;

            for (int index = 0; index < probeDirections.Length; index++)
            {
                Vector3 probeDirection = probeDirections[index];
                bool blocked = Physics.Raycast(rayOrigin, probeDirection, obstacleCheckDistance);
                float score = Vector3.Dot(probeDirection, targetDirection);

                if (blocked)
                {
                    score -= 1.5f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = probeDirection;
                }
            }

            return bestDirection;
        }

        private bool HasLineOfSight(float distanceToTarget)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 0.8f;
            Vector3 rayTarget = target.position + Vector3.up * 0.8f;
            Vector3 rayDirection = (rayTarget - rayOrigin).normalized;

            if (!Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, distanceToTarget + 0.5f))
            {
                return false;
            }

            return hit.transform.root == target.root;
        }

        private void UpdateStuckState(MovementCommand movement)
        {
            if (Mathf.Abs(movement.Throttle) < 0.1f)
            {
                stuckTimer = 0f;
                lastPosition = transform.position;
                return;
            }

            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer < 0.45f)
            {
                return;
            }

            float movedDistance = Vector3.Distance(transform.position, lastPosition);
            if (movedDistance < 0.2f)
            {
                avoidTimer = 0.7f;
                avoidTurnDirection *= -1;
            }

            stuckTimer = 0f;
            lastPosition = transform.position;
        }

        private bool CanAct()
        {
            return health.IsAlive && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing;
        }

        private readonly struct MovementCommand
        {
            public MovementCommand(float throttle, Vector3 facingDirection)
            {
                Throttle = throttle;
                FacingDirection = facingDirection.sqrMagnitude > 0.001f ? facingDirection.normalized : Vector3.forward;
            }

            public float Throttle { get; }
            public Vector3 FacingDirection { get; }
        }
    }
}
