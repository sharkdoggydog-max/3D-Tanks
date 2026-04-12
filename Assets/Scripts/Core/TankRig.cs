using UnityEngine;

namespace Tanks.Core
{
    public class TankRig : MonoBehaviour
    {
        [field: SerializeField] public Transform HullRoot { get; private set; }
        [field: SerializeField] public Transform TurretPivot { get; private set; }
        [field: SerializeField] public Transform BarrelRoot { get; private set; }
        [field: SerializeField] public Transform MuzzlePoint { get; private set; }

        public void Configure(Transform hullRoot, Transform turretPivot, Transform barrelRoot, Transform muzzlePoint)
        {
            HullRoot = hullRoot;
            TurretPivot = turretPivot;
            BarrelRoot = barrelRoot;
            MuzzlePoint = muzzlePoint;
        }
    }
}
