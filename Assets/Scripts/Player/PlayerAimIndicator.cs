using UnityEngine;

namespace Tanks.Player
{
    [RequireComponent(typeof(PlayerTankController))]
    public class PlayerAimIndicator : MonoBehaviour
    {
        private PlayerTankController controller;
        private Transform markerRoot;
        private Transform outerRing;
        private Transform centerDot;

        private void Awake()
        {
            controller = GetComponent<PlayerTankController>();
            CreateMarker();
        }

        private void LateUpdate()
        {
            if (markerRoot == null)
            {
                return;
            }

            if (!controller.HasAimPoint)
            {
                markerRoot.gameObject.SetActive(false);
                return;
            }

            markerRoot.gameObject.SetActive(true);
            markerRoot.position = new Vector3(controller.CurrentAimPoint.x, 0.08f, controller.CurrentAimPoint.z);
            markerRoot.rotation = Quaternion.Euler(90f, 0f, 0f);

            if (Camera.main != null)
            {
                float distance = Vector3.Distance(transform.position, markerRoot.position);
                float scale = Mathf.Clamp(0.6f + distance * 0.015f, 0.65f, 1.35f);
                markerRoot.localScale = Vector3.one * scale;
            }
        }

        private void CreateMarker()
        {
            markerRoot = new GameObject("AimMarker").transform;
            markerRoot.SetParent(transform, false);

            outerRing = CreateMarkerPart("OuterRing", new Vector3(0f, 0f, 0f), new Vector3(1.1f, 0.06f, 1.1f), new Color(1f, 0.92f, 0.3f, 0.8f));
            centerDot = CreateMarkerPart("CenterDot", new Vector3(0f, 0.01f, 0f), new Vector3(0.24f, 0.08f, 0.24f), new Color(1f, 1f, 1f, 0.95f));
        }

        private Transform CreateMarkerPart(string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            part.name = name;
            part.transform.SetParent(markerRoot, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().material.color = color;
            Destroy(part.GetComponent<Collider>());
            return part.transform;
        }
    }
}
