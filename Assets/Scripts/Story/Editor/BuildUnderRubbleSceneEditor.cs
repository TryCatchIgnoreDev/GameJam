using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using GameJam.Story;

namespace GameJam.Story.Editor
{
    /// <summary>
    /// End_UnderRubble sahnesinin gelişmiş mimari inşaat editör aracı.
    /// - Bölge 1: TAM KAPALI, KLOSTROFOBİK ENKAZ CEBİ (İÇ MEKAN)
    ///   * Oyuncu normal insan ölçeğinde ayakta durur.
    ///   * Gökyüzü ÇİFT KATMANLI çökmüş beton bloklarla (CollapsedSlab) tamamen kapatılmıştır; hiçbir gökyüzü açıklığı görünmez.
    ///   * Masanın yanında baygın arkadaşlar gerçekçi insan boyutundadır (oyuncuyla 1:1 ölçekli).
    ///   * Tavan yüksekliği ~2.1m - 2.4m civarındadır, oyuncu ayakta gezinebilir ama her taraf devasa beton ve kirişlerle kilitlidir.
    /// - Bölge 2: YOĞUN VE DEVASA YIKINTI ALANI (55x55m)
    ///   * Seyreklik tamamen giderildi: 50+ kırık duvar, 40+ göğe yükselen çelik kafes, 60+ moloz tepesi, 80+ scatter.
    ///   * Pavement / kaldırım zemin döşemesi.
    ///   * AFAD arama-kurtarma araçları ve tepe projektörleri.
    /// </summary>
    public static class BuildUnderRubbleSceneEditor
    {
        private const string ScenePath = "Assets/Scenes/End_UnderRubble.unity";
        private const string EnvironmentRootName = "[WRECKAGE_ENVIRONMENT]";

        // Prefab Yolları
        private const string PrefabBentPanel = "Assets/Wreckage/Prefabs/Wreckage_BentPanel.prefab";
        private const string PrefabBrokenPlanks = "Assets/Wreckage/Prefabs/Wreckage_BrokenPlanks.prefab";
        private const string PrefabCollapsedSlab = "Assets/Wreckage/Prefabs/Wreckage_CollapsedSlab.prefab";
        private const string PrefabConcrete = "Assets/Wreckage/Prefabs/Wreckage_Concrete.prefab";
        private const string PrefabConcreteCorner = "Assets/Wreckage/Prefabs/Wreckage_ConcreteCorner_Ruined.prefab";
        private const string PrefabConcreteWall = "Assets/Wreckage/Prefabs/Wreckage_ConcreteWall_Broken.prefab";
        private const string PrefabRubblePile = "Assets/Wreckage/Prefabs/Wreckage_RubblePile.prefab";
        private const string PrefabRubbleScatter = "Assets/Wreckage/Prefabs/Wreckage_RubbleScatter.prefab";
        private const string PrefabSteelBeam = "Assets/Wreckage/Prefabs/Wreckage_SteelBeam.prefab";
        private const string PrefabSteelTruss = "Assets/Wreckage/Prefabs/Wreckage_SteelGirder_Truss.prefab";

        // Kaldırım / Yol
        private const string MatPavement = "Assets/ModularLowpolyStreetsFree/Materials/Roads/Pavement.mat";
        private const string MatGroundOuter = "Assets/Wreckage/Materials/mGround_7.mat";

        // Araç ve Karakterler
        private const string PrefabTableDesk = "Assets/nappin/OfficeEssentialsPack/Prefabs/(Prb)ConferenceTable.prefab";
        private const string PrefabFriend1 = "Assets/Floreswa/Prefabs/male01_1.prefab";
        private const string PrefabFriend2 = "Assets/Floreswa/Prefabs/male02_1.prefab";
        private const string PrefabPoliceCar = "Assets/Awbmecreations/Mobile Optimize-Free Low Poly Cars/Prefabs/Police Car N_4.prefab";
        private const string PrefabRescueVan = "Assets/Awbmecreations/Mobile Optimize-Free Low Poly Cars/Prefabs/N Van_10.prefab";
        private const string PrefabPlayer = "Assets/Mini First Person Controller/First Person Controller.prefab";

        [MenuItem("GameJam/Build End_UnderRubble Scene", false, 50)]
        public static void BuildScene()
        {
            Debug.Log("[BuildUnderRubbleSceneEditor] End_UnderRubble sahnesi yoğun enkaz inşası başlatılıyor...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[BuildUnderRubbleSceneEditor] Sahne bulunamadı: {ScenePath}");
                return;
            }

            CleanupExistingEnvironment();

            GameObject envRoot = new GameObject(EnvironmentRootName);
            envRoot.transform.position = Vector3.zero;
            envRoot.transform.rotation = Quaternion.identity;

            SetupAtmosphereAndLighting(envRoot);
            Zone1Context z1Context = BuildZone1EnclosedPocket(envRoot);
            Zone2Context z2Context = BuildZone2DenseOuterWreckage(envRoot);
            SetupSceneManager(envRoot, z1Context, z2Context);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=green>[BuildUnderRubbleSceneEditor] Yoğun enkaz sahnesi başarıyla inşa edildi ve kaydedildi!</color>");
        }

        private static void CleanupExistingEnvironment()
        {
            GameObject oldRoot = GameObject.Find(EnvironmentRootName);
            if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot);

            GameObject oldPlayer = GameObject.Find("First Person Controller");
            if (oldPlayer != null) Undo.DestroyObjectImmediate(oldPlayer);

            GameObject oldCamera = GameObject.Find("Main Camera");
            if (oldCamera != null && oldCamera.transform.parent == null)
            {
                Undo.DestroyObjectImmediate(oldCamera);
            }
        }

        private static void SetupAtmosphereAndLighting(GameObject root)
        {
            GameObject lightingGroup = new GameObject("00_Lighting_And_Atmosphere");
            lightingGroup.transform.SetParent(root.transform);

            // Ay Işığı (Loş gece ışığı)
            GameObject dirLightGo = GameObject.Find("Directional Light");
            if (dirLightGo == null) dirLightGo = new GameObject("Directional Light");
            dirLightGo.transform.SetParent(lightingGroup.transform);
            dirLightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Light dirLight = dirLightGo.GetComponent<Light>();
            if (dirLight == null) dirLight = dirLightGo.AddComponent<Light>();

            dirLight.type = LightType.Directional;
            dirLight.color = new Color(0.24f, 0.32f, 0.52f);
            dirLight.intensity = 0.30f;
            dirLight.shadows = LightShadows.Soft;

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.035f, 0.045f, 0.08f);
            RenderSettings.ambientEquatorColor = new Color(0.018f, 0.024f, 0.045f);
            RenderSettings.ambientGroundColor = new Color(0.010f, 0.014f, 0.022f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.025f;
            RenderSettings.fogColor = new Color(0.015f, 0.020f, 0.035f);
        }

        private class Zone1Context
        {
            public GameObject player;
            public Light flashlight;
            public Camera playerCamera;
            public GameObject friend1;
            public GameObject friend2;
        }

        private class Zone2Context
        {
            public Camera outtroCamera;
            public Transform outtroCameraMountPoint;
            public Light rescueLight;
        }

        #region Bölge 1: Tam Kapalı Klostrofobik Enkaz Cebi (İç Mekan)
        private static Zone1Context BuildZone1EnclosedPocket(GameObject root)
        {
            Zone1Context ctx = new Zone1Context();

            GameObject z1Group = new GameObject("01_Zone1_Enclosed_RubblePocket");
            z1Group.transform.SetParent(root.transform);

            // 1. Zemin (Kaldırım Karo Tabanı)
            GameObject floorGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorGo.name = "Zone1_Floor_Pavement";
            floorGo.transform.SetParent(z1Group.transform);
            floorGo.transform.position = new Vector3(0f, -0.1f, 0f);
            floorGo.transform.localScale = new Vector3(10f, 0.2f, 10f);
            var pavementMat = AssetDatabase.LoadAssetAtPath<Material>(MatPavement);
            if (pavementMat != null) floorGo.GetComponent<Renderer>().sharedMaterial = pavementMat;

            // 2. Yaşam Üçgenini Kuran Sağlam Masa (Devrilmiş, arkaya yaslı)
            GameObject table = InstantiatePrefabSafe(PrefabTableDesk, z1Group.transform,
                new Vector3(2.2f, 0.45f, 0.4f),
                Quaternion.Euler(68f, -18f, 28f),
                Vector3.one * 1.0f);
            if (table != null) table.name = "LifeTriangle_SturdyTable";

            // 3. İki Baygın Arkadaş (Normal 1:1 insan boyutunda)
            ctx.friend1 = InstantiatePrefabSafe(PrefabFriend1, z1Group.transform,
                new Vector3(1.7f, 0.05f, -0.8f),
                Quaternion.Euler(0f, 45f, 85f),
                Vector3.one);
            if (ctx.friend1 != null)
            {
                ctx.friend1.name = "Friend_Unconscious_Male1";
                var anim = ctx.friend1.GetComponentInChildren<Animator>();
                if (anim != null) anim.enabled = false;
                foreach (var c in ctx.friend1.GetComponentsInChildren<Collider>()) c.isTrigger = true;
            }

            ctx.friend2 = InstantiatePrefabSafe(PrefabFriend2, z1Group.transform,
                new Vector3(1.8f, 0.05f, 1.4f),
                Quaternion.Euler(85f, 115f, -15f),
                Vector3.one);
            if (ctx.friend2 != null)
            {
                ctx.friend2.name = "Friend_Unconscious_Male2";
                var anim = ctx.friend2.GetComponentInChildren<Animator>();
                if (anim != null) anim.enabled = false;
                foreach (var c in ctx.friend2.GetComponentsInChildren<Collider>()) c.isTrigger = true;
            }

            // 4. TAM KAPALI TAVAN VE DUVARLAR (Gökyüzünü tamamen kapatan çift katmanlı beton bloklar)
            GameObject ceilingGroup = new GameObject("Zone1_Completely_Closed_Ceiling");
            ceilingGroup.transform.SetParent(z1Group.transform);

            // A) Ana Çökmüş Tavan Katmanı 1 (Masaya ve sol duvara dayanan dev beton tabakalar)
            InstantiatePrefabSafe(PrefabCollapsedSlab, ceilingGroup.transform,
                new Vector3(0.3f, 2.3f, 0.2f),
                Quaternion.Euler(-14f, 20f, 12f),
                new Vector3(2.2f, 1.2f, 2.2f));

            // B) Çökmüş Tavan Katmanı 2 (Arka ve üstü örten, gökyüzünü %100 kapatan çapraz beton blok)
            InstantiatePrefabSafe(PrefabCollapsedSlab, ceilingGroup.transform,
                new Vector3(-0.6f, 2.5f, -0.4f),
                Quaternion.Euler(16f, -35f, -10f),
                new Vector3(2.4f, 1.2f, 2.4f));

            // C) Ön Tavan Kapatıcı (Ön tarafı örten beton blok)
            InstantiatePrefabSafe(PrefabCollapsedSlab, ceilingGroup.transform,
                new Vector3(0.2f, 2.4f, 1.8f),
                Quaternion.Euler(22f, 15f, -14f),
                new Vector3(2.0f, 1.2f, 2.0f));

            // D) Sol Duvar (Kırık harabe köşe ve duvar)
            InstantiatePrefabSafe(PrefabConcreteCorner, ceilingGroup.transform,
                new Vector3(-2.8f, 0f, 0.2f),
                Quaternion.Euler(0f, 120f, 0f),
                Vector3.one * 1.2f);

            InstantiatePrefabSafe(PrefabConcreteWall, ceilingGroup.transform,
                new Vector3(-2.9f, 0f, -1.8f),
                Quaternion.Euler(5f, 85f, 0f),
                Vector3.one * 1.1f);

            // E) Arka Duvar (Arkayı kapatan harabe köşe)
            InstantiatePrefabSafe(PrefabConcreteCorner, ceilingGroup.transform,
                new Vector3(0.2f, 0f, -2.8f),
                Quaternion.Euler(0f, 30f, 0f),
                Vector3.one * 1.2f);

            InstantiatePrefabSafe(PrefabConcreteWall, ceilingGroup.transform,
                new Vector3(1.8f, 0f, -2.9f),
                Quaternion.Euler(-6f, -10f, 0f),
                Vector3.one * 1.1f);

            // F) Sağ ve Ön Duvarları Kapatan Molozlar ve Kirişler
            InstantiatePrefabSafe(PrefabSteelBeam, ceilingGroup.transform,
                new Vector3(2.4f, 1.4f, 0.5f),
                Quaternion.Euler(35f, 42f, -18f),
                Vector3.one * 1.2f);

            InstantiatePrefabSafe(PrefabSteelTruss, ceilingGroup.transform,
                new Vector3(-1.8f, 1.6f, 1.8f),
                Quaternion.Euler(-25f, 140f, 20f),
                Vector3.one * 1.1f);

            InstantiatePrefabSafe(PrefabBrokenPlanks, ceilingGroup.transform,
                new Vector3(0.4f, 2.0f, -1.2f),
                Quaternion.Euler(38f, 25f, -15f),
                Vector3.one);

            // 5. Enkaz Çatlaklarından Sızan Atmosferik Işık Demetleri (Zifiri kapalı ortamdaki yegane sızıntılar)
            GameObject lightLeakGroup = new GameObject("Zone1_LightLeaks");
            lightLeakGroup.transform.SetParent(z1Group.transform);
            CreateLightLeak(lightLeakGroup.transform, new Vector3(1.6f, 2.6f, 0.8f), Quaternion.LookRotation(new Vector3(-0.4f, -1f, -0.2f)), new Color(0.72f, 0.84f, 1.0f), 2.5f, 25f);
            CreateLightLeak(lightLeakGroup.transform, new Vector3(-1.8f, 2.5f, -0.6f), Quaternion.LookRotation(new Vector3(0.4f, -1f, 0.3f)), new Color(0.80f, 0.90f, 1.0f), 2.0f, 22f);

            // 6. Oyuncu (First Person Controller - Normal İnsan Boyutunda)
            ctx.player = InstantiatePrefabSafe(PrefabPlayer, root.transform, new Vector3(-0.4f, 0.05f, -0.4f), Quaternion.Euler(0f, 35f, 0f), Vector3.one);
            if (ctx.player != null)
            {
                ctx.player.name = "First Person Controller";

                var crawl = ctx.player.GetComponent<PlayerCrawlController>();
                if (crawl == null) crawl = ctx.player.AddComponent<PlayerCrawlController>();
                crawl.standingHeight = 1.75f;
                crawl.standingRadius = 0.35f;
                crawl.standingCenterY = 0.88f;
                crawl.eyeLevelHeight = 1.45f;
                crawl.exploreSpeed = 1.6f;

                Camera cam = ctx.player.GetComponentInChildren<Camera>(true);
                ctx.playerCamera = cam;
                if (cam != null)
                {
                    cam.enabled = true;
                    cam.tag = "MainCamera";
                    cam.clearFlags = CameraClearFlags.Skybox;
                    cam.nearClipPlane = 0.05f;
                    cam.transform.localPosition = new Vector3(0f, 1.45f, 0f);
                    cam.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);
                }

                Transform camTransform = cam != null ? cam.transform : ctx.player.transform;
                GameObject flGo = new GameObject("Flashlight_SpotLight");
                flGo.transform.SetParent(camTransform);
                flGo.transform.localPosition = new Vector3(0.15f, -0.15f, 0.25f);
                flGo.transform.localRotation = Quaternion.identity;

                Light flLight = flGo.AddComponent<Light>();
                flLight.type = LightType.Spot;
                flLight.range = 22f;
                flLight.spotAngle = 60f;
                flLight.innerSpotAngle = 38f;
                flLight.color = new Color(1.0f, 0.96f, 0.88f);
                flLight.intensity = 8.5f;
                flLight.shadows = LightShadows.Soft;
                flLight.enabled = false;
                ctx.flashlight = flLight;

                // Gövde mesh'ini gizle
                Transform capsuleMesh = ctx.player.transform.Find("Capsule Mesh");
                if (capsuleMesh != null)
                {
                    var rend = capsuleMesh.GetComponent<Renderer>();
                    if (rend != null) rend.enabled = false;
                }
            }

            return ctx;
        }

        private static void CreateLightLeak(Transform parent, Vector3 pos, Quaternion rot, Color col, float intensity, float angle)
        {
            GameObject leakGo = new GameObject("LightLeak_Beam");
            leakGo.transform.SetParent(parent);
            leakGo.transform.position = pos;
            leakGo.transform.rotation = rot;

            Light light = leakGo.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 5.5f;
            light.spotAngle = angle;
            light.innerSpotAngle = angle * 0.6f;
            light.color = col;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
        }
        #endregion

        #region Bölge 2: Yoğun ve Devasa Dış Yıkıntı Alanı (55x55m)
        private static Zone2Context BuildZone2DenseOuterWreckage(GameObject root)
        {
            Zone2Context ctx = new Zone2Context();

            GameObject z2Group = new GameObject("02_Zone2_OuterWreckage_Field");
            z2Group.transform.SetParent(root.transform);

            // 1. Geniş Taban: Pavement / Kaldırım Karo Izgarası (120x120m devasa alan)
            GameObject groundPlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            groundPlane.name = "Zone2_Wide_DebrisGround";
            groundPlane.transform.SetParent(z2Group.transform);
            groundPlane.transform.position = new Vector3(0f, -0.15f, 0f);
            groundPlane.transform.localScale = new Vector3(120f, 0.2f, 120f);
            var groundMat = AssetDatabase.LoadAssetAtPath<Material>(MatPavement);
            if (groundMat == null) groundMat = AssetDatabase.LoadAssetAtPath<Material>(MatGroundOuter);
            if (groundMat != null) groundPlane.GetComponent<Renderer>().sharedMaterial = groundMat;

            // 2. AŞIRI YOĞUN PROSEDÜREL YIKINTI DAĞILIMI (Deterministik Seed: 2026)
            UnityEngine.Random.InitState(2026);

            // A) Çökmüş Beton Duvarlar (75 adet - Sıkı barikat ve yıkık şehir)
            GameObject wallsGroup = new GameObject("Outer_ConcreteWalls_Broken");
            wallsGroup.transform.SetParent(z2Group.transform);

            for (int i = 0; i < 75; i++)
            {
                float r = UnityEngine.Random.Range(4.5f, 42.0f);
                float theta = (i / 75f) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.1f, 0.1f);
                Vector3 pos = new Vector3(Mathf.Cos(theta) * r, 0f, Mathf.Sin(theta) * r);

                float rotY = UnityEngine.Random.Range(0f, 360f);
                float tiltX = UnityEngine.Random.Range(-22f, 32f);
                float tiltZ = UnityEngine.Random.Range(-22f, 32f);

                InstantiatePrefabSafe(PrefabConcreteWall, wallsGroup.transform,
                    pos,
                    Quaternion.Euler(tiltX, rotY, tiltZ),
                    Vector3.one * UnityEngine.Random.Range(1.05f, 1.55f));
            }

            // B) Çelik Kafesler ve Kirişler (50 adet - Göğe yükselen endüstriyel iskeletler)
            GameObject trussesGroup = new GameObject("Outer_SteelGirder_Trusses");
            trussesGroup.transform.SetParent(z2Group.transform);

            for (int i = 0; i < 50; i++)
            {
                float r = UnityEngine.Random.Range(4.8f, 40.0f);
                float theta = (i / 50f) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.15f, 0.15f);
                Vector3 pos = new Vector3(Mathf.Cos(theta) * r, 0.3f, Mathf.Sin(theta) * r);

                float angle = UnityEngine.Random.Range(30f, 75f);
                float rotY = UnityEngine.Random.Range(0f, 360f);

                InstantiatePrefabSafe(PrefabSteelTruss, trussesGroup.transform,
                    pos,
                    Quaternion.Euler(angle, rotY, UnityEngine.Random.Range(-15f, 15f)),
                    Vector3.one * UnityEngine.Random.Range(1.1f, 1.7f));
            }

            // C) Büyük Moloz Tepeleri (90 adet - Dağ gibi yığılmış molozlar)
            GameObject rubblePilesGroup = new GameObject("Outer_RubblePiles");
            rubblePilesGroup.transform.SetParent(z2Group.transform);

            for (int i = 0; i < 90; i++)
            {
                float r = UnityEngine.Random.Range(4.0f, 44.0f);
                float theta = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                Vector3 pos = new Vector3(Mathf.Cos(theta) * r, 0f, Mathf.Sin(theta) * r);

                InstantiatePrefabSafe(PrefabRubblePile, rubblePilesGroup.transform,
                    pos,
                    Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f),
                    Vector3.one * UnityEngine.Random.Range(1.3f, 2.5f));
            }

            // D) Moloz Serpintileri (130 adet - Yerleri tamamen kaplayan kırık parçalar)
            GameObject scatterGroup = new GameObject("Outer_RubbleScatter");
            scatterGroup.transform.SetParent(z2Group.transform);

            for (int i = 0; i < 130; i++)
            {
                float r = UnityEngine.Random.Range(3.5f, 46.0f);
                float theta = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                Vector3 pos = new Vector3(Mathf.Cos(theta) * r, 0.02f, Mathf.Sin(theta) * r);

                InstantiatePrefabSafe(PrefabRubbleScatter, scatterGroup.transform,
                    pos,
                    Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f),
                    Vector3.one * UnityEngine.Random.Range(1.0f, 2.0f));
            }

            // E) Çelik Kiriş ve Paneller (55 adet - Yıkılmış saç paneller ve I-Beamler)
            GameObject metalDebrisGroup = new GameObject("Outer_MetalDebris");
            metalDebrisGroup.transform.SetParent(z2Group.transform);

            for (int i = 0; i < 55; i++)
            {
                float r = UnityEngine.Random.Range(4.2f, 42.0f);
                float theta = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                Vector3 pos = new Vector3(Mathf.Cos(theta) * r, 0.1f, Mathf.Sin(theta) * r);

                string prefab = (i % 2 == 0) ? PrefabSteelBeam : PrefabBentPanel;
                InstantiatePrefabSafe(prefab, metalDebrisGroup.transform,
                    pos,
                    Quaternion.Euler(UnityEngine.Random.Range(-25f, 35f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-25f, 25f)),
                    Vector3.one * UnityEngine.Random.Range(1.1f, 1.5f));
            }

            // 3. Acil Durum / AFAD Araçları ve Çakarlar
            GameObject rescueGroup = new GameObject("Outer_EmergencyServices_Outpost");
            rescueGroup.transform.SetParent(z2Group.transform);

            // Polis / AFAD Aracı 1
            GameObject policeCar = InstantiatePrefabSafe(PrefabPoliceCar, rescueGroup.transform,
                new Vector3(20.5f, 0f, 19.5f),
                Quaternion.Euler(0f, -145f, 0f),
                Vector3.one);
            if (policeCar != null)
            {
                policeCar.name = "AFAD_Police_Vehicle";
                CreateEmergencyBeaconPair(policeCar.transform, new Vector3(0f, 1.6f, 0.2f));
            }

            // Kurtarma Destek Minibüsü 2
            GameObject rescueVan = InstantiatePrefabSafe(PrefabRescueVan, rescueGroup.transform,
                new Vector3(-20.5f, 0f, 17.5f),
                Quaternion.Euler(0f, 130f, 0f),
                Vector3.one);
            if (rescueVan != null)
            {
                rescueVan.name = "Rescue_Support_Van";
                CreateEmergencyBeaconPair(rescueVan.transform, new Vector3(0f, 2.1f, -0.3f));
            }

            // Güçlü AFAD Projektörleri
            ctx.rescueLight = CreateRescueSearchlight(rescueGroup.transform,
                new Vector3(21.0f, 6.0f, 21.0f),
                Vector3.zero,
                new Color(0.95f, 0.98f, 1.0f), 8.5f, 48f);

            CreateRescueSearchlight(rescueGroup.transform,
                new Vector3(-21.5f, 6.0f, 18.0f),
                Vector3.zero,
                new Color(1.0f, 0.96f, 0.90f), 8.0f, 50f);

            // 4. Outtro Kamera Montaj Noktası (Tepe Sinematik Açısı)
            GameObject mountGo = new GameObject("OuttroCamera_MountPoint");
            mountGo.transform.SetParent(z2Group.transform);
            mountGo.transform.position = new Vector3(0f, 16.5f, -16.5f);
            mountGo.transform.rotation = Quaternion.Euler(44f, 0f, 0f);
            ctx.outtroCameraMountPoint = mountGo.transform;

            Camera outtroCam = mountGo.AddComponent<Camera>();
            outtroCam.fieldOfView = 52f;
            outtroCam.nearClipPlane = 0.3f;
            outtroCam.farClipPlane = 500f;
            outtroCam.clearFlags = CameraClearFlags.Skybox;
            outtroCam.enabled = false;
            ctx.outtroCamera = outtroCam;

            return ctx;
        }

        private static void CreateEmergencyBeaconPair(Transform parent, Vector3 localPos)
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
            redLight.range = 18f;
            redLight.intensity = 8.0f;

            GameObject blueGo = new GameObject("Blue_Light");
            blueGo.transform.SetParent(pairGo.transform);
            blueGo.transform.localPosition = new Vector3(0.35f, 0f, 0f);
            Light blueLight = blueGo.AddComponent<Light>();
            blueLight.type = LightType.Point;
            blueLight.color = new Color(0.12f, 0.38f, 1.0f);
            blueLight.range = 18f;
            blueLight.intensity = 8.0f;

            pairGo.AddComponent<EmergencyBeaconFlasher>();
        }

        private static Light CreateRescueSearchlight(Transform parent, Vector3 pos, Vector3 target, Color col, float intensity, float angle)
        {
            GameObject slGo = new GameObject("Rescue_Searchlight_Tower");
            slGo.transform.SetParent(parent);
            slGo.transform.position = pos;
            slGo.transform.rotation = Quaternion.LookRotation((target - pos).normalized);

            Light light = slGo.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 80f;
            light.spotAngle = angle;
            light.innerSpotAngle = angle * 0.7f;
            light.color = col;
            light.intensity = intensity * 3.0f;
            light.shadows = LightShadows.Soft;
            return light;
        }
        #endregion

        #region Sahne Yöneticisi (UnderRubbleSceneManager)
        private static void SetupSceneManager(GameObject root, Zone1Context z1, Zone2Context z2)
        {
            GameObject managerGo = new GameObject("UnderRubbleSceneManager");
            managerGo.transform.SetParent(root.transform);
            managerGo.transform.position = Vector3.zero;

            var manager = managerGo.AddComponent<UnderRubbleSceneManager>();
            manager.ConfigureReferences(
                z1.player,
                z1.flashlight,
                z1.playerCamera,
                z2.outtroCamera,
                z2.outtroCameraMountPoint,
                z1.friend1,
                z1.friend2,
                z2.rescueLight
            );

            Debug.Log("[BuildUnderRubbleSceneEditor] UnderRubbleSceneManager başarıyla yapılandırıldı.");
        }
        #endregion

        #region Yardımcı Prefab Yükleme
        private static GameObject InstantiatePrefabSafe(string path, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[BuildUnderRubbleSceneEditor] Prefab yüklenemedi: {path}");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (instance != null)
            {
                instance.transform.localPosition = localPos;
                instance.transform.localRotation = localRot;
                instance.transform.localScale = localScale;
                Undo.RegisterCreatedObjectUndo(instance, "Instantiate " + instance.name);
            }

            return instance;
        }
        #endregion
    }
}
