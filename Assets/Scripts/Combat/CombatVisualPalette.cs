using UnityEngine;

using UnityEngine.Rendering;

namespace Tanks.Combat
{
    public enum ProjectileStyle
    {
        Player = 0,
        BasicEnemy = 1,
        RaiderEnemy = 2,
        BulwarkEnemy = 3
    }

    public static class CombatVisualPalette
    {
        private static Shader opaqueRuntimeShader;
        private static Shader transparentRuntimeShader;

        public static Color GetProjectileColor(ProjectileStyle style, Team team)
        {
            return style switch
            {
                ProjectileStyle.Player => new Color(0.95f, 0.88f, 0.25f),
                ProjectileStyle.RaiderEnemy => new Color(1f, 0.82f, 0.22f),
                ProjectileStyle.BulwarkEnemy => new Color(0.7f, 0.88f, 1f),
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

        public static void ApplyRuntimeMaterial(Renderer renderer, Color color, bool transparent = false)
        {
            if (renderer == null)
            {
                return;
            }

            Material material = new(CreateRuntimeShader(transparent));
            ConfigureMaterial(material, color, transparent);
            renderer.material = material;
        }

        public static void SetRuntimeMaterialColor(Renderer renderer, Color color)
        {
            if (renderer == null || renderer.material == null)
            {
                return;
            }

            ApplyColor(renderer.material, color);
        }

        private static Shader CreateRuntimeShader(bool transparent)
        {
            if (transparent)
            {
                transparentRuntimeShader ??= ResolveTransparentShader();
                return transparentRuntimeShader;
            }

            opaqueRuntimeShader ??= ResolveOpaqueShader();
            return opaqueRuntimeShader;
        }

        private static Shader ResolveOpaqueShader()
        {
            return ResolveShader(
                "Universal Render Pipeline/Unlit",
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Lit",
                "Mobile/Diffuse",
                "Unlit/Color",
                "Legacy Shaders/Diffuse",
                "Standard");
        }

        private static Shader ResolveTransparentShader()
        {
            return ResolveShader(
                "Universal Render Pipeline/Unlit",
                "Sprites/Default",
                "Unlit/Color",
                "Universal Render Pipeline/Simple Lit",
                "Legacy Shaders/Transparent/Diffuse",
                "Standard");
        }

        private static Shader ResolveShader(params string[] shaderNames)
        {
            for (int index = 0; index < shaderNames.Length; index++)
            {
                Shader shader = Shader.Find(shaderNames[index]);
                if (shader != null)
                {
                    return shader;
                }
            }

            return Shader.Find("Hidden/InternalErrorShader");
        }

        private static void ConfigureMaterial(Material material, Color color, bool transparent)
        {
            if (material == null)
            {
                return;
            }

            ApplyColor(material, color);

            if (transparent)
            {
                material.renderQueue = 3000;
                if (material.HasFloat("_Surface"))
                {
                    material.SetFloat("_Surface", 1f);
                }

                if (material.HasFloat("_Blend"))
                {
                    material.SetFloat("_Blend", 0f);
                }

                if (material.HasInt("_SrcBlend"))
                {
                    material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                }

                if (material.HasInt("_DstBlend"))
                {
                    material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                }

                if (material.HasInt("_ZWrite"))
                {
                    material.SetInt("_ZWrite", 0);
                }

                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
            }
            else
            {
                if (material.HasFloat("_Surface"))
                {
                    material.SetFloat("_Surface", 0f);
                }

                if (material.HasInt("_ZWrite"))
                {
                    material.SetInt("_ZWrite", 1);
                }

                material.renderQueue = -1;
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Opaque");
            }

            if (material.HasFloat("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.05f);
            }

            if (material.HasFloat("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }
        }

        private static void ApplyColor(Material material, Color color)
        {
            if (material.HasColor("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasColor("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }
    }
}
