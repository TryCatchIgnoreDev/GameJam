using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameJam.Market
{
    public class MarketUIManager : MonoBehaviour
    {
        public static MarketUIManager Instance { get; private set; }

        [Header("--- Paneller & Konteynerler ---")]
        public GameObject marketRootPanel;
        public RectTransform gridContainer;
        public RectTransform itemListContainer;
        public RectTransform floatingCursorContainer;

        [Header("--- UI Elemanları ---")]
        public Button closeButton;
        public Button readyButton;
        public Button clearGridButton;
        public TextMeshProUGUI statusText;

        // Hikaye & Alışveriş Durumu
        public bool HasCompletedShopping { get; private set; }
        public event System.Action OnShoppingCompleted;

        // Mantıksal 6x6 Grid
        public MarketInventoryGrid Grid { get; private set; } = new MarketInventoryGrid();
        private MarketGridCellUI[,] cellUIs = new MarketGridCellUI[MarketInventoryGrid.GRID_SIZE, MarketInventoryGrid.GRID_SIZE];

        // Havuz (Pool) verisi ve kartları
        private List<PlacedItemInstance> itemPool = new List<PlacedItemInstance>();
        private List<MarketItemCardUI> poolCardUIs = new List<MarketItemCardUI>();

        // Sürükle-Bırak (Drag & Drop) durumu
        private PlacedItemInstance draggingItem = null;
        private Vector2Int dragGrabOffset = Vector2Int.zero;
        private Vector2Int? currentHoveredCell = null;

        // Oyuncu kontrol referansları
        private FirstPersonMovement playerMovement;
        private FirstPersonLook playerLook;

        // Vurgu renkleri
        private readonly Color validHighlightColor = new Color(0.22f, 0.78f, 0.35f, 0.65f);
        private readonly Color invalidHighlightColor = new Color(0.85f, 0.22f, 0.22f, 0.65f);

        private bool isUIInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureRuntimeEventSystem();
            InitItemPool();
        }

        private void Start()
        {
            EnsureInitialized();

            if (marketRootPanel != null && !IsMarketOpen)
            {
                marketRootPanel.SetActive(false);
            }
        }

        private void EnsureInitialized()
        {
            if (isUIInitialized) return;
            isUIInitialized = true;

            EnsureRuntimeEventSystem();
            if (itemPool.Count == 0)
            {
                InitItemPool();
            }

            FindPlayerScripts();

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseMarket);
            }

            if (readyButton != null)
            {
                readyButton.onClick.RemoveAllListeners();
                readyButton.onClick.AddListener(CloseMarket);
            }

            if (clearGridButton != null)
            {
                clearGridButton.onClick.RemoveAllListeners();
                clearGridButton.onClick.AddListener(OnClearGridClicked);
            }

            BuildGridUI();
            BuildPoolUI();
        }

        private void EnsureRuntimeEventSystem()
        {
            if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null)
                return;

            GameObject esObj = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var inputSysType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSysType != null)
            {
                esObj.AddComponent(inputSysType);
            }
            else
            {
                esObj.AddComponent<StandaloneInputModule>();
            }
#else
            esObj.AddComponent<StandaloneInputModule>();
#endif
        }

        private void FindPlayerScripts()
        {
            if (playerMovement == null)
            {
                playerMovement = FindFirstObjectByType<FirstPersonMovement>(FindObjectsInactive.Include);
            }

            if (playerLook == null)
            {
                if (playerMovement != null)
                {
                    playerLook = playerMovement.GetComponentInChildren<FirstPersonLook>(true);
                }
                if (playerLook == null && Camera.main != null)
                {
                    playerLook = Camera.main.GetComponentInParent<FirstPersonLook>() ?? Camera.main.GetComponent<FirstPersonLook>();
                }
                if (playerLook == null)
                {
                    playerLook = FindFirstObjectByType<FirstPersonLook>(FindObjectsInactive.Include);
                }
            }
        }

        /// <summary>
        /// Sınırlı Havuz (Pool): Deprem eşyaları ve arkadaşın istediği çeldirici eşyalar.
        /// 6x6 (36 birim) ızgarada tüm deprem eşyaları (29 birim) sığabilir, ancak gereksiz eşyalar sığmaz.
        /// </summary>
        private void InitItemPool()
        {
            itemPool.Clear();

            // 1. Su Bidonu / Şişeleri (Deprem Eşyası - 6 birim, 3x2 blok)
            var waterJug = new MarketItemData(
                "water_jug",
                "Su (3x Şişe / 5L)",
                "Deprem Eşyası",
                true,
                new Color(0.24f, 0.58f, 0.95f),
                new Vector2Int[] {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                    new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1)
                }
            );
            itemPool.Add(new PlacedItemInstance(waterJug));

            // 2. Deprem Düdüğü (Deprem Eşyası - 3 birim, L-şekli)
            var whistle = new MarketItemData(
                "whistle",
                "Deprem Düdüğü",
                "Deprem Eşyası",
                true,
                new Color(0.18f, 0.82f, 0.86f),
                new Vector2Int[] {
                    new Vector2Int(0, 0), new Vector2Int(1, 0),
                    new Vector2Int(1, 1)
                }
            );
            itemPool.Add(new PlacedItemInstance(whistle));

            // 3. Uyku Tulumu & Battaniye (Deprem Eşyası - 6 birim, 2x3 rulo)
            var sleepingBag = new MarketItemData(
                "sleeping_bag",
                "Uyku Tulumu & Battaniye",
                "Deprem Eşyası",
                true,
                new Color(0.32f, 0.72f, 0.42f),
                new Vector2Int[] {
                    new Vector2Int(0, 0), new Vector2Int(1, 0),
                    new Vector2Int(0, 1), new Vector2Int(1, 1),
                    new Vector2Int(0, 2), new Vector2Int(1, 2)
                }
            );
            itemPool.Add(new PlacedItemInstance(sleepingBag));

            // 4. El Feneri (Deprem Eşyası - 4 birim, J/fener şekli)
            var flashlight = new MarketItemData(
                "flashlight",
                "El Feneri",
                "Deprem Eşyası",
                true,
                new Color(0.96f, 0.78f, 0.16f),
                new Vector2Int[] {
                    new Vector2Int(0, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(0, 2), new Vector2Int(1, 2)
                }
            );
            itemPool.Add(new PlacedItemInstance(flashlight));

            // 5. Konserve Paketi (Deprem Eşyası - 5 birim, P-şekli)
            var cannedFood = new MarketItemData(
                "canned_food",
                "Konserve Paketi",
                "Deprem Eşyası",
                true,
                new Color(0.92f, 0.50f, 0.18f),
                new Vector2Int[] {
                    new Vector2Int(0, 0), new Vector2Int(1, 0),
                    new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1)
                }
            );
            itemPool.Add(new PlacedItemInstance(cannedFood));

            // 6. İlk Yardım Kiti (Deprem Eşyası - 5 birim, 2x2+1 şekli)
            var firstAid = new MarketItemData(
                "first_aid",
                "İlk Yardım Kiti",
                "Deprem Eşyası",
                true,
                new Color(0.92f, 0.24f, 0.28f),
                new Vector2Int[] {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                    new Vector2Int(0, 1), new Vector2Int(1, 1)
                }
            );
            itemPool.Add(new PlacedItemInstance(firstAid));

            // 7. Kola ve Cips Paketi (Arkadaşın İstedi - Gereksiz Eşya - 6 birim)
            var colaChips = new MarketItemData(
                "cola_chips",
                "Kola ve Cips Paketi",
                "Arkadaşın İstedi (Gereksiz)",
                false,
                new Color(0.85f, 0.32f, 0.18f),
                new Vector2Int[] {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                    new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1)
                }
            );
            itemPool.Add(new PlacedItemInstance(colaChips));

            // 8. Oyun Konsolu (Gereksiz Eşya - 6 birim)
            var console = new MarketItemData(
                "game_console",
                "Oyun Konsolu",
                "Gereksiz Eşya",
                false,
                new Color(0.65f, 0.35f, 0.90f),
                new Vector2Int[] {
                    new Vector2Int(0, 0), new Vector2Int(2, 0),
                    new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
                    new Vector2Int(1, 2)
                }
            );
            itemPool.Add(new PlacedItemInstance(console));

            // 9. Buharlı Ütü (Gereksiz Eşya - 5 birim)
            var iron = new MarketItemData(
                "steam_iron",
                "Buharlı Ütü",
                "Gereksiz Eşya",
                false,
                new Color(0.92f, 0.30f, 0.58f),
                new Vector2Int[] {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                    new Vector2Int(0, 1), new Vector2Int(1, 1)
                }
            );
            itemPool.Add(new PlacedItemInstance(iron));
        }

        private void Update()
        {
            if (marketRootPanel == null || !marketRootPanel.activeSelf)
                return;

            HandleInputAndDrag();
        }

        private void HandleInputAndDrag()
        {
            bool escPressed = false;
            bool rotatePressed = false;
            bool leftMouseReleased = false;
            Vector2 mousePos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame) escPressed = true;
                if (Keyboard.current.rKey.wasPressedThisFrame) rotatePressed = true;
            }
            if (Mouse.current != null)
            {
                mousePos = Mouse.current.position.ReadValue();
                if (Mouse.current.rightButton.wasPressedThisFrame) rotatePressed = true;
                if (Mouse.current.leftButton.wasReleasedThisFrame) leftMouseReleased = true;
            }
#else
            mousePos = Input.mousePosition;
            if (Input.GetKeyDown(KeyCode.Escape)) escPressed = true;
            if (Input.GetKeyDown(KeyCode.R)) rotatePressed = true;
            if (Input.GetMouseButtonDown(1)) rotatePressed = true;
            if (Input.GetMouseButtonUp(0)) leftMouseReleased = true;
#endif

            if (escPressed)
            {
                if (draggingItem != null)
                {
                    CancelCurrentDrag();
                }
                else
                {
                    CloseMarket();
                }
                return;
            }

            if (draggingItem != null)
            {
                if (rotatePressed)
                {
                    RotateDraggingItem();
                }

                UpdateFloatingCursorPosition(mousePos);
                UpdateHoveredGridCell(mousePos);

                if (leftMouseReleased)
                {
                    FinishDragAndDrop();
                }
            }
        }

        public void OpenMarket()
        {
            EnsureInitialized();
            FindPlayerScripts();

            if (marketRootPanel != null)
                marketRootPanel.SetActive(true);

            // Oyuncu hareketini tamamen durdur ve devre dışı bırak
            if (playerMovement != null)
            {
                playerMovement.StopMovement();
                playerMovement.enabled = false;
            }
            var allMovements = FindObjectsByType<FirstPersonMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var m in allMovements)
            {
                if (m != null)
                {
                    m.StopMovement();
                    m.enabled = false;
                }
            }

            // Kamera dönmesini durdur, input birikimini sıfırla ve devre dışı bırak
            if (playerLook != null)
            {
                playerLook.ResetLookInput();
                playerLook.enabled = false;
            }
            var allLooks = FindObjectsByType<FirstPersonLook>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var l in allLooks)
            {
                if (l != null)
                {
                    l.ResetLookInput();
                    l.enabled = false;
                }
            }

            // UI için imleci serbest bırak
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            CancelCurrentDrag();
            UpdateGridVisuals();
            RefreshPoolCards();
            UpdateStatusSummary();
        }

        public void CloseMarket()
        {
            CancelCurrentDrag();

            if (Grid != null && Grid.PlacedItems.Count > 0)
            {
                HasCompletedShopping = true;
                OnShoppingCompleted?.Invoke();
            }

            if (marketRootPanel != null)
                marketRootPanel.SetActive(false);

            // Oyun kontrolü için imleci tekrar kilitle
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Oyuncu hareketini tekrar aç
            if (playerMovement != null)
            {
                playerMovement.StopMovement();
                playerMovement.enabled = true;
            }
            var allMovements = FindObjectsByType<FirstPersonMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var m in allMovements)
            {
                if (m != null)
                {
                    m.StopMovement();
                    m.enabled = true;
                }
            }

            // Kamera bakışını tekrar aç ve mevcut karakter açısıyla senkronize et
            if (playerLook != null)
            {
                playerLook.ResetLookInput();
                playerLook.SyncRotationWithCharacter();
                playerLook.enabled = true;
            }
            var allLooks = FindObjectsByType<FirstPersonLook>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var l in allLooks)
            {
                if (l != null)
                {
                    l.ResetLookInput();
                    l.SyncRotationWithCharacter();
                    l.enabled = true;
                }
            }
        }

        public void CompleteShopping()
        {
            CloseMarket();
        }

        public bool IsMarketOpen => marketRootPanel != null && marketRootPanel.activeSelf;

        #region --- Hikaye & Envanter Sorguları ---

        public bool HasEssentialItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || Grid == null || Grid.PlacedItems == null)
                return false;

            foreach (var item in Grid.PlacedItems)
            {
                if (item != null && item.data != null && item.data.id == itemId)
                    return true;
            }
            return false;
        }

        public bool HasWhistle() => HasEssentialItem("whistle");

        public bool HasFlashlight() => HasEssentialItem("flashlight");

        public bool HasWater() => HasEssentialItem("water_jug");

        public bool HasSleepingBag() => HasEssentialItem("sleeping_bag");

        public bool HasCannedFood() => HasEssentialItem("canned_food");

        public bool HasFirstAid() => HasEssentialItem("first_aid");

        public int GetPlacedEssentialCount()
        {
            if (Grid == null || Grid.PlacedItems == null)
                return 0;

            int count = 0;
            foreach (var item in Grid.PlacedItems)
            {
                if (item != null && item.data != null && item.data.isEssential)
                    count++;
            }
            return count;
        }

        public int GetPlacedJunkCount()
        {
            if (Grid == null || Grid.PlacedItems == null)
                return 0;

            int count = 0;
            foreach (var item in Grid.PlacedItems)
            {
                if (item != null && item.data != null && !item.data.isEssential)
                    count++;
            }
            return count;
        }

        public bool HasAllCoreEssentials()
        {
            return HasWhistle() && HasFlashlight() && HasWater() && HasSleepingBag() && HasCannedFood() && HasFirstAid();
        }

        #endregion

        #region --- Sürükle & Bırak (Drag & Drop) Sistemi ---

        private void StartDragFromPool(PlacedItemInstance poolItem)
        {
            if (poolItem == null || poolItem.isPlacedOnGrid) return;

            draggingItem = poolItem;
            Vector2 center = draggingItem.GetShapeCenterOffset();
            dragGrabOffset = new Vector2Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y));

            UpdateFloatingCursorVisual();
            RefreshPoolCards();
            SetStatusMessage($"'{poolItem.data.itemName}' sürükleniyor... (Döndür: R / Sağ Tık, Bırak: Sol Tık Bırak)");
        }

        private void HandleCellPointerDown(Vector2Int coord, PointerEventData eventData)
        {
            if (draggingItem != null) return;

            PlacedItemInstance existing = Grid.GetItemAt(coord);
            if (existing != null)
            {
                // Hücredeki eşyayı tuttuğu noktadan sürüklemeye başla
                dragGrabOffset = new Vector2Int(coord.x - existing.gridPos.x, coord.y - existing.gridPos.y);
                Grid.Remove(existing);
                existing.isPlacedOnGrid = false;
                draggingItem = existing;

                UpdateGridVisuals();
                UpdateFloatingCursorVisual();
                RefreshPoolCards();
                SetStatusMessage($"'{existing.data.itemName}' taşınıyor... Havuza geri bırakmak için ızgara dışına bırakın.");
            }
        }

        private void RotateDraggingItem()
        {
            if (draggingItem == null) return;

            draggingItem.RotateClockwise();
            Vector2 center = draggingItem.GetShapeCenterOffset();
            dragGrabOffset = new Vector2Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y));

            UpdateFloatingCursorVisual();
        }

        private void UpdateHoveredGridCell(Vector2 screenMousePos)
        {
            ClearHighlights();
            currentHoveredCell = null;

            int size = MarketInventoryGrid.GRID_SIZE;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    RectTransform cellRt = cellUIs[x, y].GetComponent<RectTransform>();
                    if (RectTransformUtility.RectangleContainsScreenPoint(cellRt, screenMousePos, null))
                    {
                        currentHoveredCell = new Vector2Int(x, y);
                        break;
                    }
                }
                if (currentHoveredCell.HasValue) break;
            }

            if (currentHoveredCell.HasValue && draggingItem != null)
            {
                Vector2Int rootPos = currentHoveredCell.Value - dragGrabOffset;
                HighlightGridForShape(draggingItem, rootPos);
            }
        }

        private void FinishDragAndDrop()
        {
            if (draggingItem == null) return;

            if (currentHoveredCell.HasValue)
            {
                Vector2Int rootPos = currentHoveredCell.Value - dragGrabOffset;
                if (Grid.CanPlace(draggingItem, rootPos))
                {
                    Grid.Place(draggingItem, rootPos);
                    draggingItem.isPlacedOnGrid = true;
                    string placedName = draggingItem.data.itemName;

                    draggingItem = null;
                    if (floatingCursorContainer != null)
                        floatingCursorContainer.gameObject.SetActive(false);

                    ClearHighlights();
                    UpdateGridVisuals();
                    RefreshPoolCards();
                    UpdateStatusSummary();
                    return;
                }
            }

            // Geçersiz yere veya ızgara dışına bırakıldıysa havuza (pool) geri döner
            string returnedName = draggingItem.data.itemName;
            CancelCurrentDrag();
            UpdateStatusSummary();
        }

        public void CancelCurrentDrag()
        {
            if (draggingItem != null)
            {
                draggingItem.isPlacedOnGrid = false;
                draggingItem = null;
            }

            if (floatingCursorContainer != null)
                floatingCursorContainer.gameObject.SetActive(false);

            currentHoveredCell = null;
            ClearHighlights();
            UpdateGridVisuals();
            RefreshPoolCards();
        }

        #endregion

        #region --- Grid & Havuz UI Oluşturma ---

        private void BuildGridUI()
        {
            if (gridContainer == null) return;

            for (int i = gridContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(gridContainer.GetChild(i).gameObject);
            }

            int size = MarketInventoryGrid.GRID_SIZE;
            for (int y = size - 1; y >= 0; y--)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2Int coord = new Vector2Int(x, y);
                    GameObject cellObj = CreateCellObject(coord);
                    cellObj.transform.SetParent(gridContainer, false);

                    MarketGridCellUI cellUI = cellObj.GetComponent<MarketGridCellUI>();
                    cellUI.Init(coord);
                    cellUI.OnCellPointerDown += HandleCellPointerDown;

                    cellUIs[x, y] = cellUI;
                }
            }
        }

        private GameObject CreateCellObject(Vector2Int coord)
        {
            GameObject cell = new GameObject($"Cell_{coord.x}_{coord.y}", typeof(RectTransform), typeof(Image), typeof(MarketGridCellUI));
            Image bg = cell.GetComponent<Image>();
            bg.color = new Color(0.18f, 0.20f, 0.23f, 1f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(cell.transform, false);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = new Vector2(-4, -4);
            Image fillImg = fill.GetComponent<Image>();
            fillImg.color = Color.clear;
            fillImg.raycastTarget = false;
            fill.SetActive(false);

            GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(fill.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 13;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            textObj.SetActive(false);

            MarketGridCellUI ui = cell.GetComponent<MarketGridCellUI>();
            ui.backgroundImage = bg;
            ui.itemFillImage = fillImg;
            ui.labelText = tmp;

            return cell;
        }

        private void BuildPoolUI()
        {
            if (itemListContainer == null) return;

            for (int i = itemListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(itemListContainer.GetChild(i).gameObject);
            }
            poolCardUIs.Clear();

            foreach (var poolItem in itemPool)
            {
                GameObject cardObj = CreatePoolCardObject(poolItem);
                cardObj.transform.SetParent(itemListContainer, false);
                MarketItemCardUI cardUI = cardObj.GetComponent<MarketItemCardUI>();
                poolCardUIs.Add(cardUI);
            }
        }

        private GameObject CreatePoolCardObject(PlacedItemInstance poolItem)
        {
            GameObject card = new GameObject("PoolCard_" + poolItem.data.id, typeof(RectTransform), typeof(Image), typeof(MarketItemCardUI));
            RectTransform rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(240, 62);

            Image cardBg = card.GetComponent<Image>();
            cardBg.color = new Color(0.18f, 0.20f, 0.23f, 1f);

            GameObject preview = new GameObject("Preview", typeof(RectTransform));
            preview.transform.SetParent(card.transform, false);
            RectTransform previewRt = preview.GetComponent<RectTransform>();
            previewRt.anchorMin = new Vector2(0f, 0.5f);
            previewRt.anchorMax = new Vector2(0f, 0.5f);
            previewRt.anchoredPosition = new Vector2(40f, 0f);
            previewRt.sizeDelta = new Vector2(56, 56);

            GameObject title = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            title.transform.SetParent(card.transform, false);
            RectTransform titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.anchoredPosition = new Vector2(95f, -6f);
            titleRt.sizeDelta = new Vector2(-105, 28);
            TextMeshProUGUI titleTmp = title.GetComponent<TextMeshProUGUI>();
            titleTmp.enableAutoSizing = true;
            titleTmp.fontSizeMin = 11;
            titleTmp.fontSizeMax = 16;
            titleTmp.fontSize = 15;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = Color.white;
            titleTmp.raycastTarget = false;

            GameObject desc = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
            desc.transform.SetParent(card.transform, false);
            RectTransform descRt = desc.GetComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0f, 0f);
            descRt.anchorMax = new Vector2(1f, 0.5f);
            descRt.anchoredPosition = new Vector2(95f, 6f);
            descRt.sizeDelta = new Vector2(-105, 24);
            TextMeshProUGUI descTmp = desc.GetComponent<TextMeshProUGUI>();
            descTmp.enableAutoSizing = true;
            descTmp.fontSizeMin = 10;
            descTmp.fontSizeMax = 12;
            descTmp.fontSize = 12;
            descTmp.color = new Color(0.78f, 0.80f, 0.84f);
            descTmp.raycastTarget = false;

            MarketItemCardUI ui = card.GetComponent<MarketItemCardUI>();
            ui.cardBackground = cardBg;
            ui.titleText = titleTmp;
            ui.descText = descTmp;
            ui.previewContainer = preview.transform;
            ui.Setup(poolItem, StartDragFromPool);

            return card;
        }

        private void RefreshPoolCards()
        {
            foreach (var card in poolCardUIs)
            {
                if (card != null)
                {
                    card.RefreshState(draggingItem);
                }
            }
        }

        #endregion

        #region --- Görsel Güncellemeler ---

        private void HighlightGridForShape(PlacedItemInstance item, Vector2Int rootPos)
        {
            bool canPlace = Grid.CanPlace(item, rootPos);
            Color hlColor = canPlace ? validHighlightColor : invalidHighlightColor;

            List<Vector2Int> occupied = item.GetOccupiedCells(rootPos);
            for (int i = 0; i < occupied.Count; i++)
            {
                Vector2Int c = occupied[i];
                if (Grid.IsWithinBounds(c))
                {
                    cellUIs[c.x, c.y].SetHighlight(hlColor);
                }
            }
        }

        private void ClearHighlights()
        {
            int size = MarketInventoryGrid.GRID_SIZE;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    if (cellUIs[x, y] != null)
                        cellUIs[x, y].ClearHighlight();
                }
            }
        }

        public void UpdateGridVisuals()
        {
            int size = MarketInventoryGrid.GRID_SIZE;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    if (cellUIs[x, y] == null) continue;

                    Vector2Int coord = new Vector2Int(x, y);
                    PlacedItemInstance item = Grid.GetItemAt(coord);
                    if (item != null)
                    {
                        string sym = (coord == item.gridPos) ? item.data.itemName.Substring(0, Mathf.Min(4, item.data.itemName.Length)) : "";
                        cellUIs[x, y].SetOccupied(item.data.itemColor, sym);
                    }
                    else
                    {
                        cellUIs[x, y].ResetVisual();
                    }
                }
            }
        }

        private void OnClearGridClicked()
        {
            CancelCurrentDrag();
            Grid.Clear();
            foreach (var item in itemPool)
            {
                item.isPlacedOnGrid = false;
            }
            HasCompletedShopping = false;
            UpdateGridVisuals();
            RefreshPoolCards();
            UpdateStatusSummary();
        }

        public void UpdateStatusSummary()
        {
            int essentialCount = GetPlacedEssentialCount();
            int junkCount = GetPlacedJunkCount();

            if (essentialCount == 6)
            {
                if (junkCount > 0)
                {
                    SetStatusMessage($"Tüm deprem eşyaları çantada ({essentialCount}/6)! Ancak {junkCount} gereksiz eşya da var.");
                }
                else
                {
                    SetStatusMessage($"Tebrikler! 6/6 Deprem Eşyası çantaya sığdırıldı! Alışverişi tamamlayabilirsiniz (ESC / Hazır).");
                }
            }
            else
            {
                if (junkCount > 0)
                {
                    SetStatusMessage($"Deprem Çantası: {essentialCount}/6 Deprem Eşyası | {junkCount} Gereksiz Eşya (R: Döndür)");
                }
                else
                {
                    SetStatusMessage($"Deprem Çantası: {essentialCount}/6 Deprem Eşyası yerleştirildi. (R: Döndür)");
                }
            }
        }

        #endregion

        #region --- Floating Drag Preview ---

        private void UpdateFloatingCursorVisual()
        {
            if (floatingCursorContainer == null) return;

            if (draggingItem == null)
            {
                floatingCursorContainer.gameObject.SetActive(false);
                return;
            }

            floatingCursorContainer.gameObject.SetActive(true);

            for (int i = floatingCursorContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(floatingCursorContainer.GetChild(i).gameObject);
            }

            float blockSize = 64f;
            float spacing = 6f;

            foreach (var cell in draggingItem.currentShape)
            {
                GameObject block = new GameObject("DragBlock", typeof(RectTransform), typeof(Image));
                block.transform.SetParent(floatingCursorContainer, false);

                RectTransform rt = block.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(blockSize, blockSize);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);

                float posX = (cell.x - dragGrabOffset.x) * (blockSize + spacing);
                float posY = (cell.y - dragGrabOffset.y) * (blockSize + spacing);
                rt.anchoredPosition = new Vector2(posX, posY);

                Image img = block.GetComponent<Image>();
                Color ghostColor = draggingItem.data.itemColor;
                ghostColor.a = 0.80f;
                img.color = ghostColor;
                img.raycastTarget = false;
            }
        }

        private void UpdateFloatingCursorPosition(Vector2 screenMousePos)
        {
            if (floatingCursorContainer == null || !floatingCursorContainer.gameObject.activeSelf)
                return;

            floatingCursorContainer.position = new Vector3(screenMousePos.x, screenMousePos.y, 0f);
        }

        #endregion

        public void SetStatusMessage(string msg)
        {
            if (statusText != null)
            {
                statusText.text = msg;
            }
        }
    }
}
