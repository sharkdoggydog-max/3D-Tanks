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

            if (level == 3)
            {
                if (enemyIndex % 4 == 0)
                {
                    return EnemyVariant.Striker;
                }

                return enemyIndex % 2 == 0 ? EnemyVariant.Raider : EnemyVariant.Basic;
            }

            if (level == 4)
            {
                if (enemyIndex % 5 == 0)
                {
                    return EnemyVariant.Bulwark;
                }

                if (enemyIndex % 3 == 0)
                {
                    return EnemyVariant.Scout;
                }

                return enemyIndex % 2 == 0 ? EnemyVariant.Striker : EnemyVariant.Raider;
            }

            if (enemyIndex % 7 == 0)
            {
                return EnemyVariant.Artillery;
            }

            if (enemyIndex % 5 == 0)
            {
                return EnemyVariant.Bulwark;
            }

            if (enemyIndex % 4 == 0)
            {
                return EnemyVariant.Scout;
            }

            if (enemyIndex % 3 == 0)
            {
                return EnemyVariant.Striker;
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
                    0f,
                    0f,
                    1,
                    0f,
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
                    0f,
                    0f,
                    1,
                    0f,
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

                EnemyVariant.Artillery => new EnemyArchetype(
                    EnemyVariant.Artillery,
                    "Artillery",
                    new Color(0.46f, 0.42f, 0.28f),
                    new Color(1f, 0.78f, 0.42f),
                    3f + Mathf.Floor(levelBonus / 3f),
                    2.8f + levelBonus * 0.03f,
                    80f,
                    22f + levelBonus * 0.35f,
                    17.5f,
                    1.8f,
                    18.6f + levelBonus * 0.14f,
                    1.6f,
                    0.33f,
                    4.6f,
                    2.25f,
                    0.62f,
                    1,
                    0f,
                    1.08f,
                    0.88f,
                    1.72f,
                    1.08f,
                    92f,
                    12f,
                    6f,
                    0.22f,
                    0.05f,
                    0f,
                    0.44f,
                    0.5f),

                EnemyVariant.Striker => new EnemyArchetype(
                    EnemyVariant.Striker,
                    "Striker",
                    new Color(0.7f, 0.22f, 0.16f),
                    new Color(1f, 0.7f, 0.42f),
                    4f + Mathf.Floor(levelBonus / 3f),
                    4.95f + levelBonus * 0.06f,
                    134f,
                    10.8f + levelBonus * 0.14f,
                    5.8f,
                    1.15f,
                    22.5f + levelBonus * 0.25f,
                    0.8f,
                    0.26f,
                    2.7f,
                    0f,
                    0f,
                    2,
                    0.12f,
                    1.02f,
                    0.9f,
                    0.92f,
                    1.22f,
                    215f,
                    4.2f,
                    15f,
                    0.05f,
                    0.2f,
                    0.5f,
                    0.92f,
                    1.14f),

                EnemyVariant.Scout => new EnemyArchetype(
                    EnemyVariant.Scout,
                    "Scout",
                    new Color(0.22f, 0.36f, 0.56f),
                    new Color(0.68f, 1f, 0.95f),
                    2f + Mathf.Floor(levelBonus / 5f),
                    6.8f + levelBonus * 0.1f,
                    168f,
                    15.8f + levelBonus * 0.2f,
                    11.8f,
                    0.48f,
                    23.5f + levelBonus * 0.35f,
                    0.58f,
                    0.18f,
                    3.2f,
                    0f,
                    0f,
                    1,
                    0f,
                    0.74f,
                    0.66f,
                    1.36f,
                    0.68f,
                    330f,
                    6.6f,
                    16f,
                    0.05f,
                    1.15f,
                    0.82f,
                    1f,
                    0.84f),

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
                    0f,
                    0f,
                    1,
                    0f,
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
