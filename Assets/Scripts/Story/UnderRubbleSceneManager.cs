using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameJam.Story
{
    /// <summary>
    /// End_UnderRubble sahnesinin ana yöneticisi (Enkaz Altı Sahne Yöneticisi).
    /// Akış Sıralaması:
    /// 1. Sahne açılır: Ekran karartmadan açılır, loş ay ışığı ve enkaz çatlaklarından gelen sızıntılar görünür.
    /// 2. Oyuncu sürünme modundadır. Fener kapalıdır.
    /// 3. Ekranda "[F] El Fenerini Aç" yönlendirmesi ve Mert'in altyazısı çıkar.
    /// 4. Oyuncu F tuşuna basana kadar AFAD ASLA GELMEZ!
    /// 5. Fener açılınca klik sesi çalar, 10 saniyelik araştırma sayacı başlar.
    /// 6. 10 saniye sonra yukarıdan boğuk AFAD megafon sesi ve sirenler duyulur.
    /// 7. Ekranda "[E] Deprem Düdüğünü Çal" yönlendirmesi çıkar.
    /// 8. Oyuncu E tuşuna basınca düdük çalar, AFAD yanıt verir ("SESİ ALDIK! TAM BURADALAR!").
    /// 9. Projektör içeri vurur, oyuncu dondurulur, kamera yumuşakça tepeye süzülüp 45x45m geniş yıkıntı alanına ve çakarlara bakar.
    /// 10. Final farkındalık mesajı gösterilir.
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
        [SerializeField] private float flashlightTimer = 0f;
        [SerializeField] private const float FLASH_TO_AFAD_DELAY = 10.0f;

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

            // Ekran açılışı ve başlangıç altyazısı
            StartCoroutine(InitialSequenceRoutine());
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

            if (!hasTurnedOnFlashlight && fader != null)
            {
                fader.ShowSubtitle("[F]", "El Fenerini Aç", 8.0f);
            }
        }

        private void Update()
        {
            if (isRescued) return;

            HandleInput();

            // Fener açıldıktan sonra AFAD sayacı işler
            if (hasTurnedOnFlashlight && !afadCalling)
            {
                flashlightTimer += Time.deltaTime;
                if (flashlightTimer >= FLASH_TO_AFAD_DELAY)
                {
                    StartCoroutine(AfadCallingSequence());
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
                ToggleFlashlight();
            }

            if (ePressed)
            {
                OnWhistlePressed();
            }
        }

        public void ToggleFlashlight()
        {
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

            // Düdük yönlendirmesi
            if (fader != null && !isRescued)
            {
                fader.ShowSubtitle("[E]", "Deprem Düdüğünü Çal (AFAD'a Sesini Duyur)", 15.0f);
            }
        }

        private void OnWhistlePressed()
        {
            if (whistleSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(whistleSound, 1.0f);
            }

            // AFAD seslenmişse kurtarılma sekansını başlat
            if (afadCalling && !isRescued)
            {
                StartCoroutine(RescueSequenceRoutine());
            }
            else if (!afadCalling)
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

            yield return new WaitForSeconds(3.5f);

            // Kamerayı yukarı Outtro kamerasına geçir ve yumuşakça süzül
            if (outtroCamera != null)
            {
                if (playerCamera != null)
                {
                    // Kamerayı oyuncu konumundan outtro montaj noktasına yumuşakça süzdür
                    StartCoroutine(GlideCameraToOuttro(playerCamera.transform, outtroCamera.transform));
                }
                else
                {
                    outtroCamera.enabled = true;
                }
            }

            yield return new WaitForSeconds(5.0f);

            // Final Farkındalık Mesajı
            if (fader != null)
            {
                fader.ShowCenterMessage("HAZIRLIK HAYAT KURTARIR",
                    "O gün 'bana bir şey olmaz' demeyenler kurtuldu.\nDoğru bilgi, hazırlık ve sakinlik hayatta tutar.\nFarkındalık afetten önce başlar.",
                    16.0f);
            }
        }

        private IEnumerator GlideCameraToOuttro(Transform fromCam, Transform toCam)
        {
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
    }
}
