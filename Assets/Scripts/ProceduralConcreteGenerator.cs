using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ProceduralConcreteGenerator
{
#if UNITY_EDITOR
    [MenuItem("Tools/Generate Procedural Concrete")]
    public static void GenerateConcreteFromMenu()
    {
        Generate(1024, "Assets/Textures");
    }
#endif

    public static Material Generate(int size = 1024, string outputFolder = "Assets/Textures")
    {
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // 1. Generate heightmap with multi-octave seamless noise
        float[,] heightMap = new float[size, size];
        float minH = float.MaxValue;
        float maxH = float.MinValue;

        int octaves = 6;
        float persistence = 0.52f;
        float lacunarity = 2.05f;

        // Base frequencies
        float baseFreq = 3.5f;

        for (int y = 0; y < size; y++)
        {
            float v = (float)y / size;
            float angleV = v * Mathf.PI * 2f;
            float sinV = Mathf.Sin(angleV);
            float cosV = Mathf.Cos(angleV);

            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float angleU = u * Mathf.PI * 2f;
                float sinU = Mathf.Sin(angleU);
                float cosU = Mathf.Cos(angleU);

                float amplitude = 1f;
                float freq = baseFreq;
                float value = 0f;
                float totalAmp = 0f;

                // 4D torus mapping for 100% seamless tiling
                for (int o = 0; o < octaves; o++)
                {
                    float nx = cosU * freq;
                    float ny = sinU * freq;
                    float nz = cosV * freq;
                    float nw = sinV * freq;

                    // Pseudo-4D periodic noise using combined phase-shifted sine/perlin waves
                    float n = Mathf.PerlinNoise(nx * 0.7f + 12.3f + o * 7.1f, ny * 0.7f + 45.6f) * 0.5f +
                              Mathf.PerlinNoise(nz * 0.7f + 78.9f + o * 3.3f, nw * 0.7f + 98.7f) * 0.5f;

                    // Add high-frequency speckle/grit on higher octaves
                    if (o >= 3)
                    {
                        float grit = Mathf.PerlinNoise(nx * 3f + 100f, nz * 3f + 200f);
                        n = Mathf.Lerp(n, grit, 0.4f);
                    }

                    value += n * amplitude;
                    totalAmp += amplitude;
                    amplitude *= persistence;
                    freq *= lacunarity;
                }

                value /= totalAmp;

                // Subtle micro-porosity (tiny air holes / pores in concrete)
                float poreNoise = Mathf.PerlinNoise(cosU * 18f + 50f, sinV * 18f + 60f);
                if (poreNoise > 0.78f)
                {
                    float depth = (poreNoise - 0.78f) / 0.22f;
                    value -= depth * 0.15f;
                }

                heightMap[x, y] = value;
                if (value < minH) minH = value;
                if (value > maxH) maxH = value;
            }
        }

        // Normalize heightmap
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                heightMap[x, y] = Mathf.Clamp01((heightMap[x, y] - minH) / (maxH - minH));
            }
        }

        // 2. Build Albedo Texture
        Texture2D albedoTex = new Texture2D(size, size, TextureFormat.RGB24, true);
        Color baseConcrete = new Color(0.72f, 0.73f, 0.73f); // Architectural light grey
        Color darkCrevice = new Color(0.48f, 0.49f, 0.50f);  // Pores and subtle shadows
        Color lightAggregate = new Color(0.82f, 0.83f, 0.83f); // Mineral highlights

        Color[] albedoPixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float h = heightMap[x, y];
                Color col;
                if (h < 0.5f)
                {
                    col = Color.Lerp(darkCrevice, baseConcrete, h * 2f);
                }
                else
                {
                    col = Color.Lerp(baseConcrete, lightAggregate, (h - 0.5f) * 2f);
                }

                // Add subtle grain noise
                float grain = (Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f) % 1f;
                col += new Color(grain, grain, grain) * 0.025f;

                albedoPixels[y * size + x] = col;
            }
        }

        albedoTex.SetPixels(albedoPixels);
        albedoTex.Apply();

        // 3. Build Normal Map using Sobel Filter with periodic boundary wrapping
        Texture2D normalTex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        Color[] normalPixels = new Color[size * size];
        float bumpStrength = 4.5f;

        for (int y = 0; y < size; y++)
        {
            int yPrev = (y - 1 + size) % size;
            int yNext = (y + 1) % size;

            for (int x = 0; x < size; x++)
            {
                int xPrev = (x - 1 + size) % size;
                int xNext = (x + 1) % size;

                // 3x3 Sobel kernels
                float tl = heightMap[xPrev, yNext];
                float t  = heightMap[x,     yNext];
                float tr = heightMap[xNext, yNext];

                float l  = heightMap[xPrev, y];
                float r  = heightMap[xNext, y];

                float bl = heightMap[xPrev, yPrev];
                float b  = heightMap[x,     yPrev];
                float br = heightMap[xNext, yPrev];

                float dX = (tr + 2f * r + br) - (tl + 2f * l + bl);
                float dY = (tl + 2f * t + tr) - (bl + 2f * b + br);

                Vector3 normal = new Vector3(-dX * bumpStrength, -dY * bumpStrength, 1f).normalized;

                // Pack normal to [0, 1]
                float nr = normal.x * 0.5f + 0.5f;
                float ng = normal.y * 0.5f + 0.5f;
                float nb = normal.z * 0.5f + 0.5f;

                normalPixels[y * size + x] = new Color(nr, ng, nb, 1f);
            }
        }

        normalTex.SetPixels(normalPixels);
        normalTex.Apply();

        // 4. Save PNG files to disk
        string albedoPath = Path.Combine(outputFolder, "Procedural_Concrete_Albedo.png");
        string normalPath = Path.Combine(outputFolder, "Procedural_Concrete_Normal.png");

        File.WriteAllBytes(albedoPath, albedoTex.EncodeToPNG());
        File.WriteAllBytes(normalPath, normalTex.EncodeToPNG());

#if UNITY_EDITOR
        AssetDatabase.Refresh();

        // Configure normal map texture importer
        var importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }

        var albedoImporter = AssetImporter.GetAtPath(albedoPath) as TextureImporter;
        if (albedoImporter != null)
        {
            albedoImporter.wrapMode = TextureWrapMode.Repeat;
            albedoImporter.SaveAndReimport();
        }

        // 5. Create / Update URP Lit Material
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        string matPath = Path.Combine(outputFolder, "M_Procedural_Concrete.mat");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(litShader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        Texture2D loadedAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
        Texture2D loadedNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

        mat.mainTexture = loadedAlbedo;
        mat.SetTexture("_BumpMap", loadedNormal);
        mat.EnableKeyword("_NORMALMAP");
        mat.SetFloat("_Smoothness", 0.12f); // Rough surface
        mat.SetFloat("_Cull", 0); // Double sided

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        Debug.Log("[ProceduralConcreteGenerator] Procedural concrete textures and material generated successfully at: " + outputFolder);
        return mat;
#else
        return null;
#endif
    }
}
