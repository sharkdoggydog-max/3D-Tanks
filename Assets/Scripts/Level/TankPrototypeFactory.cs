using Tanks.Combat;
using Tanks.Core;
using UnityEngine;

namespace Tanks.Level
{
    public static class TankPrototypeFactory
    {
        public static GameObject CreateTank(string tankName, Color bodyColor, Vector3 position, Quaternion rotation)
        {
            GameObject tankRoot = new(tankName);
            tankRoot.transform.SetPositionAndRotation(position, rotation);

            Rigidbody rigidbody = tankRoot.AddComponent<Rigidbody>();
            rigidbody.mass = 4f;
            rigidbody.angularDamping = 8f;
            rigidbody.linearDamping = 1.2f;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            BoxCollider collider = tankRoot.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.size = new Vector3(1.9f, 1.8f, 2.7f);

            Transform hullRoot = new GameObject("HullRoot").transform;
            hullRoot.SetParent(tankRoot.transform, false);

            CreateVisualCube("HullBody", hullRoot, new Vector3(0f, 0.75f, 0f), new Vector3(1.9f, 0.8f, 2.6f), bodyColor);
            CreateVisualCube("FrontPlate", hullRoot, new Vector3(0f, 0.95f, 0.95f), new Vector3(1.5f, 0.28f, 0.6f), bodyColor * 1.05f);
            CreateVisualCube("RearDeck", hullRoot, new Vector3(0f, 1f, -0.65f), new Vector3(1.2f, 0.18f, 1f), bodyColor * 0.92f);
            CreateVisualCube("LeftTrack", hullRoot, new Vector3(-1.08f, 0.48f, 0f), new Vector3(0.42f, 0.55f, 2.9f), bodyColor * 0.55f);
            CreateVisualCube("RightTrack", hullRoot, new Vector3(1.08f, 0.48f, 0f), new Vector3(0.42f, 0.55f, 2.9f), bodyColor * 0.55f);
            CreateVisualCube("CommandHatch", hullRoot, new Vector3(0f, 1.18f, -0.1f), new Vector3(0.48f, 0.16f, 0.48f), bodyColor * 1.12f);

            Transform turretPivot = new GameObject("TurretPivot").transform;
            turretPivot.SetParent(tankRoot.transform, false);
            turretPivot.localPosition = new Vector3(0f, 1.18f, 0.15f);

            CreateVisualCube("TurretBase", turretPivot, new Vector3(0f, 0f, 0.1f), new Vector3(1.25f, 0.42f, 1.5f), bodyColor * 0.82f, keepCollider: true);
            CreateVisualCube("TurretTop", turretPivot, new Vector3(0f, 0.22f, -0.05f), new Vector3(0.9f, 0.18f, 0.95f), bodyColor * 0.95f);
            CreateVisualCube("TurretCheekLeft", turretPivot, new Vector3(-0.42f, 0.05f, 0.22f), new Vector3(0.18f, 0.2f, 0.55f), bodyColor * 0.75f);
            CreateVisualCube("TurretCheekRight", turretPivot, new Vector3(0.42f, 0.05f, 0.22f), new Vector3(0.18f, 0.2f, 0.55f), bodyColor * 0.75f);

            Transform barrelRoot = new GameObject("BarrelRoot").transform;
            barrelRoot.SetParent(turretPivot, false);
            barrelRoot.localPosition = new Vector3(0f, 0.02f, 0.55f);

            Transform barrel = CreateVisualCube("Barrel", barrelRoot, new Vector3(0f, 0f, 1.15f), new Vector3(0.22f, 0.22f, 1.9f), bodyColor * 0.6f, keepCollider: true);
            CreateVisualCube("MuzzleBrake", barrelRoot, new Vector3(0f, 0f, 2.12f), new Vector3(0.33f, 0.28f, 0.24f), bodyColor * 0.72f, keepCollider: true);
            Transform muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(barrelRoot, false);
            muzzle.localPosition = new Vector3(0f, 0f, 2.32f);

            Health health = tankRoot.AddComponent<Health>();
            health.Configure(3f, Team.Neutral, true);

            TankRig rig = tankRoot.AddComponent<TankRig>();
            rig.Configure(hullRoot, turretPivot, barrelRoot, muzzle);

            TankTurretAim turretAim = tankRoot.AddComponent<TankTurretAim>();
            turretAim.Configure(220f);

            tankRoot.AddComponent<TankCombatFeedback>();

            TankWeapon weapon = tankRoot.AddComponent<TankWeapon>();
            weapon.Configure(muzzle, 0.45f, 22f, 1f, 3f, 0.28f);

            return tankRoot;
        }

        private static Transform CreateVisualCube(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            bool keepCollider = false)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            cube.GetComponent<Renderer>().material.color = color;

            if (!keepCollider)
            {
                Object.Destroy(cube.GetComponent<Collider>());
            }

            return cube.transform;
        }
    }
}
