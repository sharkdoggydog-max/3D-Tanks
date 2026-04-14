using Tanks.Enemy;
using UnityEngine;

namespace Tanks.Core
{
    public class RunProgression
    {
        private const float BasePlayerMaxHealth = 5f;
        private const float MobilePlayerHealthBonus = 2f;
        private const float BaseWeaponCooldown = 0.3f;
        private const float BaseProjectileSpeed = 28f;
        private const float BaseProjectileLifetime = 2.4f;
        private const float BaseProjectileRadius = 0.3f;

        public int CurrentLevel { get; private set; } = 1;
        public int HullUpgrades { get; private set; }
        public int LoaderUpgrades { get; private set; }
        public int CannonUpgrades { get; private set; }
        public EnemyVariant SelectedPlayerTank { get; private set; } = EnemyVariant.Basic;
        public string LastUpgradeSummary { get; private set; } = "Base loadout ready.";

        public float PlayerMaxHealth => BasePlayerMaxHealth + HullUpgrades;
        public float PlayerWeaponCooldown => Mathf.Max(0.16f, BaseWeaponCooldown * Mathf.Pow(0.92f, LoaderUpgrades));
        public float PlayerProjectileSpeed => BaseProjectileSpeed + CannonUpgrades * 2.5f;
        public float PlayerProjectileLifetime => BaseProjectileLifetime + CannonUpgrades * 0.2f;
        public float PlayerProjectileRadius => BaseProjectileRadius;
        public string UpgradeStatus => $"Hull +{HullUpgrades} | Loader +{LoaderUpgrades} | Cannon +{CannonUpgrades}";

        public float GetPlayerMaxHealth(float baseMaxHealth, bool useMobileStartingHealth)
        {
            float currentMaxHealth = Mathf.Max(1f, baseMaxHealth) + HullUpgrades;
            return useMobileStartingHealth ? currentMaxHealth + MobilePlayerHealthBonus : currentMaxHealth;
        }

        public void SelectPlayerTank(EnemyVariant tankType)
        {
            SelectedPlayerTank = tankType;
        }

        public void Reset()
        {
            CurrentLevel = 1;
            HullUpgrades = 0;
            LoaderUpgrades = 0;
            CannonUpgrades = 0;
            LastUpgradeSummary = "Base loadout ready.";
        }

        public void AdvanceToNextLevel()
        {
            CurrentLevel++;
            ApplyNextUpgrade();
        }

        private void ApplyNextUpgrade()
        {
            switch ((CurrentLevel - 2) % 3)
            {
                case 0:
                    HullUpgrades++;
                    LastUpgradeSummary = "Upgrade: Reinforced Hull (+1 Max HP)";
                    break;
                case 1:
                    LoaderUpgrades++;
                    LastUpgradeSummary = "Upgrade: Faster Loader (better fire rate)";
                    break;
                default:
                    CannonUpgrades++;
                    LastUpgradeSummary = "Upgrade: Cannon Tuning (faster shells)";
                    break;
            }
        }
    }
}
