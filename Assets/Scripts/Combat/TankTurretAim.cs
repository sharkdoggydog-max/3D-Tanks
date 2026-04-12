using Tanks.Core;
using UnityEngine;

namespace Tanks.Combat
{
    [RequireComponent(typeof(TankRig))]
    public class TankTurretAim : MonoBehaviour
    {
        [SerializeField] private float turnSpeed = 220f;

        private TankRig tankRig;

        public Vector3 Forward
        {
            get
            {
                Transform source = tankRig != null && tankRig.MuzzlePoint != null ? tankRig.MuzzlePoint : transform;
                Vector3 flattenedForward = Vector3.ProjectOnPlane(source.forward, Vector3.up);
                return flattenedForward.sqrMagnitude > 0.001f ? flattenedForward.normalized : transform.forward;
            }
        }

        private void Awake()
        {
            tankRig = GetComponent<TankRig>();
        }

        public void Configure(float turretTurnSpeed)
        {
            turnSpeed = turretTurnSpeed;
        }

        public void AimAtWorldPoint(Vector3 worldPoint, bool snap = false)
        {
            if (tankRig == null || tankRig.TurretPivot == null)
            {
                return;
            }

            Vector3 desiredDirection = worldPoint - tankRig.TurretPivot.position;
            desiredDirection.y = 0f;
            AimInDirection(desiredDirection, snap);
        }

        public void AimInDirection(Vector3 worldDirection, bool snap = false)
        {
            if (tankRig == null || tankRig.TurretPivot == null)
            {
                return;
            }

            Vector3 flattenedDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up);
            if (flattenedDirection.sqrMagnitude < 0.001f)
            {
                flattenedDirection = transform.forward;
            }

            Quaternion desiredRotation = Quaternion.LookRotation(flattenedDirection.normalized, Vector3.up);
            float maxStep = snap ? 360f : turnSpeed * Time.deltaTime;
            tankRig.TurretPivot.rotation = Quaternion.RotateTowards(tankRig.TurretPivot.rotation, desiredRotation, maxStep);
        }

        public float GetAngleToWorldPoint(Vector3 worldPoint)
        {
            Vector3 desiredDirection = worldPoint - transform.position;
            desiredDirection.y = 0f;

            if (desiredDirection.sqrMagnitude < 0.001f)
            {
                return 0f;
            }

            return Vector3.Angle(Forward, desiredDirection.normalized);
        }
    }
}
