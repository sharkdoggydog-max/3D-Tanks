using UnityEngine;

namespace Tanks.Enemy
{
    public static class EnemyVariantLibrary
    {
        public static EnemyVariant PickVariant(int level, int enemyIndex)
        {
            if (level <= 1)
            {
                return EnemyVariant.Basic;
            }

            if (level == 2)
            {
                return enemyIndex % 3 == 0 ? EnemyVariant.Raider : EnemyVariant.Basic;
            }

            if (enemyIndex % 5 == 0)
            {
                return EnemyVariant.Bulwark;
            }

            if (enemyIndex % 2 == 0)
            {
                return EnemyVariant.Raider;
            }

            return EnemyVariant.Basic;
        }

        public static EnemyArchetype CreateArchetype(EnemyVariant variant, int level)
        {
            float levelBonus = Mathf.Max(0, level - 1);

            return variant switch
            {
                EnemyVariant.Raider => new EnemyArchetype(
                    EnemyVariant.Raider,
                    "Raider",
                    new Color(0.86f, 0.45f, 0.16f),
                    new Color(1f, 0.87f, 0.48f),
                    2f + Mathf.Floor(levelBonus / 4f),
                    5.6f + levelBonus * 0.08f,
                    142f,
                    12f + levelBonus * 0.18f,
                    6.75f,
                    0.52f,
                    24f + levelBonus * 0.4f,
                    0.82f,
                    0.2f,
                    3f,
                    0.86f,
                    0.78f,
                    1.2f,
                    0.82f,
                    290f,
                    4.4f,
                    14f,
                    0.08f,
                    1f,
                    0.72f,
                    1f,
                    1f),

                EnemyVariant.Bulwark => new EnemyArchetype(
                    EnemyVariant.Bulwark,
                    "Bulwark",
                    new Color(0.34f, 0.42f, 0.58f),
                    new Color(0.8f, 0.9f, 1f),
                    5f + Mathf.Floor(levelBonus / 2f),
                    3.2f + levelBonus * 0.04f,
                    95f,
                    16.5f + levelBonus * 0.3f,
                    12f,
                    1.24f,
                    16.8f + levelBonus * 0.18f,
                    1.65f,
                    0.34f,
                    3.9f,
                    1.18f,
                    1.26f,
                    0.92f,
                    1.35f,
                    135f,
                    8.4f,
                    7f,
                    0.36f,
                    0.05f,
                    0f,
                    0.48f,
                    0.72f),

                _ => new EnemyArchetype(
                    EnemyVariant.Basic,
                    "Basic",
                    new Color(0.72f, 0.2f, 0.16f),
                    new Color(1f, 0.72f, 0.62f),
                    3f + Mathf.Floor(levelBonus / 3f),
                    4f + levelBonus * 0.05f,
                    110f,
                    14f + levelBonus * 0.25f,
                    9f,
                    0.85f,
                    20f + levelBonus * 0.25f,
                    1f,
                    0.24f,
                    3f,
                    1f,
                    1f,
                    1f,
                    1f,
                    220f,
                    5.8f,
                    10f,
                    0.18f,
                    0.3f,
                    0.2f,
                    0.68f,
                    0.9f)
            };
        }
    }
}
