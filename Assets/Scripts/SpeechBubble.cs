using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Karakterlerin kafasının üzerinde çıkan, duvar/tavan arkasında bile ASLA görünmez olmayan (En Üst Katman / Always On Top),
/// ekran dışına taşmayı engelleyen (Screen Clamp), daktilo efektli modüler Sohbet Balonu bileşeni.
/// </summary>
public class SpeechBubble : MonoBehaviour
{
    [Header("--- Konum ve Kafa Takibi ---")]
    [Tooltip("Karakterin kafa kemiğini (Head) otomatik bulup takip etsin mi?")]
    public bool trackHeadBone = true;

    [Tooltip("Kafa kemiğine (veya obje köküne) göre balonun 3D yükseklik ofseti.")]
    public Vector3 offset = new Vector3(0f, 0.28f, 0f);

    [Tooltip("Metin çok uzunsa veya yukarı taşarsa ekran sınırları içinde tutulsun mu?")]
    public bool clampToScreen = true;

    [Tooltip("Ekran kenarlarından bırakılacak güvenli boşluk (piksel).")]
    public Vector2 screenPadding = new Vector2(20f, 20f);

    [Header("--- Görünüm ve Boyut Ayarları ---")]
    [Tooltip("Balonun üst kısmında görünecek karakter adı (Boş bırakılırsa gizlenir).")]
    public string speakerName = "";

    [Tooltip("Balon arka plan rengi.")]
    public Color backgroundColor = new Color(0.08f, 0.10f, 0.14f, 0.94f);

    [Tooltip("Konuşma metni rengi.")]
    public Color textColor = Color.white;

    [Tooltip("İsim başlığı rengi.")]
    public Color nameColor = new Color(1f, 0.82f, 0.3f, 1f);

    [Tooltip("Yazı boyutu.")]
    public int fontSize = 20;

    [Tooltip("Balonun maksimum genişliği (piksel). Uzun metinler otomatik alt satıra geçer.")]
    public float maxWidth = 340f;

    [Header("--- Mesafe Ölçekleme (3D Derinlik Hissi) ---")]
    [Tooltip("Kameraya olan mesafeye göre balon boyutu otomatik küçülüp büyüsün mü?")]
    public bool scaleWithDistance = true;

    [Tooltip("Balonun tam 1x boyutta görüneceği referans mesafe (metre).")]
    public float referenceDistance = 4.5f;

    [Tooltip("Çok uzaktayken inebileceği minimum ölçek.")]
    public float minScale = 0.65f;

    [Tooltip("Çok yakındayken çıkabileceği maksimum ölçek.")]
    public float maxScale = 1.15f;

    [Header("--- Animasyon & Daktilo (Typewriter) ---")]
    [Tooltip("Harflerin tek tek yazılma efekti aktif olsun mu?")]
    public bool useTypewriter = true;

    [Tooltip("Saniyede kaç harf yazılsın?")]
    public float charsPerSecond = 35f;

    // İç bileşenler
    private Transform followTarget;
    private GameObject canvasRoot;
    private RectTransform bubbleContainer;
    private RectTransform bgRect;
    private CanvasGroup canvasGroup;
    private Text messageText;
    private Text nameText;
    private Image bgImage;
    private Image tailImage;
    private LayoutElement msgLayoutElement;
    private LayoutElement nameLayoutElement;
    private Coroutine activeRoutine;
    private Camera targetCam;

    private float animScaleMultiplier = 1f;
    private float currentAlpha = 0f;

    public bool IsShowing { get; private set; }

    void Awake()
    {
        SetupFollowTarget();
        CreateBubbleUI();
        HideImmediate();
    }

    private void SetupFollowTarget()
    {
        if (trackHeadBone)
        {
            Animator anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>() ?? GetComponentInParent<Animator>();
            if (anim != null && anim.isHuman)
            {
                Transform head = anim.GetBoneTransform(HumanBodyBones.Head);
                if (head != null)
                {
                    followTarget = head;
                    return;
                }
            }

            // Humanoid değilse veya kemik bulunamadıysa, hiyerarşide kafa kemiğini ada göre ara
            Transform rootT = transform.root != null ? transform.root : transform;
            Transform namedHead = FindBoneRecursive(rootT, "spine.005")
                               ?? FindBoneRecursive(rootT, "head")
                               ?? FindBoneRecursive(rootT, "Head");
            if (namedHead != null)
            {
                followTarget = namedHead;
                return;
            }
        }
        followTarget = transform;
    }

    private Transform FindBoneRecursive(Transform parent, string boneName)
    {
        if (parent == null) return null;
        if (parent.name.Equals(boneName, System.StringComparison.OrdinalIgnoreCase))
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindBoneRecursive(parent.GetChild(i), boneName);
            if (found != null) return found;
        }
        return null;
    }


    private void CreateBubbleUI()
    {
        if (canvasRoot != null) return;

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 1. En Üst Katman (ScreenSpaceOverlay) Canvas - Duvar ve Tavan Arkasında Asla Kaybolmaz!
        canvasRoot = new GameObject($"{gameObject.name}_SpeechBubbleOverlay");
        canvasRoot.transform.SetParent(transform, false);

        Canvas canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Her şeyin üstünde (en üst layer)

        CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 2. Karakterin Kafasını Takip Eden Konteyner
        GameObject containerObj = new GameObject("BubbleAnchor");
        containerObj.transform.SetParent(canvasRoot.transform, false);
        bubbleContainer = containerObj.AddComponent<RectTransform>();
        bubbleContainer.anchorMin = Vector2.zero;
        bubbleContainer.anchorMax = Vector2.zero;
        bubbleContainer.pivot = new Vector2(0.5f, 0f);
        bubbleContainer.sizeDelta = new Vector2(maxWidth, 100f);

        canvasGroup = containerObj.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // 3. Küçük Balon Kuyruğu (Alt Ok İşareti)
        GameObject tailObj = new GameObject("Tail");
        tailObj.transform.SetParent(bubbleContainer, false);
        tailImage = tailObj.AddComponent<Image>();
        tailImage.color = backgroundColor;
        RectTransform tailRect = tailObj.GetComponent<RectTransform>();
        tailRect.sizeDelta = new Vector2(16f, 16f);
        tailRect.anchorMin = new Vector2(0.5f, 0f);
        tailRect.anchorMax = new Vector2(0.5f, 0f);
        tailRect.pivot = new Vector2(0.5f, 0.5f);
        tailRect.anchoredPosition = new Vector2(0f, 14f);
        tailRect.localEulerAngles = new Vector3(0f, 0f, 45f);

        // 4. Arka Plan Kutusu (Metne göre otomatik büyüyen kutu)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(bubbleContainer, false);

        bgImage = bgObj.AddComponent<Image>();
        bgImage.color = backgroundColor;

        VerticalLayoutGroup layout = bgObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 14, 16);
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = bgObj.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0f);
        bgRect.anchorMax = new Vector2(0.5f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0f);
        bgRect.anchoredPosition = new Vector2(0f, 14f);

        // 5. Konuşmacı Adı (Opsiyonel)
        GameObject nameObj = new GameObject("SpeakerName");
        nameObj.transform.SetParent(bgObj.transform, false);
        nameText = nameObj.AddComponent<Text>();
        nameText.font = defaultFont;
        nameText.fontSize = Mathf.Max(14, fontSize - 3);
        nameText.fontStyle = FontStyle.Bold;
        nameText.color = nameColor;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
        nameText.verticalOverflow = VerticalWrapMode.Overflow;

        nameLayoutElement = nameObj.AddComponent<LayoutElement>();
        nameLayoutElement.preferredWidth = maxWidth;

        // 6. Mesaj Metni
        GameObject msgObj = new GameObject("MessageText");
        msgObj.transform.SetParent(bgObj.transform, false);
        messageText = msgObj.AddComponent<Text>();
        messageText.font = defaultFont;
        messageText.fontSize = fontSize;
        messageText.color = textColor;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        messageText.verticalOverflow = VerticalWrapMode.Overflow;

        msgLayoutElement = msgObj.AddComponent<LayoutElement>();
        msgLayoutElement.preferredWidth = maxWidth;
    }

    void LateUpdate()
    {
        if (canvasRoot == null || !canvasRoot.activeSelf) return;

        if (targetCam == null || !targetCam.gameObject.activeInHierarchy)
        {
            targetCam = Camera.main;
            if (targetCam == null) targetCam = FindAnyObjectByType<Camera>();
            if (targetCam == null) return;
        }

        if (followTarget == null || followTarget == transform)
        {
            SetupFollowTarget();
        }

        Vector3 basePos;
        if (followTarget != null && followTarget != transform)
        {
            basePos = followTarget.position;
        }
        else
        {
            // Eğer kafa kemiği bulunamadıysa bile ASLA ayak hizasında kalmasın, insan kafa boyu (+1.7m) ekle
            basePos = transform.position + Vector3.up * 1.7f;
        }
        Vector3 worldAnchor = basePos + offset;

        // 3D Dünya koordinatını 2D Ekran koordinatına çevir
        Vector3 screenPos = targetCam.WorldToScreenPoint(worldAnchor);

        // Karakter kameranın tam arkasında kaldıysa balonu gizle
        if (screenPos.z <= 0.05f)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        // Mesafeye göre ölçek hesapla
        float distScale = 1f;
        if (scaleWithDistance)
        {
            float dist = screenPos.z;
            distScale = Mathf.Clamp(referenceDistance / Mathf.Max(0.5f, dist), minScale, maxScale);
        }

        float finalScale = distScale * animScaleMultiplier;
        bubbleContainer.localScale = Vector3.one * finalScale;

        // Canvas ölçek çarpanını al (CanvasScaler uyumu)
        float canvasScaleFactor = canvasRoot.transform.localScale.x;
        if (canvasScaleFactor <= 0.0001f) canvasScaleFactor = 1f;

        Vector2 anchoredPos = new Vector2(screenPos.x, screenPos.y) / canvasScaleFactor;

        // Metin çok uzunsa veya karakter tavana/ekran üstüne çok yakınsa ekran dışına taşmasını engelle!
        if (clampToScreen && bgRect != null)
        {
            float screenW = Screen.width / canvasScaleFactor;
            float screenH = Screen.height / canvasScaleFactor;

            float bubbleW = (bgRect.rect.width * finalScale) * 0.5f + screenPadding.x;
            float bubbleH = (bgRect.rect.height + 20f) * finalScale + screenPadding.y;

            anchoredPos.x = Mathf.Clamp(anchoredPos.x, bubbleW, Mathf.Max(bubbleW, screenW - bubbleW));
            anchoredPos.y = Mathf.Clamp(anchoredPos.y, screenPadding.y, Mathf.Max(screenPadding.y, screenH - bubbleH));
        }

        bubbleContainer.anchoredPosition = anchoredPos;
        canvasGroup.alpha = currentAlpha;
    }

    private void UpdateBubbleWidth()
    {
        if (messageText == null) return;

        // Metin kısaysa kutuyu daralt, uzunsa maxWidth'e sabitleyip alt satıra sar
        TextGenerator textGen = messageText.cachedTextGeneratorForLayout;
        TextGenerationSettings settings = messageText.GetGenerationSettings(Vector2.zero);
        float preferredW = textGen.GetPreferredWidth(messageText.text, settings) / messageText.pixelsPerUnit;

        float targetW = Mathf.Clamp(preferredW + 10f, 90f, maxWidth);
        if (msgLayoutElement != null) msgLayoutElement.preferredWidth = targetW;
        if (nameLayoutElement != null) nameLayoutElement.preferredWidth = targetW;

        if (bgRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(bgRect);
        }
    }

    /// <summary>
    /// Belirtilen metni verilen süre boyunca sohbet balonunda gösterir.
    /// </summary>
    public void Say(string text, float duration = 3f, string customSpeakerName = null)
    {
        if (canvasRoot == null) CreateBubbleUI();

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(ShowBubbleRoutine(text, duration, customSpeakerName));
    }

    /// <summary>
    /// Sohbet balonunu yumuşakça kapatır.
    /// </summary>
    public void Hide()
    {
        if (!gameObject.activeInHierarchy)
        {
            HideImmediate();
            return;
        }
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(HideRoutine());
    }

    public void HideImmediate()
    {
        IsShowing = false;
        currentAlpha = 0f;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (canvasRoot != null) canvasRoot.SetActive(false);
    }

    private IEnumerator ShowBubbleRoutine(string text, float duration, string customSpeakerName)
    {
        IsShowing = true;
        canvasRoot.SetActive(true);

        if (bgImage != null) bgImage.color = backgroundColor;
        if (tailImage != null) tailImage.color = backgroundColor;
        if (messageText != null)
        {
            messageText.color = textColor;
            messageText.fontSize = fontSize;
        }

        string finalName = !string.IsNullOrEmpty(customSpeakerName) ? customSpeakerName : speakerName;
        if (nameText != null)
        {
            bool hasName = !string.IsNullOrEmpty(finalName);
            nameText.gameObject.SetActive(hasName);
            if (hasName)
            {
                nameText.text = finalName;
                nameText.color = nameColor;
            }
        }

        // Pop-in animasyonu
        float popTime = 0.15f;
        float t = 0f;
        messageText.text = useTypewriter ? "" : text;
        UpdateBubbleWidth();

        while (t < popTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / popTime);
            currentAlpha = p;
            animScaleMultiplier = Mathf.Lerp(0.6f, 1f, p);
            yield return null;
        }
        currentAlpha = 1f;
        animScaleMultiplier = 1f;

        // Daktilo (Typewriter) efekti
        if (useTypewriter && charsPerSecond > 0f && !string.IsNullOrEmpty(text))
        {
            float delayPerChar = 1f / charsPerSecond;
            for (int i = 1; i <= text.Length; i++)
            {
                messageText.text = text.Substring(0, i);
                UpdateBubbleWidth();
                yield return new WaitForSeconds(delayPerChar);
            }
        }
        else
        {
            messageText.text = text;
            UpdateBubbleWidth();
        }

        // Ekranda kalma süresi
        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
            yield return HideRoutine();
        }
    }

    private IEnumerator HideRoutine()
    {
        if (canvasRoot == null || !canvasRoot.activeSelf) yield break;

        float fadeTime = 0.15f;
        float t = 0f;
        float startAlpha = currentAlpha;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeTime);
            currentAlpha = Mathf.Lerp(startAlpha, 0f, p);
            animScaleMultiplier = Mathf.Lerp(1f, 0.7f, p);
            yield return null;
        }

        HideImmediate();
    }

    #region Test Menüsü
    [ContextMenu("Test: Merhaba De (3 sn)")]
    private void TestSay()
    {
        if (Application.isPlaying)
        {
            Say("Merhaba! Bu balon duvar ve tavan arkasından bile her zaman görünür.", 3f);
        }
    }
    #endregion
}
