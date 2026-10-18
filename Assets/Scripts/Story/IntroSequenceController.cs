using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Intro.unity sahnesindeki "Kabus / Öngörü (Premonition)" sekansını baştan sona yöneten ana kontrolcü.
/// Karakter diyaloglarını, deprem sallantısını (Event 1), NPC kaçışını (Running) ve binanın çöküşünü (Event 2)
/// sinematik ekran efektleriyle (ScreenFaderUI) senkronize eder ve ardından EvMarket sahnesine geçiş yapar.
/// </summary>
public class IntroSequenceController : MonoBehaviour
{
    [Header("--- Başlangıç Ayarları ---")]
    [Tooltip("Sahne açıldığında intro sekansı otomatik başlasın mı?")]
    public bool autoStart = true;

    [Tooltip("Sekans başlamadan önceki bekleme süresi (saniye).")]
    public float startDelay = 1.0f;

    [Header("--- Karakter Referansları ---")]
    [Tooltip("Arkadaş 1 (male01_2) üzerindeki SpeechBubble bileşeni. Boşsa Start'ta otomatik aranır.")]
    public SpeechBubble friendA1;

    [Tooltip("Arkadaş 2 (male02_2) üzerindeki SpeechBubble bileşeni. Boşsa Start'ta otomatik aranır.")]
    public SpeechBubble friendA2;

    [Header("--- Sahne Geçişi ---")]
    [Tooltip("Intro bittikten sonra yüklenecek sahne adı.")]
    public string nextSceneName = "EvMarket";

    [Header("--- Ses Efektleri (Opsiyonel) ---")]
    public AudioClip earthquakeClip;
    public AudioClip panicScreamClip;
    public AudioClip sirenAfadClip;
    public AudioClip sadMusicClip;
    public AudioSource audioSource;

    private Coroutine introRoutine;

    private void Awake()
    {
        // Intro otomatik başlayacaksa ekranı en başta siyah tutarak ani sahne yüklenme parlamasını engelle
        if (autoStart)
        {
            ScreenFaderUI.EnsureInstance().SetBlackImmediate(true);
        }
    }

    private void Start()
    {
        // 1. male01_2 referansını otomatik bağla
        if (friendA1 == null || friendA1.transform.parent != null)
        {
            GameObject a1Obj = GameObject.Find("/male01_2") ?? GameObject.Find("male01_2");
            if (a1Obj != null)
            {
                if (a1Obj.transform.parent != null && a1Obj.transform.root != null && a1Obj.transform.root.name.StartsWith("male01"))
                {
                    a1Obj = a1Obj.transform.root.gameObject;
                }
                friendA1 = a1Obj.GetComponent<SpeechBubble>() ?? a1Obj.AddComponent<SpeechBubble>();
            }
        }

        // 2. male02_2 referansını otomatik bağla (Saate yakın olan arkadaş)
        if (friendA2 == null || friendA2.transform.parent != null)
        {
            GameObject a2Obj = GameObject.Find("/male02_2") ?? GameObject.Find("male02_2");
            if (a2Obj != null)
            {
                if (a2Obj.transform.parent != null && a2Obj.transform.root != null && a2Obj.transform.root.name.StartsWith("male02"))
                {
                    a2Obj = a2Obj.transform.root.gameObject;
                }
                friendA2 = a2Obj.GetComponent<SpeechBubble>() ?? a2Obj.AddComponent<SpeechBubble>();
            }
        }

        // 3. AudioSource kontrolü
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (autoStart)
        {
            StartSequence();
        }
    }

    /// <summary>
    /// Intro sekansını başlatır.
    /// </summary>
    public void StartSequence()
    {
        if (introRoutine != null) StopCoroutine(introRoutine);
        introRoutine = StartCoroutine(RunIntroSequence());
    }

    /// <summary>
    /// Devam eden intro sekansını durdurur.
    /// </summary>
    public void StopSequence()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        if (friendA1 != null) friendA1.Hide();
        if (friendA2 != null) friendA2.Hide();
        ScreenFaderUI.EnsureInstance().HideSubtitle();
        ScreenFaderUI.EnsureInstance().HideCenterMessage();
    }

    private IEnumerator RunIntroSequence()
    {
        Debug.Log("[IntroSequenceController] Intro kabus sekansı başladı.");

        // Başlangıç gecikmesi
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        // 1. Ekran siyahtan açılır
        ScreenFaderUI.EnsureInstance().FadeFromBlack(1.2f);
        yield return new WaitForSeconds(1.2f);

        // 2. Arkadaş 1 konuşur + Altyazı
        Speak(friendA1, "Arkadaş 1", "Harbiden dünkü parti çok iyiydi!", 2.8f);
        yield return new WaitForSeconds(3.0f);

        // 3. Arkadaş 2 konuşur + Altyazı
        Speak(friendA2, "Arkadaş 2", "Evet ya, efsaneydi.", 2.0f);
        yield return new WaitForSeconds(2.2f);

        // 4. Mert (Sen) kahve yudumlar (Sadece Altyazı)
        ScreenFaderUI.EnsureInstance().ShowSubtitle("Mert (Sen)", "(Kahvesinden bir yudum alır...)", 2.2f);
        yield return new WaitForSeconds(2.4f);

        // 5. Arkadaş 1 konuşur + Altyazı
        Speak(friendA1, "Arkadaş 1", "Mert harbi ya, sen dün gece neydin öyle!", 2.8f);
        yield return new WaitForSeconds(3.0f);

        // 6. Mert (Sen) cevap verir
        ScreenFaderUI.EnsureInstance().ShowSubtitle("Mert (Sen)", "Eyvallah, siz de fena değildiniz...", 2.5f);
        yield return new WaitForSeconds(2.7f);

        // 7. DEPREM BAŞLAR: Objeler sallanır
        Debug.Log("[IntroSequenceController] Deprem başlıyor!");
        GameEvents.TriggerEvent1(true);
        PlayAudio(earthquakeClip, 1f);

        Speak(friendA1, "Arkadaş 1", "Lan! Masa sallanıyor, deprem oluyor!", 2.5f);
        yield return new WaitForSeconds(2.5f);

        // 8. PANİK & KAÇIŞ: Karakterler koşar
        Debug.Log("[IntroSequenceController] Panik ve kaçış başlıyor!");
        GameEvents.TriggerRunning(true);
        PlayAudio(panicScreamClip, 0.9f);

        Speak(friendA2, "Arkadaş 2", "Çabuk dışarı koş!!", 2.5f);
        yield return new WaitForSeconds(2.5f);

        // 9. Ekran yavaşça kararmaya başlar (3.0s), 1.5 sn sonra bina çöker (Event 2)
        Debug.Log("[IntroSequenceController] Ekran kararıyor, bina çökecek...");
        ScreenFaderUI.EnsureInstance().FadeToBlack(3.0f);
        yield return new WaitForSeconds(1.5f);

        // Kararmaya yakın bina fiziksel olarak çöker
        GameEvents.TriggerEvent2();
        yield return new WaitForSeconds(1.5f);

        // 10. Ekran tamamen karardığında sallantı durdurulur ve merkez mesajı gösterilir
        GameEvents.TriggerEvent1(false);
        PlayAudio(sirenAfadClip, 0.85f);
        PlayAudio(sadMusicClip, 0.7f);

        ScreenFaderUI.EnsureInstance().ShowCenterMessage(
            "[ Enkaz Altı ]",
            "Uzaktan boğuk siren sesleri, AFAD çağrıları ve çığlıklar duyuluyor...",
            4.5f
        );
        yield return new WaitForSeconds(4.8f);

        // 11. Event durumları sıfırlanır ve sonraki sahneye geçilir
        GameEvents.ResetEvents();
        LoadNextScene();
    }

    private void Speak(SpeechBubble speaker, string speakerName, string text, float duration)
    {
        if (speaker != null)
        {
            speaker.Say(text, duration, speakerName);
        }

        ScreenFaderUI.EnsureInstance().ShowSubtitle(speakerName, text, duration);
    }

    private void PlayAudio(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("[IntroSequenceController] nextSceneName boş bırakılmış, sahne yüklenmedi.");
            return;
        }

        Debug.Log($"[IntroSequenceController] Sahne geçişi yapılıyor: {nextSceneName}");

        try
        {
            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogWarning($"[IntroSequenceController] '{nextSceneName}' sahnesi Build Settings'de kayıtlı görünmüyor. Yine de LoadScene çağrılıyor...");
                SceneManager.LoadScene(nextSceneName);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[IntroSequenceController] '{nextSceneName}' sahnesi yüklenirken hata oluştu: {ex.Message}");
        }
    }

    #region Context Menüleri
    [ContextMenu("Sekansı Şimdi Başlat")]
    public void ContextPlay() => StartSequence();

    [ContextMenu("Sekansı Durdur")]
    public void ContextStop() => StopSequence();
    #endregion
}
