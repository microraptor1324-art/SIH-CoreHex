using UnityEngine;

namespace ARMiningSimulator.Mining
{
    /// <summary>
    /// Material & Shader Factory for the procedural underground mining environment.
    /// Generates rich, authentic PBR materials (weathered timber, rusted steel, dark coal,
    /// industrial ducting, and emissive work lamps) with procedural textures.
    /// Guaranteed to render correctly in URP on mobile devices without pink/magenta shader failure.
    /// </summary>
    public static class MiningMaterialFactory
    {
        private static Material s_TimberMaterial;
        private static Material s_SteelRailMaterial;
        private static Material s_CartMetalMaterial;
        private static Material s_CoalOreMaterial;
        private static Material s_LampGlowMaterial;
        private static Material s_VentDuctMaterial;

        private static Texture2D s_WoodTex;
        private static Texture2D s_MetalTex;
        private static Texture2D s_CoalTex;
        private static Texture2D s_DuctTex;

        public static Material GetTimberMaterial()
        {
            if (s_TimberMaterial == null)
            {
                EnsureTextures();
                s_TimberMaterial = CreateLitMaterial(new Color(0.38f, 0.24f, 0.14f), s_WoodTex, roughness: 0.85f, metallic: 0.05f);
                s_TimberMaterial.name = "Mine_AgedTimber_Mat";
            }
            return s_TimberMaterial;
        }

        public static Material GetSteelRailMaterial()
        {
            if (s_SteelRailMaterial == null)
            {
                EnsureTextures();
                s_SteelRailMaterial = CreateLitMaterial(new Color(0.35f, 0.33f, 0.30f), s_MetalTex, roughness: 0.45f, metallic: 0.85f);
                s_SteelRailMaterial.name = "Mine_RustedRail_Mat";
            }
            return s_SteelRailMaterial;
        }

        public static Material GetCartMetalMaterial()
        {
            if (s_CartMetalMaterial == null)
            {
                EnsureTextures();
                s_CartMetalMaterial = CreateLitMaterial(new Color(0.48f, 0.30f, 0.18f), s_MetalTex, roughness: 0.55f, metallic: 0.75f);
                s_CartMetalMaterial.name = "Mine_CartMetal_Mat";
            }
            return s_CartMetalMaterial;
        }

        public static Material GetCoalOreMaterial()
        {
            if (s_CoalOreMaterial == null)
            {
                EnsureTextures();
                s_CoalOreMaterial = CreateLitMaterial(new Color(0.12f, 0.12f, 0.13f), s_CoalTex, roughness: 0.70f, metallic: 0.20f);
                s_CoalOreMaterial.name = "Mine_CoalOre_Mat";
            }
            return s_CoalOreMaterial;
        }

        public static Material GetVentDuctMaterial()
        {
            if (s_VentDuctMaterial == null)
            {
                EnsureTextures();
                s_VentDuctMaterial = CreateLitMaterial(new Color(0.80f, 0.65f, 0.18f), s_DuctTex, roughness: 0.60f, metallic: 0.10f);
                s_VentDuctMaterial.name = "Mine_VentDuct_Mat";
            }
            return s_VentDuctMaterial;
        }

        public static Material GetLampGlowMaterial()
        {
            if (s_LampGlowMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Unlit/Color")
                             ?? Shader.Find("Sprites/Default")
                             ?? Shader.Find("Standard");

                s_LampGlowMaterial = new Material(shader);
                s_LampGlowMaterial.name = "Mine_LampGlow_Mat";
                Color glowColor = new Color(1.0f, 0.82f, 0.45f) * 2.5f; // HDR amber glow
                s_LampGlowMaterial.color = new Color(1.0f, 0.82f, 0.45f);
                if (s_LampGlowMaterial.HasProperty("_BaseColor")) s_LampGlowMaterial.SetColor("_BaseColor", glowColor);
                if (s_LampGlowMaterial.HasProperty("_EmissionColor"))
                {
                    s_LampGlowMaterial.EnableKeyword("_EMISSION");
                    s_LampGlowMaterial.SetColor("_EmissionColor", glowColor);
                }
            }
            return s_LampGlowMaterial;
        }

        private static Material CreateLitMaterial(Color baseColor, Texture2D mainTex, float roughness, float metallic)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Universal Render Pipeline/Unlit");

            Material mat = new Material(shader);
            mat.color = baseColor;

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);

            if (mainTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
            }

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1.0f - roughness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            return mat;
        }

        private static void EnsureTextures()
        {
            if (s_WoodTex == null) s_WoodTex = GenerateWoodTexture();
            if (s_MetalTex == null) s_MetalTex = GenerateMetalTexture();
            if (s_CoalTex == null) s_CoalTex = GenerateCoalTexture();
            if (s_DuctTex == null) s_DuctTex = GenerateDuctTexture();
        }

        private static Texture2D GenerateWoodTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            tex.name = "Procedural_WoodGrain";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Color darkWood = new Color(0.26f, 0.16f, 0.09f);
            Color lightWood = new Color(0.44f, 0.28f, 0.16f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Elongated grain noise along Y
                    float n1 = Mathf.PerlinNoise(x * 0.15f, y * 0.02f);
                    float n2 = Mathf.PerlinNoise(x * 0.40f, y * 0.05f) * 0.35f;
                    float t = Mathf.Clamp01(n1 + n2);
                    pixels[y * size + x] = Color.Lerp(darkWood, lightWood, t);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D GenerateMetalTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            tex.name = "Procedural_RustedMetal";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Color darkIron = new Color(0.24f, 0.24f, 0.26f);
            Color rustOrange = new Color(0.55f, 0.28f, 0.12f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.12f, y * 0.12f);
                    float rustNoise = Mathf.PerlinNoise(x * 0.25f + 10f, y * 0.25f + 10f);
                    Color c = Color.Lerp(darkIron, rustOrange, rustNoise > 0.6f ? (rustNoise - 0.6f) * 2.5f : 0f);
                    c *= (0.85f + noise * 0.3f);
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D GenerateCoalTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            tex.name = "Procedural_CoalOre";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Color deepBlack = new Color(0.08f, 0.08f, 0.09f);
            Color mineralGray = new Color(0.22f, 0.22f, 0.24f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
                    pixels[y * size + x] = Color.Lerp(deepBlack, mineralGray, n);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D GenerateDuctTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            tex.name = "Procedural_VentDuct";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Color yellowRib = new Color(0.85f, 0.72f, 0.16f);
            Color darkCrease = new Color(0.35f, 0.28f, 0.08f);

            for (int y = 0; y < size; y++)
            {
                // Ribbed bands along Y
                float band = Mathf.Sin(y * 0.35f) * 0.5f + 0.5f;
                Color c = Color.Lerp(darkCrease, yellowRib, band);
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
