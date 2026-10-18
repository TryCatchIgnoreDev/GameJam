using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Saat 21:00 depremi başladığında oyuncunun arkadaşlarını yönlendirmesi ve enkaz altında
/// düdük/fener kullanması için kendi ScreenSpaceOverlay Canvas'ını (sortingOrder = 920) otomatik kuran Singleton arayüz yöneticisi.
/// </summary>
public class FriendChoiceUI : MonoBehaviour
{
    public static FriendChoiceUI Instance { get; private set; }

    [Header("--- Durum Değişkenleri ---")]
    public bool IsChoiceActive { get; private set; }
    public bool IsRubblePromptActive { get; private set; }

    // Geri çağırımlar (Callbacks)
    private Action<bool> currentChoiceCallback;
    private Action onFlashlightCallback;
    private Action onWhistleCallback;

    // Enkaz aşaması: 0 = Fener bekleniyor ([F]), 1 = Düdük bekleniyor ([E])
    private int rubbleStep = 0;

    // UI Eleman Referansları
    private Canvas rootCanvas;
    private GameObject choicePanel;
    private GameObject rubblePanel;
    private Image rubbleBlackOverlay;
    private Image rubbleFlashlightGlow;
    private TextMeshProUGUI rubbleHeaderText;
    private TextMeshProUGUI rubblePromptText;

    private Button option1Button;
    private Button option2Button;

    // Ses ve Efektler
    private AudioSource audioSource;
    private AudioClip flashlightClickClip;
    private AudioClip whistleClip;

    // Dokular
    private Texture2D radialGlowTex;

    /// <summary>
    /// Singleton örneğini garanti eder. Yoksa sahnede arar veya hiyerarşide yeni bir obje oluşturur.
    /// </summary>
    public static FriendChoiceUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        FriendChoiceUI found = FindFirstObjectByType<FriendChoiceUI>();
        if (found != null)
        {
            Instance = found;
            Instance.EnsureInitialized();
            return Instance;
        }

        GameObject go = new GameObject("FriendChoiceUI");
        Instance = go.AddComponent<FriendChoiceUI>();
        Instance.EnsureInitialized();
        DontDestroyOnLoad(go);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        EnsureRuntimeEventSystem();
        EnsureAudioSource();
        EnsureCanvasHierarchy();
    }

    private void Update()
    {
        // 1. Acil Durum Seçimi Aktifken Tuş Kontrolleri ([1] veya [2])
        if (IsChoiceActive)
        {
            if (IsKey1Pressed())
            {
                OnOptionSelected(false);
            }
            else if (IsKey2Pressed())
            {
                OnOptionSelected(true);
            }
        }

        // 2. Enkaz Altı Hayatta Kalma Komutları
        if (IsRubblePromptActive)
        {
            if (rubbleStep == 0)
            {
                // [F] Fener bekleniyor
                if (IsFPressed())
                {
                    ActivateFlashlight();
                }
            }
            else if (rubbleStep == 1)
            {
                // [E] Düdük bekleniyor
                if (IsEPressed())
                {
                    ActivateWhistle();
                }
            }
        }
    }

    #region Genel Public Metodlar
    /// <summary>
    /// Saat 21:00'de deprem başladığında acil durum seçim panelini açar.
    /// Oyuncu [1] seçerse onChoiceMade(false), [2] seçerse onChoiceMade(true) döner.
    /// </summary>
    public void ShowFriendDirectionChoice(Action<bool> onChoiceMade)
    {
        EnsureInitialized();
        currentChoiceCallback = onChoiceMade;
        IsChoiceActive = true;

        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
        }

        // Fare imlecini serbest bırak
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[FriendChoiceUI] Arkadaş Yönlendirme Seçimi Ekranı Açıldı.");
    }

    /// <summary>
    /// Seçim panelini kapatır ve imleci eski haline kilitler.
    /// </summary>
    public void HideChoice()
    {
        IsChoiceActive = false;
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }

        // Fare imlecini geri kilitle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Enkaz altında karanlıkta sırasıyla Fener [F] ve Düdük [E] promptlarını gösterir.
    /// </summary>
    public void ShowRubbleSurvivalPrompt(Action onFlashlightUsed, Action onWhistleUsed)
    {
        EnsureInitialized();
        onFlashlightCallback = onFlashlightUsed;
        onWhistleCallback = onWhistleUsed;

        IsRubblePromptActive = true;
        rubbleStep = 0;

        if (rubblePanel != null)
        {
            rubblePanel.SetActive(true);
        }

        // Başlangıçta fener kapalı, ekran zifiri karanlık
        if (rubbleBlackOverlay != null)
        {
            rubbleBlackOverlay.color = new Color(0.01f, 0.01f, 0.02f, 0.99f);
        }
        if (rubbleFlashlightGlow != null)
        {
            rubbleFlashlightGlow.gameObject.SetActive(false);
        }

        UpdateRubbleTexts(
            "ENKAZ ALTINDASIN... HAVA TOZ DUMAN VE ZİFİRİ KARANLIK",
            "<color=#FFE866>[F] El Fenerini Yak</color>"
        );

        Debug.Log("[FriendChoiceUI] Enkaz Altı Hayatta Kalma Promptu Başlatıldı (Aşama 1: [F] Fener).");
    }

    /// <summary>
    /// Enkaz panelini ve karanlık efektini kapatır.
    /// </summary>
    public void HideRubblePrompt()
    {
        IsRubblePromptActive = false;
        rubbleStep = 0;

        if (rubblePanel != null)
        {
            rubblePanel.SetActive(false);
        }

        Debug.Log("[FriendChoiceUI] Enkaz Altı Promptu Kapatıldı.");
    }
    #endregion

    #region İç Mantık ve Aksiyonlar
    private void OnOptionSelected(bool choseSafeTable)
    {
        PlayClickSound();
        HideChoice();

        Debug.Log($"[FriendChoiceUI] Oyuncu Seçimi: {(choseSafeTable ? "[2] Sağlam Masaya Çök-Kapan-Tutun (DOĞRU)" : "[1] Merdiven/Pencereye Koşun (YANLIŞ)")}");
        currentChoiceCallback?.Invoke(choseSafeTable);
        currentChoiceCallback = null;
    }

    private void ActivateFlashlight()
    {
        rubbleStep = 1;
        PlayFlashlightSound();

        // Fener efektini aç
        if (rubbleFlashlightGlow != null)
        {
            rubbleFlashlightGlow.gameObject.SetActive(true);
        }
        // Arka plan karanlığını hafif aydınlat
        if (rubbleBlackOverlay != null)
        {
            rubbleBlackOverlay.color = new Color(0.02f, 0.02f, 0.03f, 0.78f);
        }

        UpdateRubbleTexts(
            "IŞIK AÇILDI. DIŞARIDAN YIKINTILAR ARASINDAN KURTARMA SESLERİ GELİYOR!",
            "<color=#FFCC00>[E] Deprem Düdüğünü Çal (AFAD'a Ses Duyur)</color>"
        );

        Debug.Log("[FriendChoiceUI] [F] El Feneri Yakıldı -> Aşama 2: [E] Düdük bekleniyor.");
        onFlashlightCallback?.Invoke();
    }

    private void ActivateWhistle()
    {
        PlayWhistleSound();
        Debug.Log("[FriendChoiceUI] [E] Deprem Düdüğü Çalındı (AFAD'a ses duyuruldu).");

        onWhistleCallback?.Invoke();
        HideRubblePrompt();
    }

    private void UpdateRubbleTexts(string header, string prompt)
    {
        if (rubbleHeaderText != null) rubbleHeaderText.text = header;
        if (rubblePromptText != null) rubblePromptText.text = prompt;
    }
    #endregion

    #region Input Kontrolleri
    private bool IsKey1Pressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
#endif
    }

    private bool IsKey2Pressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
#endif
    }

    private bool IsFPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }

    private bool IsEPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }
    #endregion

    #region UI ve Canvas Kurulumu
    private void EnsureCanvasHierarchy()
    {
        if (rootCanvas != null) return;

        rootCanvas = GetComponent<Canvas>();
        if (rootCanvas == null)
        {
            rootCanvas = gameObject.AddComponent<Canvas>();
        }

        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 920;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null) gameObject.AddComponent<GraphicRaycaster>();

        BuildChoicePanelUI();
        BuildRubblePanelUI();
    }

    private void BuildChoicePanelUI()
    {
        if (choicePanel != null) return;

        // Kök Panel (Ekranı hafif karartan arka plan)
        choicePanel = new GameObject("ChoicePanelRoot", typeof(RectTransform), typeof(Image));
        choicePanel.transform.SetParent(rootCanvas.transform, false);

        RectTransform rootRt = choicePanel.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        Image rootImg = choicePanel.GetComponent<Image>();
        rootImg.color = new Color(0.04f, 0.04f, 0.07f, 0.85f);

        // Ana Pencere
        GameObject modal = new GameObject("ChoiceModal", typeof(RectTransform), typeof(Image));
        modal.transform.SetParent(choicePanel.transform, false);
        RectTransform modalRt = modal.GetComponent<RectTransform>();
        modalRt.anchorMin = new Vector2(0.5f, 0.5f);
        modalRt.anchorMax = new Vector2(0.5f, 0.5f);
        modalRt.pivot = new Vector2(0.5f, 0.5f);
        modalRt.sizeDelta = new Vector2(960f, 540f);

        Image modalImg = modal.GetComponent<Image>();
        modalImg.color = new Color(0.10f, 0.11f, 0.15f, 0.98f);

        // Acil Durum Başlığı
        GameObject headerObj = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
        headerObj.transform.SetParent(modal.transform, false);
        RectTransform headerRt = headerObj.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -24f);
        headerRt.sizeDelta = new Vector2(-60f, 50f);

        TextMeshProUGUI headerTmp = headerObj.GetComponent<TextMeshProUGUI>();
        headerTmp.text = "[!] 21:00 - BÜYÜK DEPREM BAŞLADI!";
        headerTmp.fontSize = 32;
        headerTmp.fontStyle = FontStyles.Bold;
        headerTmp.alignment = TextAlignmentOptions.Center;
        headerTmp.color = new Color(1f, 0.28f, 0.28f, 1f);

        // Soru Açıklaması
        GameObject descObj = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(modal.transform, false);
        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0f, 1f);
        descRt.anchorMax = new Vector2(1f, 1f);
        descRt.pivot = new Vector2(0.5f, 1f);
        descRt.anchoredPosition = new Vector2(0f, -80f);
        descRt.sizeDelta = new Vector2(-80f, 65f);

        TextMeshProUGUI descTmp = descObj.GetComponent<TextMeshProUGUI>();
        descTmp.text = "Ev şiddetle sarsılıyor, duvarlar çatlıyor! Arkadaşların şok içinde sana bakıyor.\nOnlara ne yapmalarını haykıracaksın?";
        descTmp.fontSize = 20;
        descTmp.alignment = TextAlignmentOptions.Center;
        descTmp.color = Color.white;

        // --- SEÇENEK 1 (TEHLİKELİ / KIRMIZI) ---
        GameObject btn1Obj = new GameObject("Option1_Button", typeof(RectTransform), typeof(Image), typeof(Button));
        btn1Obj.transform.SetParent(modal.transform, false);
        RectTransform btn1Rt = btn1Obj.GetComponent<RectTransform>();
        btn1Rt.anchorMin = new Vector2(0f, 0.5f);
        btn1Rt.anchorMax = new Vector2(0f, 0.5f);
        btn1Rt.pivot = new Vector2(0f, 0.5f);
        btn1Rt.anchoredPosition = new Vector2(45f, -60f);
        btn1Rt.sizeDelta = new Vector2(410f, 290f);

        Image btn1Img = btn1Obj.GetComponent<Image>();
        btn1Img.color = new Color(0.24f, 0.08f, 0.09f, 0.95f);

        option1Button = btn1Obj.GetComponent<Button>();
        option1Button.onClick.AddListener(() => OnOptionSelected(false));

        // Seçenek 1 İç Metni
        GameObject btn1TextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btn1TextObj.transform.SetParent(btn1Obj.transform, false);
        RectTransform b1tRt = btn1TextObj.GetComponent<RectTransform>();
        b1tRt.anchorMin = Vector2.zero;
        b1tRt.anchorMax = Vector2.one;
        b1tRt.offsetMin = new Vector2(16f, 16f);
        b1tRt.offsetMax = new Vector2(-16f, -16f);

        TextMeshProUGUI b1Tmp = btn1TextObj.GetComponent<TextMeshProUGUI>();
        b1Tmp.text = "<color=#FF5555><size=26><b>[1] SEÇENEK</b></size></color>\n\n" +
                     "<size=22><b>\"Pencere kenarına / Merdivenlere koşun!\"</b></size>\n\n" +
                     "<color=#CC8888><size=16>✗ Tehlike: Sarsıntı anında merdivenler aniden yıkılabilir, camlar ölümcül şekilde patlar.</size></color>";
        b1Tmp.alignment = TextAlignmentOptions.Center;
        b1Tmp.color = Color.white;
        b1Tmp.enableWordWrapping = true;

        // --- SEÇENEK 2 (GÜVENLİ / YEŞİL) ---
        GameObject btn2Obj = new GameObject("Option2_Button", typeof(RectTransform), typeof(Image), typeof(Button));
        btn2Obj.transform.SetParent(modal.transform, false);
        RectTransform btn2Rt = btn2Obj.GetComponent<RectTransform>();
        btn2Rt.anchorMin = new Vector2(1f, 0.5f);
        btn2Rt.anchorMax = new Vector2(1f, 0.5f);
        btn2Rt.pivot = new Vector2(1f, 0.5f);
        btn2Rt.anchoredPosition = new Vector2(-45f, -60f);
        btn2Rt.sizeDelta = new Vector2(410f, 290f);

        Image btn2Img = btn2Obj.GetComponent<Image>();
        btn2Img.color = new Color(0.08f, 0.22f, 0.12f, 0.95f);

        option2Button = btn2Obj.GetComponent<Button>();
        option2Button.onClick.AddListener(() => OnOptionSelected(true));

        // Seçenek 2 İç Metni
        GameObject btn2TextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btn2TextObj.transform.SetParent(btn2Obj.transform, false);
        RectTransform b2tRt = btn2TextObj.GetComponent<RectTransform>();
        b2tRt.anchorMin = Vector2.zero;
        b2tRt.anchorMax = Vector2.one;
        b2tRt.offsetMin = new Vector2(16f, 16f);
        b2tRt.offsetMax = new Vector2(-16f, -16f);

        TextMeshProUGUI b2Tmp = btn2TextObj.GetComponent<TextMeshProUGUI>();
        b2Tmp.text = "<color=#44FF77><size=26><b>[2] SEÇENEK</b></size></color>\n\n" +
                     "<size=22><b>\"Sağlam masanın yanına ÇÖK - KAPAN - TUTUN!\"</b></size>\n\n" +
                     "<color=#88CC99><size=16>✓ Hayat Üçgeni: Anneannenin posterindeki kurala uyarak baş ve enseyi koruma altına alın.</size></color>";
        b2Tmp.alignment = TextAlignmentOptions.Center;
        b2Tmp.color = Color.white;
        b2Tmp.enableWordWrapping = true;

        choicePanel.SetActive(false);
    }

    private void BuildRubblePanelUI()
    {
        if (rubblePanel != null) return;

        rubblePanel = new GameObject("RubblePanelRoot", typeof(RectTransform));
        rubblePanel.transform.SetParent(rootCanvas.transform, false);
        RectTransform rootRt = rubblePanel.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // 1. Zifiri Karanlık Katmanı
        GameObject blackObj = new GameObject("BlackOverlay", typeof(RectTransform), typeof(Image));
        blackObj.transform.SetParent(rubblePanel.transform, false);
        RectTransform blackRt = blackObj.GetComponent<RectTransform>();
        blackRt.anchorMin = Vector2.zero;
        blackRt.anchorMax = Vector2.one;
        blackRt.offsetMin = Vector2.zero;
        blackRt.offsetMax = Vector2.zero;
        rubbleBlackOverlay = blackObj.GetComponent<Image>();
        rubbleBlackOverlay.color = new Color(0.01f, 0.01f, 0.02f, 0.99f);

        // 2. Fener Işık Halesi (Radial Glow)
        GameObject glowObj = new GameObject("FlashlightGlow", typeof(RectTransform), typeof(Image));
        glowObj.transform.SetParent(rubblePanel.transform, false);
        RectTransform glowRt = glowObj.GetComponent<RectTransform>();
        glowRt.anchorMin = new Vector2(0.5f, 0.5f);
        glowRt.anchorMax = new Vector2(0.5f, 0.5f);
        glowRt.pivot = new Vector2(0.5f, 0.5f);
        glowRt.sizeDelta = new Vector2(1400f, 1400f);

        rubbleFlashlightGlow = glowObj.GetComponent<Image>();
        rubbleFlashlightGlow.sprite = CreateRadialGlowSprite();
        rubbleFlashlightGlow.color = new Color(1f, 0.95f, 0.75f, 0.55f);
        glowObj.SetActive(false);

        // 3. Prompt Kartı (Alt-Orta)
        GameObject cardObj = new GameObject("PromptCard", typeof(RectTransform), typeof(Image));
        cardObj.transform.SetParent(rubblePanel.transform, false);
        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0f);
        cardRt.anchorMax = new Vector2(0.5f, 0f);
        cardRt.pivot = new Vector2(0.5f, 0f);
        cardRt.anchoredPosition = new Vector2(0f, 60f);
        cardRt.sizeDelta = new Vector2(780f, 150f);

        Image cardImg = cardObj.GetComponent<Image>();
        cardImg.color = new Color(0.06f, 0.07f, 0.10f, 0.92f);

        // Header Metni
        GameObject headerObj = new GameObject("HeaderText", typeof(RectTransform), typeof(TextMeshProUGUI));
        headerObj.transform.SetParent(cardObj.transform, false);
        RectTransform headerRt = headerObj.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -14f);
        headerRt.sizeDelta = new Vector2(-40f, 40f);

        rubbleHeaderText = headerObj.GetComponent<TextMeshProUGUI>();
        rubbleHeaderText.fontSize = 19;
        rubbleHeaderText.fontStyle = FontStyles.Bold;
        rubbleHeaderText.alignment = TextAlignmentOptions.Center;
        rubbleHeaderText.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        // Prompt Metni ([F] veya [E])
        GameObject promptObj = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
        promptObj.transform.SetParent(cardObj.transform, false);
        RectTransform promptRt = promptObj.GetComponent<RectTransform>();
        promptRt.anchorMin = new Vector2(0f, 0f);
        promptRt.anchorMax = new Vector2(1f, 0f);
        promptRt.pivot = new Vector2(0.5f, 0f);
        promptRt.anchoredPosition = new Vector2(0f, 18f);
        promptRt.sizeDelta = new Vector2(-40f, 65f);

        rubblePromptText = promptObj.GetComponent<TextMeshProUGUI>();
        rubblePromptText.fontSize = 28;
        rubblePromptText.fontStyle = FontStyles.Bold;
        rubblePromptText.alignment = TextAlignmentOptions.Center;
        rubblePromptText.color = new Color(1f, 0.88f, 0.25f, 1f);

        rubblePanel.SetActive(false);
    }

    private Sprite CreateRadialGlowSprite()
    {
        int size = 256;
        radialGlowTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        radialGlowTex.filterMode = FilterMode.Bilinear;
        radialGlowTex.wrapMode = TextureWrapMode.Clamp;

        float center = size * 0.5f;
        float maxRadius = size * 0.5f;
        Color32[] cols = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float t = Mathf.Clamp01(dist / maxRadius);
                // Dışa doğru yumuşak karesel düşüş
                float alpha = Mathf.SmoothStep(1f, 0f, t);
                cols[y * size + x] = new Color(1f, 0.96f, 0.85f, alpha);
            }
        }

        radialGlowTex.SetPixels32(cols);
        radialGlowTex.Apply(false, false);

        return Sprite.Create(radialGlowTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void EnsureRuntimeEventSystem()
    {
        if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject esObj = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        var inputSysType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
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

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D ses
            }
        }

        if (flashlightClickClip == null)
        {
            flashlightClickClip = CreateClickAudioClip();
        }

        if (whistleClip == null)
        {
            whistleClip = CreateRescueWhistleAudioClip();
        }
    }
    #endregion

    #region Prosedürel Ses Üretimi
    private void PlayClickSound()
    {
        if (audioSource != null && flashlightClickClip != null)
        {
            audioSource.PlayOneShot(flashlightClickClip, 0.7f);
        }
    }

    private void PlayFlashlightSound()
    {
        if (audioSource != null && flashlightClickClip != null)
        {
            audioSource.PlayOneShot(flashlightClickClip, 1f);
        }
    }

    private void PlayWhistleSound()
    {
        if (audioSource != null && whistleClip != null)
        {
            audioSource.PlayOneShot(whistleClip, 1f);
        }
    }

    /// <summary>
    /// Fener ve buton basışı için kısa ve net klik sesi üretir.
    /// </summary>
    private static AudioClip CreateClickAudioClip()
    {
        int sampleRate = 44100;
        int samples = (int)(sampleRate * 0.06f);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(1200f, 400f, t);
            float envelope = Mathf.Exp(-t * 28f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * envelope * 0.6f;
        }

        AudioClip clip = AudioClip.Create("FlashlightClick", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>
    /// Enkaz altında yardım çağrısı için 3 darbe vuruşlu acil durum deprem düdüğü sesi üretir (~2900 Hz).
    /// </summary>
    private static AudioClip CreateRescueWhistleAudioClip()
    {
        int sampleRate = 44100;
        float totalDuration = 1.3f;
        int samples = (int)(sampleRate * totalDuration);
        float[] data = new float[samples];

        float pulseDuration = 0.32f;
        float pauseDuration = 0.12f;

        for (int i = 0; i < samples; i++)
        {
            float currentTime = (float)i / sampleRate;
            float cycleTime = currentTime % (pulseDuration + pauseDuration);

            if (cycleTime < pulseDuration && currentTime < 1.25f)
            {
                float pulseT = cycleTime / pulseDuration;
                // Çift tonlu tiz düdük harmonikleri (2850 Hz ve 3100 Hz)
                float freq1 = 2850f + Mathf.Sin(currentTime * 40f) * 45f;
                float freq2 = 3100f;

                float envelope = Mathf.Sin(pulseT * Mathf.PI); // Yumuşak giriş-çıkış
                float sample = (Mathf.Sin(2f * Mathf.PI * freq1 * currentTime) * 0.65f +
                               Mathf.Sin(2f * Mathf.PI * freq2 * currentTime) * 0.35f) * envelope;

                data[i] = Mathf.Clamp(sample * 0.75f, -1f, 1f);
            }
            else
            {
                data[i] = 0f;
            }
        }

        AudioClip clip = AudioClip.Create("RescueWhistle", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    #endregion
}
