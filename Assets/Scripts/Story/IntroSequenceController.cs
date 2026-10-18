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

    // NOT: Oyunda GÖSTERİLEN isimler -> Arkadaş 1 = "Can", Arkadaş 2 = "Emre".
    // Kod ve yorumlar bilinçli olarak "Arkadaş 1/2" şeklinde bırakıldı.
    [Header("--- Karakter Referansları ---")]
    [Tooltip("Arkadaş 1 (male01_2) üzerindeki SpeechBubble bileşeni. Boşsa Start'ta otomatik aranır.")]
    public SpeechBubble friendA1;

    [Tooltip("Arkadaş 2 (male02_2) üzerindeki SpeechBubble bileşeni. Boşsa Start'ta otomatik aranır.")]
    public SpeechBubble friendA2;

    [Header("--- Sahne Geçişi ---")]
    [Tooltip("Intro bittikten sonra yüklenecek sahne adı.")]
    public string nextSceneName = "EvMarket";

    [Header("--- Diyalog Modu & Seslendirme ---")]
    [Tooltip("Yeni seslendirmeli diyalog sistemini kullanır. Kapatılırsa anında eski orijinal diyaloglara geri döner.")]
    public bool useNewDialoguesWithVoice = true;

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
        CrosshairUI.EnsureInstance();

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

        SetupEscapeWaypoints();

        if (autoStart)
        {
            StartSequence();
        }
    }

    private void SetupEscapeWaypoints()
    {
        if (friendA1 != null)
        {
            var rc1 = friendA1.GetComponent<NPCRunController>() ?? friendA1.gameObject.AddComponent<NPCRunController>();
            rc1.moveForwardWhenRunning = true;
            rc1.runMoveSpeed = 3.8f;
            rc1.rotationSpeed = 12.0f;
            rc1.SetEscapeKeyframes(
                new Vector3(27.5f, friendA1.transform.position.y, -1862.5f), // 1. Keyframe: Masadan koridora çıkış
                new Vector3(29.8f, friendA1.transform.position.y, -1859.2f)  // 2. Keyframe: Dış kapıya yöneliş
            );
        }

        if (friendA2 != null)
        {
            var rc2 = friendA2.GetComponent<NPCRunController>() ?? friendA2.gameObject.AddComponent<NPCRunController>();
            rc2.moveForwardWhenRunning = true;
            rc2.runMoveSpeed = 4.0f;
            rc2.rotationSpeed = 12.0f;
            rc2.SetEscapeKeyframes(
                new Vector3(29.8f, friendA2.transform.position.y, -1864.5f), // 1. Keyframe: Masadan koridora çıkış
                new Vector3(29.8f, friendA2.transform.position.y, -1859.2f)  // 2. Keyframe: Dış kapıya yöneliş
            );
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

        if (useNewDialoguesWithVoice)
        {
            // YENİ SESLİ DİYALOGLAR (Bölüm 1)
            // 2. Arkadaş 1: "Harbiden dünkü parti çok iyiydi!" {1a.mp3}
            Speak(friendA1, "Can", "Harbiden dünkü parti çok iyiydi!", 2.8f, "1a");
            yield return new WaitForSeconds(3.0f);

            // 3. Arkadaş 2: "Evet ya, efsaneydi." {2.mp3}
            Speak(friendA2, "Emre", "Evet ya, efsaneydi.", 2.2f, "2");
            yield return new WaitForSeconds(2.4f);

            // 4. Mert (Sen) kahve yudumlar (Sadece Altyazı)
            ScreenFaderUI.EnsureInstance().ShowSubtitle("Mert (Sen)", "(Kahvesinden bir yudum alır...)", 2.2f);
            yield return new WaitForSeconds(2.4f);

            // 5. Arkadaş 1: "Mert harbi ya, sen dün gece neydin öyle!" {3.mp3}
            Speak(friendA1, "Can", "Mert harbi ya, sen dün gece neydin öyle!", 2.8f, "3");
            yield return new WaitForSeconds(3.0f);

            // 6. Mert (Sen): "Eyvallah, siz de fena değildiniz..." {4.mp3}
            Speak(null, "Mert (Sen)", "Eyvallah, siz de fena değildiniz...", 2.6f, "4");
            yield return new WaitForSeconds(2.8f);

            // 7. DEPREM BAŞLAR: Objeler sallanır
            Debug.Log("[IntroSequenceController] Deprem başlıyor!");
            GameEvents.TriggerEvent1(true);
            PlayAudio(earthquakeClip, 1f);

            // Arkadaş 1: "Lan! Masa sallanıyor, deprem oluyor!" {5.mp3}
            Speak(friendA1, "Can", "Lan! Masa sallanıyor, deprem oluyor!", 2.8f, "5");
            yield return new WaitForSeconds(2.8f);

            // 8. PANİK & KAÇIŞ: Karakterler koşar
            Debug.Log("[IntroSequenceController] Panik ve kaçış başlıyor!");
            GameEvents.TriggerRunning(true);
            PlayAudio(panicScreamClip, 0.9f);

            // Arkadaş 2: "Ne yapacağımı bilmiyorum!!" {6.mp3} (Yeni değişen replik)
            Speak(friendA2, "Emre", "Ne yapacağımı bilmiyorum!!", 2.6f, "6");
            yield return new WaitForSeconds(2.6f);
        }
        else
        {
            // ESKİ ORİJİNAL DİYALOGLAR (Geriye uyumlu)
            Speak(friendA1, "Can", "Harbiden dünkü parti çok iyiydi!", 2.8f);
            yield return new WaitForSeconds(3.0f);

            Speak(friendA2, "Emre", "Evet ya, efsaneydi.", 2.0f);
            yield return new WaitForSeconds(2.2f);

            ScreenFaderUI.EnsureInstance().ShowSubtitle("Mert (Sen)", "(Kahvesinden bir yudum alır...)", 2.2f);
            yield return new WaitForSeconds(2.4f);

            Speak(friendA1, "Can", "Mert harbi ya, sen dün gece neydin öyle!", 2.8f);
            yield return new WaitForSeconds(3.0f);

            ScreenFaderUI.EnsureInstance().ShowSubtitle("Mert (Sen)", "Eyvallah, siz de fena değildiniz...", 2.5f);
            yield return new WaitForSeconds(2.7f);

            Debug.Log("[IntroSequenceController] Deprem başlıyor!");
            GameEvents.TriggerEvent1(true);
            PlayAudio(earthquakeClip, 1f);

            Speak(friendA1, "Can", "Lan! Masa sallanıyor, deprem oluyor!", 2.5f);
            yield return new WaitForSeconds(2.5f);

            Debug.Log("[IntroSequenceController] Panik ve kaçış başlıyor!");
            GameEvents.TriggerRunning(true);
            PlayAudio(panicScreamClip, 0.9f);

            Speak(friendA2, "Emre", "Çabuk dışarı koş!!", 2.5f);
            yield return new WaitForSeconds(2.5f);
        }

        // 9. Ekran yavaşça kararmaya başlar (3.0s), 1.5 sn sonra bina çöker (Event 2)
        Debug.Log("[IntroSequenceController] Ekran kararıyor, bina çökecek...");
        ScreenFaderUI.EnsureInstance().FadeToBlack(3.0f);
        yield return new WaitForSeconds(1.5f);

        // Kararmaya yakın bina fiziksel olarak çöker (Event 2)
        GameEvents.TriggerEvent2();

        // En son yıkılma / çökme anında bu arkadaşlara Rigidbody ve Collider ekle / savrulma fiziğini aktif et
        friendA1?.GetComponent<NPCRunController>()?.EnableCollapsePhysics();
        friendA2?.GetComponent<NPCRunController>()?.EnableCollapsePhysics();

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

    private void Speak(SpeechBubble speaker, string speakerName, string text, float duration, string voiceClipName = null)
    {
        if (speaker != null)
        {
            speaker.Say(text, duration, speakerName);
        }

        ScreenFaderUI.EnsureInstance().ShowSubtitle(speakerName, text, duration);

        if (!string.IsNullOrEmpty(voiceClipName) && useNewDialoguesWithVoice)
        {
            DialogueAudioPlayer.PlayVoice(voiceClipName);
        }
    }

    private void PlayAudio(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        // Sahne ses kaynağı atanmamışsa (siren/AFAD seslerinin kaybolmaması için) global kaynağa düş.
        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
        else
        {
            DialogueAudioPlayer.PlaySfx(clip, volume);
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

        if (SceneCoordinator.Instance != null)
        {
            SceneCoordinator.Instance.RequestSceneChange(nextSceneName, 1.0f);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    #region Context Menüleri
    [ContextMenu("Sekansı Şimdi Başlat")]
    public void ContextPlay() => StartSequence();

    [ContextMenu("Sekansı Durdur")]
    public void ContextStop() => StopSequence();
    #endregion
}
