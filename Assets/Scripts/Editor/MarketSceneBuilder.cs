using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using GameJam.Market;
using UnityEngine.EventSystems;
using UnityEditor.SceneManagement;

namespace GameJam.Market.Editor
{
    public static class MarketSceneBuilder
    {
        public const string PrefabFolder = "Assets/Prefabs";
        public const string PrefabPath = "Assets/Prefabs/MarketSystem.prefab";
        public const string CrystalMatPath = "Assets/Prefabs/MarketCrystal.mat";

        [MenuItem("GameJam/Setup Market System", false, 1)]
        public static void SetupMarketSystem()
        {
            EnsurePrefabFolder();
            EnsureEventSystem();

            // Mevcut sahnedeki eski objelerin konumunu koru ve temizle
            Vector3 spawnPos = GetTargetSpawnPosition();
            CleanupExistingSceneObjects();

            // Tek bir kök obje altında tüm Market sistemini (Trigger + 3D Görsel + Canvas UI) oluştur
            GameObject tempRoot = BuildUnifiedMarketObject();

            // Prefab olarak kaydet
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(tempRoot, PrefabPath);
            Object.DestroyImmediate(tempRoot);

            // Sahneye Prefab Instance olarak yerleştir
            if (prefabAsset != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
                instance.name = "MarketSystem";
                instance.transform.position = spawnPos;
                Undo.RegisterCreatedObjectUndo(instance, "Create MarketSystem Prefab Instance");
                Selection.activeGameObject = instance;
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[MarketSceneBuilder] Tek parça Market Prefabı oluşturuldu ({PrefabPath}) ve sahneye yerleştirildi.");
        }

        private static void EnsurePrefabFolder()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
        }

        private static Vector3 GetTargetSpawnPosition()
        {
            GameObject existingSystem = GameObject.Find("MarketSystem");
            if (existingSystem != null)
                return existingSystem.transform.position;

            GameObject existingTrigger = GameObject.Find("MarketTriggerZone");
            if (existingTrigger != null)
                return existingTrigger.transform.position;

            GameObject player = GameObject.Find("First Person Controller");
            if (player != null)
                return player.transform.position + player.transform.forward * 2.5f;

            return new Vector3(30.64f, -37.70f, -1853.88f);
        }

        private static void CleanupExistingSceneObjects()
        {
            string[] namesToRemove = { "MarketSystem", "MarketTriggerZone", "MarketCanvas" };
            foreach (string objName in namesToRemove)
            {
                GameObject existing = GameObject.Find(objName);
                while (existing != null)
                {
                    Undo.DestroyObjectImmediate(existing);
                    existing = GameObject.Find(objName);
                }
            }
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject esObj = new GameObject("EventSystem", typeof(EventSystem));
                var inputSysType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSysType != null)
                {
                    esObj.AddComponent(inputSysType);
                }
                else
                {
                    esObj.AddComponent<StandaloneInputModule>();
                }
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }
        }

        private static Material GetOrCreateCrystalMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(CrystalMatPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.color = new Color(0.2f, 0.75f, 0.95f);
                AssetDatabase.CreateAsset(mat, CrystalMatPath);
            }
            return mat;
        }

        private static GameObject BuildUnifiedMarketObject()
        {
            // 1. Ana Kök: 3D Trigger + Görsel Stand + Altında UI Canvas
            GameObject rootObj = new GameObject("MarketSystem");
            rootObj.transform.position = Vector3.zero;

            BoxCollider col = rootObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(3.0f, 3.0f, 3.0f);
            col.center = new Vector3(0, 1.5f, 0);

            GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stand.name = "StandBase";
            stand.transform.SetParent(rootObj.transform, false);
            stand.transform.localPosition = new Vector3(0, 0.4f, 0);
            stand.transform.localScale = new Vector3(1.2f, 0.4f, 1.2f);
            var standCol = stand.GetComponent<Collider>();
            if (standCol != null) Object.DestroyImmediate(standCol);

            GameObject floatingCrystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floatingCrystal.name = "FloatingCrystal";
            floatingCrystal.transform.SetParent(rootObj.transform, false);
            floatingCrystal.transform.localPosition = new Vector3(0, 1.6f, 0);
            floatingCrystal.transform.localScale = new Vector3(0.5f, 0.7f, 0.5f);
            floatingCrystal.transform.localRotation = Quaternion.Euler(45, 45, 45);
            var crystalCol = floatingCrystal.GetComponent<Collider>();
            if (crystalCol != null) Object.DestroyImmediate(crystalCol);

            var mr = floatingCrystal.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = GetOrCreateCrystalMaterial();
            }

            MarketTrigger mt = rootObj.AddComponent<MarketTrigger>();
            mt.visualMesh = floatingCrystal.transform;
            mt.bobbingSpeed = 2.5f;
            mt.bobbingAmount = 0.2f;
            mt.rotationSpeed = 60f;

            // 2. Alt Obje Olarak MarketCanvas
            MarketUIManager uiManager = BuildMarketCanvasChild(rootObj.transform);
            mt.marketUI = uiManager;

            return rootObj;
        }

        private static MarketUIManager BuildMarketCanvasChild(Transform parent)
        {
            GameObject canvasObj = new GameObject("MarketCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(parent, false);

            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            MarketUIManager uiManager = canvasObj.AddComponent<MarketUIManager>();

            // Arka Plan Karartma
            GameObject rootPanel = new GameObject("MarketRootPanel", typeof(RectTransform), typeof(Image));
            rootPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rootRt = rootPanel.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            Image rootBg = rootPanel.GetComponent<Image>();
            rootBg.color = new Color(0.06f, 0.07f, 0.09f, 0.90f);

            uiManager.marketRootPanel = rootPanel;

            // Düz Ana Pencere
            GameObject window = new GameObject("Window", typeof(RectTransform), typeof(Image));
            window.transform.SetParent(rootPanel.transform, false);
            RectTransform winRt = window.GetComponent<RectTransform>();
            winRt.anchorMin = new Vector2(0.5f, 0.5f);
            winRt.anchorMax = new Vector2(0.5f, 0.5f);
            winRt.pivot = new Vector2(0.5f, 0.5f);
            winRt.sizeDelta = new Vector2(1180, 700);

            Image winBg = window.GetComponent<Image>();
            winBg.color = new Color(0.14f, 0.15f, 0.18f, 1f);

            // Başlık
            GameObject header = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
            header.transform.SetParent(window.transform, false);
            RectTransform headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0, -18);
            headerRt.sizeDelta = new Vector2(-120, 40);

            TextMeshProUGUI headerTmp = header.GetComponent<TextMeshProUGUI>();
            headerTmp.text = "MARKET RAFI & DEPREM ÇANTASI";
            headerTmp.fontSize = 26;
            headerTmp.fontStyle = FontStyles.Bold;
            headerTmp.alignment = TextAlignmentOptions.Center;
            headerTmp.color = Color.white;

            // Sol Panel: 6x6 Grid
            GameObject gridPanel = new GameObject("GridPanel", typeof(RectTransform), typeof(Image));
            gridPanel.transform.SetParent(window.transform, false);
            RectTransform gridPanelRt = gridPanel.GetComponent<RectTransform>();
            gridPanelRt.anchorMin = new Vector2(0f, 0.5f);
            gridPanelRt.anchorMax = new Vector2(0f, 0.5f);
            gridPanelRt.pivot = new Vector2(0f, 0.5f);
            gridPanelRt.anchoredPosition = new Vector2(45, 5);
            gridPanelRt.sizeDelta = new Vector2(500, 500);

            Image gridPanelBg = gridPanel.GetComponent<Image>();
            gridPanelBg.color = new Color(0.10f, 0.11f, 0.13f, 1f);

            GameObject gridTitle = new GameObject("GridTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            gridTitle.transform.SetParent(gridPanel.transform, false);
            RectTransform gridTitleRt = gridTitle.GetComponent<RectTransform>();
            gridTitleRt.anchorMin = new Vector2(0f, 1f);
            gridTitleRt.anchorMax = new Vector2(1f, 1f);
            gridTitleRt.pivot = new Vector2(0f, 0f);
            gridTitleRt.anchoredPosition = new Vector2(4, 6);
            gridTitleRt.sizeDelta = new Vector2(450, 28);
            TextMeshProUGUI gridTitleTmp = gridTitle.GetComponent<TextMeshProUGUI>();
            gridTitleTmp.text = "Deprem Çantası (6x6)";
            gridTitleTmp.fontSize = 18;
            gridTitleTmp.fontStyle = FontStyles.Bold;
            gridTitleTmp.alignment = TextAlignmentOptions.Left;
            gridTitleTmp.color = new Color(0.85f, 0.88f, 0.92f);

            GameObject gridContainer = new GameObject("GridContainer", typeof(RectTransform), typeof(GridLayoutGroup));
            gridContainer.transform.SetParent(gridPanel.transform, false);
            RectTransform gcRt = gridContainer.GetComponent<RectTransform>();
            gcRt.anchorMin = new Vector2(0.5f, 0.5f);
            gcRt.anchorMax = new Vector2(0.5f, 0.5f);
            gcRt.pivot = new Vector2(0.5f, 0.5f);
            gcRt.sizeDelta = new Vector2(460, 460);

            GridLayoutGroup glg = gridContainer.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(70, 70);
            glg.spacing = new Vector2(6, 6);
            glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.MiddleCenter;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 6;

            uiManager.gridContainer = gcRt;

            // Sağ Panel: Market Rafı (Havuz / Pool)
            GameObject catalogPanel = new GameObject("CatalogPanel", typeof(RectTransform), typeof(Image));
            catalogPanel.transform.SetParent(window.transform, false);
            RectTransform catRt = catalogPanel.GetComponent<RectTransform>();
            catRt.anchorMin = new Vector2(1f, 0.5f);
            catRt.anchorMax = new Vector2(1f, 0.5f);
            catRt.pivot = new Vector2(1f, 0.5f);
            catRt.anchoredPosition = new Vector2(-45, 5);
            catRt.sizeDelta = new Vector2(540, 500);

            Image catBg = catalogPanel.GetComponent<Image>();
            catBg.color = new Color(0.10f, 0.11f, 0.13f, 1f);

            GameObject catTitle = new GameObject("CatalogTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            catTitle.transform.SetParent(catalogPanel.transform, false);
            RectTransform catTitleRt = catTitle.GetComponent<RectTransform>();
            catTitleRt.anchorMin = new Vector2(0f, 1f);
            catTitleRt.anchorMax = new Vector2(1f, 1f);
            catTitleRt.pivot = new Vector2(0f, 0f);
            catTitleRt.anchoredPosition = new Vector2(4, 6);
            catTitleRt.sizeDelta = new Vector2(500, 28);
            TextMeshProUGUI catTitleTmp = catTitle.GetComponent<TextMeshProUGUI>();
            catTitleTmp.text = "Market Rafı (Basılı Tut & Çantaya Sürükle)";
            catTitleTmp.fontSize = 18;
            catTitleTmp.fontStyle = FontStyles.Bold;
            catTitleTmp.alignment = TextAlignmentOptions.Left;
            catTitleTmp.color = new Color(0.85f, 0.88f, 0.92f);

            GameObject scrollRectObj = new GameObject("ScrollContainer", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollRectObj.transform.SetParent(catalogPanel.transform, false);
            RectTransform srRt = scrollRectObj.GetComponent<RectTransform>();
            srRt.anchorMin = Vector2.zero;
            srRt.anchorMax = Vector2.one;
            srRt.sizeDelta = new Vector2(-16, -16);
            Image srImg = scrollRectObj.GetComponent<Image>();
            srImg.color = Color.clear;
            ScrollRect sr = scrollRectObj.GetComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(scrollRectObj.transform, false);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;
            Image vpImg = viewport.GetComponent<Image>();
            vpImg.color = Color.white;
            Mask mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = false;
            sr.viewport = vpRt;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 500);

            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = contentRt;
            uiManager.itemListContainer = contentRt;

            // Alt Çubuk
            GameObject bottomBar = new GameObject("BottomBar", typeof(RectTransform));
            bottomBar.transform.SetParent(window.transform, false);
            RectTransform bbRt = bottomBar.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0f, 0f);
            bbRt.anchorMax = new Vector2(1f, 0f);
            bbRt.pivot = new Vector2(0.5f, 0f);
            bbRt.anchoredPosition = new Vector2(0, 16);
            bbRt.sizeDelta = new Vector2(-90, 48);

            GameObject statusObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
            statusObj.transform.SetParent(bottomBar.transform, false);
            RectTransform statusRt = statusObj.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0f, 0.5f);
            statusRt.anchorMax = new Vector2(0.65f, 1f);
            statusRt.offsetMin = Vector2.zero;
            statusRt.offsetMax = Vector2.zero;
            TextMeshProUGUI statusTmp = statusObj.GetComponent<TextMeshProUGUI>();
            statusTmp.text = "Raftaki eşyaları basılı tutup 6x6 çantaya sürükleyin.";
            statusTmp.fontSize = 15;
            statusTmp.color = Color.white;
            uiManager.statusText = statusTmp;

            GameObject tipObj = new GameObject("TipText", typeof(RectTransform), typeof(TextMeshProUGUI));
            tipObj.transform.SetParent(bottomBar.transform, false);
            RectTransform tipRt = tipObj.GetComponent<RectTransform>();
            tipRt.anchorMin = new Vector2(0f, 0f);
            tipRt.anchorMax = new Vector2(0.65f, 0.5f);
            tipRt.offsetMin = Vector2.zero;
            tipRt.offsetMax = Vector2.zero;
            TextMeshProUGUI tipTmp = tipObj.GetComponent<TextMeshProUGUI>();
            tipTmp.text = "[Sol Tık Basılı Tut]: Sürükle & Bırak   |   [R / Sağ Tık]: Döndür   |   [ESC]: Kapat";
            tipTmp.fontSize = 13;
            tipTmp.color = new Color(0.68f, 0.72f, 0.78f);

            // Temizle Butonu
            GameObject clearBtnObj = new GameObject("ClearGridButton", typeof(RectTransform), typeof(Image), typeof(Button));
            clearBtnObj.transform.SetParent(bottomBar.transform, false);
            RectTransform clearRt = clearBtnObj.GetComponent<RectTransform>();
            clearRt.anchorMin = new Vector2(0.70f, 0f);
            clearRt.anchorMax = new Vector2(0.84f, 1f);
            clearRt.offsetMin = Vector2.zero;
            clearRt.offsetMax = Vector2.zero;
            Image clearImg = clearBtnObj.GetComponent<Image>();
            clearImg.color = new Color(0.26f, 0.28f, 0.32f, 1f);
            Button clearBtn = clearBtnObj.GetComponent<Button>();
            uiManager.clearGridButton = clearBtn;

            GameObject clearTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            clearTxt.transform.SetParent(clearBtnObj.transform, false);
            RectTransform ctxtRt = clearTxt.GetComponent<RectTransform>();
            ctxtRt.anchorMin = Vector2.zero;
            ctxtRt.anchorMax = Vector2.one;
            TextMeshProUGUI ctmp = clearTxt.GetComponent<TextMeshProUGUI>();
            ctmp.text = "Çantayı Boşalt";
            ctmp.fontSize = 14;
            ctmp.fontStyle = FontStyles.Bold;
            ctmp.alignment = TextAlignmentOptions.Center;
            ctmp.color = Color.white;

            // Hazır Butonu
            GameObject readyBtnObj = new GameObject("ReadyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            readyBtnObj.transform.SetParent(bottomBar.transform, false);
            RectTransform readyRt = readyBtnObj.GetComponent<RectTransform>();
            readyRt.anchorMin = new Vector2(0.86f, 0f);
            readyRt.anchorMax = new Vector2(1f, 1f);
            readyRt.offsetMin = Vector2.zero;
            readyRt.offsetMax = Vector2.zero;
            Image readyImg = readyBtnObj.GetComponent<Image>();
            readyImg.color = new Color(0.20f, 0.68f, 0.35f, 1f);
            Button readyBtn = readyBtnObj.GetComponent<Button>();
            uiManager.closeButton = readyBtn;

            GameObject readyTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            readyTxt.transform.SetParent(readyBtnObj.transform, false);
            RectTransform rtxtRt = readyTxt.GetComponent<RectTransform>();
            rtxtRt.anchorMin = Vector2.zero;
            rtxtRt.anchorMax = Vector2.one;
            TextMeshProUGUI rtmp = readyTxt.GetComponent<TextMeshProUGUI>();
            rtmp.text = "HAZIR";
            rtmp.fontSize = 15;
            rtmp.fontStyle = FontStyles.Bold;
            rtmp.alignment = TextAlignmentOptions.Center;
            rtmp.color = Color.white;

            // Floating Drag Container
            GameObject floatCursor = new GameObject("FloatingCursor", typeof(RectTransform));
            floatCursor.transform.SetParent(rootPanel.transform, false);
            RectTransform fcRt = floatCursor.GetComponent<RectTransform>();
            fcRt.sizeDelta = new Vector2(100, 100);
            fcRt.pivot = new Vector2(0.5f, 0.5f);
            floatCursor.SetActive(false);

            uiManager.floatingCursorContainer = fcRt;

            rootPanel.SetActive(false);

            return uiManager;
        }
    }
}
