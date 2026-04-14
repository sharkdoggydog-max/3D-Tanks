using UnityEngine;

namespace Tanks.Combat
{
    public enum ProjectileStyle
    {
        Player = 0,
        BasicEnemy = 1,
        RaiderEnemy = 2,
        BulwarkEnemy = 3,
        ArtilleryEnemy = 4,
        StrikerEnemy = 5,
        ScoutEnemy = 6
    }

    public static class CombatVisualPalette
    {
        public static Color GetProjectileColor(ProjectileStyle style, Team team)
        {
            return style switch
            {
                ProjectileStyle.Player => new Color(0.95f, 0.88f, 0.25f),
                ProjectileStyle.RaiderEnemy => new Color(1f, 0.82f, 0.22f),
                ProjectileStyle.BulwarkEnemy => new Color(0.7f, 0.88f, 1f),
                ProjectileStyle.ArtilleryEnemy => new Color(1f, 0.72f, 0.32f),
                ProjectileStyle.StrikerEnemy => new Color(1f, 0.52f, 0.32f),
                ProjectileStyle.ScoutEnemy => new Color(0.62f, 1f, 0.96f),
                ProjectileStyle.BasicEnemy => new Color(1f, 0.42f, 0.22f),
                _ => GetProjectileColor(team)
            };
        }

        public static Color GetProjectileColor(Team team)
        {
            return team switch
            {
                Team.Player => new Color(0.95f, 0.88f, 0.25f),
                Team.Enemy => new Color(1f, 0.42f, 0.22f),
                _ => new Color(0.85f, 0.85f, 0.85f)
            };
        }

        public static Color GetDamageFlashColor(Team team)
        {
            return team switch
            {
                Team.Player => new Color(1f, 0.92f, 0.5f),
                Team.Enemy => new Color(1f, 0.72f, 0.62f),
                _ => Color.white
            };
        }

        public static Color GetDeathColor(Team team)
        {
            return team switch
            {
                Team.Player => new Color(1f, 0.88f, 0.45f),
                Team.Enemy => new Color(1f, 0.48f, 0.25f),
                _ => new Color(0.9f, 0.9f, 0.9f)
            };
        }

        public static Color GetMuzzleFlashColor(ProjectileStyle style, Team team)
        {
            return style switch
            {
                ProjectileStyle.Player => new Color(1f, 0.95f, 0.55f),
                ProjectileStyle.RaiderEnemy => new Color(1f, 0.76f, 0.3f),
                ProjectileStyle.BulwarkEnemy => new Color(0.82f, 0.94f, 1f),
                ProjectileStyle.ArtilleryEnemy => new Color(1f, 0.84f, 0.46f),
                ProjectileStyle.StrikerEnemy => new Color(1f, 0.68f, 0.36f),
                ProjectileStyle.ScoutEnemy => new Color(0.78f, 1f, 0.98f),
                ProjectileStyle.BasicEnemy => new Color(1f, 0.6f, 0.3f),
                _ => GetMuzzleFlashColor(team)
            };
        }

        public static Color GetMuzzleFlashColor(Team team)
        {
            return team switch
            {
                Team.Player => new Color(1f, 0.95f, 0.55f),
                Team.Enemy => new Color(1f, 0.6f, 0.3f),
                _ => Color.white
            };
        }

        public static Color GetImpactColor(ProjectileStyle style, Team impactedTeam)
        {
            return style switch
            {
                ProjectileStyle.Player => new Color(1f, 0.9f, 0.35f),
                ProjectileStyle.RaiderEnemy => new Color(1f, 0.55f, 0.22f),
                ProjectileStyle.BulwarkEnemy => new Color(0.78f, 0.94f, 1f),
                ProjectileStyle.ArtilleryEnemy => new Color(1f, 0.82f, 0.38f),
                ProjectileStyle.StrikerEnemy => new Color(1f, 0.62f, 0.28f),
                ProjectileStyle.ScoutEnemy => new Color(0.72f, 1f, 0.94f),
                ProjectileStyle.BasicEnemy => new Color(1f, 0.72f, 0.32f),
                _ => GetImpactColor(impactedTeam)
            };
        }

        public static Color GetImpactColor(Team team)
        {
            return team switch
            {
                Team.Player => new Color(1f, 0.28f, 0.22f),
                Team.Enemy => new Color(1f, 0.9f, 0.35f),
                _ => new Color(0.92f, 0.92f, 0.92f)
            };
        }
    }
}
