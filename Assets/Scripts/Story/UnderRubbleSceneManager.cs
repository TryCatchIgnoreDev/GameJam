using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameJam.Story
{
    /// <summary>
    /// End_UnderRubble sahnesinin ana yöneticisi (Enkaz Altı Sahne Yöneticisi).
    /// Hayatta Kalma Akışı:
    /// 1. Sahne açılır: Ekran karartmadan açılır, loş ay ışığı ve enkaz çatlaklarından gelen sızıntılar görünür.
    /// 2. Çantada Fener yoksa -> Zifiri karanlıkta kalınır, fener arama başarısız olur ve karanlıkta ezilme ölümü gerçekleşir.
    /// 3. Fener varsa -> [F] tuşuyla fener açılır, 10 saniyelik araştırma sayacı başlar.
    /// 4. 10 saniye sonra yukarıdan boğuk AFAD megafon sesi ve sirenler duyulur.
    /// 5. Çantada Düdük yoksa -> Boğaz tozla dolu olduğundan ses çıkmaz, AFAD enkazı boş zannedip gider ve düdük eksikliği ölümü gerçekleşir.
    /// 6. Düdük varsa -> [E] tuşuyla düdük çalınır, AFAD sesi alır ve kazı başlar.
    /// 7. Kurtarma saatler sürerken çantadaki diğer 4 hayati öğe kontrol edilir:
    ///    - Su yoksa -> Dehidrasyon ve böbrek yetmezliği ölümü.
    ///    - İlk yardım kiti yoksa -> Kan kaybı ve yara enfeksiyonu ölümü.
    ///    - Uyku tulumu / battaniye yoksa -> Dondurucu soğukta şiddetli hipotermi ölümü.
    ///    - Konserve gıda yoksa -> Açlık ve enerji yetmezliği ölümü.
    /// 8. Biri bile eksikse dramatik ölüm ekranı açılır, gerekçe ve eksik eşyalar yazılır, süre bitince oyun başa (EvMarket/Intro) döner.
    /// 9. 6/6 tüm hayati eşyalar tamsa tam kurtarılma gerçekleşir, kamera tepeye süzülür ve "HAZIRLIK HAYAT KURTARIR" final mesajı gösterilir.
    /// </summary>
    public class UnderRubbleSceneManager : MonoBehaviour
    {
        public static UnderRubbleSceneManager Instance { get; private set; }

        [Header("Oyuncu ve Fener")]
        [SerializeField] private GameObject player;
        [SerializeField] private Light flashlightLight;
        [SerializeField] private bool flashlightOn = false;

        [Header("Arkadaşlar")]
        [SerializeField] private GameObject friend1;
        [SerializeField] private GameObject friend2;

        [Header("Kameralar ve Geçiş")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Camera outtroCamera;
        [SerializeField] private Transform outtroCameraMountPoint;

        [Header("Işık Efektleri")]
        [SerializeField] private Light rescueSpotlight;

        [Header("Ses Efektleri")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip whistleSound;
        [SerializeField] private AudioClip flashlightClickSound;
        [SerializeField] private AudioClip distantSirenSound;

        [Header("Durum Akışı")]
        [SerializeField] private bool hasTurnedOnFlashlight = false;
        [SerializeField] private bool afadCalling = false;
        [SerializeField] private bool isRescued = false;
        public bool IsRescued => isRescued;
        [SerializeField] private float flashlightTimer = 0f;
        [SerializeField] private const float FLASH_TO_AFAD_DELAY = 10.0f;

        [Header("Başa Dönüş (Zaman Döngüsü)")]
        [Tooltip("Ölüm durumunda başa dönülecek sahnenin adı.")]
        [SerializeField] private string restartSceneName = "EvMarket";
        [SerializeField] private bool isDeathSequenceRunning = false;

        [Header("Test ve Hile Ayarları (Editor)")]
        [Tooltip("İşaretliyse veya EvMarket oynanmadan sahne doğrudan açıldıysa bu test değerleri kullanılır.")]
        [SerializeField] private bool overrideSurvivalDataForTesting = false;
        [SerializeField] private bool testHasFlashlight = true;
        [SerializeField] private bool testHasWhistle = true;
        [SerializeField] private bool testHasWater = true;
        [SerializeField] private bool testHasFirstAid = true;
        [SerializeField] private bool testHasBlanket = true;
        [SerializeField] private bool testHasFood = true;

        private Coroutine activeFlashlightWaitRoutine = null;
        private Coroutine activeAfadCallingRoutine = null;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Start()
        {
            CrosshairUI.EnsureInstance();

            // Test veya varsayılan hayatta kalma verisi entegrasyonu
            InitializeSurvivalData();

            // Fener başlangıçta KAPALI
            if (flashlightLight != null)
            {
                flashlightLight.enabled = false;
            }
            flashlightOn = false;

            // Kamera başlangıç ayarları: Oyuncu kamerası aktif, outtro kapalı
            if (playerCamera != null)
            {
                playerCamera.enabled = true;
                playerCamera.tag = "MainCamera";
            }
            if (outtroCamera != null)
            {
                outtroCamera.enabled = false;
            }

            // Dışarıdan gelen boğuk siren ambiyansı
            if (distantSirenSound != null && audioSource != null)
            {
                audioSource.clip = distantSirenSound;
                audioSource.loop = true;
                audioSource.volume = 0.28f;
                audioSource.Play();
            }

            // Ekran açılışı ve başlangıç sekansı
            StartCoroutine(InitialSequenceRoutine());
        }

        private void InitializeSurvivalData()
        {
            // Eğer editörde test modu açıksa veya alışveriş verisi hiç set edilmemişse test ayarlarını uygula
            if (overrideSurvivalDataForTesting || !PlayerSurvivalData.HasCompletedShopping)
            {
                PlayerSurvivalData.HasCompletedShopping = true;
                PlayerSurvivalData.HasFlashlight = testHasFlashlight;
                PlayerSurvivalData.HasWhistle = testHasWhistle;
                PlayerSurvivalData.HasWater = testHasWater;
                PlayerSurvivalData.HasFirstAid = testHasFirstAid;
                PlayerSurvivalData.HasBlanket = testHasBlanket;
                PlayerSurvivalData.HasFood = testHasFood;
                PlayerSurvivalData.EssentialItemCount = 
                    (testHasFlashlight ? 1 : 0) + (testHasWhistle ? 1 : 0) +
                    (testHasWater ? 1 : 0) + (testHasFirstAid ? 1 : 0) +
                    (testHasBlanket ? 1 : 0) + (testHasFood ? 1 : 0);

                Debug.Log($"[UnderRubbleSceneManager] Test Hayatta Kalma Verisi Uygulandı. Fener:{testHasFlashlight}, Düdük:{testHasWhistle}, Su:{testHasWater}, İlkYardım:{testHasFirstAid}, Battaniye:{testHasBlanket}, Konserve:{testHasFood}");
            }
            else
            {
                Debug.Log($"[UnderRubbleSceneManager] Oyuncu Hayatta Kalma Verisi Yüklendi. Fener:{PlayerSurvivalData.HasFlashlight}, Düdük:{PlayerSurvivalData.HasWhistle}, Su:{PlayerSurvivalData.HasWater}, İlkYardım:{PlayerSurvivalData.HasFirstAid}, Battaniye:{PlayerSurvivalData.HasBlanket}, Konserve:{PlayerSurvivalData.HasFood} (Toplam: {PlayerSurvivalData.EssentialItemCount}/6)");
            }
        }

        private IEnumerator InitialSequenceRoutine()
        {
            ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();
            if (fader != null)
            {
                fader.FadeFromBlack(2.0f);
            }

            yield return new WaitForSeconds(1.0f);

            if (fader != null)
            {
                fader.ShowSubtitle("Mert (Sen)", "Gözlerimi açtım... Zifiri karanlık, molozların altındayız. Feneri bulmalıyım!", 4.0f);
            }

            yield return new WaitForSeconds(4.2f);

            if (!hasTurnedOnFlashlight && !isDeathSequenceRunning && fader != null)
            {
                if (PlayerSurvivalData.HasFlashlight)
                {
                    fader.ShowSubtitle("[F]", "El Fenerini Aç", 8.0f);
                }
                else
                {
                    fader.ShowSubtitle("[F]", "El Fenerini Ara (Çantayı Yokla)", 8.0f);
                    activeFlashlightWaitRoutine = StartCoroutine(WaitForFlashlightOrTimeout());
                }
            }
        }

        private IEnumerator WaitForFlashlightOrTimeout()
        {
            // Feneri olmayan oyuncuya 7.5 saniye arama şansı verilir, basmazsa karanlık nefesini keser
            yield return new WaitForSeconds(7.5f);
            if (!hasTurnedOnFlashlight && !isDeathSequenceRunning)
            {
                OnNoFlashlightFailure();
            }
        }

        private void Update()
        {
            if (isRescued || isDeathSequenceRunning) return;

            HandleInput();
            HandleDebugInput();

            // Fener açıldıktan sonra AFAD sayacı işler
            if (hasTurnedOnFlashlight && !afadCalling)
            {
                flashlightTimer += Time.deltaTime;
                if (flashlightTimer >= FLASH_TO_AFAD_DELAY)
                {
                    activeAfadCallingRoutine = StartCoroutine(AfadCallingSequence());
                }
            }
        }

        private void HandleInput()
        {
            bool fPressed = false;
            bool ePressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.fKey.wasPressedThisFrame) fPressed = true;
                if (Keyboard.current.eKey.wasPressedThisFrame) ePressed = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.F)) fPressed = true;
            if (Input.GetKeyDown(KeyCode.E)) ePressed = true;
#endif

            if (fPressed)
            {
                OnFlashlightInputPressed();
            }

            if (ePressed)
            {
                OnWhistleInputPressed();
            }
        }

        private void OnFlashlightInputPressed()
        {
            if (isDeathSequenceRunning) return;

            // FENER KONTROLÜ
            if (!PlayerSurvivalData.HasFlashlight)
            {
                OnNoFlashlightFailure();
                return;
            }

            ToggleFlashlight();
        }

        private void OnNoFlashlightFailure()
        {
            if (isDeathSequenceRunning) return;

            if (activeFlashlightWaitRoutine != null)
            {
                StopCoroutine(activeFlashlightWaitRoutine);
                activeFlashlightWaitRoutine = null;
            }

            StartCoroutine(NoFlashlightDeathSequenceRoutine());
        }

        private IEnumerator NoFlashlightDeathSequenceRoutine()
        {
            isDeathSequenceRunning = true;
            ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();

            // Fener arama / klik sesi ama ışık yok
            if (flashlightClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(flashlightClickSound, 0.85f);
            }

            if (fader != null)
            {
                fader.ShowSubtitle("Mert (Sen)", "Kahretsin... Çantama el feneri almamıştım! Zifiri karanlık, hiçbir şey göremiyorum!", 3.8f);
            }

            yield return new WaitForSeconds(4.0f);

            if (fader != null)
            {
                fader.ShowSubtitle("Mert (Sen)", "Tavan... tavan kayıyor! Nereye sığınacağımı göremiyorum...", 3.2f);
            }

            yield return new WaitForSeconds(2.5f);

            TriggerDeathSequence("flashlight");
        }

        public void ToggleFlashlight()
        {
            if (!PlayerSurvivalData.HasFlashlight)
            {
                OnNoFlashlightFailure();
                return;
            }

            flashlightOn = !flashlightOn;
            if (flashlightLight != null)
            {
                flashlightLight.enabled = flashlightOn;
            }

            if (flashlightClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(flashlightClickSound, 0.85f);
            }

            if (flashlightOn && !hasTurnedOnFlashlight)
            {
                hasTurnedOnFlashlight = true;
                ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();
                if (fader != null)
                {
                    fader.ShowSubtitle("Mert (Sen)", "Yaşam üçgeni bizi korumuş ama tavan her an çökebilir... Bir ses çıkarmam lazım!", 4.5f);
                }
                Debug.Log("[UnderRubbleSceneManager] Fener açıldı! 10 saniyelik AFAD varış sayacı başladı.");
            }
        }

        private IEnumerator AfadCallingSequence()
        {
            afadCalling = true;
            ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();

            // AFAD MegaFon Çağrısı
            if (fader != null)
            {
                fader.ShowSubtitle("AFAD Arama Kurtarma", "AŞAĞIDA KİMSE VAR MI?! SESİMİ DUYAN VARSA SES VERSİN YA DA BİR YERE VURSUN!", 5.0f);
            }

            yield return new WaitForSeconds(5.2f);

            // Düdük / Ses Yönlendirmesi
            if (fader != null && !isRescued && !isDeathSequenceRunning)
            {
                if (PlayerSurvivalData.HasWhistle)
                {
                    fader.ShowSubtitle("[E]", "Deprem Düdüğünü Çal (AFAD'a Sesini Duyur)", 15.0f);
                }
                else
                {
                    fader.ShowSubtitle("[E]", "Sesini Duyurmaya Çalış (Bağır)", 15.0f);
                }
            }
        }

        private void OnWhistleInputPressed()
        {
            if (isDeathSequenceRunning || isRescued) return;

            // DÜDÜK KONTROLÜ
            if (!PlayerSurvivalData.HasWhistle)
            {
                OnNoWhistleFailure();
                return;
            }

            OnWhistlePressed();
        }

        private void OnNoWhistleFailure()
        {
            if (isDeathSequenceRunning) return;
            StartCoroutine(NoWhistleDeathSequenceRoutine());
        }

        private IEnumerator NoWhistleDeathSequenceRoutine()
        {
            isDeathSequenceRunning = true;
            ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();

            if (fader != null)
            {
                fader.ShowSubtitle("Mert (Sen)", "(Boğazın moloz ve tozla dolu, sesin kısılmış! Bağırmaya çalışıyorsun ama sesin çıkmıyor: 'Hııı... hııı...')", 4.0f);
            }

            yield return new WaitForSeconds(4.2f);

            if (fader != null)
            {
                fader.ShowSubtitle("Mert (Sen)", "Lanet olsun... Çantama deprem düdüğü almamıştım! Sesimi yukarıya duyuramıyorum!", 3.8f);
            }

            yield return new WaitForSeconds(4.0f);

            // AFAD ses alamayınca gider
            if (fader != null)
            {
                fader.ShowSubtitle("AFAD Görevlisi", "Aşağıdan ses gelmiyor... Burası boş, ekip diğer binaya geçsin!", 4.0f);
            }

            // Siren sesi yavaşça uzaklaşır
            if (audioSource != null)
            {
                StartCoroutine(FadeOutAudioSource(3.0f));
            }

            yield return new WaitForSeconds(3.5f);

            if (fader != null)
            {
                fader.ShowSubtitle("Mert (Sen)", "Hayır... Hayır gitmeyin! Buradayız... (Karanlık ve derin bir sessizlik geri dönüyor...)", 3.5f);
            }

            yield return new WaitForSeconds(3.0f);

            TriggerDeathSequence("whistle");
        }

        private void OnWhistlePressed()
        {
            if (whistleSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(whistleSound, 1.0f);
            }

            // AFAD seslenmişse kurtarılma sekansını başlat
            if (afadCalling && !isRescued && !isDeathSequenceRunning)
            {
                StartCoroutine(RescueSequenceRoutine());
            }
            else if (!afadCalling && !isDeathSequenceRunning)
            {
                ScreenFaderUI.EnsureInstance()?.ShowSubtitle("Mert (Sen)", "(Düdük çaldın ama dışarıdan henüz bir ses gelmiyor...)", 2.5f);
            }
        }

        private IEnumerator RescueSequenceRoutine()
        {
            isRescued = true;
            ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();

            // Mert seslenir
            if (fader != null)
            {
                fader.ShowSubtitle("Mert (Sen)", "BURADAYIZ! ENKAZIN ALTINDAYIZ!", 2.8f);
            }

            yield return new WaitForSeconds(3.0f);

            // AFAD yanıt verir
            if (fader != null)
            {
                fader.ShowSubtitle("AFAD Görevlisi", "SESİ ALDIK! TAM BURADALAR! EKİP BURAYA GELSİN, ÇIKARIYORUZ!", 4.0f);
            }

            // Projektör içeri vurur
            if (rescueSpotlight != null)
            {
                rescueSpotlight.enabled = true;
            }

            // Oyuncuyu dondur
            if (PlayerCrawlController.Instance != null)
            {
                PlayerCrawlController.Instance.SetFrozen(true);
            }

            // El fenerini kameradan/oyuncudan bağımsızlaştır ve dünyada sabit bırak (yukarı uçmasın)
            DetachFlashlight();

            // Dondurulduktan tam 1.0 saniye sonra kamera yükselmeye başlar
            yield return new WaitForSeconds(1.0f);

            // Kamerayı yukarı Outtro kamerasına geçir ve yumuşakça süzül
            if (outtroCamera != null)
            {
                if (playerCamera != null)
                {
                    StartCoroutine(GlideCameraToOuttro(playerCamera.transform, outtroCamera.transform));
                }
                else
                {
                    outtroCamera.enabled = true;
                }
            }

            // DİĞER HAYATİ EŞYALARIN KONTROLÜ (Su, İlk Yardım, Uyku Tulumu/Battaniye, Konserve Gıda)
            bool hasAllEssentials = PlayerSurvivalData.HasAllEssentialItems();

            if (!hasAllEssentials)
            {
                // Biri bile eksikse saatler süren kazı sırasında ölüm sekansı başlar!
                yield return new WaitForSeconds(3.0f);

                if (fader != null)
                {
                    fader.ShowSubtitle("AFAD Arama Kurtarma", "Ağır beton blokları kaldırmak saatler sürecek, dayanın gençler!", 4.0f);
                }

                yield return new WaitForSeconds(4.2f);

                // Zaman atlaması ve dramatik ölüm sebebi
                string missingItem = DeterminePostRescueMissingItem();
                StartCoroutine(PostRescueDeathSequenceRoutine(missingItem));
                yield break;
            }

            // TÜM 6 EŞYA TAM! TAM VE GERÇEK KURTULUŞ!
            yield return new WaitForSeconds(4.0f);

            if (fader != null)
            {
                fader.ShowSubtitle("Mert (Sen)", "Dayandık çocuklar... Çantamızdaki su ve malzemelerle ekipler bize ulaşana kadar direndik. Yaşıyoruz!", 4.5f);
            }

            yield return new WaitForSeconds(5.0f);

            // Final Farkındalık Mesajı
            if (fader != null)
            {
                fader.ShowCenterMessage("HAZIRLIK HAYAT KURTARIR",
                    "O gün 'bana bir şey olmaz' demeyenler kurtuldu.\nEksiksiz deprem çantası, doğru bilgi ve soğukkanlılık hayat kurtardı.\nFarkındalık afetten önce başlar.",
                    18.0f);
            }
        }

        private string DeterminePostRescueMissingItem()
        {
            if (!PlayerSurvivalData.HasWater) return "water_jug";
            if (!PlayerSurvivalData.HasFirstAid) return "first_aid";
            if (!PlayerSurvivalData.HasBlanket) return "sleeping_bag";
            if (!PlayerSurvivalData.HasFood) return "canned_food";
            return PlayerSurvivalData.GetPrimaryMissingItemId();
        }

        private IEnumerator PostRescueDeathSequenceRoutine(string missingItemId)
        {
            isDeathSequenceRunning = true;
            ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();

            // Ekran yavaşça kararırken geçen saatler anlatılır
            yield return new WaitForSeconds(1.0f);

            switch (missingItemId)
            {
                case "water_jug":
                    if (fader != null) fader.ShowSubtitle("Mert (Sen)", "Saatler geçiyor... Ağzım damağım kurudu, bilincim kapanıyor... Çantada bir yudum su bile yok...", 4.2f);
                    break;

                case "first_aid":
                    if (fader != null) fader.ShowSubtitle("Mert (Sen)", "Çöküş anında bacağıma demir saplanmıştı... Çantamda ilk yardım kiti yok, kanamayı durduramıyorum...", 4.2f);
                    break;

                case "sleeping_bag":
                    if (fader != null) fader.ShowSubtitle("Mert (Sen)", "Gece oldu... Sıcaklık sıfırın altına indi. Çantada battaniye yok, vücudumu hissedemiyorum, donuyorum...", 4.2f);
                    break;

                case "canned_food":
                    if (fader != null) fader.ShowSubtitle("Mert (Sen)", "Saatler süren bekleyişte kan şekerim sıfıra indi, gücüm bitti... Çantamda dayanıklı gıda yoktu...", 4.2f);
                    break;

                default:
                    if (fader != null) fader.ShowSubtitle("Mert (Sen)", "Dayanamıyorum... Çantamda hayati eksikler vardı...", 3.5f);
                    break;
            }

            yield return new WaitForSeconds(4.5f);

            TriggerDeathSequence(missingItemId);
        }

        /// <summary>
        /// Dramatik ölüm ekranını tetikler, gerekçeyi gösterir ve oyunu başa (EvMarket/Intro) sarar.
        /// </summary>
        public void TriggerDeathSequence(string missingItemId)
        {
            isDeathSequenceRunning = true;

            string reason = PlayerSurvivalData.GetDeathReasonForMissingItem(missingItemId);
            List<string> missingNames = PlayerSurvivalData.GetMissingEssentialItemNames();
            string missingNotice = (missingNames != null && missingNames.Count > 0)
                ? "[ Eksik Hayati Eşyalar: " + string.Join(", ", missingNames) + " ]"
                : "";

            Debug.LogWarning($"[UnderRubbleSceneManager] ÖLÜM EKRANI TETİKLENDİ. Sebep: {missingItemId}. Eksikler: {missingNotice}");

            ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();
            if (fader != null)
            {
                fader.HideSubtitle();
                fader.ShowDeathScreen(
                    "ENKAZ ALTINDA KAYIP",
                    reason,
                    missingNotice,
                    7.5f,
                    OnDeathScreenFinished
                );
            }
            else
            {
                StartCoroutine(FallbackRestartRoutine());
            }
        }

        private void OnDeathScreenFinished()
        {
            Debug.Log($"[UnderRubbleSceneManager] Ölüm sekansı tamamlandı. Oyun başa sarılıyor: '{restartSceneName}'");

            // Çanta ve hayatta kalma verilerini yeni döngü için sıfırla
            PlayerSurvivalData.ResetData();
            GameEvents.ResetEvents();

            RequestSceneChange(restartSceneName);
        }

        /// <summary>
        /// Sahne geçişini merkezi SceneCoordinator üzerinden yapar.
        /// </summary>
        public static void RequestSceneChange(string targetScene)
        {
            if (SceneCoordinator.Instance != null)
            {
                Debug.Log($"[UnderRubbleSceneManager] SceneCoordinator.Instance.RequestSceneChange('{targetScene}') çağrılıyor.");
                SceneCoordinator.Instance.RequestSceneChange(targetScene, 1.0f);
            }
            else
            {
                Debug.Log($"[UnderRubbleSceneManager] SceneCoordinator hazır değil, SceneManager.LoadScene('{targetScene}') kullanılıyor.");
                if (Application.CanStreamedLevelBeLoaded(targetScene))
                {
                    SceneManager.LoadScene(targetScene);
                }
                else
                {
                    Debug.LogWarning($"[UnderRubbleSceneManager] '{targetScene}' sahnesi bulunamadı, Intro yükleniyor.");
                    if (Application.CanStreamedLevelBeLoaded("Intro"))
                    {
                        SceneManager.LoadScene("Intro");
                    }
                    else
                    {
                        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    }
                }
            }
        }

        private IEnumerator FallbackRestartRoutine()
        {
            yield return new WaitForSeconds(5.0f);
            OnDeathScreenFinished();
        }

        private IEnumerator FadeOutAudioSource(float duration)
        {
            if (audioSource == null) yield break;
            float startVol = audioSource.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
            audioSource.Stop();
        }

        private IEnumerator GlideCameraToOuttro(Transform fromCam, Transform toCam)
        {
            // Kamera süzülmeden önce fenerin kesinlikle kameradan ayrıldığından emin ol
            DetachFlashlight();

            Vector3 startPos = fromCam.position;
            Quaternion startRot = fromCam.rotation;
            Vector3 endPos = toCam.position;
            Quaternion endRot = toCam.rotation;

            float duration = 6.0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                fromCam.position = Vector3.Lerp(startPos, endPos, t);
                fromCam.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }

            fromCam.position = endPos;
            fromCam.rotation = endRot;

            if (outtroCamera != null)
            {
                outtroCamera.enabled = true;
                if (playerCamera != null) playerCamera.enabled = false;
            }
        }

        /// <summary>
        /// Kurtarma anında el fenerini kameradan/oyuncudan ayırarak dünyada sabitler.
        /// Böylece dışarı çıkış/outtro kamerası yukarı süzülürken el feneri yukarı uçmaz,
        /// enkaz altında o anki konum ve açısında kalarak yeri aydınlatmaya devam eder.
        /// </summary>
        public void DetachFlashlight()
        {
            Light targetLight = flashlightLight;
            if (targetLight == null && PlayerCrawlController.Instance != null)
            {
                targetLight = PlayerCrawlController.Instance.flashlight;
            }

            if (targetLight != null)
            {
                if (targetLight.transform.parent != null)
                {
                    targetLight.transform.SetParent(null, true);
                    Debug.Log($"[UnderRubbleSceneManager] El feneri kameradan bağımsızlaştırıldı ve dünyada sabitlendi. Pos: {targetLight.transform.position}");
                }

                // Fener açıksa açık kalmaya devam etsin (enkaz altını aydınlatsın)
                if (flashlightOn || hasTurnedOnFlashlight)
                {
                    targetLight.enabled = true;
                }
            }
        }

        #region Setup Helpers (Editor Tarafından Kullanılır)
        public void ConfigureReferences(GameObject plyr, Light flLight, Camera plyrCam, Camera outCam, Transform outMount, GameObject f1, GameObject f2, Light rescueLight = null)
        {
            player = plyr;
            flashlightLight = flLight;
            playerCamera = plyrCam;
            outtroCamera = outCam;
            outtroCameraMountPoint = outMount;
            friend1 = f1;
            friend2 = f2;
            rescueSpotlight = rescueLight;
        }
        #endregion

        #region Hile ve Test Klavye Tuşları (1 - 7 Tuşları)
        private void HandleDebugInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return;
            if (Keyboard.current.digit1Key.wasPressedThisFrame) TestDeathMissingFlashlight();
            if (Keyboard.current.digit2Key.wasPressedThisFrame) TestDeathMissingWhistle();
            if (Keyboard.current.digit3Key.wasPressedThisFrame) TestDeathMissingWater();
            if (Keyboard.current.digit4Key.wasPressedThisFrame) TestDeathMissingFirstAid();
            if (Keyboard.current.digit5Key.wasPressedThisFrame) TestDeathMissingBlanket();
            if (Keyboard.current.digit6Key.wasPressedThisFrame) TestDeathMissingFood();
            if (Keyboard.current.digit7Key.wasPressedThisFrame) TestWinAllEssentials();
#else
            if (Input.GetKeyDown(KeyCode.Alpha1)) TestDeathMissingFlashlight();
            if (Input.GetKeyDown(KeyCode.Alpha2)) TestDeathMissingWhistle();
            if (Input.GetKeyDown(KeyCode.Alpha3)) TestDeathMissingWater();
            if (Input.GetKeyDown(KeyCode.Alpha4)) TestDeathMissingFirstAid();
            if (Input.GetKeyDown(KeyCode.Alpha5)) TestDeathMissingBlanket();
            if (Input.GetKeyDown(KeyCode.Alpha6)) TestDeathMissingFood();
            if (Input.GetKeyDown(KeyCode.Alpha7)) TestWinAllEssentials();
#endif
        }

        [ContextMenu("Test: Fener Eksik (Karanlık Ölümü)")]
        public void TestDeathMissingFlashlight()
        {
            PlayerSurvivalData.HasFlashlight = false;
            TriggerDeathSequence("flashlight");
        }

        [ContextMenu("Test: Düdük Eksik (Ses Duyuramama Ölümü)")]
        public void TestDeathMissingWhistle()
        {
            PlayerSurvivalData.HasWhistle = false;
            TriggerDeathSequence("whistle");
        }

        [ContextMenu("Test: Su Eksik (Dehidrasyon Ölümü)")]
        public void TestDeathMissingWater()
        {
            PlayerSurvivalData.HasWater = false;
            TriggerDeathSequence("water_jug");
        }

        [ContextMenu("Test: İlk Yardım Eksik (Kan Kaybı Ölümü)")]
        public void TestDeathMissingFirstAid()
        {
            PlayerSurvivalData.HasFirstAid = false;
            TriggerDeathSequence("first_aid");
        }

        [ContextMenu("Test: Battaniye Eksik (Hipotermi Ölümü)")]
        public void TestDeathMissingBlanket()
        {
            PlayerSurvivalData.HasBlanket = false;
            TriggerDeathSequence("sleeping_bag");
        }

        [ContextMenu("Test: Konserve Eksik (Açlık/Takatsizlik Ölümü)")]
        public void TestDeathMissingFood()
        {
            PlayerSurvivalData.HasFood = false;
            TriggerDeathSequence("canned_food");
        }

        [ContextMenu("Test: Tüm Eşyalar Tam (Kurtuluş)")]
        public void TestWinAllEssentials()
        {
            PlayerSurvivalData.HasFlashlight = true;
            PlayerSurvivalData.HasWhistle = true;
            PlayerSurvivalData.HasWater = true;
            PlayerSurvivalData.HasFirstAid = true;
            PlayerSurvivalData.HasBlanket = true;
            PlayerSurvivalData.HasFood = true;
            PlayerSurvivalData.EssentialItemCount = 6;
            Debug.Log("[UnderRubbleSceneManager] HİLE: 6/6 tüm hayati eşyalar çantaya verildi!");
        }
        #endregion
    }
}
