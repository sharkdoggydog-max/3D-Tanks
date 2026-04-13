using Tanks.Combat;
using Tanks.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(TankWeapon))]
    [RequireComponent(typeof(Health))]
    public class PlayerTankController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float turnSpeed = 120f;

        private Rigidbody tankRigidbody;
        private TankWeapon tankWeapon;
        private TankTurretAim turretAim;
        private Health health;
        private float moveInput;
        private float turnInput;
        private bool fireHeld;

        public bool HasAimPoint { get; private set; }
        public Vector3 CurrentAimPoint { get; private set; }

        private void Awake()
        {
            tankRigidbody = GetComponent<Rigidbody>();
            tankWeapon = GetComponent<TankWeapon>();
            turretAim = GetComponent<TankTurretAim>();
            health = GetComponent<Health>();
        }

        private void Update()
        {
            if (!CanControl())
            {
                moveInput = 0f;
                turnInput = 0f;
                fireHeld = false;
                HasAimPoint = false;
                return;
            }

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            moveInput = 0f;
            turnInput = 0f;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    moveInput += 1f;
                }

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    moveInput -= 1f;
                }

                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    turnInput -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    turnInput += 1f;
                }
            }

            fireHeld = (keyboard != null && keyboard.spaceKey.isPressed) ||
                       (mouse != null && mouse.leftButton.isPressed);

            UpdateTurretAim(mouse);
        }

        private void FixedUpdate()
        {
            if (!CanControl())
            {
                tankRigidbody.linearVelocity = Vector3.zero;
                tankRigidbody.angularVelocity = Vector3.zero;
                return;
            }

            Vector3 moveStep = transform.forward * (moveInput * moveSpeed * Time.fixedDeltaTime);
            Quaternion turnStep = Quaternion.Euler(0f, turnInput * turnSpeed * Time.fixedDeltaTime, 0f);

            tankRigidbody.MovePosition(tankRigidbody.position + moveStep);
            tankRigidbody.MoveRotation(tankRigidbody.rotation * turnStep);
        }

        private void LateUpdate()
        {
            if (fireHeld)
            {
                tankWeapon.TryFire();
            }
        }

        public void Configure(float speed, float rotationSpeed)
        {
            moveSpeed = speed;
            turnSpeed = rotationSpeed;
        }

        private bool CanControl()
        {
            return health.IsAlive && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing;
        }

        private void UpdateTurretAim(Mouse mouse)
        {
            if (turretAim == null)
            {
                return;
            }

            if (TryGetMouseAimPoint(mouse, out Vector3 aimPoint))
            {
                HasAimPoint = true;
                CurrentAimPoint = aimPoint;
                turretAim.AimAtWorldPoint(aimPoint);
                return;
            }

            HasAimPoint = false;
            turretAim.AimInDirection(transform.forward);
        }

        private bool TryGetMouseAimPoint(Mouse mouse, out Vector3 aimPoint)
        {
            Camera gameplayCamera = Camera.main;
            if (gameplayCamera == null || mouse == null)
            {
                aimPoint = default;
                return false;
            }

            Ray aimRay = gameplayCamera.ScreenPointToRay(mouse.position.ReadValue());
            Plane aimPlane = new(Vector3.up, transform.position + Vector3.up * 0.8f);
            if (aimPlane.Raycast(aimRay, out float enterDistance))
            {
                aimPoint = aimRay.GetPoint(enterDistance);
                return true;
            }

            aimPoint = default;
            return false;
        }
    }
}
