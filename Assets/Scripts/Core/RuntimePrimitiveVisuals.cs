using UnityEngine;

namespace Tanks.Core
{
    public enum RuntimeMaterialKind
    {
        Opaque = 0,
        Transparent = 1
    }

    public static class RuntimePrimitiveVisuals
    {
        private const string OpaqueMaterialResourcePath = "RuntimePrototypeLit";
        private const string TransparentMaterialResourcePath = "RuntimePrototypeFx";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly MaterialPropertyBlock ScratchPropertyBlock = new();

        private static Material opaqueTemplate;
        private static Material transparentTemplate;
        private static bool missingOpaqueTemplateLogged;
        private static bool missingTransparentTemplateLogged;

        public static GameObject CreatePrimitive(
            PrimitiveType primitiveType,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            RuntimeMaterialKind materialKind = RuntimeMaterialKind.Opaque,
            bool keepCollider = true)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;

            ConfigureRenderer(primitive.GetComponent<Renderer>(), materialKind, color);

            if (!keepCollider)
            {
                Collider collider = primitive.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.Destroy(collider);
                }
            }

            return primitive;
        }

        public static void ConfigureRenderer(Renderer renderer, RuntimeMaterialKind materialKind, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            Material template = GetTemplate(materialKind);
            if (template != null)
            {
                renderer.sharedMaterial = template;
            }

            SetColor(renderer, color);
        }

        public static void SetColor(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.GetPropertyBlock(ScratchPropertyBlock);
            ScratchPropertyBlock.SetColor(BaseColorId, color);
            ScratchPropertyBlock.SetColor(ColorId, color);
            renderer.SetPropertyBlock(ScratchPropertyBlock);
            ScratchPropertyBlock.Clear();
        }

        public static Color GetColor(Renderer renderer, Color fallback)
        {
            if (renderer == null)
            {
                return fallback;
            }

            renderer.GetPropertyBlock(ScratchPropertyBlock);

            Color blockColor = ScratchPropertyBlock.GetColor(BaseColorId);
            if (blockColor.maxColorComponent > 0f || blockColor.a > 0f)
            {
                ScratchPropertyBlock.Clear();
                return blockColor;
            }

            blockColor = ScratchPropertyBlock.GetColor(ColorId);
            ScratchPropertyBlock.Clear();
            if (blockColor.maxColorComponent > 0f || blockColor.a > 0f)
            {
                return blockColor;
            }

            Material sharedMaterial = renderer.sharedMaterial;
            if (sharedMaterial != null)
            {
                if (sharedMaterial.HasProperty(BaseColorId))
                {
                    return sharedMaterial.GetColor(BaseColorId);
                }

                if (sharedMaterial.HasProperty(ColorId))
                {
                    return sharedMaterial.GetColor(ColorId);
                }
            }

            return fallback;
        }

        private static Material GetTemplate(RuntimeMaterialKind materialKind)
        {
            switch (materialKind)
            {
                case RuntimeMaterialKind.Transparent:
                    if (transparentTemplate == null)
                    {
                        transparentTemplate = Resources.Load<Material>(TransparentMaterialResourcePath);
                        if (transparentTemplate == null && !missingTransparentTemplateLogged)
                        {
                            missingTransparentTemplateLogged = true;
                            Debug.LogError($"[RuntimePrimitiveVisuals] Missing material resource '{TransparentMaterialResourcePath}'.");
                        }
                    }

                    return transparentTemplate;

                default:
                    if (opaqueTemplate == null)
                    {
                        opaqueTemplate = Resources.Load<Material>(OpaqueMaterialResourcePath);
                        if (opaqueTemplate == null && !missingOpaqueTemplateLogged)
                        {
                            missingOpaqueTemplateLogged = true;
                            Debug.LogError($"[RuntimePrimitiveVisuals] Missing material resource '{OpaqueMaterialResourcePath}'.");
                        }
                    }

                    return opaqueTemplate;
            }
        }
    }
}
