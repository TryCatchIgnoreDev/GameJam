#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using GameJam.Art;
using GameJam.Story;
using BKPureNature;

namespace GameJam.Editor
{
    public static class MainMenuSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string IntroScenePath = "Assets/Scenes/Intro.unity";
        private const string EvMarketScenePath = "Assets/Scenes/EvMarket.unity";
        private const string EndUnderRubbleScenePath = "Assets/Scenes/End_UnderRubble.unity";

        // Araçlar
        private const string PrefabPoliceCar = "Assets/Awbmecreations/Mobile Optimize-Free Low Poly Cars/Prefabs/Police Car N_4.prefab";
        private const string PrefabRescueVan = "Assets/Awbmecreations/Mobile Optimize-Free Low Poly Cars/Prefabs/N Van_10.prefab";
        private const string PrefabCrushedCar = "Assets/Awbmecreations/Mobile Optimize-Free Low Poly Cars/Prefabs/Hatchback Car_15.prefab";

        // Binalar (Arka Plan Silüetleri & Şehir Blokları)
        private const string PrefabBuilding1 = "Assets/Extra_Buildings/Prefabs/Brownstone_Harlem_1.prefab";
        private const string PrefabBuilding2 = "Assets/Extra_Buildings/Prefabs/Brownstone_Commercial_1.prefab";
        private const string PrefabBuilding3 = "Assets/Extra_Buildings/Prefabs/Brownstone_FlatFacadeComplex_1.prefab";
        private const string PrefabBuilding4 = "Assets/Extra_Buildings/Prefabs/Brownstone_ClassicComplex_1.prefab";

        // Yol & Sokak Detayları
        private const string PrefabStreetPole = "Assets/ModularLowpolyStreetsFree/Prefabs/Other/Pole1.prefab";
        private const string PrefabBarrier = "Assets/Road props for games/Prefabs/BarrierFence.prefab";
        private const string PrefabTrafficCone = "Assets/Road props for games/Prefabs/TrafficCone.prefab";

        // Enkaz Prefabları
        private const string PrefabConcreteWall = "Assets/Wreckage/Prefabs/Wreckage_ConcreteWall_Broken.prefab";
        private const string PrefabConcreteCorner = "Assets/Wreckage/Prefabs/Wreckage_ConcreteCorner_Ruined.prefab";
        private const string PrefabCollapsedSlab = "Assets/Wreckage/Prefabs/Wreckage_CollapsedSlab.prefab";
        private const string PrefabSteelBeam = "Assets/Wreckage/Prefabs/Wreckage_SteelBeam.prefab";
        private const string PrefabSteelTruss = "Assets/Wreckage/Prefabs/Wreckage_SteelGirder_Truss.prefab";
        private const string PrefabRubblePile = "Assets/Wreckage/Prefabs/Wreckage_RubblePile.prefab";
        private const string PrefabRubbleScatter = "Assets/Wreckage/Prefabs/Wreckage_RubbleScatter.prefab";
        private const string PrefabBentPanel = "Assets/Wreckage/Prefabs/Wreckage_BentPanel.prefab";
        private const string PrefabBrokenPlanks = "Assets/Wreckage/Prefabs/Wreckage_BrokenPlanks.prefab";

        // Materyaller
        private const string MatPavement = "Assets/ModularLowpolyStreetsFree/Materials/Roads/Pavement.mat";
        private const string MatGround = "Assets/Wreckage/Materials/mGround_7.mat";

        // Fontlar
        private const string FontOswald = "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Oswald Bold SDF.asset";
        private const string FontElectronic = "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset";
        private const string FontLiberation = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [MenuItem("GameJam/Build Main Menu Scene (21:00)", false, 10)]
        public static void BuildMainMenuScene()
        {
            Debug.Log("[MainMenuSceneBuilder] '21:00' Yoğun Enkaz Ana Menü Sahnesi inşası başlatılıyor...");

            // 1. Sahneyi oluştur
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Kök Hiyerarşiyi Kur
            GameObject envRoot = new GameObject("[ENVIRONMENT]");
            GameObject lightingRoot = new GameObject("[LIGHTING_AND_ATMOSPHERE]");
            GameObject cameraObj = SetupCamera(envRoot.transform);
            SetupEventSystem();

            // 3. Çok Katmanlı Yoğun Enkaz Alanını Kur
            SetupBackgroundBuildings(envRoot.transform);
            SetupGroundAndWreckage(envRoot.transform);
            SetupVehiclesAndProps(envRoot.transform);
            SetupAtmosphericDustParticles(envRoot.transform);

            // 4. Atmosfer, Sis ve Post-Processing Ayarları
            SetupLightingAndAtmosphere(lightingRoot.transform);

            // 5. UI Canvas ve Paneller
            SetupMainMenuCanvas(cameraObj.GetComponent<MainMenuCameraController>());

            // 6. Sahneyi Kaydet
            bool saveOk = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!saveOk)
            {
                Debug.LogError($"[MainMenuSceneBuilder] Sahne kaydedilemedi: {ScenePath}");
                return;
            }

            // 7. Build Settings Güncellemesi (MainMenu en başa)
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[MainMenuSceneBuilder] Yoğun ve dramatik enkazlı '21:00' Ana Menü Sahnesi başarıyla inşa edildi ve kaydedildi!</color>");
        }

        #region Kamera Kurulumu
        private static GameObject SetupCamera(Transform parent)
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            // Kamerayı hafif sol açılı, enkaz vadisine ve polis aracına bakacak şekilde konumlandır
            camGo.transform.position = new Vector3(-1.4f, 2.0f, -8.0f);
            camGo.transform.rotation = Quaternion.Euler(5.5f, 11.0f, 0f);

            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = GameSettings.CameraFOV;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 250f;

            camGo.AddComponent<AudioListener>();

            var camCtrl = camGo.AddComponent<MainMenuCameraController>();
            camCtrl.basePosition = camGo.transform.position;
            camCtrl.baseRotationEuler = camGo.transform.eulerAngles;
            camCtrl.swaySpeed = 0.3f;
            camCtrl.swayAmountX = 0.22f;
            camCtrl.swayAmountY = 0.10f;
            camCtrl.mouseParallaxAmount = 0.16f;

            return camGo;
        }
        #endregion

        #region EventSystem
        private static void SetupEventSystem()
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();

            var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
            {
                esGo.AddComponent(inputModuleType);
            }
            else
            {
                esGo.AddComponent<StandaloneInputModule>();
            }
        }
        #endregion

        #region Arka Plan Binaları (Silüetler ve Derinlik)
        private static void SetupBackgroundBuildings(Transform envRoot)
        {
            GameObject bgGroup = new GameObject("00_Background_City_Silhouettes");
            bgGroup.transform.SetParent(envRoot);

            // Binaların ön cepheleri (facade) kameraya bakacak ve zemin seviyesinde duracak şekilde ayarlandı
            InstantiatePrefabSafe(PrefabBuilding1, bgGroup.transform,
                new Vector3(-16f, 14.5f, 16f), Quaternion.Euler(0f, 95f, 0f), new Vector3(1.1f, 1.1f, 1.1f));

            InstantiatePrefabSafe(PrefabBuilding3, bgGroup.transform,
                new Vector3(-8f, 10.3f, 18f), Quaternion.Euler(0f, -85f, -3f), new Vector3(1.1f, 1.15f, 1.1f));

            InstantiatePrefabSafe(PrefabBuilding2, bgGroup.transform,
                new Vector3(0f, 2.9f, 20f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.2f, 1.2f, 1.2f));

            InstantiatePrefabSafe(PrefabBuilding4, bgGroup.transform,
                new Vector3(10f, 11.3f, 17f), Quaternion.Euler(0f, -95f, 3f), new Vector3(1.1f, 1.1f, 1.1f));

            InstantiatePrefabSafe(PrefabBuilding1, bgGroup.transform,
                new Vector3(18f, 14.5f, 19f), Quaternion.Euler(0f, 85f, 0f), new Vector3(1.1f, 1.1f, 1.1f));

            // Binaların önünde göğe yükselen devasa hasarlı çelik kafes kuleler
            InstantiatePrefabSafe(PrefabSteelTruss, bgGroup.transform,
                new Vector3(-7.5f, 0f, 11f), Quaternion.Euler(-25f, 45f, 20f), new Vector3(1.5f, 2.2f, 1.5f));

            InstantiatePrefabSafe(PrefabSteelTruss, bgGroup.transform,
                new Vector3(9.5f, 0f, 10f), Quaternion.Euler(22f, -65f, -18f), new Vector3(1.6f, 2.0f, 1.6f));

            // Devrilmiş sokak direkleri
            InstantiatePrefabSafe(PrefabStreetPole, bgGroup.transform,
                new Vector3(-6.5f, 0f, 9f), Quaternion.Euler(30f, 35f, 0f), Vector3.one);

            InstantiatePrefabSafe(PrefabStreetPole, bgGroup.transform,
                new Vector3(6.0f, 0f, 8.5f), Quaternion.Euler(-40f, -55f, 20f), Vector3.one);
        }
        #endregion

        #region Enkaz ve Zemin Kurulumu (Ön, Orta, Arka Plan)
        private static void SetupGroundAndWreckage(Transform envRoot)
        {
            GameObject groundGroup = new GameObject("01_Ground");
            groundGroup.transform.SetParent(envRoot);

            // Zemin Döşemesi (Geniş asfalt/pavement zemin - ufuk çizgisini taşır)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Ground_Plane";
            floor.transform.SetParent(groundGroup.transform);
            floor.transform.position = new Vector3(0f, -0.05f, 5f);
            floor.transform.localScale = new Vector3(30f, 1f, 30f);

            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>(MatPavement) ??
                               AssetDatabase.LoadAssetAtPath<Material>(MatGround);
            if (floorMat != null)
            {
                floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            }

            GameObject wreckageGroup = new GameObject("02_Wreckage_Structures");
            wreckageGroup.transform.SetParent(envRoot);

            // --- KATMAN A: YIKINTI DUVARLARI VE KÖŞELER (Yana Açılı & Yıkılmış Zemin Blokları) ---
            InstantiatePrefabSafe(PrefabConcreteCorner, wreckageGroup.transform, new Vector3(-12f, 0f, 6f), Quaternion.Euler(0f, 55f, 0f), new Vector3(1.5f, 1.6f, 1.5f));
            InstantiatePrefabSafe(PrefabConcreteWall, wreckageGroup.transform, new Vector3(-9f, 0.2f, 8f), Quaternion.Euler(65f, 25f, -10f), new Vector3(1.4f, 1.5f, 1.4f));
            InstantiatePrefabSafe(PrefabConcreteCorner, wreckageGroup.transform, new Vector3(12f, 0f, 5f), Quaternion.Euler(0f, -65f, 0f), new Vector3(1.5f, 1.6f, 1.5f));
            InstantiatePrefabSafe(PrefabConcreteWall, wreckageGroup.transform, new Vector3(8.5f, 0.2f, 7.5f), Quaternion.Euler(70f, -35f, 12f), new Vector3(1.4f, 1.5f, 1.4f));
            InstantiatePrefabSafe(PrefabConcreteWall, wreckageGroup.transform, new Vector3(-1.5f, 0.1f, 11f), Quaternion.Euler(80f, 15f, 0f), new Vector3(1.6f, 1.6f, 1.6f)); // Yere yıkılmış duvar
            InstantiatePrefabSafe(PrefabConcreteWall, wreckageGroup.transform, new Vector3(3.5f, 0.1f, 10.5f), Quaternion.Euler(75f, -20f, 5f), new Vector3(1.5f, 1.5f, 1.5f)); // Yere yıkılmış duvar

            // --- KATMAN B: ÇÖKMÜŞ BETON BLOKLAR (SLABS - Çapraz Katmanlar) ---
            InstantiatePrefabSafe(PrefabCollapsedSlab, wreckageGroup.transform, new Vector3(-4.5f, 0.8f, 3.8f), Quaternion.Euler(-24f, 45f, 16f), new Vector3(1.5f, 1.5f, 1.5f));
            InstantiatePrefabSafe(PrefabCollapsedSlab, wreckageGroup.transform, new Vector3(-1.0f, 0.5f, 5.5f), Quaternion.Euler(15f, 15f, -8f), new Vector3(1.6f, 1.6f, 1.6f));
            InstantiatePrefabSafe(PrefabCollapsedSlab, wreckageGroup.transform, new Vector3(2.5f, 0.9f, 3.2f), Quaternion.Euler(20f, -50f, -15f), new Vector3(1.5f, 1.5f, 1.5f));
            InstantiatePrefabSafe(PrefabCollapsedSlab, wreckageGroup.transform, new Vector3(6.5f, 0.6f, 2.0f), Quaternion.Euler(-18f, -75f, 12f), new Vector3(1.4f, 1.4f, 1.4f));
            InstantiatePrefabSafe(PrefabCollapsedSlab, wreckageGroup.transform, new Vector3(-7.5f, 0.5f, 2.0f), Quaternion.Euler(12f, 60f, -10f), new Vector3(1.4f, 1.4f, 1.4f));
            InstantiatePrefabSafe(PrefabCollapsedSlab, wreckageGroup.transform, new Vector3(0f, 1.6f, 4.0f), Quaternion.Euler(-30f, 0f, 0f), new Vector3(1.8f, 1.5f, 1.8f));

            // --- KATMAN C: ÇELİK KİRİŞLER (Jagged Steel Beams) ---
            InstantiatePrefabSafe(PrefabSteelBeam, wreckageGroup.transform, new Vector3(-5.2f, 0.3f, 2.0f), Quaternion.Euler(-20f, 60f, 35f), new Vector3(1.3f, 1.3f, 1.3f));
            InstantiatePrefabSafe(PrefabSteelBeam, wreckageGroup.transform, new Vector3(-2.8f, 0.1f, -1.2f), Quaternion.Euler(5f, 95f, -12f), new Vector3(1.1f, 1.1f, 1.1f));
            InstantiatePrefabSafe(PrefabSteelBeam, wreckageGroup.transform, new Vector3(1.0f, 0.4f, 2.5f), Quaternion.Euler(-15f, -25f, 50f), new Vector3(1.4f, 1.4f, 1.4f));
            InstantiatePrefabSafe(PrefabSteelBeam, wreckageGroup.transform, new Vector3(5.0f, 0.2f, -0.5f), Quaternion.Euler(18f, -115f, 28f), new Vector3(1.3f, 1.3f, 1.3f));
            InstantiatePrefabSafe(PrefabSteelBeam, wreckageGroup.transform, new Vector3(-0.6f, 0f, -3.5f), Quaternion.Euler(0f, 75f, 8f), new Vector3(1f, 1f, 0.9f));

            // --- KATMAN D: DEVASA MOLOZ TEPELERİ (Rubble Piles) ---
            InstantiatePrefabSafe(PrefabRubblePile, wreckageGroup.transform, new Vector3(-5.5f, 0f, 1.5f), Quaternion.Euler(0f, 55f, 0f), new Vector3(2.0f, 1.6f, 2.0f));
            InstantiatePrefabSafe(PrefabRubblePile, wreckageGroup.transform, new Vector3(-1.5f, 0f, 3.8f), Quaternion.Euler(0f, 125f, 0f), new Vector3(2.2f, 1.8f, 2.2f));
            InstantiatePrefabSafe(PrefabRubblePile, wreckageGroup.transform, new Vector3(2.2f, 0f, 4.2f), Quaternion.Euler(0f, -40f, 0f), new Vector3(2.0f, 1.7f, 2.0f));
            InstantiatePrefabSafe(PrefabRubblePile, wreckageGroup.transform, new Vector3(6.2f, 0f, 1.8f), Quaternion.Euler(0f, -110f, 0f), new Vector3(2.0f, 1.6f, 2.0f));
            InstantiatePrefabSafe(PrefabRubblePile, wreckageGroup.transform, new Vector3(9.0f, 0f, -0.5f), Quaternion.Euler(0f, 45f, 0f), new Vector3(1.8f, 1.5f, 1.8f));
            InstantiatePrefabSafe(PrefabRubblePile, wreckageGroup.transform, new Vector3(0f, 0f, 7.0f), Quaternion.Euler(0f, 15f, 0f), new Vector3(2.6f, 2.2f, 2.6f));
            InstantiatePrefabSafe(PrefabRubblePile, wreckageGroup.transform, new Vector3(-8.5f, 0f, 4.5f), Quaternion.Euler(0f, 70f, 0f), new Vector3(2.2f, 1.8f, 2.2f));
            InstantiatePrefabSafe(PrefabRubblePile, wreckageGroup.transform, new Vector3(8.0f, 0f, 5.5f), Quaternion.Euler(0f, -80f, 0f), new Vector3(2.2f, 1.8f, 2.2f));

            // --- KATMAN E: ÖN PLAN MOLOZ VE DETAY SCATTER (Kamera Önü Derinlik) ---
            InstantiatePrefabSafe(PrefabRubbleScatter, wreckageGroup.transform, new Vector3(-2.2f, 0f, -4.2f), Quaternion.Euler(0f, 40f, 0f), new Vector3(1.3f, 1.3f, 1.3f));
            InstantiatePrefabSafe(PrefabRubbleScatter, wreckageGroup.transform, new Vector3(0.6f, 0f, -3.5f), Quaternion.Euler(0f, 115f, 0f), new Vector3(1.4f, 1.4f, 1.4f));
            InstantiatePrefabSafe(PrefabRubbleScatter, wreckageGroup.transform, new Vector3(2.2f, 0f, -4.6f), Quaternion.Euler(0f, 200f, 0f), new Vector3(1.2f, 1.2f, 1.2f));
            InstantiatePrefabSafe(PrefabRubbleScatter, wreckageGroup.transform, new Vector3(-0.8f, 0f, -6.0f), Quaternion.Euler(0f, 85f, 0f), new Vector3(1.1f, 1.1f, 1.1f));
            InstantiatePrefabSafe(PrefabRubbleScatter, wreckageGroup.transform, new Vector3(1.2f, 0f, -5.5f), Quaternion.identity, new Vector3(1.3f, 1.3f, 1.3f));

            // Kırık Kalaslar ve Ezik Metal Levhalar
            InstantiatePrefabSafe(PrefabBrokenPlanks, wreckageGroup.transform, new Vector3(-1.0f, 0.05f, -3.8f), Quaternion.Euler(0f, 40f, 4f), Vector3.one);
            InstantiatePrefabSafe(PrefabBrokenPlanks, wreckageGroup.transform, new Vector3(1.8f, 0.05f, -2.6f), Quaternion.Euler(0f, -60f, 0f), Vector3.one);
            InstantiatePrefabSafe(PrefabBentPanel, wreckageGroup.transform, new Vector3(-3.0f, 0.1f, -1.8f), Quaternion.Euler(8f, 30f, -10f), Vector3.one);
            InstantiatePrefabSafe(PrefabBentPanel, wreckageGroup.transform, new Vector3(4.2f, 0.15f, 0.3f), Quaternion.Euler(-10f, -70f, 16f), Vector3.one);
        }
        #endregion

        #region Araçlar, Bariyerler ve Acil Durum Işıkları
        private static void SetupVehiclesAndProps(Transform envRoot)
        {
            GameObject vehiclesGroup = new GameObject("03_Emergency_Vehicles_And_Props");
            vehiclesGroup.transform.SetParent(envRoot);

            // 1. POLİS ARACI (Ön Planda Sağda, Enkaz Alanını Aydınlatır)
            GameObject policeCar = InstantiatePrefabSafe(PrefabPoliceCar, vehiclesGroup.transform,
                new Vector3(2.6f, 0f, -2.0f),
                Quaternion.Euler(0f, -40f, 0f),
                Vector3.one);

            if (policeCar != null)
            {
                policeCar.name = "Police_Vehicle_Main";

                // Kırmızı - Mavi Çakar Tepe Lambaları
                CreateEmergencyBeaconPair(policeCar.transform, new Vector3(0f, 1.55f, 0.15f), 12.0f);

                // Ön Farlar (Enkaz merkezine güçlü sıcak sarı ışık)
                CreateHeadlight(policeCar.transform, new Vector3(-0.65f, 0.65f, 1.9f), Quaternion.Euler(8f, -10f, 0f), 5.0f);
                CreateHeadlight(policeCar.transform, new Vector3(0.65f, 0.65f, 1.9f), Quaternion.Euler(8f, 10f, 0f), 5.0f);
            }

            // 2. ENKAZDA EZİLMİŞ / YAN YATMIŞ SİVİL ARAÇ (Orta Planda Solda)
            GameObject crushedCar = InstantiatePrefabSafe(PrefabCrushedCar, vehiclesGroup.transform,
                new Vector3(-4.0f, 0.8f, 2.5f),
                Quaternion.Euler(45f, 70f, -30f),
                Vector3.one);

            if (crushedCar != null)
            {
                crushedCar.name = "Crushed_Civilian_Car";

                // Üzerine çökmüş bir beton blok
                InstantiatePrefabSafe(PrefabCollapsedSlab, crushedCar.transform,
                    new Vector3(0f, 1.2f, 0f), Quaternion.Euler(-15f, 30f, 20f), new Vector3(1.3f, 1.3f, 1.3f));

                // Yanıp sönen sarı acil durum flaşörü (Tehlike dörtlüsü)
                CreateHazardBlinker(crushedCar.transform, new Vector3(0f, 0.5f, -1.8f));
            }

            // 3. AFAD KURTARMA DESTEK ARACI (Derin Sol Planda)
            GameObject rescueVan = InstantiatePrefabSafe(PrefabRescueVan, vehiclesGroup.transform,
                new Vector3(-7.8f, 0f, 6.5f),
                Quaternion.Euler(0f, 68f, -6f),
                Vector3.one);

            if (rescueVan != null)
            {
                rescueVan.name = "AFAD_Rescue_Van";
                CreateEmergencyBeaconPair(rescueVan.transform, new Vector3(0f, 2.1f, -0.3f), 8.5f);
            }

            // 4. POLİS GÜVENLİK BARİYERLERİ VE TRAFİK KONİLERİ
            InstantiatePrefabSafe(PrefabBarrier, vehiclesGroup.transform,
                new Vector3(0.4f, 0f, -2.8f), Quaternion.Euler(0f, -20f, 0f), Vector3.one);

            InstantiatePrefabSafe(PrefabBarrier, vehiclesGroup.transform,
                new Vector3(-2.0f, 0f, -3.2f), Quaternion.Euler(15f, 35f, 0f), Vector3.one);

            InstantiatePrefabSafe(PrefabTrafficCone, vehiclesGroup.transform,
                new Vector3(1.6f, 0f, -2.8f), Quaternion.identity, Vector3.one);

            InstantiatePrefabSafe(PrefabTrafficCone, vehiclesGroup.transform,
                new Vector3(3.2f, 0f, -4.2f), Quaternion.Euler(60f, 25f, 0f), Vector3.one); // Devrilmiş koni

            InstantiatePrefabSafe(PrefabTrafficCone, vehiclesGroup.transform,
                new Vector3(-0.6f, 0f, -3.8f), Quaternion.identity, Vector3.one);

            // 5. ACİL DURUM İŞARET FİŞEĞİ (Flicker Flare)
            CreateRoadFlare(vehiclesGroup.transform, new Vector3(0.1f, 0.1f, -1.6f));
        }

        private static void CreateEmergencyBeaconPair(Transform parent, Vector3 localPos, float intensity)
        {
            GameObject pairGo = new GameObject("Emergency_Flashing_Beacons");
            pairGo.transform.SetParent(parent);
            pairGo.transform.localPosition = localPos;
            pairGo.transform.localRotation = Quaternion.identity;

            GameObject redGo = new GameObject("Red_Light");
            redGo.transform.SetParent(pairGo.transform);
            redGo.transform.localPosition = new Vector3(-0.35f, 0f, 0f);
            Light redLight = redGo.AddComponent<Light>();
            redLight.type = LightType.Point;
            redLight.color = new Color(1.0f, 0.08f, 0.08f);
            redLight.range = 26f;
            redLight.intensity = intensity;

            GameObject blueGo = new GameObject("Blue_Light");
            blueGo.transform.SetParent(pairGo.transform);
            blueGo.transform.localPosition = new Vector3(0.35f, 0f, 0f);
            Light blueLight = blueGo.AddComponent<Light>();
            blueLight.type = LightType.Point;
            blueLight.color = new Color(0.15f, 0.45f, 1.0f);
            blueLight.range = 26f;
            blueLight.intensity = intensity;

            pairGo.AddComponent<EmergencyBeaconFlasher>();
        }

        private static void CreateHeadlight(Transform parent, Vector3 localPos, Quaternion localRot, float intensity)
        {
            GameObject hlGo = new GameObject("Headlight_Spot");
            hlGo.transform.SetParent(parent);
            hlGo.transform.localPosition = localPos;
            hlGo.transform.localRotation = localRot;

            Light spot = hlGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 35f;
            spot.spotAngle = 50f;
            spot.innerSpotAngle = 32f;
            spot.color = new Color(1.0f, 0.94f, 0.82f);
            spot.intensity = intensity;
            spot.shadows = LightShadows.Soft;
        }

        private static void CreateHazardBlinker(Transform parent, Vector3 localPos)
        {
            GameObject blinkerGo = new GameObject("Hazard_Blinker");
            blinkerGo.transform.SetParent(parent);
            blinkerGo.transform.localPosition = localPos;

            Light light = blinkerGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.6f, 0.05f);
            light.range = 12f;
            light.intensity = 4.0f;

            var flasher = blinkerGo.AddComponent<EmergencyFlickerLight>();
            flasher.minIntensity = 0.5f;
            flasher.maxIntensity = 4.5f;
            flasher.flickerSpeed = 6.0f;
        }

        private static void CreateRoadFlare(Transform parent, Vector3 pos)
        {
            GameObject flareGo = new GameObject("Road_Emergency_Flare");
            flareGo.transform.SetParent(parent);
            flareGo.transform.position = pos;

            Light light = flareGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.28f, 0.08f);
            light.range = 16f;
            light.intensity = 4.0f;
            light.shadows = LightShadows.Soft;

            flareGo.AddComponent<EmergencyFlickerLight>();
        }
        #endregion

        #region Atmosferik Toz ve Kül Parçacıkları (Particle System)
        private static void SetupAtmosphericDustParticles(Transform envRoot)
        {
            GameObject psGo = new GameObject("Atmospheric_Dust_Particles");
            psGo.transform.SetParent(envRoot);
            psGo.transform.position = new Vector3(0f, 3.0f, 0f);

            ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 9.0f;
            main.startSpeed = 0.35f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.16f);
            main.startColor = new Color(0.85f, 0.88f, 0.95f, 0.45f);
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 45f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(25f, 8f, 25f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.3f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.15f);
            vel.z = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.3f;
        }
        #endregion

        #region Işık, Atmosfer ve Sis
        private static void SetupLightingAndAtmosphere(Transform root)
        {
            // 1. Ay Işığı / Gece Kasvet Işığı (Directional Light)
            GameObject dirLightGo = new GameObject("Directional_Moonlight");
            dirLightGo.transform.SetParent(root);
            dirLightGo.transform.position = new Vector3(0f, 20f, 0f);
            dirLightGo.transform.rotation = Quaternion.Euler(35f, -48f, 0f);

            Light dirLight = dirLightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(0.24f, 0.30f, 0.45f);
            dirLight.intensity = 0.42f;
            dirLight.shadows = LightShadows.Soft;

            // 2. Enkaz Tepe Arama Projektörleri (Spotlights)
            GameObject searchlight1 = new GameObject("Searchlight_Center_Rubble");
            searchlight1.transform.SetParent(root);
            searchlight1.transform.position = new Vector3(-3f, 12f, -5f);
            searchlight1.transform.rotation = Quaternion.Euler(54f, 30f, 0f);

            Light sl1 = searchlight1.AddComponent<Light>();
            sl1.type = LightType.Spot;
            sl1.color = new Color(0.85f, 0.92f, 1.0f);
            sl1.range = 45f;
            sl1.spotAngle = 60f;
            sl1.innerSpotAngle = 36f;
            sl1.intensity = 3.5f;
            sl1.shadows = LightShadows.Soft;

            GameObject searchlight2 = new GameObject("Searchlight_Background_Towers");
            searchlight2.transform.SetParent(root);
            searchlight2.transform.position = new Vector3(6f, 14f, -2f);
            searchlight2.transform.rotation = Quaternion.Euler(46f, -22f, 0f);

            Light sl2 = searchlight2.AddComponent<Light>();
            sl2.type = LightType.Spot;
            sl2.color = new Color(0.75f, 0.85f, 1.0f);
            sl2.range = 55f;
            sl2.spotAngle = 55f;
            sl2.innerSpotAngle = 30f;
            sl2.intensity = 2.6f;
            sl2.shadows = LightShadows.Soft;

            // 3. AtmosphereSceneController
            GameObject atmosGo = new GameObject("[Atmosphere_Controller]");
            atmosGo.transform.SetParent(root);
            var atmosCtrl = atmosGo.AddComponent<AtmosphereSceneController>();
            atmosCtrl.atmosphereMode = AtmosphereType.EvMarket_Aksham_Sisli;
            atmosCtrl.ApplyAtmosphere();

            // 4. RetroHorrorPostProcess
            var retroPost = atmosGo.AddComponent<RetroHorrorPostProcess>();
            retroPost.InitializeVolume();
            retroPost.ApplySettings();

            // 5. PureVolumetricFog ve Terrain Proxy
            GameObject terrainProxy = new GameObject("[Fog_TerrainProxy]");
            terrainProxy.transform.SetParent(root);
            terrainProxy.transform.position = new Vector3(-80f, 0f, -80f);

            TerrainData tData = new TerrainData { size = new Vector3(160f, 30f, 160f), heightmapResolution = 65 };
            string tPath = "Assets/BK/Pure_VolumetricFog/RubbleTerrainData.asset";
            if (File.Exists(tPath))
            {
                tData = AssetDatabase.LoadAssetAtPath<TerrainData>(tPath) ?? tData;
            }

            Terrain terrain = terrainProxy.AddComponent<Terrain>();
            terrain.terrainData = tData;
            terrain.drawHeightmap = false;
            terrain.drawTreesAndFoliage = false;

            GameObject fogObj = new GameObject("[Pure_VolumetricFog]");
            fogObj.transform.SetParent(root);
            PureVolumetricFog fog = fogObj.AddComponent<PureVolumetricFog>();
            fog.SetTerrain(terrain);

            SerializedObject sFog = new SerializedObject(fog);
            SetSerializedFloat(sFog, "groundFogHeight", 16f);
            SetSerializedFloat(sFog, "density", 0.09f);
            SetSerializedColor(sFog, "fogColor", new Color(0.04f, 0.05f, 0.08f, 1f));
            SetSerializedBool(sFog, "followSceneFogColor", true);
            SetSerializedBool(sFog, "followSceneAmbientColor", true);
            SetSerializedFloat(sFog, "noiseAmount", 0.45f);
            SetSerializedFloat(sFog, "windSpeed", 1.8f);
            sFog.ApplyModifiedProperties();
            fog.Refresh();
        }

        private static void SetSerializedFloat(SerializedObject so, string propName, float val)
        {
            var p = so.FindProperty(propName);
            if (p != null) p.floatValue = val;
        }

        private static void SetSerializedColor(SerializedObject so, string propName, Color col)
        {
            var p = so.FindProperty(propName);
            if (p != null) p.colorValue = col;
        }

        private static void SetSerializedBool(SerializedObject so, string propName, bool val)
        {
            var p = so.FindProperty(propName);
            if (p != null) p.boolValue = val;
        }
        #endregion

        #region Canvas ve Kullanıcı Arayüzü (UI)
        private static void SetupMainMenuCanvas(MainMenuCameraController camCtrl)
        {
            GameObject canvasGo = new GameObject("[CANVAS_MAIN_MENU]");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            MainMenuManager manager = canvasGo.AddComponent<MainMenuManager>();
            manager.cameraController = camCtrl;

            TMP_FontAsset titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontElectronic) ??
                                      AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontOswald) ??
                                      AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontLiberation) ??
                                      TMP_Settings.defaultFontAsset;

            TMP_FontAsset regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontOswald) ??
                                        AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontLiberation) ??
                                        TMP_Settings.defaultFontAsset;

            // Sol Koyu Gradyan Şerit (Metinlerin arkasında okunabilirliği garantiler)
            GameObject shadowStrip = CreateUIObject("Left_Shadow_Strip", canvasGo.transform,
                new Vector2(0f, 0f), new Vector2(0.42f, 1f), Vector2.zero, Vector2.zero);
            Image stripImg = shadowStrip.AddComponent<Image>();
            stripImg.color = new Color(0.04f, 0.05f, 0.08f, 0.76f);
            stripImg.raycastTarget = false;

            // 1. BAŞLIK VE LOGO ("21:00")
            GameObject titleGroup = CreateUIObject("Title_Group", canvasGo.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(90f, -80f), new Vector2(550f, 220f));
            titleGroup.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);

            GameObject titleTextObj = CreateUIObject("Title_Text", titleGroup.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 120f));
            titleTextObj.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);
            TextMeshProUGUI tmpTitle = titleTextObj.AddComponent<TextMeshProUGUI>();
            if (titleFont != null) tmpTitle.font = titleFont;
            tmpTitle.text = "21<color=#E63946>:</color>00";
            tmpTitle.fontSize = 118;
            tmpTitle.fontStyle = FontStyles.Bold;
            tmpTitle.color = new Color(0.96f, 0.93f, 0.90f);
            tmpTitle.alignment = TextAlignmentOptions.Left;
            manager.titleText = tmpTitle;

            GameObject subtitleObj = CreateUIObject("Subtitle_Text", titleGroup.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 10f), new Vector2(0f, 40f));
            subtitleObj.GetComponent<RectTransform>().pivot = new Vector2(0f, 0f);
            TextMeshProUGUI tmpSubtitle = subtitleObj.AddComponent<TextMeshProUGUI>();
            if (regularFont != null) tmpSubtitle.font = regularFont;
            tmpSubtitle.text = "BİR DEPREM VE HAYATTA KALMA HİKAYESİ";
            tmpSubtitle.fontSize = 20;
            tmpSubtitle.characterSpacing = 8f;
            tmpSubtitle.color = new Color(0.75f, 0.78f, 0.88f, 0.95f);
            manager.subtitleText = tmpSubtitle;

            // 2. ANA MENÜ BUTONLARI
            GameObject buttonsContainer = CreateUIObject("Main_Buttons_Container", canvasGo.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(90f, -40f), new Vector2(380f, 290f));
            buttonsContainer.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
            manager.mainButtonsContainer = buttonsContainer;

            VerticalLayoutGroup btnLayout = buttonsContainer.AddComponent<VerticalLayoutGroup>();
            btnLayout.spacing = 18f;
            btnLayout.childControlHeight = false;
            btnLayout.childControlWidth = true;
            btnLayout.childForceExpandHeight = false;
            btnLayout.childForceExpandWidth = true;

            Button btnStart = CreateStyledMenuButton("Btn_StartGame", buttonsContainer.transform, "OYUNA BAŞLA",
                new Color(0.85f, 0.18f, 0.22f, 0.92f), regularFont, 28);
            btnStart.onClick.AddListener(manager.StartGame);

            Button btnSettings = CreateStyledMenuButton("Btn_Settings", buttonsContainer.transform, "AYARLAR",
                new Color(0.16f, 0.19f, 0.27f, 0.88f), regularFont, 24);
            btnSettings.onClick.AddListener(manager.OpenSettings);

            Button btnInfo = CreateStyledMenuButton("Btn_Info", buttonsContainer.transform, "BİLGİLENDİRME",
                new Color(0.16f, 0.19f, 0.27f, 0.88f), regularFont, 24);
            btnInfo.onClick.AddListener(manager.OpenInfo);

            // 3. AYARLAR PANELİ
            SetupSettingsPanel(canvasGo.transform, manager, regularFont);

            // 4. BİLGİLENDİRME PANELİ
            SetupInfoPanel(canvasGo.transform, manager, regularFont);
        }

        private static void SetupSettingsPanel(Transform canvasTransform, MainMenuManager manager, TMP_FontAsset font)
        {
            GameObject settingsPanel = CreateUIObject("Settings_Panel", canvasTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 480f));
            Image bg = settingsPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.10f, 0.15f, 0.96f);
            manager.settingsPanel = settingsPanel;

            // Başlık
            GameObject headerObj = CreateUIObject("Header", settingsPanel.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -30f), new Vector2(0f, 60f));
            TextMeshProUGUI tmpHeader = headerObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpHeader.font = font;
            tmpHeader.text = "AYARLAR";
            tmpHeader.fontSize = 36;
            tmpHeader.fontStyle = FontStyles.Bold;
            tmpHeader.alignment = TextAlignmentOptions.Center;
            tmpHeader.color = new Color(0.95f, 0.25f, 0.30f);

            // Ayar Satırları Container
            GameObject contentGroup = CreateUIObject("Content", settingsPanel.transform,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 20f), new Vector2(-100f, 220f));

            // Satır 1: Fare Hassasiyeti
            CreateSliderRow("Row_Sensitivity", contentGroup.transform, "Fare Hassasiyeti",
                out Slider sensSlider, out TextMeshProUGUI sensValText, font, 20f);
            manager.sensitivitySlider = sensSlider;
            manager.sensitivityValueText = sensValText;

            // Satır 2: Görüş Açısı (FOV)
            CreateSliderRow("Row_FOV", contentGroup.transform, "Görüş Açısı (FOV)",
                out Slider fovSlider, out TextMeshProUGUI fovValText, font, -60f);
            manager.fovSlider = fovSlider;
            manager.fovValueText = fovValText;

            // Alt Bilgi Notu
            GameObject noteObj = CreateUIObject("Note_Text", settingsPanel.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 110f), new Vector2(-100f, 35f));
            TextMeshProUGUI tmpNote = noteObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpNote.font = font;
            tmpNote.text = "Ayar değişiklikleri anında kaydedilir ve oyuna yansıtılır.";
            tmpNote.fontSize = 16;
            tmpNote.alignment = TextAlignmentOptions.Center;
            tmpNote.color = new Color(0.6f, 0.65f, 0.75f, 0.8f);

            // Geri Dön Butonu
            Button btnBack = CreateStyledMenuButton("Btn_BackSettings", settingsPanel.transform, "GERİ DÖN",
                new Color(0.85f, 0.18f, 0.22f, 0.95f), font, 24);
            RectTransform backRect = btnBack.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0f);
            backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(0f, 50f);
            backRect.sizeDelta = new Vector2(240f, 52f);
            btnBack.onClick.AddListener(manager.CloseSettings);

            settingsPanel.SetActive(false);
        }

        private static void CreateSliderRow(string name, Transform parent, string labelText,
            out Slider slider, out TextMeshProUGUI valText, TMP_FontAsset font, float yPos)
        {
            GameObject rowObj = CreateUIObject(name, parent,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, yPos), new Vector2(0f, 50f));

            // Label
            GameObject labelObj = CreateUIObject("Label", rowObj.transform,
                new Vector2(0f, 0f), new Vector2(0.42f, 1f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI tmpLabel = labelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpLabel.font = font;
            tmpLabel.text = labelText;
            tmpLabel.fontSize = 20;
            tmpLabel.alignment = TextAlignmentOptions.Left;
            tmpLabel.color = Color.white;

            // Slider Object
            GameObject sliderObj = CreateUIObject("Slider", rowObj.transform,
                new Vector2(0.45f, 0.5f), new Vector2(0.82f, 0.5f), Vector2.zero, new Vector2(0f, 22f));

            GameObject bgObj = CreateUIObject("Background", sliderObj.transform,
                new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), Vector2.zero, Vector2.zero);
            Image sliderBg = bgObj.AddComponent<Image>();
            sliderBg.color = new Color(0.2f, 0.24f, 0.32f, 1f);

            GameObject fillArea = CreateUIObject("Fill Area", sliderObj.transform,
                new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), Vector2.zero, Vector2.zero);
            GameObject fillObj = CreateUIObject("Fill", fillArea.transform,
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            Image sliderFill = fillObj.AddComponent<Image>();
            sliderFill.color = new Color(0.88f, 0.22f, 0.26f, 1f);

            GameObject handleArea = CreateUIObject("Handle Slide Area", sliderObj.transform,
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            GameObject handleObj = CreateUIObject("Handle", handleArea.transform,
                new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(24f, 0f));
            Image sliderHandle = handleObj.AddComponent<Image>();
            sliderHandle.color = new Color(0.95f, 0.95f, 0.95f, 1f);

            slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillObj.GetComponent<RectTransform>();
            slider.handleRect = handleObj.GetComponent<RectTransform>();
            slider.targetGraphic = sliderHandle;
            slider.direction = Slider.Direction.LeftToRight;

            // Value Display
            GameObject valObj = CreateUIObject("ValueText", rowObj.transform,
                new Vector2(0.85f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            valText = valObj.AddComponent<TextMeshProUGUI>();
            if (font != null) valText.font = font;
            valText.fontSize = 20;
            valText.alignment = TextAlignmentOptions.Right;
            valText.color = new Color(0.95f, 0.85f, 0.4f);
        }

        private static void SetupInfoPanel(Transform canvasTransform, MainMenuManager manager, TMP_FontAsset font)
        {
            GameObject infoPanel = CreateUIObject("Info_Panel", canvasTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 760f));
            Image bg = infoPanel.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.08f, 0.12f, 0.97f);
            manager.infoPanel = infoPanel;

            // Başlık
            GameObject headerObj = CreateUIObject("Header", infoPanel.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -32f), new Vector2(0f, 55f));
            TextMeshProUGUI tmpHeader = headerObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpHeader.font = font;
            tmpHeader.text = "DEPREM BİLİNÇLENDİRME REHBERİ";
            tmpHeader.fontSize = 32;
            tmpHeader.fontStyle = FontStyles.Bold;
            tmpHeader.alignment = TextAlignmentOptions.Center;
            tmpHeader.color = new Color(1.0f, 0.78f, 0.2f);

            GameObject subheaderObj = CreateUIObject("Subheader", infoPanel.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -72f), new Vector2(0f, 30f));
            TextMeshProUGUI tmpSubheader = subheaderObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpSubheader.font = font;
            tmpSubheader.text = "Deprem Öncesinde, Sarsıntı Anında ve Enkaz Altında Hayatta Kalma Kuralları";
            tmpSubheader.fontSize = 17;
            tmpSubheader.alignment = TextAlignmentOptions.Center;
            tmpSubheader.color = new Color(0.7f, 0.75f, 0.85f);

            // 3 Sütunlu Bilgilendirme Kutuları
            GameObject cardsContainer = CreateUIObject("Cards_Container", infoPanel.transform,
                new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.86f), Vector2.zero, Vector2.zero);

            HorizontalLayoutGroup cardLayout = cardsContainer.AddComponent<HorizontalLayoutGroup>();
            cardLayout.spacing = 20f;
            cardLayout.childControlHeight = true;
            cardLayout.childControlWidth = true;
            cardLayout.childForceExpandHeight = true;
            cardLayout.childForceExpandWidth = true;

            // Kart 1: Deprem Öncesi
            CreateInfoCard(cardsContainer.transform, "1. DEPREM ÖNCESİ (HAZIRLIK)",
                "• <b>Mobilyaları Sabitleyin:</b> Dolap, kitaplık ve ağır beyaz eşyaları duvara L-demirleriyle sabitleyin.\n\n" +
                "• <b>Afet Çantası Hazırlayın:</b> Su, konserve, deprem düdüğü, fener, pilli radyo ve ilk yardım setini hazır bulundurun.\n\n" +
                "• <b>Toplanma Alanı:</b> e-Devlet AFAD sistemi üzerinden mahallenizdeki toplanma alanını önceden öğrenin.",
                new Color(0.12f, 0.15f, 0.22f, 0.9f), font);

            // Kart 2: Deprem Anı
            CreateInfoCard(cardsContainer.transform, "2. DEPREM ANI (ÇÖK-KAPAN-TUTUN)",
                "• <b>ÇÖK - KAPAN - TUTUN:</b> Sağlam bir masa veya bazanın yanına çökün, baş ve enseyi koruyarak mobilyaya sıkıca tutunun.\n\n" +
                "• <b>ASLA KOŞMAYIN:</b> Merdivenlere, yangın merdivenlerine ve asansörlere kesinlikle koşmayın! Binalarda ilk çöken kısımlardır.\n\n" +
                "• <b>Camlardan Uzak Durun:</b> Pencerelerden ve balkonlardan derhal uzaklaşın.",
                new Color(0.24f, 0.12f, 0.14f, 0.9f), font);

            // Kart 3: Enkaz Altı ve Sonrası
            CreateInfoCard(cardsContainer.transform, "3. ENKAZ ALTI & SONRASI",
                "• <b>Sakin Olun:</b> Enerjinizi ve oksijeninizi koruyun. Ağız ve burnunuzu bir giysiyle kapatarak tozu filtreleyin.\n\n" +
                "• <b>Ritmik Ses Çıkarın:</b> Kurtarma ekipleri seslendiğinde bağırmak yerine düdük çalın veya boru/betona ritmik vurun.\n\n" +
                "• <b>Ateş Yakmayın:</b> Gaz sızıntısı tehlikesine karşı çakmak/kibrit yakmayın, elektrik düğmelerine dokunmayın.",
                new Color(0.12f, 0.18f, 0.16f, 0.9f), font);

            // Kapat Butonu
            Button btnClose = CreateStyledMenuButton("Btn_CloseInfo", infoPanel.transform, "ANLADIM / KAPAT",
                new Color(0.85f, 0.18f, 0.22f, 0.95f), font, 24);
            RectTransform closeRect = btnClose.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 48f);
            closeRect.sizeDelta = new Vector2(260f, 52f);
            btnClose.onClick.AddListener(manager.CloseInfo);

            infoPanel.SetActive(false);
        }

        private static void CreateInfoCard(Transform parent, string title, string body, Color bgColor, TMP_FontAsset font)
        {
            GameObject cardObj = CreateUIObject("Card", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image bg = cardObj.AddComponent<Image>();
            bg.color = bgColor;

            VerticalLayoutGroup vlg = cardObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 22, 20);
            vlg.spacing = 14f;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            GameObject titleObj = CreateUIObject("Title", cardObj.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0f, 38f));
            TextMeshProUGUI tmpTitle = titleObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpTitle.font = font;
            tmpTitle.text = title;
            tmpTitle.fontSize = 20;
            tmpTitle.fontStyle = FontStyles.Bold;
            tmpTitle.alignment = TextAlignmentOptions.TopLeft;
            tmpTitle.color = new Color(1f, 0.85f, 0.4f);

            GameObject bodyObj = CreateUIObject("Body", cardObj.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0f, 360f));
            TextMeshProUGUI tmpBody = bodyObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpBody.font = font;
            tmpBody.text = body;
            tmpBody.fontSize = 17;
            tmpBody.lineSpacing = 16f;
            tmpBody.alignment = TextAlignmentOptions.TopLeft;
            tmpBody.color = new Color(0.92f, 0.94f, 0.98f);
        }

        private static Button CreateStyledMenuButton(string name, Transform parent, string labelText, Color normalColor, TMP_FontAsset font, int fontSize)
        {
            GameObject btnObj = CreateUIObject(name, parent, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(360f, 62f));
            Image img = btnObj.AddComponent<Image>();
            img.color = normalColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = normalColor;
            cb.highlightedColor = normalColor * 1.25f;
            cb.pressedColor = normalColor * 0.85f;
            cb.selectedColor = normalColor;
            btn.colors = cb;

            GameObject textObj = CreateUIObject("Text", btnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = labelText;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }

        private static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return go;
        }
        #endregion

        #region Build Settings Yapılandırması
        public static void UpdateBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

            // 0: MainMenu.unity (İlk açılan sahne)
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));

            // 1: Intro.unity
            scenes.Add(new EditorBuildSettingsScene(IntroScenePath, true));

            // 2: EvMarket.unity
            scenes.Add(new EditorBuildSettingsScene(EvMarketScenePath, true));

            // 3: End_UnderRubble.unity
            scenes.Add(new EditorBuildSettingsScene(EndUnderRubbleScenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[MainMenuSceneBuilder] EditorBuildSettings güncellendi:");
            for (int i = 0; i < scenes.Count; i++)
            {
                Debug.Log($"  [{i}] {scenes[i].path} (Enabled: {scenes[i].enabled})");
            }
        }
        #endregion

        #region Prefab Yardımcısı
        private static GameObject InstantiatePrefabSafe(string path, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[MainMenuSceneBuilder] Prefab bulunamadı: {path}");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (instance != null)
            {
                instance.transform.localPosition = localPos;
                instance.transform.localRotation = localRot;
                instance.transform.localScale = localScale;
            }

            return instance;
        }
        #endregion
    }
}
#endif
