using UnityEngine;

namespace Tanks.Core
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float followDistance = 8.5f;
        [SerializeField] private float followHeight = 5f;
        [SerializeField] private float lookAheadDistance = 7f;
        [SerializeField] private float lookHeight = 1.8f;
        [SerializeField] private float followSmoothTime = 0.12f;
        [SerializeField] private float rotationSmoothSpeed = 10f;
        [SerializeField] private float collisionProbeRadius = 0.35f;
        [SerializeField] private float minFollowDistance = 3f;
        [SerializeField] private float mobileFollowDistance = 7.2f;
        [SerializeField] private float mobileFollowHeight = 4.25f;
        [SerializeField] private float mobileLookAheadDistance = 6.6f;
        [SerializeField] private float mobileLookHeight = 1.85f;

        private Vector3 velocity;
        private Quaternion currentRotation;

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;

            if (target == null)
            {
                return;
            }

            Vector3 immediatePosition = GetResolvedCameraPosition();
            transform.position = immediatePosition;
            transform.rotation = GetDesiredRotation(immediatePosition);
            currentRotation = transform.rotation;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = GetResolvedCameraPosition();
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, followSmoothTime);
            Quaternion desiredRotation = GetDesiredRotation(transform.position);
            currentRotation = Quaternion.Slerp(currentRotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
            transform.rotation = currentRotation;
        }

        private Vector3 GetResolvedCameraPosition()
        {
            float activeLookHeight = GetLookHeight();
            Vector3 focusPoint = target.position + Vector3.up * activeLookHeight;
            Vector3 desiredPosition = focusPoint - target.forward * GetFollowDistance() + Vector3.up * (GetFollowHeight() - activeLookHeight);
            Vector3 rayDirection = desiredPosition - focusPoint;
            float rayDistance = rayDirection.magnitude;

            if (rayDistance > 0.001f)
            {
                RaycastHit[] hits = Physics.SphereCastAll(focusPoint, collisionProbeRadius, rayDirection.normalized, rayDistance);
                float closestDistance = rayDistance;

                for (int index = 0; index < hits.Length; index++)
                {
                    if (hits[index].transform.root == target.root)
                    {
                        continue;
                    }

                    closestDistance = Mathf.Min(closestDistance, hits[index].distance);
                }

                if (closestDistance < rayDistance)
                {
                    float clampedDistance = Mathf.Max(minFollowDistance, closestDistance - 0.1f);
                    desiredPosition = focusPoint + rayDirection.normalized * clampedDistance;
                }
            }

            return desiredPosition;
        }

        private Quaternion GetDesiredRotation(Vector3 cameraPosition)
        {
            Vector3 lookTarget = target.position + Vector3.up * GetLookHeight() + target.forward * GetLookAheadDistance();
            Vector3 lookDirection = lookTarget - cameraPosition;
            if (lookDirection.sqrMagnitude < 0.001f)
            {
                lookDirection = target.forward;
            }

            return Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }

        private float GetFollowDistance()
        {
            return IsMobileCameraProfile() ? mobileFollowDistance : followDistance;
        }

        private float GetFollowHeight()
        {
            return IsMobileCameraProfile() ? mobileFollowHeight : followHeight;
        }

        private float GetLookAheadDistance()
        {
            return IsMobileCameraProfile() ? mobileLookAheadDistance : lookAheadDistance;
        }

        private float GetLookHeight()
        {
            return IsMobileCameraProfile() ? mobileLookHeight : lookHeight;
        }

        private static bool IsMobileCameraProfile()
        {
            return Application.isMobilePlatform;
        }
    }
}
