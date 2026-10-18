using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Basit, şık ve modüler Crosshair (Nişangah) GUI sistemi.
/// Harici görsel (Sprite) gerektirmeden kendi Canvas ve UI parçalarını otomatik oluşturur.
/// Hem aydınlık hem karanlık ortamlarda net görünmesi için gölge/kontur (Shadow) içerir.
/// </summary>
public class CrosshairUI : MonoBehaviour
{
    public enum CrosshairStyle
    {
        DotOnly,        // Sadece ortada minimal nokta
        CrossOnly,      // Sadece 4 yönlü artı (+) çizgileri
        DotAndCross     // Hem nokta hem artı (+) çizgileri
    }

    public static CrosshairUI Instance { get; private set; }

    [Header("--- Görünüm Stili ---")]
    [Tooltip("Crosshair tipi: Sadece Nokta, Sadece Artı (+) veya İkisi Birden.")]
    public CrosshairStyle style = CrosshairStyle.DotAndCross;

    [Tooltip("Normal durumdaki crosshair rengi.")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.85f);

    [Tooltip("Etkileşimli bir nesneye (örn. Kapı) bakarken alacağı renk.")]
    public Color interactColor = new Color(1f, 0.85f, 0.35f, 1f);

    [Tooltip("Açık renkli duvarlarda da net görünmesi için dış gölge/kontur rengi.")]
    public Color shadowColor = new Color(0f, 0f, 0f, 0.7f);

    [Header("--- Boyut ve Ölçüler (Piksel) ---")]
    [Tooltip("Merkezdeki noktanın boyutu.")]
    [Range(1f, 20f)]
    public float dotSize = 4f;

    [Tooltip("Artı (+) çizgilerinin uzunluğu.")]
    [Range(2f, 40f)]
    public float lineLength = 8f;

    [Tooltip("Artı (+) çizgilerinin kalınlığı.")]
    [Range(1f, 10f)]
    public float lineThickness = 2f;

    [Tooltip("Merkez ile çizgiler arasındaki boşluk.")]
    [Range(0f, 30f)]
    public float centerGap = 5f;

    [Header("--- Dinamik Efektler ---")]
    [Tooltip("Etkileşimli nesnelere (Kapı vb.) bakarken crosshair rengi ve aralığı değişsin mi?")]
    public bool highlightOnInteractable = true;

    [Tooltip("Etkileşim sırasında çizgilerin dışa açılma miktarı.")]
    public float interactGapBonus = 3f;

    [Tooltip("Fare imleci serbestken (örn. Market veya menü açıkken) crosshair otomatik gizlensin mi?")]
    public bool hideWhenCursorUnlocked = true;

    // UI Referansları
    private Canvas crosshairCanvas;
    private RectTransform rootRect;
    private Image dotImage;
    private Image topLine;
    private Image bottomLine;
    private Image leftLine;
    private Image rightLine;

    private float currentGap;
    private Color currentColor;
    private bool isHoveringInteractable;
    private Camera playerCam;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildCrosshairUI();
        currentGap = centerGap;
        currentColor = normalColor;
    }

    void Start()
    {
        FindCamera();
        ApplyVisualSettings(true);
    }

    void Update()
    {
        if (crosshairCanvas == null) return;

        // Menü/Market açıkken (Cursor serbestken) gizleme kontrolü
        if (hideWhenCursorUnlocked)
        {
            bool shouldShow = Cursor.lockState == CursorLockMode.Locked || Application.isEditor && !IsAnyMenuBlocking();
            // Eğer imleç görünür ve serbestse (ör. Market ekranı açıldıysa) gizle
            if (Cursor.visible && Cursor.lockState == CursorLockMode.None)
            {
                shouldShow = false;
            }

            if (rootRect.gameObject.activeSelf != shouldShow)
            {
                rootRect.gameObject.SetActive(shouldShow);
            }

            if (!shouldShow) return;
        }

        CheckInteractableHover();
        UpdateDynamicVisuals();
    }

    private bool IsAnyMenuBlocking()
    {
        return Cursor.visible && Cursor.lockState == CursorLockMode.None;
    }

    private void FindCamera()
    {
        if (Camera.main != null)
        {
            playerCam = Camera.main;
        }
        else
        {
            playerCam = FindAnyObjectByType<Camera>();
        }
    }

    private void CheckInteractableHover()
    {
        isHoveringInteractable = false;
        if (!highlightOnInteractable) return;

        if (playerCam == null || !playerCam.gameObject.activeInHierarchy)
        {
            FindCamera();
            if (playerCam == null) return;
        }

        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 4f))
        {
            DoorInteract door = hit.collider.GetComponentInParent<DoorInteract>();
            if (door != null && Vector3.Distance(playerCam.transform.position, door.transform.position) <= door.interactDistance)
            {
                isHoveringInteractable = true;
                return;
            }

            StoryInteractable storyObj = hit.collider.GetComponentInParent<StoryInteractable>();
            if (storyObj != null && storyObj.isInteractable && Vector3.Distance(playerCam.transform.position, storyObj.transform.position) <= storyObj.interactDistance)
            {
                isHoveringInteractable = true;
                return;
            }
        }
    }

    private void UpdateDynamicVisuals()
    {
        float targetGap = isHoveringInteractable ? (centerGap + interactGapBonus) : centerGap;
        Color targetCol = isHoveringInteractable ? interactColor : normalColor;

        currentGap = Mathf.Lerp(currentGap, targetGap, Time.unscaledDeltaTime * 14f);
        currentColor = Color.Lerp(currentColor, targetCol, Time.unscaledDeltaTime * 14f);

        ApplyVisualSettings(false);
    }

    private void ApplyVisualSettings(bool instant)
    {
        if (rootRect == null) return;

        float gap = instant ? centerGap : currentGap;
        Color col = instant ? normalColor : currentColor;

        bool showDot = (style == CrosshairStyle.DotOnly || style == CrosshairStyle.DotAndCross);
        bool showCross = (style == CrosshairStyle.CrossOnly || style == CrosshairStyle.DotAndCross);

        if (dotImage != null)
        {
            dotImage.gameObject.SetActive(showDot);
            dotImage.rectTransform.sizeDelta = new Vector2(dotSize, dotSize);
            dotImage.color = col;
        }

        if (topLine != null)
        {
            topLine.gameObject.SetActive(showCross);
            topLine.rectTransform.sizeDelta = new Vector2(lineThickness, lineLength);
            topLine.rectTransform.anchoredPosition = new Vector2(0f, gap + lineLength * 0.5f);
            topLine.color = col;
        }

        if (bottomLine != null)
        {
            bottomLine.gameObject.SetActive(showCross);
            bottomLine.rectTransform.sizeDelta = new Vector2(lineThickness, lineLength);
            bottomLine.rectTransform.anchoredPosition = new Vector2(0f, -(gap + lineLength * 0.5f));
            bottomLine.color = col;
        }

        if (leftLine != null)
        {
            leftLine.gameObject.SetActive(showCross);
            leftLine.rectTransform.sizeDelta = new Vector2(lineLength, lineThickness);
            leftLine.rectTransform.anchoredPosition = new Vector2(-(gap + lineLength * 0.5f), 0f);
            leftLine.color = col;
        }

        if (rightLine != null)
        {
            rightLine.gameObject.SetActive(showCross);
            rightLine.rectTransform.sizeDelta = new Vector2(lineLength, lineThickness);
            rightLine.rectTransform.anchoredPosition = new Vector2(gap + lineLength * 0.5f, 0f);
            rightLine.color = col;
        }
    }

    private void BuildCrosshairUI()
    {
        if (crosshairCanvas != null) return;

        GameObject canvasObj = new GameObject("Crosshair_HUD_Canvas");
        canvasObj.transform.SetParent(transform, false);

        crosshairCanvas = canvasObj.AddComponent<Canvas>();
        crosshairCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        crosshairCanvas.sortingOrder = 900; // Oyun içi HUD katmanı

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject rootObj = new GameObject("CrosshairRoot");
        rootObj.transform.SetParent(canvasObj.transform, false);
        rootRect = rootObj.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(100f, 100f);

        dotImage = CreatePart("Dot", rootRect);
        topLine = CreatePart("TopLine", rootRect);
        bottomLine = CreatePart("BottomLine", rootRect);
        leftLine = CreatePart("LeftLine", rootRect);
        rightLine = CreatePart("RightLine", rootRect);
    }

    private Image CreatePart(string partName, Transform parent)
    {
        GameObject obj = new GameObject(partName);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        Image img = obj.AddComponent<Image>();
        img.raycastTarget = false;
        img.color = normalColor;

        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = shadowColor;
        shadow.effectDistance = new Vector2(1f, -1f);

        return img;
    }

    /// <summary>
    /// Dışarıdan crosshair görünürlüğünü açıp kapatmak için kullanılabilir.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (rootRect != null)
        {
            rootRect.gameObject.SetActive(visible);
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying && rootRect != null)
        {
            ApplyVisualSettings(true);
        }
    }
}
