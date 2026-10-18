using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Basit, şık ve modüler Crosshair (Nişangah) GUI sistemi.
/// Sahneler arası kalıcıdır (DontDestroyOnLoad), harici görsel gerektirmeden kendi Canvas'ını oluşturur.
/// Etkileşime girilebilir nesnelerde (DoorInteract, StoryInteractable vb.) dinamik renk/aralık animasyonu yapar.
/// Menü/Market/Sinematik ekranlarda otomatik olarak gizlenir ve kapandığında geri döner.
/// </summary>
public class CrosshairUI : MonoBehaviour
{
    public enum CrosshairStyle
    {
        DotOnly,        // Sadece ortada minimal nokta
        CrossOnly,      // Sadece 4 yönlü artı (+) çizgileri
        DotAndCross     // Hem nokta hem artı (+) çizgileri
    }

    private static CrosshairUI _instance;

    public static CrosshairUI Instance
    {
        get
        {
            if (_instance == null)
            {
                EnsureInstance();
            }
            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }

    [Header("--- Görünüm Stili ---")]
    [Tooltip("Crosshair tipi: Sadece Nokta, Sadece Artı (+) veya İkisi Birden.")]
    public CrosshairStyle style = CrosshairStyle.DotAndCross;

    [Tooltip("Normal durumdaki crosshair rengi.")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.85f);

    [Tooltip("Etkileşimli bir nesneye (örn. Kapı, Poster) bakarken alacağı renk.")]
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
    private float currentDotScale = 1.0f;
    private Color currentColor;
    private bool isHoveringInteractable;
    private bool isExternallyHidden = false;
    private Camera playerCam;

    /// <summary>
    /// Singleton instance'ın var olduğundan emin olur, yoksa DontDestroyOnLoad ile otomatik oluşturur.
    /// </summary>
    public static CrosshairUI EnsureInstance()
    {
        if (_instance != null)
        {
            if (_instance.crosshairCanvas == null)
            {
                _instance.BuildCrosshairUI();
            }
            return _instance;
        }

        // Sahnedeki mevcut CrosshairUI bileşenini ara
        CrosshairUI found = FindFirstObjectByType<CrosshairUI>();
        if (found != null)
        {
            // Eğer bir Player nesnesi üzerindeyse veya hiyerarşide alt nesneyse, kalıcı ayrı GameObject'e taşı
            if (found.transform.parent != null || found.GetComponent<FirstPersonMovement>() != null || found.GetComponent<CharacterController>() != null || found.gameObject.name != "[Crosshair_HUD_System]")
            {
                GameObject dedicated = new GameObject("[Crosshair_HUD_System]");
                DontDestroyOnLoad(dedicated);
                CrosshairUI newInstance = dedicated.AddComponent<CrosshairUI>();
                CopySettings(found, newInstance);
                _instance = newInstance;
                _instance.BuildCrosshairUI();
                _instance.currentGap = _instance.centerGap;
                _instance.currentColor = _instance.normalColor;
                Destroy(found);
                return _instance;
            }

            _instance = found;
            if (_instance.transform.parent != null)
            {
                _instance.transform.SetParent(null);
            }
            DontDestroyOnLoad(_instance.gameObject);
            _instance.BuildCrosshairUI();
            _instance.currentGap = _instance.centerGap;
            _instance.currentColor = _instance.normalColor;
            return _instance;
        }

        // Sahnede hiç yoksa yeni bir GameObject oluştur
        GameObject go = new GameObject("[Crosshair_HUD_System]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<CrosshairUI>();
        _instance.BuildCrosshairUI();
        _instance.currentGap = _instance.centerGap;
        _instance.currentColor = _instance.normalColor;
        return _instance;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        EnsureInstance();
    }

    private static void CopySettings(CrosshairUI source, CrosshairUI dest)
    {
        if (source == null || dest == null) return;
        dest.style = source.style;
        dest.normalColor = source.normalColor;
        dest.interactColor = source.interactColor;
        dest.shadowColor = source.shadowColor;
        dest.dotSize = source.dotSize;
        dest.lineLength = source.lineLength;
        dest.lineThickness = source.lineThickness;
        dest.centerGap = source.centerGap;
        dest.highlightOnInteractable = source.highlightOnInteractable;
        dest.interactGapBonus = source.interactGapBonus;
        dest.hideWhenCursorUnlocked = source.hideWhenCursorUnlocked;
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // Sahnede zaten kalıcı bir instance var, bu nesneyi değil sadece bu component'i sil
            Destroy(this);
            return;
        }

        // Eğer bu component bir Player veya alt nesne üzerine takılmışsa, kalıcı sisteme delege et
        if (transform.parent != null || GetComponent<FirstPersonMovement>() != null || GetComponent<CharacterController>() != null || gameObject.name != "[Crosshair_HUD_System]")
        {
            EnsureInstance();
            if (_instance != this)
            {
                Destroy(this);
                return;
            }
        }

        _instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        BuildCrosshairUI();
        currentGap = centerGap;
        currentColor = normalColor;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindCamera();
        isHoveringInteractable = false;
        currentGap = centerGap;
        currentColor = normalColor;
        currentDotScale = 1.0f;

        ApplyVisualSettings(true);
    }

    void Start()
    {
        FindCamera();
        ApplyVisualSettings(true);
    }

    void Update()
    {
        if (crosshairCanvas == null || rootRect == null) return;

        bool shouldShow = !ShouldHideCrosshair();
        if (rootRect.gameObject.activeSelf != shouldShow)
        {
            rootRect.gameObject.SetActive(shouldShow);
        }

        if (!shouldShow) return;

        CheckInteractableHover();
        UpdateDynamicVisuals();
    }

    /// <summary>
    /// Menüler, market, seçim ekranları, sinematik anlar veya imlecin serbest olduğu durumlar için görünürlük kontrolü.
    /// </summary>
    private bool ShouldHideCrosshair()
    {
        // 1. Dışarıdan elle gizleme emri verilmişse
        if (isExternallyHidden) return true;

        // 2. Ana Menü sahnesinde gizle
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (activeSceneName == "MainMenu") return true;

        // 3. Market arayüzü açıksa gizle
        if (GameJam.Market.MarketUIManager.Instance != null && GameJam.Market.MarketUIManager.Instance.IsMarketOpen)
        {
            return true;
        }

        // 4. Arkadaş yönlendirme seçim menüsü açıksa gizle
        if (FriendChoiceUI.Instance != null && FriendChoiceUI.Instance.IsChoiceActive)
        {
            return true;
        }

        // 5. Tam ekran ölüm veya merkez farkındalık mesajı gösteriliyorsa gizle
        if (ScreenFaderUI.Instance != null && (ScreenFaderUI.Instance.IsDeathScreenActive || ScreenFaderUI.Instance.IsCenterMessageActive))
        {
            return true;
        }

        // 6. Enkaz altı final kurtarılma veya sinematik tepe kamerası aktifse gizle
        if (GameJam.Story.UnderRubbleSceneManager.Instance != null && GameJam.Story.UnderRubbleSceneManager.Instance.IsRescued)
        {
            return true;
        }

        // 7. Fare imleci serbest ve görünür durumdaysa (herhangi bir UI penceresi açıkken)
        if (hideWhenCursorUnlocked)
        {
            if (Cursor.visible && Cursor.lockState == CursorLockMode.None)
            {
                return true;
            }
        }

        // 8. Sahne içinde geçerli bir oyuncu kamerası bulunamıyorsa gizle
        if (playerCam == null || !playerCam.gameObject.activeInHierarchy || !playerCam.isActiveAndEnabled)
        {
            FindCamera();
            if (playerCam == null || !playerCam.gameObject.activeInHierarchy || !playerCam.isActiveAndEnabled)
            {
                return true;
            }
        }

        return false;
    }

    private void FindCamera()
    {
        // 1. FirstPersonLook bileşeni olan kamerayı bul (oyuncu gözü)
        FirstPersonLook fpl = FindFirstObjectByType<FirstPersonLook>();
        if (fpl != null)
        {
            Camera cam = fpl.GetComponent<Camera>() ?? fpl.GetComponentInChildren<Camera>();
            if (cam != null && cam.isActiveAndEnabled)
            {
                playerCam = cam;
                return;
            }
        }

        // 2. Tag'i MainCamera olan kamera
        if (Camera.main != null && Camera.main.isActiveAndEnabled)
        {
            playerCam = Camera.main;
            return;
        }

        // 3. Sahnedeki diğer aktif kameralar
        Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var c in cams)
        {
            if (c != null && c.isActiveAndEnabled && c.gameObject.name != "OuttroCamera")
            {
                playerCam = c;
                return;
            }
        }
    }

    private void CheckInteractableHover()
    {
        isHoveringInteractable = false;
        if (!highlightOnInteractable) return;

        // 1. StoryInteractable tarafından o an odaklanılmış nesne varsa anında hover kabul et
        if (StoryInteractable.CurrentFocused != null && StoryInteractable.CurrentFocused.isInteractable)
        {
            isHoveringInteractable = true;
            return;
        }

        // 2. Oyuncu kamerası kontrolü
        if (playerCam == null || !playerCam.gameObject.activeInHierarchy || !playerCam.isActiveAndEnabled)
        {
            FindCamera();
            if (playerCam == null) return;
        }

        // 3. Kamera merkezinden ileriye Raycast kontrolü (Kapılar ve diğer etkileşim objeleri)
        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, 4.5f, ~0, QueryTriggerInteraction.Collide);
        if (hits != null && hits.Length > 0)
        {
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (col == null) continue;

                // Oyuncunun kendi collider'larını atla
                if (col.GetComponentInParent<FirstPersonMovement>() != null || col.GetComponentInParent<CharacterController>() != null)
                {
                    continue;
                }

                DoorInteract door = col.GetComponentInParent<DoorInteract>();
                if (door != null)
                {
                    if (hits[i].distance <= door.interactDistance)
                    {
                        isHoveringInteractable = true;
                        return;
                    }
                }

                StoryInteractable storyObj = col.GetComponentInParent<StoryInteractable>();
                if (storyObj != null && storyObj.isInteractable)
                {
                    if (hits[i].distance <= storyObj.interactDistance)
                    {
                        isHoveringInteractable = true;
                        return;
                    }
                }

                // Katı bir engele çarptıysak ve bu bir etkileşim nesnesi değilse arkasını görme
                if (!col.isTrigger)
                {
                    break;
                }
            }
        }
    }

    private void UpdateDynamicVisuals()
    {
        float targetGap = isHoveringInteractable ? (centerGap + interactGapBonus) : centerGap;
        Color targetCol = isHoveringInteractable ? interactColor : normalColor;
        float targetDotScale = isHoveringInteractable ? 1.25f : 1.0f;

        currentGap = Mathf.Lerp(currentGap, targetGap, Time.unscaledDeltaTime * 14f);
        currentColor = Color.Lerp(currentColor, targetCol, Time.unscaledDeltaTime * 14f);
        currentDotScale = Mathf.Lerp(currentDotScale, targetDotScale, Time.unscaledDeltaTime * 14f);

        ApplyVisualSettings(false);
    }

    private void ApplyVisualSettings(bool instant)
    {
        if (rootRect == null) return;

        float gap = instant ? centerGap : currentGap;
        Color col = instant ? normalColor : currentColor;
        float dScale = instant ? 1.0f : currentDotScale;

        bool showDot = (style == CrosshairStyle.DotOnly || style == CrosshairStyle.DotAndCross);
        bool showCross = (style == CrosshairStyle.CrossOnly || style == CrosshairStyle.DotAndCross);

        if (dotImage != null)
        {
            dotImage.gameObject.SetActive(showDot);
            dotImage.rectTransform.sizeDelta = new Vector2(dotSize * dScale, dotSize * dScale);
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
        crosshairCanvas.sortingOrder = 50; // Oyun içi HUD katmanı (menü ve fader'ların altında)

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
        isExternallyHidden = !visible;
        if (rootRect != null)
        {
            rootRect.gameObject.SetActive(!isExternallyHidden && !ShouldHideCrosshair());
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
