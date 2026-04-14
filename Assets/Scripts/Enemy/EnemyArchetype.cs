using UnityEngine;

namespace Tanks.Enemy
{
    public readonly struct EnemyArchetype
    {
        public EnemyArchetype(
            EnemyVariant variant,
            string displayName,
            Color color,
            Color accentColor,
            float maxHealth,
            float moveSpeed,
            float turnSpeed,
            float attackRange,
            float holdDistance,
            float weaponCooldown,
            float projectileSpeed,
            float projectileDamage,
            float projectileRadius,
            float projectileLifetime,
            float splashRadius,
            float splashDamageMultiplier,
            int burstCount,
            float burstInterval,
            float hullScale,
            float turretScale,
            float barrelLengthScale,
            float barrelThicknessScale,
            float turretTurnSpeed,
            float retreatDistance,
            float aimTolerance,
            float aimConfirmTime,
            float orbitStrength,
            float holdMoveThrottle,
            float repositionThrottle,
            float advanceThrottle)
        {
            Variant = variant;
            DisplayName = displayName;
            Color = color;
            AccentColor = accentColor;
            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            TurnSpeed = turnSpeed;
            AttackRange = attackRange;
            HoldDistance = holdDistance;
            WeaponCooldown = weaponCooldown;
            ProjectileSpeed = projectileSpeed;
            ProjectileDamage = projectileDamage;
            ProjectileRadius = projectileRadius;
            ProjectileLifetime = projectileLifetime;
            SplashRadius = splashRadius;
            SplashDamageMultiplier = splashDamageMultiplier;
            BurstCount = burstCount;
            BurstInterval = burstInterval;
            HullScale = hullScale;
            TurretScale = turretScale;
            BarrelLengthScale = barrelLengthScale;
            BarrelThicknessScale = barrelThicknessScale;
            TurretTurnSpeed = turretTurnSpeed;
            RetreatDistance = retreatDistance;
            AimTolerance = aimTolerance;
            AimConfirmTime = aimConfirmTime;
            OrbitStrength = orbitStrength;
            HoldMoveThrottle = holdMoveThrottle;
            RepositionThrottle = repositionThrottle;
            AdvanceThrottle = advanceThrottle;
        }

        public EnemyVariant Variant { get; }
        public string DisplayName { get; }
        public Color Color { get; }
        public Color AccentColor { get; }
        public float MaxHealth { get; }
        public float MoveSpeed { get; }
        public float TurnSpeed { get; }
        public float AttackRange { get; }
        public float HoldDistance { get; }
        public float WeaponCooldown { get; }
        public float ProjectileSpeed { get; }
        public float ProjectileDamage { get; }
        public float ProjectileRadius { get; }
        public float ProjectileLifetime { get; }
        public float SplashRadius { get; }
        public float SplashDamageMultiplier { get; }
        public int BurstCount { get; }
        public float BurstInterval { get; }
        public float HullScale { get; }
        public float TurretScale { get; }
        public float BarrelLengthScale { get; }
        public float BarrelThicknessScale { get; }
        public float TurretTurnSpeed { get; }
        public float RetreatDistance { get; }
        public float AimTolerance { get; }
        public float AimConfirmTime { get; }
        public float OrbitStrength { get; }
        public float HoldMoveThrottle { get; }
        public float RepositionThrottle { get; }
        public float AdvanceThrottle { get; }
    }
}
