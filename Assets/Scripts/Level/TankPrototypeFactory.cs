using Tanks.Combat;
using Tanks.Core;
using Tanks.Enemy;
using UnityEngine;

namespace Tanks.Level
{
    public static class TankPrototypeFactory
    {
        public static GameObject CreateTank(
            string tankName,
            Color bodyColor,
            Vector3 position,
            Quaternion rotation,
            Color? accentColor = null,
            float hullScale = 1f,
            float turretScale = 1f,
            float barrelLengthScale = 1f,
            float barrelThicknessScale = 1f,
            EnemyVariant? enemyVariant = null,
            int variationIndex = 0)
        {
            GameObject tankRoot = new(tankName);
            tankRoot.transform.SetPositionAndRotation(position, rotation);

            Color trimColor = accentColor ?? Color.Lerp(bodyColor, Color.white, 0.3f);
            TankFrameStyle frameStyle = ResolveFrameStyle(enemyVariant);
            int variation = Mathf.Abs(variationIndex % 3);

            Rigidbody rigidbody = tankRoot.AddComponent<Rigidbody>();
            rigidbody.mass = 4f;
            rigidbody.angularDamping = 8f;
            rigidbody.linearDamping = 1.2f;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            BoxCollider collider = tankRoot.AddComponent<BoxCollider>();
            collider.center = GetColliderCenter(frameStyle, hullScale, turretScale);
            collider.size = GetColliderSize(frameStyle, hullScale, turretScale);

            Transform hullRoot = new GameObject("HullRoot").transform;
            hullRoot.SetParent(tankRoot.transform, false);
            BuildHull(frameStyle, hullRoot, bodyColor, trimColor, hullScale, variation);

            Transform turretPivot = new GameObject("TurretPivot").transform;
            turretPivot.SetParent(tankRoot.transform, false);
            turretPivot.localPosition = GetTurretPivotPosition(frameStyle, hullScale);
            BuildTurret(frameStyle, turretPivot, bodyColor, trimColor, turretScale, variation);

            Transform barrelRoot = new GameObject("BarrelRoot").transform;
            barrelRoot.SetParent(turretPivot, false);
            barrelRoot.localPosition = GetBarrelRootPosition(frameStyle, turretScale);

            float barrelLength = GetBaseBarrelLength(frameStyle) * barrelLengthScale;
            float barrelThickness = GetBaseBarrelThickness(frameStyle) * barrelThicknessScale;

            CreateVisualCube(
                "Barrel",
                barrelRoot,
                new Vector3(0f, 0f, barrelLength * 0.6f),
                new Vector3(barrelThickness, barrelThickness, barrelLength),
                bodyColor * 0.58f,
                keepCollider: true);

            CreateVisualCube(
                "MuzzleBrake",
                barrelRoot,
                new Vector3(0f, 0f, barrelLength * 1.1f),
                new Vector3(barrelThickness * 1.5f, barrelThickness * 1.3f, barrelThickness * 1.1f),
                trimColor,
                keepCollider: true);

            Transform muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(barrelRoot, false);
            muzzle.localPosition = new Vector3(0f, 0f, barrelLength * 1.24f);

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

        private static TankFrameStyle ResolveFrameStyle(EnemyVariant? enemyVariant)
        {
            return enemyVariant switch
            {
                EnemyVariant.Artillery => TankFrameStyle.Artillery,
                EnemyVariant.Raider => TankFrameStyle.Raider,
                EnemyVariant.Striker => TankFrameStyle.Striker,
                EnemyVariant.Scout => TankFrameStyle.Scout,
                EnemyVariant.Bulwark => TankFrameStyle.Bulwark,
                _ => TankFrameStyle.Standard
            };
        }

        private static Vector3 GetColliderCenter(TankFrameStyle style, float hullScale, float turretScale)
        {
            Vector3 baseCenter = style switch
            {
                TankFrameStyle.Artillery => new Vector3(0f, 0.94f, -0.12f),
                TankFrameStyle.Raider => new Vector3(0f, 0.74f, 0.08f),
                TankFrameStyle.Striker => new Vector3(0f, 0.86f, 0.12f),
                TankFrameStyle.Scout => new Vector3(0f, 0.68f, 0.04f),
                TankFrameStyle.Bulwark => new Vector3(0f, 1.08f, 0f),
                _ => new Vector3(0f, 0.9f, 0f)
            };

            return new Vector3(baseCenter.x * hullScale, baseCenter.y * Mathf.Max(hullScale, turretScale), baseCenter.z * hullScale);
        }

        private static Vector3 GetColliderSize(TankFrameStyle style, float hullScale, float turretScale)
        {
            Vector3 baseSize = style switch
            {
                TankFrameStyle.Artillery => new Vector3(2.15f, 1.85f, 3.85f),
                TankFrameStyle.Raider => new Vector3(1.55f, 1.45f, 3.1f),
                TankFrameStyle.Striker => new Vector3(2.1f, 1.65f, 2.85f),
                TankFrameStyle.Scout => new Vector3(1.35f, 1.25f, 3.35f),
                TankFrameStyle.Bulwark => new Vector3(2.5f, 2.2f, 3.15f),
                _ => new Vector3(1.9f, 1.8f, 2.7f)
            };

            return new Vector3(baseSize.x * hullScale, baseSize.y * Mathf.Max(hullScale, turretScale), baseSize.z * hullScale);
        }

        private static Vector3 GetTurretPivotPosition(TankFrameStyle style, float hullScale)
        {
            Vector3 basePosition = style switch
            {
                TankFrameStyle.Artillery => new Vector3(0f, 1.22f, -0.18f),
                TankFrameStyle.Raider => new Vector3(0f, 1.04f, 0.35f),
                TankFrameStyle.Striker => new Vector3(0f, 1.14f, 0.18f),
                TankFrameStyle.Scout => new Vector3(0f, 0.96f, 0.16f),
                TankFrameStyle.Bulwark => new Vector3(0f, 1.36f, 0.05f),
                _ => new Vector3(0f, 1.18f, 0.15f)
            };

            return basePosition * hullScale;
        }

        private static Vector3 GetBarrelRootPosition(TankFrameStyle style, float turretScale)
        {
            Vector3 basePosition = style switch
            {
                TankFrameStyle.Artillery => new Vector3(0f, 0.06f, 0.96f),
                TankFrameStyle.Raider => new Vector3(0f, 0.02f, 0.42f),
                TankFrameStyle.Striker => new Vector3(0f, 0.03f, 0.68f),
                TankFrameStyle.Scout => new Vector3(0f, 0.04f, 0.78f),
                TankFrameStyle.Bulwark => new Vector3(0f, 0.04f, 0.72f),
                _ => new Vector3(0f, 0.02f, 0.55f)
            };

            return basePosition * turretScale;
        }

        private static float GetBaseBarrelLength(TankFrameStyle style)
        {
            return style switch
            {
                TankFrameStyle.Artillery => 2.65f,
                TankFrameStyle.Raider => 2.35f,
                TankFrameStyle.Striker => 1.55f,
                TankFrameStyle.Scout => 2.5f,
                TankFrameStyle.Bulwark => 1.45f,
                _ => 1.9f
            };
        }

        private static float GetBaseBarrelThickness(TankFrameStyle style)
        {
            return style switch
            {
                TankFrameStyle.Artillery => 0.26f,
                TankFrameStyle.Raider => 0.18f,
                TankFrameStyle.Striker => 0.28f,
                TankFrameStyle.Scout => 0.14f,
                TankFrameStyle.Bulwark => 0.3f,
                _ => 0.22f
            };
        }

        private static void BuildHull(TankFrameStyle style, Transform hullRoot, Color bodyColor, Color trimColor, float hullScale, int variation)
        {
            switch (style)
            {
                case TankFrameStyle.Artillery:
                    BuildArtilleryHull(hullRoot, bodyColor, trimColor, hullScale, variation);
                    break;
                case TankFrameStyle.Raider:
                    BuildRaiderHull(hullRoot, bodyColor, trimColor, hullScale, variation);
                    break;
                case TankFrameStyle.Striker:
                    BuildStrikerHull(hullRoot, bodyColor, trimColor, hullScale, variation);
                    break;
                case TankFrameStyle.Scout:
                    BuildScoutHull(hullRoot, bodyColor, trimColor, hullScale, variation);
                    break;
                case TankFrameStyle.Bulwark:
                    BuildBulwarkHull(hullRoot, bodyColor, trimColor, hullScale, variation);
                    break;
                default:
                    BuildStandardHull(hullRoot, bodyColor, trimColor, hullScale, variation);
                    break;
            }
        }

        private static void BuildTurret(TankFrameStyle style, Transform turretPivot, Color bodyColor, Color trimColor, float turretScale, int variation)
        {
            switch (style)
            {
                case TankFrameStyle.Artillery:
                    BuildArtilleryTurret(turretPivot, bodyColor, trimColor, turretScale, variation);
                    break;
                case TankFrameStyle.Raider:
                    BuildRaiderTurret(turretPivot, bodyColor, trimColor, turretScale, variation);
                    break;
                case TankFrameStyle.Striker:
                    BuildStrikerTurret(turretPivot, bodyColor, trimColor, turretScale, variation);
                    break;
                case TankFrameStyle.Scout:
                    BuildScoutTurret(turretPivot, bodyColor, trimColor, turretScale, variation);
                    break;
                case TankFrameStyle.Bulwark:
                    BuildBulwarkTurret(turretPivot, bodyColor, trimColor, turretScale, variation);
                    break;
                default:
                    BuildStandardTurret(turretPivot, bodyColor, trimColor, turretScale, variation);
                    break;
            }
        }

        private static void BuildStandardHull(Transform hullRoot, Color bodyColor, Color trimColor, float hullScale, int variation)
        {
            CreateVisualCube("HullBody", hullRoot, new Vector3(0f, 0.75f, 0f) * hullScale, new Vector3(1.9f, 0.8f, 2.6f) * hullScale, bodyColor);
            CreateVisualCube("FrontPlate", hullRoot, new Vector3(0f, 0.95f, 0.98f) * hullScale, new Vector3(1.5f, 0.26f, 0.62f) * hullScale, trimColor);
            CreateVisualCube("RearDeck", hullRoot, new Vector3(0f, 1f, -0.68f) * hullScale, new Vector3(1.16f, 0.18f, 1.04f) * hullScale, bodyColor * 0.92f);
            CreateVisualCube("LeftTrack", hullRoot, new Vector3(-1.08f, 0.48f, 0f) * hullScale, new Vector3(0.42f, 0.55f, 2.9f) * hullScale, bodyColor * 0.42f);
            CreateVisualCube("RightTrack", hullRoot, new Vector3(1.08f, 0.48f, 0f) * hullScale, new Vector3(0.42f, 0.55f, 2.9f) * hullScale, bodyColor * 0.42f);
            CreateVisualCube("CommandHatch", hullRoot, new Vector3(0f, 1.18f, -0.1f) * hullScale, new Vector3(0.48f, 0.16f, 0.48f) * hullScale, trimColor);

            if (variation == 0)
            {
                CreateVisualCube("SideStowage", hullRoot, new Vector3(-0.78f, 0.96f, -0.92f) * hullScale, new Vector3(0.34f, 0.28f, 0.65f) * hullScale, trimColor * 0.92f);
                CreateVisualCylinder("Antenna", hullRoot, new Vector3(0.76f, 1.42f, -0.92f) * hullScale, new Vector3(0.05f, 0.38f, 0.05f) * hullScale, trimColor);
            }
            else if (variation == 1)
            {
                CreateVisualCube("RearExhaustLeft", hullRoot, new Vector3(-0.5f, 0.92f, -1.42f) * hullScale, new Vector3(0.28f, 0.22f, 0.3f) * hullScale, bodyColor * 0.55f);
                CreateVisualCube("RearExhaustRight", hullRoot, new Vector3(0.5f, 0.92f, -1.42f) * hullScale, new Vector3(0.28f, 0.22f, 0.3f) * hullScale, bodyColor * 0.55f);
            }
            else
            {
                CreateVisualCube("FrontSpacedArmorLeft", hullRoot, new Vector3(-0.54f, 0.9f, 1.32f) * hullScale, new Vector3(0.42f, 0.22f, 0.22f) * hullScale, trimColor * 0.92f);
                CreateVisualCube("FrontSpacedArmorRight", hullRoot, new Vector3(0.54f, 0.9f, 1.32f) * hullScale, new Vector3(0.42f, 0.22f, 0.22f) * hullScale, trimColor * 0.92f);
            }
        }

        private static void BuildRaiderHull(Transform hullRoot, Color bodyColor, Color trimColor, float hullScale, int variation)
        {
            CreateVisualCube("HullBody", hullRoot, new Vector3(0f, 0.6f, -0.02f) * hullScale, new Vector3(1.45f, 0.62f, 2.95f) * hullScale, bodyColor);
            CreateVisualCube("NoseWedge", hullRoot, new Vector3(0f, 0.75f, 1.28f) * hullScale, new Vector3(0.92f, 0.16f, 0.78f) * hullScale, trimColor);
            CreateVisualCube("EngineDeck", hullRoot, new Vector3(0f, 0.82f, -0.92f) * hullScale, new Vector3(0.98f, 0.14f, 1.1f) * hullScale, bodyColor * 0.9f);
            CreateVisualCube("LeftTrack", hullRoot, new Vector3(-0.92f, 0.38f, 0f) * hullScale, new Vector3(0.3f, 0.42f, 3.05f) * hullScale, bodyColor * 0.32f);
            CreateVisualCube("RightTrack", hullRoot, new Vector3(0.92f, 0.38f, 0f) * hullScale, new Vector3(0.3f, 0.42f, 3.05f) * hullScale, bodyColor * 0.32f);
            CreateVisualCube("DriverPod", hullRoot, new Vector3(0f, 0.98f, 0.18f) * hullScale, new Vector3(0.52f, 0.18f, 0.5f) * hullScale, trimColor);

            if (variation == 0)
            {
                CreateVisualCube("LeftSidePod", hullRoot, new Vector3(-0.62f, 0.82f, -0.92f) * hullScale, new Vector3(0.2f, 0.22f, 0.8f) * hullScale, trimColor * 0.92f);
                CreateVisualCylinder("SensorMast", hullRoot, new Vector3(0.52f, 1.22f, -0.68f) * hullScale, new Vector3(0.04f, 0.32f, 0.04f) * hullScale, trimColor);
            }
            else if (variation == 1)
            {
                CreateVisualCube("RearBoosterLeft", hullRoot, new Vector3(-0.32f, 0.72f, -1.62f) * hullScale, new Vector3(0.22f, 0.26f, 0.42f) * hullScale, bodyColor * 0.45f);
                CreateVisualCube("RearBoosterRight", hullRoot, new Vector3(0.32f, 0.72f, -1.62f) * hullScale, new Vector3(0.22f, 0.26f, 0.42f) * hullScale, bodyColor * 0.45f);
            }
            else
            {
                CreateVisualCube("RadarBlock", hullRoot, new Vector3(-0.4f, 1.05f, 0.65f) * hullScale, new Vector3(0.24f, 0.18f, 0.24f) * hullScale, trimColor);
                CreateVisualCube("RightStowage", hullRoot, new Vector3(0.58f, 0.78f, -0.7f) * hullScale, new Vector3(0.16f, 0.18f, 0.9f) * hullScale, trimColor * 0.88f);
            }
        }

        private static void BuildBulwarkHull(Transform hullRoot, Color bodyColor, Color trimColor, float hullScale, int variation)
        {
            CreateVisualCube("HullBody", hullRoot, new Vector3(0f, 0.92f, 0f) * hullScale, new Vector3(2.45f, 1.05f, 2.75f) * hullScale, bodyColor);
            CreateVisualCube("FrontArmor", hullRoot, new Vector3(0f, 1.06f, 1.18f) * hullScale, new Vector3(2.1f, 0.34f, 0.5f) * hullScale, trimColor);
            CreateVisualCube("UpperGlacis", hullRoot, new Vector3(0f, 1.28f, 0.78f) * hullScale, new Vector3(1.5f, 0.2f, 0.86f) * hullScale, bodyColor * 0.96f);
            CreateVisualCube("LeftTrack", hullRoot, new Vector3(-1.32f, 0.55f, 0f) * hullScale, new Vector3(0.56f, 0.72f, 3.2f) * hullScale, bodyColor * 0.36f);
            CreateVisualCube("RightTrack", hullRoot, new Vector3(1.32f, 0.55f, 0f) * hullScale, new Vector3(0.56f, 0.72f, 3.2f) * hullScale, bodyColor * 0.36f);
            CreateVisualCube("LeftSideSkirt", hullRoot, new Vector3(-0.98f, 0.86f, 0f) * hullScale, new Vector3(0.32f, 0.5f, 2.65f) * hullScale, trimColor * 0.86f);
            CreateVisualCube("RightSideSkirt", hullRoot, new Vector3(0.98f, 0.86f, 0f) * hullScale, new Vector3(0.32f, 0.5f, 2.65f) * hullScale, trimColor * 0.86f);
            CreateVisualCube("CommandBlock", hullRoot, new Vector3(0f, 1.54f, -0.32f) * hullScale, new Vector3(0.8f, 0.2f, 0.62f) * hullScale, trimColor);

            if (variation == 0)
            {
                CreateVisualCube("SideArmorLeftFront", hullRoot, new Vector3(-1.05f, 1.08f, 0.95f) * hullScale, new Vector3(0.34f, 0.34f, 0.78f) * hullScale, trimColor * 0.92f);
                CreateVisualCube("SideArmorRightFront", hullRoot, new Vector3(1.05f, 1.08f, 0.95f) * hullScale, new Vector3(0.34f, 0.34f, 0.78f) * hullScale, trimColor * 0.92f);
            }
            else if (variation == 1)
            {
                CreateVisualCube("RearVentLeft", hullRoot, new Vector3(-0.52f, 1.18f, -1.42f) * hullScale, new Vector3(0.42f, 0.22f, 0.52f) * hullScale, bodyColor * 0.55f);
                CreateVisualCube("RearVentRight", hullRoot, new Vector3(0.52f, 1.18f, -1.42f) * hullScale, new Vector3(0.42f, 0.22f, 0.52f) * hullScale, bodyColor * 0.55f);
            }
            else
            {
                CreateVisualCube("HullBunkerLeft", hullRoot, new Vector3(-0.78f, 1.38f, -0.38f) * hullScale, new Vector3(0.34f, 0.26f, 0.54f) * hullScale, trimColor);
                CreateVisualCube("HullBunkerRight", hullRoot, new Vector3(0.78f, 1.38f, -0.38f) * hullScale, new Vector3(0.34f, 0.26f, 0.54f) * hullScale, trimColor);
            }
        }

        private static void BuildArtilleryHull(Transform hullRoot, Color bodyColor, Color trimColor, float hullScale, int variation)
        {
            CreateVisualCube("HullBody", hullRoot, new Vector3(0f, 0.78f, -0.12f) * hullScale, new Vector3(2f, 0.78f, 3.75f) * hullScale, bodyColor);
            CreateVisualCube("FrontCab", hullRoot, new Vector3(0f, 1.02f, 1.14f) * hullScale, new Vector3(1.28f, 0.24f, 0.92f) * hullScale, trimColor);
            CreateVisualCube("RearDeck", hullRoot, new Vector3(0f, 1.04f, -1.28f) * hullScale, new Vector3(1.48f, 0.16f, 1.58f) * hullScale, bodyColor * 0.92f);
            CreateVisualCube("LeftTrack", hullRoot, new Vector3(-1.16f, 0.42f, -0.08f) * hullScale, new Vector3(0.38f, 0.5f, 4.05f) * hullScale, bodyColor * 0.34f);
            CreateVisualCube("RightTrack", hullRoot, new Vector3(1.16f, 0.42f, -0.08f) * hullScale, new Vector3(0.38f, 0.5f, 4.05f) * hullScale, bodyColor * 0.34f);
            CreateVisualCube("SupportLeft", hullRoot, new Vector3(-0.82f, 0.94f, -0.9f) * hullScale, new Vector3(0.18f, 0.24f, 1.68f) * hullScale, trimColor * 0.9f);
            CreateVisualCube("SupportRight", hullRoot, new Vector3(0.82f, 0.94f, -0.9f) * hullScale, new Vector3(0.18f, 0.24f, 1.68f) * hullScale, trimColor * 0.9f);

            if (variation == 0)
            {
                CreateVisualCube("AmmoRackLeft", hullRoot, new Vector3(-0.54f, 1.18f, -1.62f) * hullScale, new Vector3(0.34f, 0.18f, 0.84f) * hullScale, trimColor);
                CreateVisualCube("AmmoRackRight", hullRoot, new Vector3(0.54f, 1.18f, -1.62f) * hullScale, new Vector3(0.34f, 0.18f, 0.84f) * hullScale, trimColor);
            }
            else if (variation == 1)
            {
                CreateVisualCylinder("RangeFinder", hullRoot, new Vector3(0.78f, 1.4f, 0.84f) * hullScale, new Vector3(0.06f, 0.34f, 0.06f) * hullScale, trimColor);
                CreateVisualCube("RearToolbox", hullRoot, new Vector3(0f, 1.16f, -2.02f) * hullScale, new Vector3(0.84f, 0.2f, 0.42f) * hullScale, trimColor * 0.92f);
            }
            else
            {
                CreateVisualCube("StabilizerLeft", hullRoot, new Vector3(-0.74f, 0.68f, -2.04f) * hullScale, new Vector3(0.22f, 0.16f, 0.58f) * hullScale, bodyColor * 0.52f);
                CreateVisualCube("StabilizerRight", hullRoot, new Vector3(0.74f, 0.68f, -2.04f) * hullScale, new Vector3(0.22f, 0.16f, 0.58f) * hullScale, bodyColor * 0.52f);
            }
        }

        private static void BuildStrikerHull(Transform hullRoot, Color bodyColor, Color trimColor, float hullScale, int variation)
        {
            CreateVisualCube("HullBody", hullRoot, new Vector3(0f, 0.74f, 0f) * hullScale, new Vector3(2.05f, 0.76f, 2.72f) * hullScale, bodyColor);
            CreateVisualCube("AssaultWedge", hullRoot, new Vector3(0f, 0.92f, 1.14f) * hullScale, new Vector3(1.48f, 0.22f, 0.72f) * hullScale, trimColor);
            CreateVisualCube("RamPlate", hullRoot, new Vector3(0f, 0.68f, 1.52f) * hullScale, new Vector3(1.18f, 0.16f, 0.18f) * hullScale, trimColor * 0.94f);
            CreateVisualCube("LeftTrack", hullRoot, new Vector3(-1.12f, 0.44f, 0f) * hullScale, new Vector3(0.42f, 0.5f, 2.95f) * hullScale, bodyColor * 0.34f);
            CreateVisualCube("RightTrack", hullRoot, new Vector3(1.12f, 0.44f, 0f) * hullScale, new Vector3(0.42f, 0.5f, 2.95f) * hullScale, bodyColor * 0.34f);
            CreateVisualCube("EngineBlock", hullRoot, new Vector3(0f, 1.02f, -1f) * hullScale, new Vector3(1.12f, 0.18f, 0.98f) * hullScale, bodyColor * 0.92f);

            if (variation == 0)
            {
                CreateVisualCube("BreachLeft", hullRoot, new Vector3(-0.7f, 0.96f, 0.32f) * hullScale, new Vector3(0.26f, 0.22f, 0.92f) * hullScale, trimColor * 0.9f);
                CreateVisualCube("BreachRight", hullRoot, new Vector3(0.7f, 0.96f, 0.32f) * hullScale, new Vector3(0.26f, 0.22f, 0.92f) * hullScale, trimColor * 0.9f);
            }
            else if (variation == 1)
            {
                CreateVisualCube("ChargePackLeft", hullRoot, new Vector3(-0.52f, 1.18f, -0.8f) * hullScale, new Vector3(0.34f, 0.16f, 0.42f) * hullScale, trimColor);
                CreateVisualCube("ChargePackRight", hullRoot, new Vector3(0.52f, 1.18f, -0.8f) * hullScale, new Vector3(0.34f, 0.16f, 0.42f) * hullScale, trimColor);
            }
            else
            {
                CreateVisualCylinder("ShockMast", hullRoot, new Vector3(0.72f, 1.3f, 0.02f) * hullScale, new Vector3(0.045f, 0.3f, 0.045f) * hullScale, trimColor);
                CreateVisualCube("RearArmor", hullRoot, new Vector3(0f, 0.94f, -1.48f) * hullScale, new Vector3(0.94f, 0.24f, 0.28f) * hullScale, trimColor * 0.86f);
            }
        }

        private static void BuildScoutHull(Transform hullRoot, Color bodyColor, Color trimColor, float hullScale, int variation)
        {
            CreateVisualCube("HullBody", hullRoot, new Vector3(0f, 0.54f, 0f) * hullScale, new Vector3(1.22f, 0.5f, 3.1f) * hullScale, bodyColor);
            CreateVisualCube("NoseProbe", hullRoot, new Vector3(0f, 0.66f, 1.42f) * hullScale, new Vector3(0.72f, 0.12f, 0.74f) * hullScale, trimColor);
            CreateVisualCube("SensorDeck", hullRoot, new Vector3(0f, 0.82f, -0.16f) * hullScale, new Vector3(0.76f, 0.14f, 1.48f) * hullScale, bodyColor * 0.92f);
            CreateVisualCube("LeftTrack", hullRoot, new Vector3(-0.82f, 0.32f, 0f) * hullScale, new Vector3(0.24f, 0.34f, 3.22f) * hullScale, bodyColor * 0.3f);
            CreateVisualCube("RightTrack", hullRoot, new Vector3(0.82f, 0.32f, 0f) * hullScale, new Vector3(0.24f, 0.34f, 3.22f) * hullScale, bodyColor * 0.3f);
            CreateVisualCube("TailBoomLeft", hullRoot, new Vector3(-0.42f, 0.78f, -1.18f) * hullScale, new Vector3(0.18f, 0.16f, 1.06f) * hullScale, trimColor * 0.86f);
            CreateVisualCube("TailBoomRight", hullRoot, new Vector3(0.42f, 0.78f, -1.18f) * hullScale, new Vector3(0.18f, 0.16f, 1.06f) * hullScale, trimColor * 0.86f);

            if (variation == 0)
            {
                CreateVisualCylinder("SensorMast", hullRoot, new Vector3(-0.28f, 1.12f, -0.78f) * hullScale, new Vector3(0.04f, 0.34f, 0.04f) * hullScale, trimColor);
                CreateVisualCube("SensorPod", hullRoot, new Vector3(0.22f, 1.14f, -0.18f) * hullScale, new Vector3(0.26f, 0.14f, 0.34f) * hullScale, trimColor);
            }
            else if (variation == 1)
            {
                CreateVisualCube("LeftFin", hullRoot, new Vector3(-0.58f, 0.92f, -1.82f) * hullScale, new Vector3(0.08f, 0.42f, 0.24f) * hullScale, trimColor);
                CreateVisualCube("RightFin", hullRoot, new Vector3(0.58f, 0.92f, -1.82f) * hullScale, new Vector3(0.08f, 0.42f, 0.24f) * hullScale, trimColor);
            }
            else
            {
                CreateVisualCube("ScanArrayLeft", hullRoot, new Vector3(-0.36f, 0.94f, 0.78f) * hullScale, new Vector3(0.16f, 0.12f, 0.48f) * hullScale, trimColor * 0.9f);
                CreateVisualCube("ScanArrayRight", hullRoot, new Vector3(0.36f, 0.94f, 0.78f) * hullScale, new Vector3(0.16f, 0.12f, 0.48f) * hullScale, trimColor * 0.9f);
            }
        }

        private static void BuildStandardTurret(Transform turretPivot, Color bodyColor, Color trimColor, float turretScale, int variation)
        {
            CreateVisualCube("TurretBase", turretPivot, new Vector3(0f, 0f, 0.1f) * turretScale, new Vector3(1.25f, 0.42f, 1.5f) * turretScale, bodyColor * 0.82f, keepCollider: true);
            CreateVisualCube("TurretTop", turretPivot, new Vector3(0f, 0.22f, -0.05f) * turretScale, new Vector3(0.9f, 0.18f, 0.95f) * turretScale, trimColor);
            CreateVisualCube("TurretCheekLeft", turretPivot, new Vector3(-0.42f, 0.05f, 0.22f) * turretScale, new Vector3(0.18f, 0.2f, 0.55f) * turretScale, bodyColor * 0.75f);
            CreateVisualCube("TurretCheekRight", turretPivot, new Vector3(0.42f, 0.05f, 0.22f) * turretScale, new Vector3(0.18f, 0.2f, 0.55f) * turretScale, bodyColor * 0.75f);

            if (variation == 0)
            {
                CreateVisualCube("TurretBustle", turretPivot, new Vector3(0f, 0.04f, -0.92f) * turretScale, new Vector3(0.72f, 0.24f, 0.48f) * turretScale, trimColor * 0.9f);
            }
            else if (variation == 2)
            {
                CreateVisualCylinder("TurretAntenna", turretPivot, new Vector3(-0.32f, 0.42f, -0.58f) * turretScale, new Vector3(0.04f, 0.3f, 0.04f) * turretScale, trimColor);
            }
        }

        private static void BuildRaiderTurret(Transform turretPivot, Color bodyColor, Color trimColor, float turretScale, int variation)
        {
            CreateVisualCube("TurretBase", turretPivot, new Vector3(0f, 0f, 0.18f) * turretScale, new Vector3(0.88f, 0.28f, 1.08f) * turretScale, bodyColor * 0.82f, keepCollider: true);
            CreateVisualCube("TurretCanopy", turretPivot, new Vector3(0f, 0.18f, -0.02f) * turretScale, new Vector3(0.58f, 0.14f, 0.56f) * turretScale, trimColor);
            CreateVisualCube("TurretNose", turretPivot, new Vector3(0f, 0.04f, 0.72f) * turretScale, new Vector3(0.44f, 0.16f, 0.34f) * turretScale, bodyColor * 0.74f, keepCollider: true);

            if (variation == 0)
            {
                CreateVisualCube("SensorStub", turretPivot, new Vector3(0.3f, 0.28f, -0.34f) * turretScale, new Vector3(0.16f, 0.14f, 0.2f) * turretScale, trimColor);
            }
            else if (variation == 1)
            {
                CreateVisualCube("RearCounterweight", turretPivot, new Vector3(0f, -0.02f, -0.62f) * turretScale, new Vector3(0.42f, 0.16f, 0.26f) * turretScale, bodyColor * 0.6f);
            }
            else
            {
                CreateVisualCylinder("ScoutAntenna", turretPivot, new Vector3(-0.24f, 0.36f, -0.22f) * turretScale, new Vector3(0.035f, 0.28f, 0.035f) * turretScale, trimColor);
            }
        }

        private static void BuildBulwarkTurret(Transform turretPivot, Color bodyColor, Color trimColor, float turretScale, int variation)
        {
            CreateVisualCube("TurretBase", turretPivot, new Vector3(0f, 0.04f, 0.06f) * turretScale, new Vector3(1.62f, 0.58f, 1.78f) * turretScale, bodyColor * 0.82f, keepCollider: true);
            CreateVisualCube("TurretTop", turretPivot, new Vector3(0f, 0.34f, -0.12f) * turretScale, new Vector3(1.08f, 0.2f, 1.08f) * turretScale, trimColor);
            CreateVisualCube("TurretShieldLeft", turretPivot, new Vector3(-0.64f, 0.1f, 0.34f) * turretScale, new Vector3(0.28f, 0.3f, 0.72f) * turretScale, bodyColor * 0.72f);
            CreateVisualCube("TurretShieldRight", turretPivot, new Vector3(0.64f, 0.1f, 0.34f) * turretScale, new Vector3(0.28f, 0.3f, 0.72f) * turretScale, bodyColor * 0.72f);
            CreateVisualCube("Mantlet", turretPivot, new Vector3(0f, 0.06f, 0.92f) * turretScale, new Vector3(0.74f, 0.34f, 0.34f) * turretScale, trimColor, keepCollider: true);

            if (variation == 0)
            {
                CreateVisualCube("TurretRearRack", turretPivot, new Vector3(0f, 0.02f, -1.12f) * turretScale, new Vector3(0.98f, 0.28f, 0.44f) * turretScale, bodyColor * 0.56f);
            }
            else if (variation == 1)
            {
                CreateVisualCube("TurretTopBlockLeft", turretPivot, new Vector3(-0.36f, 0.62f, -0.12f) * turretScale, new Vector3(0.24f, 0.18f, 0.32f) * turretScale, trimColor);
                CreateVisualCube("TurretTopBlockRight", turretPivot, new Vector3(0.36f, 0.62f, -0.12f) * turretScale, new Vector3(0.24f, 0.18f, 0.32f) * turretScale, trimColor);
            }
            else
            {
                CreateVisualCylinder("HeavyAntenna", turretPivot, new Vector3(0.5f, 0.58f, -0.6f) * turretScale, new Vector3(0.05f, 0.34f, 0.05f) * turretScale, trimColor);
            }
        }

        private static void BuildArtilleryTurret(Transform turretPivot, Color bodyColor, Color trimColor, float turretScale, int variation)
        {
            CreateVisualCube("TurretBase", turretPivot, new Vector3(0f, 0.06f, 0.06f) * turretScale, new Vector3(1.22f, 0.42f, 2.05f) * turretScale, bodyColor * 0.8f, keepCollider: true);
            CreateVisualCube("GunHousing", turretPivot, new Vector3(0f, 0.18f, 0.74f) * turretScale, new Vector3(0.74f, 0.22f, 0.86f) * turretScale, trimColor);
            CreateVisualCube("RearCounterweight", turretPivot, new Vector3(0f, 0.12f, -1.24f) * turretScale, new Vector3(0.92f, 0.26f, 0.62f) * turretScale, bodyColor * 0.64f);

            if (variation == 0)
            {
                CreateVisualCube("LeftRangeBox", turretPivot, new Vector3(-0.52f, 0.28f, -0.46f) * turretScale, new Vector3(0.18f, 0.16f, 0.42f) * turretScale, trimColor);
            }
            else if (variation == 1)
            {
                CreateVisualCube("TopSight", turretPivot, new Vector3(0f, 0.46f, 0.12f) * turretScale, new Vector3(0.28f, 0.14f, 0.44f) * turretScale, trimColor);
            }
            else
            {
                CreateVisualCylinder("RearAntenna", turretPivot, new Vector3(0.44f, 0.48f, -0.92f) * turretScale, new Vector3(0.045f, 0.34f, 0.045f) * turretScale, trimColor);
            }
        }

        private static void BuildStrikerTurret(Transform turretPivot, Color bodyColor, Color trimColor, float turretScale, int variation)
        {
            CreateVisualCube("TurretBase", turretPivot, new Vector3(0f, 0.04f, 0.1f) * turretScale, new Vector3(1.16f, 0.34f, 1.22f) * turretScale, bodyColor * 0.8f, keepCollider: true);
            CreateVisualCube("TurretMantlet", turretPivot, new Vector3(0f, 0.08f, 0.72f) * turretScale, new Vector3(0.62f, 0.24f, 0.3f) * turretScale, trimColor, keepCollider: true);
            CreateVisualCube("LeftCheek", turretPivot, new Vector3(-0.42f, 0.12f, 0.18f) * turretScale, new Vector3(0.22f, 0.22f, 0.56f) * turretScale, bodyColor * 0.72f);
            CreateVisualCube("RightCheek", turretPivot, new Vector3(0.42f, 0.12f, 0.18f) * turretScale, new Vector3(0.22f, 0.22f, 0.56f) * turretScale, bodyColor * 0.72f);

            if (variation == 0)
            {
                CreateVisualCube("BurstPackLeft", turretPivot, new Vector3(-0.28f, 0.34f, -0.3f) * turretScale, new Vector3(0.22f, 0.16f, 0.24f) * turretScale, trimColor);
                CreateVisualCube("BurstPackRight", turretPivot, new Vector3(0.28f, 0.34f, -0.3f) * turretScale, new Vector3(0.22f, 0.16f, 0.24f) * turretScale, trimColor);
            }
            else if (variation == 1)
            {
                CreateVisualCube("RearBrace", turretPivot, new Vector3(0f, 0.02f, -0.74f) * turretScale, new Vector3(0.58f, 0.14f, 0.26f) * turretScale, bodyColor * 0.58f);
            }
            else
            {
                CreateVisualCylinder("AssaultAntenna", turretPivot, new Vector3(-0.26f, 0.42f, -0.42f) * turretScale, new Vector3(0.04f, 0.26f, 0.04f) * turretScale, trimColor);
            }
        }

        private static void BuildScoutTurret(Transform turretPivot, Color bodyColor, Color trimColor, float turretScale, int variation)
        {
            CreateVisualCube("TurretBase", turretPivot, new Vector3(0f, 0.02f, 0.1f) * turretScale, new Vector3(0.72f, 0.24f, 1.18f) * turretScale, bodyColor * 0.82f, keepCollider: true);
            CreateVisualCube("SensorCanopy", turretPivot, new Vector3(0f, 0.18f, -0.08f) * turretScale, new Vector3(0.44f, 0.12f, 0.48f) * turretScale, trimColor);
            CreateVisualCube("NoseBlock", turretPivot, new Vector3(0f, 0.04f, 0.72f) * turretScale, new Vector3(0.32f, 0.12f, 0.28f) * turretScale, bodyColor * 0.74f, keepCollider: true);

            if (variation == 0)
            {
                CreateVisualCylinder("DishMast", turretPivot, new Vector3(0.18f, 0.34f, -0.42f) * turretScale, new Vector3(0.035f, 0.24f, 0.035f) * turretScale, trimColor);
            }
            else if (variation == 1)
            {
                CreateVisualCube("LeftSensorEar", turretPivot, new Vector3(-0.34f, 0.18f, -0.1f) * turretScale, new Vector3(0.12f, 0.12f, 0.26f) * turretScale, trimColor);
                CreateVisualCube("RightSensorEar", turretPivot, new Vector3(0.34f, 0.18f, -0.1f) * turretScale, new Vector3(0.12f, 0.12f, 0.26f) * turretScale, trimColor);
            }
            else
            {
                CreateVisualCube("RearLink", turretPivot, new Vector3(0f, 0.02f, -0.62f) * turretScale, new Vector3(0.34f, 0.1f, 0.22f) * turretScale, bodyColor * 0.56f);
            }
        }

        private static Transform CreateVisualCube(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            bool keepCollider = false)
        {
            GameObject cube = RuntimePrimitiveVisuals.CreatePrimitive(
                PrimitiveType.Cube,
                name,
                parent,
                localPosition,
                localScale,
                color,
                RuntimeMaterialKind.Opaque,
                keepCollider);

            return cube.transform;
        }

        private static Transform CreateVisualCylinder(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            bool keepCollider = false)
        {
            GameObject cylinder = RuntimePrimitiveVisuals.CreatePrimitive(
                PrimitiveType.Cylinder,
                name,
                parent,
                localPosition,
                localScale,
                color,
                RuntimeMaterialKind.Opaque,
                keepCollider);

            return cylinder.transform;
        }

        private enum TankFrameStyle
        {
            Standard = 0,
            Raider = 1,
            Bulwark = 2,
            Artillery = 3,
            Striker = 4,
            Scout = 5
        }
    }
}
