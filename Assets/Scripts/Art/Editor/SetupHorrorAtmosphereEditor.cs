#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
using BKPureNature;

namespace GameJam.Art.Editor
{
    public static class SetupHorrorAtmosphereEditor
    {
        [MenuItem("GameJam/Setup All Lighting and PostFX")]
        public static void SetupAllLightingAndPostFX()
        {
            Debug.Log("[ArtSetup] Starting Horror Lighting & PostFX setup...");

            ConfigureGlobalPostProcessing();
            ConfigureEvMarketScene();
            ConfigureEndUnderRubbleScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ArtSetup] SUCCESS! All lighting, retro post-processing, and Pure Volumetric Fog have been configured and saved.");
        }

        private static void ConfigureGlobalPostProcessing()
        {
            // 1. Configure URP Asset Render Scale
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
            {
                urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            }

            if (urpAsset != null)
            {
                urpAsset.renderScale = 0.72f;
                EditorUtility.SetDirty(urpAsset);
                Debug.Log("[ArtSetup] URP Render Scale set to 0.72 for retro-horror feel.");
            }

            // 2. Configure DefaultVolumeProfile.asset
            string profilePath = "Assets/Settings/DefaultVolumeProfile.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile != null)
            {
                // Vignette: very subtle
                if (!profile.TryGet(out Vignette vignette))
                    vignette = profile.Add<Vignette>(true);
                vignette.active = true;
                vignette.intensity.overrideState = true;
                vignette.intensity.value = 0.22f;
                vignette.smoothness.overrideState = true;
                vignette.smoothness.value = 0.40f;

                // FilmGrain: very subtle
                if (!profile.TryGet(out FilmGrain grain))
                    grain = profile.Add<FilmGrain>(true);
                grain.active = true;
                grain.type.overrideState = true;
                grain.type.value = FilmGrainLookup.Thin1;
                grain.intensity.overrideState = true;
                grain.intensity.value = 0.18f;
                grain.response.overrideState = true;
                grain.response.value = 0.85f;

                // Chromatic Aberration: very subtle
                if (!profile.TryGet(out ChromaticAberration ca))
                    ca = profile.Add<ChromaticAberration>(true);
                ca.active = true;
                ca.intensity.overrideState = true;
                ca.intensity.value = 0.10f;

                // Color Adjustments: gloomy horror mood
                if (!profile.TryGet(out ColorAdjustments colorAdj))
                    colorAdj = profile.Add<ColorAdjustments>(true);
                colorAdj.active = true;
                colorAdj.postExposure.overrideState = true;
                colorAdj.postExposure.value = -0.15f;
                colorAdj.contrast.overrideState = true;
                colorAdj.contrast.value = 18f;
                colorAdj.saturation.overrideState = true;
                colorAdj.saturation.value = -12f;

                // Shadows Midtones: gloomy navy/cold shadows
                if (!profile.TryGet(out ShadowsMidtonesHighlights smh))
                    smh = profile.Add<ShadowsMidtonesHighlights>(true);
                smh.active = true;
                smh.shadows.overrideState = true;
                smh.shadows.value = new Vector4(0.85f, 0.90f, 1.05f, 0f);

                EditorUtility.SetDirty(profile);
                Debug.Log("[ArtSetup] DefaultVolumeProfile updated with subtle horror effects.");
            }
        }

        private static void ConfigureEvMarketScene()
        {
            string scenePath = "Assets/Scenes/EvMarket.unity";
            if (!File.Exists(scenePath))
            {
                Debug.LogWarning($"[ArtSetup] Scene not found: {scenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Configure Atmosphere Controller
            GameObject atmosphereObj = GameObject.Find("[Atmosphere_Controller]");
            if (atmosphereObj == null)
            {
                atmosphereObj = new GameObject("[Atmosphere_Controller]");
            }

            AtmosphereSceneController controller = atmosphereObj.GetComponent<AtmosphereSceneController>();
            if (controller == null)
            {
                controller = atmosphereObj.AddComponent<AtmosphereSceneController>();
            }

            RetroHorrorPostProcess retroPost = atmosphereObj.GetComponent<RetroHorrorPostProcess>();
            if (retroPost == null)
            {
                retroPost = atmosphereObj.AddComponent<RetroHorrorPostProcess>();
            }
            retroPost.InitializeVolume();
            retroPost.ApplySettings();

            // Set scene evening foggy RenderSettings
            controller.ApplyAtmosphere();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ArtSetup] EvMarket scene configured with evening foggy atmosphere.");
        }

        private static void ConfigureEndUnderRubbleScene()
        {
            string scenePath = "Assets/Scenes/End_UnderRubble.unity";
            if (!File.Exists(scenePath))
            {
                Debug.LogWarning($"[ArtSetup] Scene not found: {scenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 1. Atmosphere Controller
            GameObject atmosphereObj = GameObject.Find("[Atmosphere_Controller]");
            if (atmosphereObj == null)
            {
                atmosphereObj = new GameObject("[Atmosphere_Controller]");
            }

            AtmosphereSceneController controller = atmosphereObj.GetComponent<AtmosphereSceneController>();
            if (controller == null)
            {
                controller = atmosphereObj.AddComponent<AtmosphereSceneController>();
            }

            // Set mode to Rubble
            var serializedController = new SerializedObject(controller);
            var modeProp = serializedController.FindProperty("atmosphereMode");
            if (modeProp != null)
            {
                modeProp.enumValueIndex = (int)AtmosphereType.End_UnderRubble_Claustrophobic;
                serializedController.ApplyModifiedProperties();
            }
            controller.ApplyAtmosphere();

            RetroHorrorPostProcess retroPost = atmosphereObj.GetComponent<RetroHorrorPostProcess>();
            if (retroPost == null)
            {
                retroPost = atmosphereObj.AddComponent<RetroHorrorPostProcess>();
            }
            retroPost.InitializeVolume();
            retroPost.ApplySettings();

            // 2. Setup Terrain Proxy for PureVolumetricFog
            Terrain activeTerrain = Terrain.activeTerrain;
            if (activeTerrain == null)
            {
                GameObject terrainObj = GameObject.Find("[Fog_TerrainProxy]");
                if (terrainObj == null)
                {
                    terrainObj = new GameObject("[Fog_TerrainProxy]");
                    terrainObj.transform.position = new Vector3(-100f, 0f, -100f);

                    TerrainData tData = new TerrainData
                    {
                        size = new Vector3(200f, 30f, 200f),
                        heightmapResolution = 65
                    };

                    string terrainDataPath = "Assets/BK/Pure_VolumetricFog/RubbleTerrainData.asset";
                    if (!File.Exists(terrainDataPath))
                    {
                        AssetDatabase.CreateAsset(tData, terrainDataPath);
                    }
                    else
                    {
                        tData = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainDataPath);
                    }

                    activeTerrain = terrainObj.AddComponent<Terrain>();
                    activeTerrain.terrainData = tData;
                    activeTerrain.drawHeightmap = false; // Invisible proxy so it doesn't obstruct rubble meshes
                    activeTerrain.drawTreesAndFoliage = false;

                    TerrainCollider collider = terrainObj.AddComponent<TerrainCollider>();
                    collider.enabled = false;
                }
                else
                {
                    activeTerrain = terrainObj.GetComponent<Terrain>();
                }
            }

            // 3. Setup PureVolumetricFog Component
            GameObject fogObj = GameObject.Find("[Pure_VolumetricFog]");
            if (fogObj == null)
            {
                fogObj = new GameObject("[Pure_VolumetricFog]");
            }

            PureVolumetricFog fog = fogObj.GetComponent<PureVolumetricFog>();
            if (fog == null)
            {
                fog = fogObj.AddComponent<PureVolumetricFog>();
            }

            if (activeTerrain != null)
            {
                fog.SetTerrain(activeTerrain);
            }

            // Configure PureVolumetricFog Serialized Properties according to design specifications
            SerializedObject serializedFog = new SerializedObject(fog);

            SetSerializedFloat(serializedFog, "groundFogHeight", 18f); // 15-20m claustrophobic ground fog
            SetSerializedFloat(serializedFog, "density", 0.09f);        // 0.08 - 0.15f
            SetSerializedColor(serializedFog, "fogColor", new Color(0.035f, 0.045f, 0.07f, 1f)); // koyu lacivert/duman rengi
            SetSerializedColor(serializedFog, "ambientColor", new Color(0.02f, 0.025f, 0.04f, 1f));
            SetSerializedBool(serializedFog, "followSceneFogColor", true);
            SetSerializedBool(serializedFog, "followSceneAmbientColor", true);
            SetSerializedFloat(serializedFog, "ambientStrength", 0.8f);
            SetSerializedFloat(serializedFog, "ambientShadowStrength", 0.85f);
            SetSerializedBool(serializedFog, "useSceneDepth", true);
            SetSerializedBool(serializedFog, "enableVolumetricShadows", true);
            SetSerializedBool(serializedFog, "useUnityRealtimeShadows", true);
            SetSerializedFloat(serializedFog, "noiseAmount", 0.45f);
            SetSerializedFloat(serializedFog, "windSpeed", 2f);
            SetSerializedFloat(serializedFog, "terrainFollow", 0.8f);

            serializedFog.ApplyModifiedProperties();
            fog.Refresh();

            // 4. Setup AFAD Emergency Searchlights in Rubble Scene
            GameObject afadLights = GameObject.Find("[AFAD_Rescue_Lights]");
            if (afadLights == null)
            {
                afadLights = new GameObject("[AFAD_Rescue_Lights]");
                afadLights.transform.position = new Vector3(0f, 6f, 15f);

                // Blue emergency beacon
                GameObject blueBeacon = new GameObject("AFAD_BlueBeacon");
                blueBeacon.transform.SetParent(afadLights.transform);
                blueBeacon.transform.localPosition = new Vector3(-2f, 0f, 0f);
                Light blueLight = blueBeacon.AddComponent<Light>();
                blueLight.type = LightType.Point;
                blueLight.color = new Color(0.1f, 0.4f, 1f, 1f);
                blueLight.range = 25f;
                blueLight.intensity = 3.5f;

                // Red emergency beacon
                GameObject redBeacon = new GameObject("AFAD_RedBeacon");
                redBeacon.transform.SetParent(afadLights.transform);
                redBeacon.transform.localPosition = new Vector3(2f, 0f, 0f);
                Light redLight = redBeacon.AddComponent<Light>();
                redLight.type = LightType.Point;
                redLight.color = new Color(1f, 0.15f, 0.1f, 1f);
                redLight.range = 25f;
                redLight.intensity = 3.5f;

                // Rescue searchlight penetrating fog
                GameObject searchlight = new GameObject("AFAD_Searchlight");
                searchlight.transform.SetParent(afadLights.transform);
                searchlight.transform.localPosition = new Vector3(0f, 4f, 5f);
                searchlight.transform.rotation = Quaternion.Euler(35f, 180f, 0f);
                Light spot = searchlight.AddComponent<Light>();
                spot.type = LightType.Spot;
                spot.color = new Color(0.9f, 0.95f, 1f, 1f);
                spot.range = 50f;
                spot.spotAngle = 40f;
                spot.intensity = 8f;
                spot.shadows = LightShadows.Soft;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ArtSetup] End_UnderRubble scene configured with PureVolumetricFog and AFAD lights.");
        }

        private static void SetSerializedFloat(SerializedObject so, string propertyName, float value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null) prop.floatValue = value;
        }

        private static void SetSerializedBool(SerializedObject so, string propertyName, bool value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null) prop.boolValue = value;
        }

        private static void SetSerializedColor(SerializedObject so, string propertyName, Color value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null) prop.colorValue = value;
        }
    }
}
#endif
