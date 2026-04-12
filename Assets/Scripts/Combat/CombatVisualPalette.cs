using UnityEngine;

namespace Tanks.Combat
{
    public static class CombatVisualPalette
    {
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

        public static Color GetMuzzleFlashColor(Team team)
        {
            return team switch
            {
                Team.Player => new Color(1f, 0.95f, 0.55f),
                Team.Enemy => new Color(1f, 0.6f, 0.3f),
                _ => Color.white
            };
        }
    }
}
