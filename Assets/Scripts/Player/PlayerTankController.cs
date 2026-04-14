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
        [SerializeField] private float touchAimDistance = 14f;

        private Rigidbody tankRigidbody;
        private TankWeapon tankWeapon;
        private TankTurretAim turretAim;
        private Health health;
        private float moveInput;
        private float turnInput;
        private bool fireHeld;
        private int moveTouchId = -1;
        private int aimTouchId = -1;
        private int fireTouchId = -1;
        private Vector2 moveStickInput;
        private Vector2 aimScreenPosition;

        public bool HasAimPoint { get; private set; }
        public Vector3 CurrentAimPoint { get; private set; }
        public bool TouchControlsVisible => MobileControlLayout.ShouldUseTouchControls();
        public bool IsFireTouchHeld => fireTouchId >= 0;
        public Vector2 MoveStickVisualCenter => MobileControlLayout.GetMoveStickCenter();
        public Vector2 MoveStickVisualOffset => moveStickInput * MobileControlLayout.GetStickRadius();
        public Vector2 AimTouchGuiPosition => MobileControlLayout.ToGuiPosition(aimScreenPosition);
        public bool HasAimTouch => aimTouchId >= 0;

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
            fireHeld = false;

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

            moveInput = Mathf.Clamp(moveInput, -1f, 1f);
            turnInput = Mathf.Clamp(turnInput, -1f, 1f);
            fireHeld = (keyboard != null && keyboard.spaceKey.isPressed) ||
                       (mouse != null && mouse.leftButton.isPressed);

            if (TouchControlsVisible)
            {
                UpdateTouchInput();
                moveInput = Mathf.Clamp(moveInput + moveStickInput.y, -1f, 1f);
                turnInput = Mathf.Clamp(turnInput + moveStickInput.x, -1f, 1f);
                fireHeld |= fireTouchId >= 0;
            }
            else
            {
                ClearTouchState();
            }

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

            if (TouchControlsVisible && TryGetTouchAimPoint(out Vector3 touchAimPoint))
            {
                HasAimPoint = true;
                CurrentAimPoint = touchAimPoint;
                turretAim.AimAtWorldPoint(touchAimPoint);
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

        private void UpdateTouchInput()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                ClearTouchState();
                return;
            }

            Rect moveZone = MobileControlLayout.GetMovementZone();
            Rect aimZone = MobileControlLayout.GetAimZone();
            Rect fireRect = MobileControlLayout.GetFireButtonRect();

            bool moveTouchFound = false;
            bool aimTouchFound = false;
            bool fireTouchFound = false;
            Vector2 currentMovePosition = Vector2.zero;

            foreach (var touch in touchscreen.touches)
            {
                if (!touch.press.isPressed)
                {
                    continue;
                }

                int touchId = touch.touchId.ReadValue();
                Vector2 position = touch.position.ReadValue();

                if (touchId == moveTouchId)
                {
                    moveTouchFound = true;
                    currentMovePosition = position;
                    continue;
                }

                if (touchId == fireTouchId)
                {
                    fireTouchFound = true;
                    continue;
                }

                if (touchId == aimTouchId)
                {
                    aimTouchFound = true;
                    aimScreenPosition = position;
                    continue;
                }

                if (fireRect.Contains(position))
                {
                    fireTouchId = touchId;
                    fireTouchFound = true;
                    continue;
                }

                if (moveTouchId < 0 && moveZone.Contains(position))
                {
                    moveTouchId = touchId;
                    moveTouchFound = true;
                    currentMovePosition = position;
                    continue;
                }

                if (aimTouchId < 0 && aimZone.Contains(position))
                {
                    aimTouchId = touchId;
                    aimTouchFound = true;
                    aimScreenPosition = position;
                }
            }

            if (!fireTouchFound)
            {
                fireTouchId = -1;
            }

            if (!moveTouchFound)
            {
                moveTouchId = -1;
                moveStickInput = Vector2.zero;
            }
            else
            {
                Vector2 localOffset = currentMovePosition - MobileControlLayout.GetMoveStickCenter();
                moveStickInput = Vector2.ClampMagnitude(localOffset / MobileControlLayout.GetStickRadius(), 1f);
            }

            if (!aimTouchFound)
            {
                aimTouchId = -1;
            }
        }

        private bool TryGetTouchAimPoint(out Vector3 aimPoint)
        {
            Camera gameplayCamera = Camera.main;
            if (gameplayCamera == null || aimTouchId < 0)
            {
                aimPoint = default;
                return false;
            }

            Vector2 aimInput = MobileControlLayout.GetNormalizedAimInput(aimScreenPosition);
            if (aimInput.sqrMagnitude < 0.01f)
            {
                aimPoint = default;
                return false;
            }

            Vector3 cameraRight = Vector3.ProjectOnPlane(gameplayCamera.transform.right, Vector3.up);
            Vector3 cameraForward = Vector3.ProjectOnPlane(gameplayCamera.transform.forward, Vector3.up);

            if (cameraRight.sqrMagnitude < 0.001f)
            {
                cameraRight = Vector3.right;
            }

            if (cameraForward.sqrMagnitude < 0.001f)
            {
                cameraForward = transform.forward;
            }

            cameraRight.Normalize();
            cameraForward.Normalize();

            Vector3 desiredDirection = cameraRight * aimInput.x + cameraForward * aimInput.y;
            if (desiredDirection.sqrMagnitude < 0.001f)
            {
                aimPoint = default;
                return false;
            }

            desiredDirection.Normalize();
            Vector3 aimOrigin = turretAim != null ? turretAim.transform.position + Vector3.up * 0.8f : transform.position + Vector3.up * 0.8f;
            aimPoint = aimOrigin + desiredDirection * touchAimDistance;
            return true;
        }

        private void ClearTouchState()
        {
            moveTouchId = -1;
            aimTouchId = -1;
            fireTouchId = -1;
            moveStickInput = Vector2.zero;
        }
    }
}
