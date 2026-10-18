using System.Collections;
using UnityEngine;

/// <summary>
/// Camera.main üzerinde veya sahnede dinamik olarak çalışan Kamera Efektleri Yöneticisi.
/// Deprem sarsıntısı, flashback kamera daralması/nabzı ve final sinematik outtro kamerasını yönetir.
/// </summary>
public class CameraEffectsController : MonoBehaviour
{
    private static CameraEffectsController _instance;

    public static CameraEffectsController Instance
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
    /// Singleton örneğini döndürür; sahnede yoksa Camera.main üzerine veya yeni objeye ekler.
    /// </summary>
    public static CameraEffectsController EnsureInstance()
    {
        if (_instance != null) return _instance;

        _instance = FindFirstObjectByType<CameraEffectsController>();
        if (_instance != null) return _instance;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            _instance = mainCam.GetComponent<CameraEffectsController>();
            if (_instance == null)
            {
                _instance = mainCam.gameObject.AddComponent<CameraEffectsController>();
            }
            return _instance;
        }

        Camera anyCam = FindFirstObjectByType<Camera>();
        if (anyCam != null)
        {
            _instance = anyCam.gameObject.AddComponent<CameraEffectsController>();
            return _instance;
        }

        GameObject go = new GameObject("CameraEffectsController");
        _instance = go.AddComponent<CameraEffectsController>();
        return _instance;
    }

    [Header("--- Referanslar ---")]
    [Tooltip("Efekt uygulanacak kamera. Boş bırakılırsa bu objedeki veya Camera.main seçilir.")]
    public Camera targetCamera;

    [Header("--- Sarsıntı Ayarları ---")]
    [Tooltip("Deprem sarsıntısının temel hızı.")]
    public float shakeFrequency = 24f;

    // Sarsıntı durumu
    private bool isEarthquakeShaking = false;
    private float earthquakeIntensity = 0.12f;

    private bool isPulseShaking = false;
    private float pulseIntensity = 0f;

    private Vector3 lastPosOffset = Vector3.zero;
    private Quaternion lastRotOffset = Quaternion.identity;

    private Coroutine pulseCoroutine = null;
    private Coroutine outtroCoroutine = null;
    private bool isOuttroActive = false;

    private float baseFOV = 60f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }
        _instance = this;

        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) targetCamera = FindFirstObjectByType<Camera>();
        }

        if (targetCamera != null)
        {
            baseFOV = targetCamera.fieldOfView;
        }
    }

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (targetCamera != null)
            {
                baseFOV = targetCamera.fieldOfView;
            }
        }
    }

    private void LateUpdate()
    {
        if (isOuttroActive) return;

        // Önceki karede uygulanan geçici ofsetleri geri al
        if (lastPosOffset != Vector3.zero || lastRotOffset != Quaternion.identity)
        {
            transform.localPosition -= lastPosOffset;
            transform.localRotation = transform.localRotation * Quaternion.Inverse(lastRotOffset);
            lastPosOffset = Vector3.zero;
            lastRotOffset = Quaternion.identity;
        }

        float totalIntensity = 0f;
        if (isEarthquakeShaking) totalIntensity += earthquakeIntensity;
        if (isPulseShaking) totalIntensity += pulseIntensity;

        if (totalIntensity > 0.001f)
        {
            float time = Time.time * shakeFrequency;

            // Perlin Noise tabanlı organik sarsıntı
            Vector3 posOffset = new Vector3(
                (Mathf.PerlinNoise(time, 0.123f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(0.456f, time) - 0.5f) * 2f,
                (Mathf.PerlinNoise(time * 0.7f, time * 0.7f) - 0.5f) * 0.6f
            ) * totalIntensity;

            Vector3 rotOffsetEuler = new Vector3(
                (Mathf.PerlinNoise(time * 0.85f, 15.3f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(23.7f, time * 0.85f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(7.1f, time * 0.6f) - 0.5f) * 3f
            ) * (totalIntensity * 18f);

            lastPosOffset = posOffset;
            lastRotOffset = Quaternion.Euler(rotOffsetEuler);

            transform.localPosition += lastPosOffset;
            transform.localRotation = transform.localRotation * lastRotOffset;
        }
    }

    /// <summary>
    /// Deprem kamera sarsıntısını başlatır.
    /// </summary>
    public void StartEarthquakeShake(float intensity = 0.12f)
    {
        earthquakeIntensity = intensity;
        isEarthquakeShaking = true;
    }

    /// <summary>
    /// Deprem kamera sarsıntısını durdurur.
    /// </summary>
    public void StopEarthquakeShake()
    {
        isEarthquakeShaking = false;
        earthquakeIntensity = 0f;

        if (lastPosOffset != Vector3.zero || lastRotOffset != Quaternion.identity)
        {
            transform.localPosition -= lastPosOffset;
            transform.localRotation = transform.localRotation * Quaternion.Inverse(lastRotOffset);
            lastPosOffset = Vector3.zero;
            lastRotOffset = Quaternion.identity;
        }
    }

    /// <summary>
    /// Postere ilk bakışta veya ani şok anlarında FOV daralıp açılan nabız & sarsıntı efekti.
    /// </summary>
    public void PlayFlashbackPulse(float duration = 0.6f)
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        pulseCoroutine = StartCoroutine(FlashbackPulseRoutine(duration));
    }

    private IEnumerator FlashbackPulseRoutine(float duration)
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        }

        float originalFov = targetCamera != null ? targetCamera.fieldOfView : baseFOV;
        float targetFov = Mathf.Max(25f, originalFov - 14f);

        isPulseShaking = true;
        pulseIntensity = 0.16f;

        // Ekran fader sisteminde glitch parlaması tetikle
        ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();
        if (fader != null)
        {
            fader.FlashGlitch(duration * 0.5f);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Sinusoidal daralıp genişleme eğrisi (0 -> 1 -> 0)
            float pulseProgress = Mathf.Sin(t * Mathf.PI);

            if (targetCamera != null)
            {
                targetCamera.fieldOfView = Mathf.Lerp(originalFov, targetFov, pulseProgress);
            }

            pulseIntensity = Mathf.Lerp(0.18f, 0f, t);

            yield return null;
        }

        if (targetCamera != null)
        {
            targetCamera.fieldOfView = originalFov;
        }

        isPulseShaking = false;
        pulseIntensity = 0f;
        pulseCoroutine = null;
    }

    #region --- Poster Odaklanma & Zoom Kadrajı (Cinematic Zoom) ---

    private bool isPosterFocusActive = false;
    private Vector3 preZoomCamPos = Vector3.zero;
    private Quaternion preZoomCamRot = Quaternion.identity;
    private float preZoomFov = 60f;

    /// <summary>
    /// Postere doğru yumuşak, temiz bir yakınlaştırma (smooth zoom-in FOV ve kamera hareketi) başlatır.
    /// focusOnBottomRight false ise postere genel yumuşak odaklanma (1. bakış).
    /// focusOnBottomRight true ise posterin sağ altındaki "O gün binlerce insan bana bir şey olmaz diyerek uyudu." yazısına ekstra odaklanan detay kadrajı (2. bakış).
    /// Sarsıntılar (shake) tamamen kapatılır.
    /// </summary>
    public IEnumerator FocusOnPosterRoutine(Transform posterTransform, bool focusOnBottomRight, float duration = 0f)
    {
        // 1. Ekran sarsıntılarını tamamen durdur ve sıfırla
        StopEarthquakeShake();
        isPulseShaking = false;
        pulseIntensity = 0f;

        if (targetCamera == null)
        {
            targetCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        }
        if (targetCamera == null || posterTransform == null) yield break;

        isPosterFocusActive = true;

        // 2. Oyuncu hareket ve bakış kontrollerini geçici olarak durdur
        FirstPersonMovement fpm = FindFirstObjectByType<FirstPersonMovement>();
        if (fpm != null) fpm.enabled = false;

        FirstPersonLook fpl = FindFirstObjectByType<FirstPersonLook>();
        if (fpl != null) fpl.enabled = false;

        Transform camTr = targetCamera.transform;
        preZoomCamPos = camTr.position;
        preZoomCamRot = camTr.rotation;
        preZoomFov = targetCamera.fieldOfView > 10f ? targetCamera.fieldOfView : baseFOV;

        // 3. Hedef kadraj noktasını ve hedef FOV'yi hesapla
        Vector3 posterCenter = posterTransform.position;
        Vector3 toCam = preZoomCamPos - posterCenter;
        float distToPoster = toCam.magnitude;
        Vector3 toCamNorm = distToPoster > 0.001f ? toCam.normalized : posterTransform.forward;

        // İzleyici bakış açısına göre sağ ve alt yönler
        Vector3 viewerRight = Vector3.Cross(toCamNorm, Vector3.up).normalized;
        Vector3 viewerDown = Vector3.down;

        Vector3 targetPoint;
        float targetFov;
        float forwardNudge;

        if (focusOnBottomRight)
        {
            // 2. BAKIŞ: Posterin sağ altındaki "O gün binlerce insan..." yazısına ekstra odaklanan zoom/detay kadrajı
            // Standart poster boyutu ~1.4m x 0.7m
            targetPoint = posterCenter + (viewerRight * 0.38f) + (viewerDown * 0.18f);
            targetFov = 18f; // Ekstra odaklanan dar açı
            forwardNudge = Mathf.Clamp(distToPoster * 0.28f, 0.25f, 0.55f);
        }
        else
        {
            // 1. BAKIŞ: Postere genel yumuşak, temiz yakınlaştırma
            targetPoint = posterCenter;
            targetFov = 36f; // Temiz poster odak açısı
            forwardNudge = Mathf.Clamp(distToPoster * 0.20f, 0.15f, 0.40f);
        }

        Vector3 targetCamPos = preZoomCamPos - (toCamNorm * forwardNudge);
        Quaternion targetCamRot = Quaternion.LookRotation((targetPoint - targetCamPos).normalized, Vector3.up);

        // 4. Yumuşak geçişle kamerayı ve FOV'yi hedefe kaydır (0.85 sn)
        float zoomInTime = 0.85f;
        float elapsed = 0f;
        while (elapsed < zoomInTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / zoomInTime);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            camTr.position = Vector3.Lerp(preZoomCamPos, targetCamPos, smoothT);
            camTr.rotation = Quaternion.Slerp(preZoomCamRot, targetCamRot, smoothT);
            targetCamera.fieldOfView = Mathf.Lerp(preZoomFov, targetFov, smoothT);

            yield return null;
        }

        camTr.position = targetCamPos;
        camTr.rotation = targetCamRot;
        targetCamera.fieldOfView = targetFov;

        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }
    }

    /// <summary>
    /// Postere odaklanan kamerayı yumuşakça orijinal pozisyona, açıya ve FOV'ye döndürür.
    /// </summary>
    public IEnumerator ReturnFromPosterFocusRoutine(float returnDuration = 0.8f)
    {
        if (!isPosterFocusActive || targetCamera == null) yield break;

        Transform camTr = targetCamera.transform;
        Vector3 startPos = camTr.position;
        Quaternion startRot = camTr.rotation;
        float startFov = targetCamera.fieldOfView;

        float elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / returnDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            camTr.position = Vector3.Lerp(startPos, preZoomCamPos, smoothT);
            camTr.rotation = Quaternion.Slerp(startRot, preZoomCamRot, smoothT);
            targetCamera.fieldOfView = Mathf.Lerp(startFov, preZoomFov, smoothT);

            yield return null;
        }

        camTr.position = preZoomCamPos;
        camTr.rotation = preZoomCamRot;
        targetCamera.fieldOfView = preZoomFov;

        // Oyuncu kontrollerini yeniden aç ve mouse rotasyonunu sıçrama yapmayacak şekilde senkronize et
        FirstPersonLook fpl = FindFirstObjectByType<FirstPersonLook>();
        if (fpl != null)
        {
            fpl.SyncRotation(preZoomCamRot);
            fpl.enabled = true;
        }

        FirstPersonMovement fpm = FindFirstObjectByType<FirstPersonMovement>();
        if (fpm != null)
        {
            fpm.enabled = true;
        }

        isPosterFocusActive = false;
    }

    #endregion


    /// <summary>
    /// Final sahnede oyuncu kontrolünü kapatıp kamerayı yukarıya/dışarıya yumuşakça kaydırır.
    /// </summary>
    public void PlayFinalOuttroCamera(Transform targetViewPoint = null, float duration = 8f)
    {
        if (outtroCoroutine != null)
        {
            StopCoroutine(outtroCoroutine);
        }

        // Sarsıntıları temizle
        StopEarthquakeShake();
        isPulseShaking = false;

        outtroCoroutine = StartCoroutine(FinalOuttroRoutine(targetViewPoint, duration));
    }

    private IEnumerator FinalOuttroRoutine(Transform targetViewPoint, float duration)
    {
        isOuttroActive = true;

        // 1. Oyuncu hareket ve bakış kontrolcülerini devre dışı bırak
        FirstPersonMovement movement = FindFirstObjectByType<FirstPersonMovement>();
        if (movement != null) movement.enabled = false;

        FirstPersonLook look = FindFirstObjectByType<FirstPersonLook>();
        if (look != null) look.enabled = false;

        // 2. Kamera oyuncunun alt objesiyse fizik ve hareketlerden bağımsız kılmak için unparent et
        Transform camTransform = targetCamera != null ? targetCamera.transform : transform;
        camTransform.SetParent(null);

        Vector3 startPos = camTransform.position;
        Quaternion startRot = camTransform.rotation;

        Vector3 endPos;
        Quaternion endRot;

        if (targetViewPoint != null)
        {
            endPos = targetViewPoint.position;
            endRot = targetViewPoint.rotation;
        }
        else
        {
            // Belirlenen hedef nokta yoksa mevcut konumdan 18 metre yukarıya ve 25 metre geriye açıl
            endPos = startPos + new Vector3(0f, 18f, -25f);
            Vector3 lookTarget = startPos + Vector3.up * 1.5f;
            endRot = Quaternion.LookRotation(lookTarget - endPos);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            camTransform.position = Vector3.Lerp(startPos, endPos, smoothT);
            camTransform.rotation = Quaternion.Slerp(startRot, endRot, smoothT);

            yield return null;
        }

        camTransform.position = endPos;
        camTransform.rotation = endRot;
    }

    private void OnDisable()
    {
        if (lastPosOffset != Vector3.zero || lastRotOffset != Quaternion.identity)
        {
            transform.localPosition -= lastPosOffset;
            transform.localRotation = transform.localRotation * Quaternion.Inverse(lastRotOffset);
            lastPosOffset = Vector3.zero;
            lastRotOffset = Quaternion.identity;
        }
    }
}
