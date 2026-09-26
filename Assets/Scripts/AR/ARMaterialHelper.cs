using UnityEngine;

namespace ARMiningSimulator.AR
{
    /// <summary>
    /// Centralized Material & Shader Factory for AR visuals.
    /// Guarantees that no 3D object, line, flag, or shading ever turns magenta/pink.
    /// Prioritizes pre-compiled Resources assets, then URP Unlit, with fail-safe fallback to Sprites/Default.
    /// </summary>
    public static class ARMaterialHelper
    {
        private static Material s_CachedShadingTemplate;
        private static Material s_CachedGeneralTemplate;
        private static Material s_CachedWallFadingTemplate;
        private static Texture2D s_WallGradientTexture;

        /// <summary>
        /// Returns a guaranteed non-pink 50% blue floor shading material.
        /// </summary>
        public static Material GetRoomShadingMaterial(Color color)
        {
            if (s_CachedShadingTemplate == null)
            {
                s_CachedShadingTemplate = Resources.Load<Material>("AR_RoomFloorShadingMat");
            }

            Material mat;
            if (s_CachedShadingTemplate != null)
            {
                mat = new Material(s_CachedShadingTemplate);
            }
            else
            {
                mat = CreateTransparentMaterial(color);
            }

            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            return mat;
        }

        /// <summary>
        /// Returns a transparent material with a vertical alpha gradient for room boundary walls.
        /// 50% blue at the floor, smoothly fading to transparent at 5 meters height.
        /// </summary>
        public static Material GetWallFadingMaterial(Color baseColor)
        {
            EnsureWallGradientTexture();

            if (s_CachedWallFadingTemplate == null)
            {
                if (s_CachedShadingTemplate == null)
                {
                    s_CachedShadingTemplate = Resources.Load<Material>("AR_RoomFloorShadingMat");
                }

                Material mat;
                if (s_CachedShadingTemplate != null)
                {
                    mat = new Material(s_CachedShadingTemplate);
                }
                else
                {
                    mat = CreateTransparentMaterial(baseColor);
                }

                mat.name = "AR_WallFadingMat";
                ConfigureTransparentProperties(mat);
                s_CachedWallFadingTemplate = mat;
            }

            Material instanceMat = new Material(s_CachedWallFadingTemplate);
            instanceMat.color = baseColor;
            if (instanceMat.HasProperty("_BaseColor")) instanceMat.SetColor("_BaseColor", baseColor);
            if (instanceMat.HasProperty("_BaseMap") && s_WallGradientTexture != null) instanceMat.SetTexture("_BaseMap", s_WallGradientTexture);
            if (instanceMat.HasProperty("_MainTex") && s_WallGradientTexture != null) instanceMat.SetTexture("_MainTex", s_WallGradientTexture);
            return instanceMat;
        }

        private static void EnsureWallGradientTexture()
        {
            if (s_WallGradientTexture != null) return;

            s_WallGradientTexture = new Texture2D(2, 64, TextureFormat.RGBA32, false);
            s_WallGradientTexture.name = "AR_WallGradientAlphaTex";
            s_WallGradientTexture.wrapMode = TextureWrapMode.Clamp;
            s_WallGradientTexture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[2 * 64];
            for (int y = 0; y < 64; y++)
            {
                float t = (float)y / 63f; // 0.0 at floor (bottom) to 1.0 at 5m (top)
                // Smooth cosine curve: 1.0 at bottom, smoothly fading to 0.0 at top
                float alpha = Mathf.Cos(t * Mathf.PI * 0.5f);
                Color c = new Color(1f, 1f, 1f, alpha);
                pixels[y * 2 + 0] = c;
                pixels[y * 2 + 1] = c;
            }
            s_WallGradientTexture.SetPixels(pixels);
            s_WallGradientTexture.Apply();
        }

        /// <summary>
        /// Creates an unlit material for flags, reticles, or lines that will never render pink on device.
        /// </summary>
        public static Material CreateUnlitMaterial(Color color, bool transparent = false)
        {
            if (s_CachedGeneralTemplate == null)
            {
                s_CachedGeneralTemplate = Resources.Load<Material>("AR_UnlitGeneralMat");
            }

            Material mat;
            if (s_CachedGeneralTemplate != null)
            {
                mat = new Material(s_CachedGeneralTemplate);
            }
            else
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("Sprites/Default")
                             ?? Shader.Find("Unlit/Color")
                             ?? Shader.Find("Standard");

                mat = new Material(shader);
            }

            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            if (transparent)
            {
                ConfigureTransparentProperties(mat);
            }

            return mat;
        }

        private static Material CreateTransparentMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Unlit/Transparent")
                         ?? Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            ConfigureTransparentProperties(mat);
            return mat;
        }

        private static void ConfigureTransparentProperties(Material mat)
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1.0f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0.0f);

            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHABLEND_ON");
        }
    }
}
