using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Harici prefab veya sprite gerektirmeden kendi ScreenSpaceOverlay Canvas'ını (sortingOrder = 950)
/// çalışma anında otomatik oluşturan Singleton ekran ve UI efekt yöneticisi.
/// Ekran karartma/açma (Fade), Flash/Glitch efekti, altyazı sistemi, merkez mesajları ve geri sayım sayacını yönetir.
/// </summary>
public class ScreenFaderUI : MonoBehaviour
{
    private static ScreenFaderUI _instance;

    public static ScreenFaderUI Instance
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

    /// <summary>
    /// Singleton instance'ın var olduğundan emin olur, sahnede yoksa otomatik oluşturur.
    /// </summary>
    public static ScreenFaderUI EnsureInstance()
    {
        if (_instance == null)
        {
            _instance = FindAnyObjectByType<ScreenFaderUI>();
            if (_instance == null)
            {
                GameObject go = new GameObject("ScreenFaderUI");
                _instance = go.AddComponent<ScreenFaderUI>();
            }
        }

        if (_instance != null && _instance.canvasRoot == null)
        {
            _instance.BuildUI();
        }

        return _instance;
    }

    // --- Durum Özellikleri ---
    public bool IsCountdownRunning { get; private set; }
    public float RemainingTime { get; private set; }
    public bool IsCenterMessageActive => centerMessagePanel != null && centerMessagePanel.activeSelf;
    public bool IsDeathScreenActive => deathScreenPanel != null && deathScreenPanel.activeSelf;
    public float CurrentFaderAlpha => faderAlpha;
    public bool IsFadedToBlack => faderAlpha >= 0.95f;

    // --- UI Bileşenleri ---
    private GameObject canvasRoot;
    private Canvas canvas;
    private Image faderImage;
    private Image flashImage;

    // Altyazı UI
    private GameObject subtitlePanel;
    private CanvasGroup subtitleCanvasGroup;
    private Text subtitleSpeakerText;
    private Text subtitleBodyText;

    // Merkez Mesaj UI
    private GameObject centerMessagePanel;
    private CanvasGroup centerMessageCanvasGroup;
    private Text centerTitleText;
    private Text centerSubtitleText;

    // Geri Sayım UI
    private GameObject countdownPanel;
    private CanvasGroup countdownCanvasGroup;
    private Image countdownBoxImage;
    private Text countdownTimerText;
    private Action onCountdownExpiredCallback;

    // Dramatik Ölüm / Kayıp Ekranı UI
    private GameObject deathScreenPanel;
    private CanvasGroup deathCanvasGroup;
    private Text deathTitleText;
    private Text deathReasonText;
    private Text deathMissingNoticeText;
    private Text deathLoopNoteText;
    private Coroutine deathScreenRoutine;
    private AudioSource proceduralAudioSource;

    // Coroutine Referansları
    private Coroutine fadeRoutine;
    private Action pendingFadeCallback; // Fade iptal edilse bile bekleyen tamamlanma çağrısının kaybolmaması için
    private Coroutine flashRoutine;
    private Coroutine subtitleRoutine;
    private Coroutine centerMessageRoutine;

    private float faderAlpha = 0f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        BuildUI();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            _instance = null;
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        HideCenterMessage();
        HideDeathScreen();
        StartCoroutine(EnsureUnfadedAfterSceneLoad());
    }

    private IEnumerator EnsureUnfadedAfterSceneLoad()
    {
        yield return new WaitForSecondsRealtime(1.8f);
        if (faderAlpha > 0.5f && fadeRoutine == null)
        {
            FadeFromBlack(1.2f);
        }
    }

    private void Update()
    {
        if (IsCountdownRunning)
        {
            RemainingTime -= Time.deltaTime;
            if (RemainingTime <= 0f)
            {
                RemainingTime = 0f;
                IsCountdownRunning = false;
                UpdateCountdownDisplay();

                Action callback = onCountdownExpiredCallback;
                onCountdownExpiredCallback = null;
                callback?.Invoke();
            }
            else
            {
                UpdateCountdownDisplay();
            }
        }
    }

    #region UI İnşası (Zero-Dependency Kod ile Oluşturma)

    private Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    private void BuildUI()
    {
        if (canvasRoot != null) return;

        Font defaultFont = GetDefaultFont();

        // 1. Screen Space Overlay Canvas (sortingOrder = 950)
        canvasRoot = new GameObject("ScreenFaderCanvas");
        canvasRoot.transform.SetParent(transform, false);

        canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;

        CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRoot.AddComponent<GraphicRaycaster>();

        // 2. Fader Panel (Tam Ekran Siyah Perde)
        GameObject faderObj = new GameObject("FaderPanel");
        faderObj.transform.SetParent(canvasRoot.transform, false);

        RectTransform faderRect = faderObj.AddComponent<RectTransform>();
        faderRect.anchorMin = Vector2.zero;
        faderRect.anchorMax = Vector2.one;
        faderRect.sizeDelta = Vector2.zero;
        faderRect.anchoredPosition = Vector2.zero;

        faderImage = faderObj.AddComponent<Image>();
        faderImage.color = new Color(0f, 0f, 0f, faderAlpha);
        faderImage.raycastTarget = false;
        faderObj.SetActive(faderAlpha > 0.001f);

        // 3. Flash / Glitch Panel (Tam Ekran Flash Efekti)
        GameObject flashObj = new GameObject("FlashPanel");
        flashObj.transform.SetParent(canvasRoot.transform, false);

        RectTransform flashRect = flashObj.AddComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.sizeDelta = Vector2.zero;
        flashRect.anchoredPosition = Vector2.zero;

        flashImage = flashObj.AddComponent<Image>();
        flashImage.color = new Color(1f, 1f, 1f, 0f);
        flashImage.raycastTarget = false;
        flashObj.SetActive(false);

        // 4. Geri Sayım Sayacı (Sağ Üst Köşe)
        BuildCountdownUI(defaultFont);

        // 5. Altyazı Kutusu (Alt-Orta)
        BuildSubtitleUI(defaultFont);

        // 6. Merkez Dramatik Mesaj (Tam Orta - Siyah Perdenin Üstünde!)
        BuildCenterMessageUI(defaultFont);

        // 7. Dramatik Ölüm / Kayıp Ekranı (Tam Ekran Kan Kırmızı / Siyah Perde)
        BuildDeathScreenUI(defaultFont);
    }

    private void BuildCountdownUI(Font font)
    {
        countdownPanel = new GameObject("CountdownBox");
        countdownPanel.transform.SetParent(canvasRoot.transform, false);

        RectTransform rt = countdownPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-40f, -40f);
        rt.sizeDelta = new Vector2(180f, 72f);

        countdownBoxImage = countdownPanel.AddComponent<Image>();
        countdownBoxImage.color = new Color(0.08f, 0.10f, 0.14f, 0.88f);

        countdownCanvasGroup = countdownPanel.AddComponent<CanvasGroup>();
        countdownCanvasGroup.interactable = false;
        countdownCanvasGroup.blocksRaycasts = false;

        VerticalLayoutGroup layout = countdownPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.spacing = 2f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Başlık (KALAN SÜRE)
        GameObject lblObj = new GameObject("LabelText");
        lblObj.transform.SetParent(countdownPanel.transform, false);
        Text lblText = lblObj.AddComponent<Text>();
        lblText.font = font;
        lblText.text = "KALAN SÜRE";
        lblText.fontSize = 12;
        lblText.fontStyle = FontStyle.Bold;
        lblText.color = new Color(0.7f, 0.75f, 0.82f, 1f);
        lblText.alignment = TextAnchor.MiddleCenter;
        lblText.supportRichText = false;

        // Sayaç Metni (01:30)
        GameObject timerObj = new GameObject("TimerText");
        timerObj.transform.SetParent(countdownPanel.transform, false);
        countdownTimerText = timerObj.AddComponent<Text>();
        countdownTimerText.font = font;
        countdownTimerText.text = "00:00";
        countdownTimerText.fontSize = 30;
        countdownTimerText.fontStyle = FontStyle.Bold;
        countdownTimerText.color = Color.white;
        countdownTimerText.alignment = TextAnchor.MiddleCenter;
        countdownTimerText.supportRichText = false;

        countdownPanel.SetActive(false);
    }

    private void BuildSubtitleUI(Font font)
    {
        subtitlePanel = new GameObject("SubtitlePanel");
        subtitlePanel.transform.SetParent(canvasRoot.transform, false);

        RectTransform rt = subtitlePanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 55f);

        Image bg = subtitlePanel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.12f, 0.86f);

        subtitleCanvasGroup = subtitlePanel.AddComponent<CanvasGroup>();
        subtitleCanvasGroup.interactable = false;
        subtitleCanvasGroup.blocksRaycasts = false;

        VerticalLayoutGroup layout = subtitlePanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(26, 26, 12, 14);
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = subtitlePanel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Konuşmacı Adı
        GameObject speakerObj = new GameObject("SpeakerText");
        speakerObj.transform.SetParent(subtitlePanel.transform, false);
        subtitleSpeakerText = speakerObj.AddComponent<Text>();
        subtitleSpeakerText.font = font;
        subtitleSpeakerText.fontSize = 17;
        subtitleSpeakerText.fontStyle = FontStyle.Bold;
        subtitleSpeakerText.color = new Color(1f, 0.82f, 0.35f, 1f); // Sıcak sarı / altın
        subtitleSpeakerText.alignment = TextAnchor.MiddleCenter;
        subtitleSpeakerText.supportRichText = true;

        LayoutElement spkLayout = speakerObj.AddComponent<LayoutElement>();
        spkLayout.preferredWidth = 840f;

        // Altyazı Metni
        GameObject bodyObj = new GameObject("BodyText");
        bodyObj.transform.SetParent(subtitlePanel.transform, false);
        subtitleBodyText = bodyObj.AddComponent<Text>();
        subtitleBodyText.font = font;
        subtitleBodyText.fontSize = 21;
        subtitleBodyText.color = Color.white;
        subtitleBodyText.alignment = TextAnchor.MiddleCenter;
        subtitleBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        subtitleBodyText.verticalOverflow = VerticalWrapMode.Overflow;
        subtitleBodyText.supportRichText = true;

        LayoutElement bodyLayout = bodyObj.AddComponent<LayoutElement>();
        bodyLayout.preferredWidth = 840f;

        subtitlePanel.SetActive(false);
    }

    private void BuildCenterMessageUI(Font font)
    {
        centerMessagePanel = new GameObject("CenterMessagePanel");
        centerMessagePanel.transform.SetParent(canvasRoot.transform, false);

        RectTransform rt = centerMessagePanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        Image bg = centerMessagePanel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.05f, 0.08f, 0.82f);

        centerMessageCanvasGroup = centerMessagePanel.AddComponent<CanvasGroup>();
        centerMessageCanvasGroup.interactable = false;
        centerMessageCanvasGroup.blocksRaycasts = false;

        VerticalLayoutGroup layout = centerMessagePanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(42, 42, 28, 30);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = centerMessagePanel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Büyük Dramatik Başlık
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(centerMessagePanel.transform, false);
        centerTitleText = titleObj.AddComponent<Text>();
        centerTitleText.font = font;
        centerTitleText.fontSize = 38;
        centerTitleText.fontStyle = FontStyle.Bold;
        centerTitleText.color = new Color(0.96f, 0.32f, 0.32f, 1f); // Dramatik Kırmızı/Beyaz ton
        centerTitleText.alignment = TextAnchor.MiddleCenter;
        centerTitleText.supportRichText = true;

        LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.preferredWidth = 920f;

        // Alt Metin
        GameObject subObj = new GameObject("SubtitleText");
        subObj.transform.SetParent(centerMessagePanel.transform, false);
        centerSubtitleText = subObj.AddComponent<Text>();
        centerSubtitleText.font = font;
        centerSubtitleText.fontSize = 22;
        centerSubtitleText.fontStyle = FontStyle.Italic;
        centerSubtitleText.color = new Color(0.90f, 0.92f, 0.96f, 1f);
        centerSubtitleText.alignment = TextAnchor.MiddleCenter;
        centerSubtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        centerSubtitleText.verticalOverflow = VerticalWrapMode.Overflow;
        centerSubtitleText.supportRichText = true;

        LayoutElement subLayout = subObj.AddComponent<LayoutElement>();
        subLayout.preferredWidth = 920f;

        centerMessagePanel.SetActive(false);
    }

    private void BuildDeathScreenUI(Font font)
    {
        deathScreenPanel = new GameObject("DeathScreenPanel");
        deathScreenPanel.transform.SetParent(canvasRoot.transform, false);

        RectTransform rt = deathScreenPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        Image bg = deathScreenPanel.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.02f, 0.02f, 0.96f); // Yoğun kan kırmızısı / siyah ton

        deathCanvasGroup = deathScreenPanel.AddComponent<CanvasGroup>();
        deathCanvasGroup.interactable = false;
        deathCanvasGroup.blocksRaycasts = false;
        deathCanvasGroup.alpha = 0f;

        // Ortalanmış İçerik Kutusu
        GameObject contentBox = new GameObject("DeathContentBox");
        contentBox.transform.SetParent(deathScreenPanel.transform, false);
        RectTransform cRt = contentBox.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 0.5f);
        cRt.anchorMax = new Vector2(0.5f, 0.5f);
        cRt.pivot = new Vector2(0.5f, 0.5f);
        cRt.sizeDelta = new Vector2(1100f, 600f);

        VerticalLayoutGroup layout = contentBox.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 30, 30);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentBox.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 1. Üst Uyarı İbaresi
        GameObject alertObj = new GameObject("AlertLabel");
        alertObj.transform.SetParent(contentBox.transform, false);
        Text alertText = alertObj.AddComponent<Text>();
        alertText.font = font;
        alertText.fontSize = 18;
        alertText.fontStyle = FontStyle.Bold;
        alertText.color = new Color(1f, 0.35f, 0.35f, 0.9f);
        alertText.alignment = TextAnchor.MiddleCenter;
        alertText.text = "— ENKAZ ALTINDA KRİTİK EKSİKLİK —";
        alertText.supportRichText = true;

        // 2. Büyük Dramatik Başlık
        GameObject titleObj = new GameObject("DeathTitle");
        titleObj.transform.SetParent(contentBox.transform, false);
        deathTitleText = titleObj.AddComponent<Text>();
        deathTitleText.font = font;
        deathTitleText.fontSize = 44;
        deathTitleText.fontStyle = FontStyle.Bold;
        deathTitleText.color = new Color(0.98f, 0.18f, 0.18f, 1f); // Parlak Kan Kırmızısı
        deathTitleText.alignment = TextAnchor.MiddleCenter;
        deathTitleText.text = "HAYATINI KAYBETTİN";
        deathTitleText.supportRichText = true;

        LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.preferredWidth = 1000f;

        // 3. Detaylı Ölüm / Kayıp Gerekçesi
        GameObject reasonObj = new GameObject("DeathReason");
        reasonObj.transform.SetParent(contentBox.transform, false);
        deathReasonText = reasonObj.AddComponent<Text>();
        deathReasonText.font = font;
        deathReasonText.fontSize = 22;
        deathReasonText.fontStyle = FontStyle.Italic;
        deathReasonText.color = new Color(0.95f, 0.95f, 0.98f, 1f);
        deathReasonText.alignment = TextAnchor.MiddleCenter;
        deathReasonText.horizontalOverflow = HorizontalWrapMode.Wrap;
        deathReasonText.verticalOverflow = VerticalWrapMode.Overflow;
        deathReasonText.supportRichText = true;

        LayoutElement reasonLayout = reasonObj.AddComponent<LayoutElement>();
        reasonLayout.preferredWidth = 1000f;

        // 4. Eksik Eşyalar Vurgusu
        GameObject missingObj = new GameObject("MissingNotice");
        missingObj.transform.SetParent(contentBox.transform, false);
        deathMissingNoticeText = missingObj.AddComponent<Text>();
        deathMissingNoticeText.font = font;
        deathMissingNoticeText.fontSize = 18;
        deathMissingNoticeText.fontStyle = FontStyle.Bold;
        deathMissingNoticeText.color = new Color(1f, 0.72f, 0.22f, 1f); // Sarı/Amber uyarısı
        deathMissingNoticeText.alignment = TextAnchor.MiddleCenter;
        deathMissingNoticeText.horizontalOverflow = HorizontalWrapMode.Wrap;
        deathMissingNoticeText.verticalOverflow = VerticalWrapMode.Overflow;
        deathMissingNoticeText.supportRichText = true;

        LayoutElement missingLayout = missingObj.AddComponent<LayoutElement>();
        missingLayout.preferredWidth = 1000f;

        // 5. Zaman Döngüsü Alt Notu
        GameObject loopNoteObj = new GameObject("LoopNote");
        loopNoteObj.transform.SetParent(contentBox.transform, false);
        deathLoopNoteText = loopNoteObj.AddComponent<Text>();
        deathLoopNoteText.font = font;
        deathLoopNoteText.fontSize = 16;
        deathLoopNoteText.color = new Color(0.70f, 0.72f, 0.80f, 0.85f);
        deathLoopNoteText.alignment = TextAnchor.MiddleCenter;
        deathLoopNoteText.text = "Zaman döngüsü başa sarılıyor... Hazırlık hayat kurtarır.";
        deathLoopNoteText.supportRichText = true;

        deathScreenPanel.SetActive(false);
    }

    #endregion

    #region Fade Fonksiyonları

    /// <summary>
    /// Ekranı belirtilen sürede yumuşakça siyaha karartır.
    /// </summary>
    public void FadeToBlack(float duration = 1.5f, Action onComplete = null)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(1f, duration, onComplete));
    }

    /// <summary>
    /// Ekranı siyahtan normale yumuşakça açar.
    /// </summary>
    public void FadeFromBlack(float duration = 1.5f, Action onComplete = null)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(0f, duration, onComplete));
    }

    /// <summary>
    /// Ekranı beklemeden anında siyah veya şeffaf yapar.
    /// </summary>
    public void SetBlackImmediate(bool isBlack)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        // Fade iptal edilse bile bekleyen tamamlanma çağrısını tetikle: geçiş yapan sistemler kilitli kalmasın.
        InvokePendingFadeCallback();

        faderAlpha = isBlack ? 1f : 0f;
        ApplyFaderAlpha();
    }

    /// <summary>
    /// Bekleyen fade tamamlanma çağrısı varsa tetikler ve temizler.
    /// </summary>
    private void InvokePendingFadeCallback()
    {
        Action callback = pendingFadeCallback;
        pendingFadeCallback = null;
        callback?.Invoke();
    }

    private void ApplyFaderAlpha()
    {
        if (faderImage != null)
        {
            Color c = faderImage.color;
            c.a = faderAlpha;
            faderImage.color = c;
            faderImage.gameObject.SetActive(faderAlpha > 0.001f);
            faderImage.raycastTarget = faderAlpha > 0.8f;
        }
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, Action onComplete)
    {
        // Önceki fade iptal edildiyse onun bekleyen çağrısını kaybetme.
        InvokePendingFadeCallback();
        pendingFadeCallback = onComplete;

        float startAlpha = faderAlpha;

        if (duration <= 0.001f)
        {
            faderAlpha = targetAlpha;
            ApplyFaderAlpha();
            fadeRoutine = null;
            InvokePendingFadeCallback();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            faderAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            ApplyFaderAlpha();
            yield return null;
        }

        faderAlpha = targetAlpha;
        ApplyFaderAlpha();
        fadeRoutine = null;
        InvokePendingFadeCallback();
    }

    #endregion

    #region Flash / Glitch Efekti

    /// <summary>
    /// Saat 21:01 flashback / anı şoku anı için ekranda kısa beyaz/kırmızımsı flash darbesi yapar.
    /// </summary>
    public void FlashGlitch(float duration = 0.35f)
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashGlitchRoutine(duration));
    }

    private IEnumerator FlashGlitchRoutine(float duration)
    {
        if (flashImage == null) yield break;
        flashImage.gameObject.SetActive(true);

        float peakTime = Mathf.Min(0.08f, duration * 0.25f);
        float elapsed = 0f;

        // Tepe noktaya ani çıkış (Beyaz/Sıcak parıltı)
        Color peakColor = new Color(1f, 0.95f, 0.92f, 0.92f);
        while (elapsed < peakTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / peakTime);
            flashImage.color = Color.Lerp(new Color(1f, 1f, 1f, 0f), peakColor, t);
            yield return null;
        }

        // Kırmızımsı glitch parazitiyle sönümlenme
        float decayTime = Mathf.Max(0.01f, duration - peakTime);
        elapsed = 0f;

        while (elapsed < decayTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / decayTime);

            float jitter = (UnityEngine.Random.value > 0.72f) ? UnityEngine.Random.Range(0.05f, 0.18f) : 0f;
            Color decayed = Color.Lerp(new Color(0.95f, 0.25f, 0.25f, 0.75f), new Color(1f, 1f, 1f, 0f), t);
            decayed.a = Mathf.Clamp01(decayed.a + jitter);
            flashImage.color = decayed;

            yield return null;
        }

        flashImage.color = new Color(1f, 1f, 1f, 0f);
        flashImage.gameObject.SetActive(false);
        flashRoutine = null;
    }

    #endregion

    #region Altyazı Sistemi

    /// <summary>
    /// Ekranın alt-orta kısmında şık yarı saydam kutu içinde konuşmacı adı ve altyazı metni gösterir.
    /// </summary>
    public void ShowSubtitle(string speaker, string text, float duration = 3f)
    {
        if (subtitlePanel == null) BuildUI();

        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        subtitleRoutine = StartCoroutine(ShowSubtitleRoutine(speaker, text, duration));
    }

    /// <summary>
    /// Açık olan altyazıyı anında gizler.
    /// </summary>
    public void HideSubtitle()
    {
        if (subtitleRoutine != null)
        {
            StopCoroutine(subtitleRoutine);
            subtitleRoutine = null;
        }

        if (subtitlePanel != null)
        {
            subtitleCanvasGroup.alpha = 0f;
            subtitlePanel.SetActive(false);
        }
    }

    private IEnumerator ShowSubtitleRoutine(string speaker, string text, float duration)
    {
        subtitlePanel.SetActive(true);

        bool hasSpeaker = !string.IsNullOrEmpty(speaker);
        subtitleSpeakerText.gameObject.SetActive(hasSpeaker);
        if (hasSpeaker) subtitleSpeakerText.text = speaker;

        subtitleBodyText.text = text;

        // Layout güncelle
        LayoutRebuilder.ForceRebuildLayoutImmediate(subtitlePanel.GetComponent<RectTransform>());

        // Yumuşak beliriş
        float fadeTime = 0.18f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            subtitleCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeTime);
            yield return null;
        }
        subtitleCanvasGroup.alpha = 1f;

        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);

            // Yumuşak kayboluş
            elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                subtitleCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                yield return null;
            }

            subtitleCanvasGroup.alpha = 0f;
            subtitlePanel.SetActive(false);
        }

        subtitleRoutine = null;
    }

    #endregion

    #region Merkez Dramatik Mesaj

    /// <summary>
    /// Ekranın tam ortasında büyük dramatik başlık ve alt metin gösterir.
    /// Ölüm, loop ve final anlarındaki enkaz altı/farkındalık mesajları için idealdir.
    /// </summary>
    public void ShowCenterMessage(string title, string subtitle = "", float duration = 4f)
    {
        if (centerMessagePanel == null) BuildUI();

        if (centerMessageRoutine != null) StopCoroutine(centerMessageRoutine);
        centerMessageRoutine = StartCoroutine(ShowCenterMessageRoutine(title, subtitle, duration));
    }

    /// <summary>
    /// Merkez mesajını anında gizler.
    /// </summary>
    public void HideCenterMessage()
    {
        if (centerMessageRoutine != null)
        {
            StopCoroutine(centerMessageRoutine);
            centerMessageRoutine = null;
        }

        if (centerMessagePanel != null)
        {
            centerMessageCanvasGroup.alpha = 0f;
            centerMessagePanel.SetActive(false);
        }
    }

    private IEnumerator ShowCenterMessageRoutine(string title, string subtitle, float duration)
    {
        centerMessagePanel.SetActive(true);

        centerTitleText.text = title;

        bool hasSub = !string.IsNullOrEmpty(subtitle);
        centerSubtitleText.gameObject.SetActive(hasSub);
        if (hasSub) centerSubtitleText.text = subtitle;

        LayoutRebuilder.ForceRebuildLayoutImmediate(centerMessagePanel.GetComponent<RectTransform>());

        // Yumuşak beliriş
        float fadeTime = 0.28f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            centerMessageCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeTime);
            yield return null;
        }
        centerMessageCanvasGroup.alpha = 1f;

        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);

            // Yumuşak kayboluş
            elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                centerMessageCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                yield return null;
            }

            centerMessageCanvasGroup.alpha = 0f;
            centerMessagePanel.SetActive(false);
        }

        centerMessageRoutine = null;
    }

    #endregion

    #region Dramatik Ölüm / Kayıp Ekranı

    /// <summary>
    /// Çantada eksik hayati eşya olduğunda dramatik ölüm/kayıp ekranını gösterir.
    /// Ekranda gerekçe ve eksik eşyalar yazar, süre bitiminde onComplete çağrılır.
    /// </summary>
    public void ShowDeathScreen(string title, string reason, string missingItemsNotice = "", float duration = 6.5f, Action onComplete = null)
    {
        if (deathScreenPanel == null) BuildUI();

        if (deathScreenRoutine != null) StopCoroutine(deathScreenRoutine);
        deathScreenRoutine = StartCoroutine(ShowDeathScreenRoutine(title, reason, missingItemsNotice, duration, onComplete));
    }

    /// <summary>
    /// Ölüm ekranını kapatır.
    /// </summary>
    public void HideDeathScreen()
    {
        if (deathScreenRoutine != null)
        {
            StopCoroutine(deathScreenRoutine);
            deathScreenRoutine = null;
        }

        if (deathScreenPanel != null)
        {
            deathCanvasGroup.alpha = 0f;
            deathScreenPanel.SetActive(false);
        }
    }

    private IEnumerator ShowDeathScreenRoutine(string title, string reason, string missingItemsNotice, float duration, Action onComplete)
    {
        deathScreenPanel.SetActive(true);
        deathCanvasGroup.alpha = 0f;

        if (deathTitleText != null) deathTitleText.text = title;
        if (deathReasonText != null) deathReasonText.text = reason;

        bool hasMissing = !string.IsNullOrEmpty(missingItemsNotice);
        if (deathMissingNoticeText != null)
        {
            deathMissingNoticeText.gameObject.SetActive(hasMissing);
            if (hasMissing) deathMissingNoticeText.text = missingItemsNotice;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(deathScreenPanel.GetComponent<RectTransform>());

        // Dramatik kalp atışı ve glitch efekti
        PlayProceduralDeathAudio();
        FlashGlitch(0.4f);

        // Yumuşak beliriş (0.6 saniye)
        float fadeInTime = 0.6f;
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.unscaledDeltaTime;
            deathCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInTime);
            yield return null;
        }
        deathCanvasGroup.alpha = 1f;

        // Ekranda kalma süresi
        if (duration > 0f)
        {
            float waitTimer = duration;
            while (waitTimer > 0f)
            {
                waitTimer -= Time.unscaledDeltaTime;
                yield return null;
            }

            // Siyaha tam karartma (1.5 saniye)
            float fadeOutTime = 1.5f;
            elapsed = 0f;
            FadeToBlack(fadeOutTime);

            while (elapsed < fadeOutTime)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            deathCanvasGroup.alpha = 0f;
            deathScreenPanel.SetActive(false);
        }

        deathScreenRoutine = null;
        onComplete?.Invoke();
    }

    private void PlayProceduralDeathAudio()
    {
        if (proceduralAudioSource == null)
        {
            proceduralAudioSource = gameObject.AddComponent<AudioSource>();
            proceduralAudioSource.playOnAwake = false;
        }

        try
        {
            int sampleRate = 44100;
            float length = 1.2f;
            int totalSamples = (int)(sampleRate * length);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                // İki boğuk kalp vuruşu (55Hz ve 48Hz)
                float pulse1 = Mathf.Exp(-t * 16f) * Mathf.Sin(2f * Mathf.PI * 55f * t);
                float pulse2 = (t > 0.35f) ? Mathf.Exp(-(t - 0.35f) * 16f) * Mathf.Sin(2f * Mathf.PI * 48f * (t - 0.35f)) : 0f;
                data[i] = (pulse1 + pulse2) * 0.7f;
            }

            AudioClip clip = AudioClip.Create("DeathHeartbeat", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            proceduralAudioSource.PlayOneShot(clip, 0.9f);
        }
        catch (Exception)
        {
            // Ses üretilemezse sessizce devam et
        }
    }

    #endregion

    #region Geri Sayım Sayacı

    /// <summary>
    /// Sağ üst köşede 01:30 formatında geri sayım sayacı kutusu açar, son 20 saniyede kırmızı yanıp söner,
    /// süre bitince onTimerExpired tetikler.
    /// </summary>
    public void StartCountdown(float seconds, Action onTimerExpired = null)
    {
        if (countdownPanel == null) BuildUI();

        RemainingTime = Mathf.Max(0f, seconds);
        IsCountdownRunning = true;
        onCountdownExpiredCallback = onTimerExpired;

        countdownPanel.SetActive(true);
        countdownCanvasGroup.alpha = 1f;

        UpdateCountdownDisplay();
    }

    /// <summary>
    /// Devam eden geri sayım sayacını durdurur ve gizler.
    /// </summary>
    public void StopCountdown()
    {
        IsCountdownRunning = false;
        onCountdownExpiredCallback = null;

        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }
    }

    private void UpdateCountdownDisplay()
    {
        if (countdownTimerText == null) return;

        int totalSec = Mathf.Max(0, Mathf.CeilToInt(RemainingTime));
        int minutes = totalSec / 60;
        int seconds = totalSec % 60;
        countdownTimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        // Son 20 saniyede kırmızı yanıp söner
        if (RemainingTime <= 20f && RemainingTime > 0f)
        {
            float pulse = Mathf.PingPong(Time.unscaledTime * 4f, 1f);
            countdownTimerText.color = Color.Lerp(new Color(1f, 0.2f, 0.2f, 1f), Color.white, pulse);

            if (countdownBoxImage != null)
            {
                countdownBoxImage.color = Color.Lerp(
                    new Color(0.48f, 0.08f, 0.08f, 0.95f),
                    new Color(0.12f, 0.12f, 0.16f, 0.88f),
                    pulse
                );
            }
        }
        else
        {
            countdownTimerText.color = Color.white;
            if (countdownBoxImage != null)
            {
                countdownBoxImage.color = new Color(0.08f, 0.10f, 0.14f, 0.88f);
            }
        }
    }

    #endregion

    #region Context Menüleri / Test Fonksiyonları
    [ContextMenu("Test: Fade to Black (1.5s)")]
    private void TestFadeToBlack() => FadeToBlack(1.5f);

    [ContextMenu("Test: Fade from Black (1.5s)")]
    private void TestFadeFromBlack() => FadeFromBlack(1.5f);

    [ContextMenu("Test: Flash Glitch")]
    private void TestFlashGlitch() => FlashGlitch(0.35f);

    [ContextMenu("Test: Subtitle (3s)")]
    private void TestSubtitle() => ShowSubtitle("Mert", "Bu bir altyazı testidir.", 3f);

    [ContextMenu("Test: Center Message (4s)")]
    private void TestCenterMessage() => ShowCenterMessage("[ Enkaz Altı ]", "Uzaktan boğuk siren sesleri duyuluyor...", 4f);

    [ContextMenu("Test: Start Countdown (30s)")]
    private void TestCountdown() => StartCountdown(30f, () => Debug.Log("Sayaç bitti!"));

    [ContextMenu("Test: Death Screen (5s)")]
    private void TestDeathScreen() => ShowDeathScreen("HAYATINI KAYBETTİN", "Deprem çantanda düdük olmadığı için sesini AFAD ekiplerine duyuramadın...", "[ Eksik Hayati Eşyalar: Deprem Düdüğü ]", 5f);
    #endregion
}
