using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Merkezi Sahne, Play ve Edit Mod Geçiş Koordinatörü (SceneCoordinator).
/// Projedeki tüm sahne geçişlerini, Play/Edit modu durum değişikliklerini ve editör sahne yüklemelerini
/// tek merkezden denetler ve yönetir.
/// 
/// KURAL:
/// "Sahne değiştirmek, oyunu başlatmak/durdurmak veya editörde sahne açmak isteyen tüm sistemler
/// SceneCoordinator üzerinden istek atmak zorundadır."
/// 
/// Özellikler:
/// - Singleton ve DontDestroyOnLoad kalıcı mimari.
/// - [RuntimeInitializeOnLoadMethod] ile sahneler başlamadan otomatik ayağa kalkar.
/// - ScreenFaderUI ile tam entegre: Yumuşak kararma (Fade to Black), asenkron yükleme (LoadSceneAsync)
///   ve yumuşak açılma (Fade from Black).
/// - Race condition ve mükerrer tıklama koruması (isTransitioning kilidi).
/// - Play & Stop Mode istekleri: RequestPlayMode(), RequestStopPlayMode(), RequestTogglePlayMode().
/// - Edit Mode sahne açma & kaydetme: RequestOpenSceneInEditor(...), RequestSaveActiveSceneInEditor().
/// - Olaylar / Delegeler: OnBeforeSceneChange, OnSceneChanged, OnPlayModeChanged, OnEditorSceneOpened.
/// </summary>
public class SceneCoordinator : MonoBehaviour
{
    private static SceneCoordinator _instance;

    public static SceneCoordinator Instance
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
    /// Runtime sahneleri başlamadan önce koordinatörü otomatik oluşturur.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        EnsureInstance();
    }

    /// <summary>
    /// Singleton instance'ın var olduğundan emin olur, yoksa oluşturur.
    /// </summary>
    public static SceneCoordinator EnsureInstance()
    {
        if (_instance == null)
        {
            _instance = FindAnyObjectByType<SceneCoordinator>();
            if (_instance == null)
            {
                GameObject go = new GameObject("[SceneCoordinator]");
                _instance = go.AddComponent<SceneCoordinator>();
                DontDestroyOnLoad(go);
            }
        }
        return _instance;
    }

    // --- Durum Özellikleri ---
    private bool _isTransitioning;
    private float _transitionStartTime;

    /// <summary>
    /// Bir geçiş için tanınan azami süre (sn). Aşılırsa takılı kalan kilit zorla bırakılır.
    /// </summary>
    private const float MaxTransitionDuration = 20f;

    public bool IsTransitioning => _isTransitioning;

    public string CurrentTargetScene { get; private set; } = string.Empty;
    public float TransitionProgress { get; private set; } = 0f;
    public string ActiveSceneName => SceneManager.GetActiveScene().name;

    // --- Olaylar / Delegeler (Instance) ---
    public event Action<string> OnBeforeSceneChange;
    public event Action<string> OnSceneChanged;
    public event Action<bool> OnPlayModeChanged; // true = enter play mode, false = exit play mode
    public event Action<string> OnEditorSceneOpened;

    // --- Olaylar / Delegeler (Global Static Kolay Erişim) ---
    public static event Action<string> BeforeSceneChange;
    public static event Action<string> SceneChanged;
    public static event Action<bool> PlayModeChanged;
    public static event Action<string> EditorSceneOpened;

    private Coroutine activeTransitionRoutine;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    #region Runtime Sahne Geçiş API'si

    /// <summary>
    /// Merkezi sahne değiştirme isteği atar.
    /// Ekranı ScreenFaderUI ile yumuşakça karartır, yeni sahneyi asenkron yükler ve ekranı aydınlatır.
    /// </summary>
    /// <param name="sceneName">Hedef sahnenin adı</param>
    /// <param name="fadeDuration">Kararma ve açılma süresi (sn)</param>
    /// <param name="onComplete">Geçiş tamamlandığında çağrılacak opsiyonel delege</param>
    /// <returns>İstek kabul edildiyse true, kilitliyse (çift tıklama/geçiş sürüyorsa) false döner.</returns>
    public bool RequestSceneChange(string sceneName, float fadeDuration = 1.0f, Action onComplete = null)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneCoordinator] Sahne geçiş isteği başarısız: Sahne adı boş veya null!");
            return false;
        }

        if (_isTransitioning)
        {
            // Takılı kalma koruması: geçiş beklenenden uzun sürdüyse kilidi zorla bırak, yeni isteği kabul et.
            if (Time.realtimeSinceStartup - _transitionStartTime > MaxTransitionDuration)
            {
                Debug.LogWarning($"[SceneCoordinator] Geçiş {MaxTransitionDuration:F0}s'den uzun sürdü (takılı kilit). Kilit zorla bırakılıyor, yeni istek kabul edildi: '{sceneName}'.");
                if (activeTransitionRoutine != null)
                {
                    StopCoroutine(activeTransitionRoutine);
                    activeTransitionRoutine = null;
                }
                _isTransitioning = false;
            }
            else
            {
                Debug.LogWarning($"[SceneCoordinator] Sahne geçişi devam ederken yeni bir istek alındı ('{sceneName}'). İstek engellendi (Race condition koruması).");
                return false;
            }
        }

        _isTransitioning = true;
        _transitionStartTime = Time.realtimeSinceStartup;
        CurrentTargetScene = sceneName;
        TransitionProgress = 0f;

        activeTransitionRoutine = StartCoroutine(TransitionRoutine(sceneName, fadeDuration, onComplete));
        return true;
    }

    /// <summary>
    /// Mevcut aktif sahneyi yeniden yükler (Örn. Oyun döngüsü / Game Over anları).
    /// </summary>
    public bool ReloadCurrentScene(float fadeDuration = 1.0f, Action onComplete = null)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        return RequestSceneChange(currentScene, fadeDuration, onComplete);
    }

    /// <summary>
    /// Statik hızlı çağrı metodu.
    /// </summary>
    public static bool Request(string sceneName, float fadeDuration = 1.0f, Action onComplete = null)
    {
        return Instance.RequestSceneChange(sceneName, fadeDuration, onComplete);
    }

    /// <summary>
    /// Statik sahneyi yeniden yükleme metodu.
    /// </summary>
    public static bool Reload(float fadeDuration = 1.0f, Action onComplete = null)
    {
        return Instance.ReloadCurrentScene(fadeDuration, onComplete);
    }

    #endregion

    #region Play Mode & Edit Mode Kontrol API'si

    /// <summary>
    /// Unity Editor Play moduna geçiş isteğinde bulunur.
    /// Editörde çalışıyorsa EditorApplication.isPlaying = true yapar.
    /// </summary>
    public static bool RequestPlayMode()
    {
        Debug.Log("[SceneCoordinator] Play Mode başlatma isteği onaylandı.");
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
        {
            Instance.NotifyPlayModeChanged(true);
            EditorApplication.isPlaying = true;
            return true;
        }
        Debug.LogWarning("[SceneCoordinator] Editor zaten Play modunda!");
        return false;
#else
        Debug.LogWarning("[SceneCoordinator] RequestPlayMode sadece Unity Editor ortamında geçerlidir.");
        return false;
#endif
    }

    /// <summary>
    /// Unity Editor Play modundan çıkış (Edit moduna dönüş) isteğinde bulunur.
    /// </summary>
    public static bool RequestStopPlayMode()
    {
        Debug.Log("[SceneCoordinator] Play Mode durdurma isteği onaylandı.");
#if UNITY_EDITOR
        if (EditorApplication.isPlaying)
        {
            Instance.NotifyPlayModeChanged(false);
            EditorApplication.isPlaying = false;
            return true;
        }
        Debug.LogWarning("[SceneCoordinator] Editor zaten Edit modunda (Play modunda değil)!");
        return false;
#else
        Debug.LogWarning("[SceneCoordinator] RequestStopPlayMode sadece Unity Editor ortamında geçerlidir.");
        Application.Quit();
        return true;
#endif
    }

    /// <summary>
    /// Play modu durumunu tersine çevirir (Play <-> Stop).
    /// </summary>
    public static bool RequestTogglePlayMode()
    {
#if UNITY_EDITOR
        if (EditorApplication.isPlaying)
            return RequestStopPlayMode();
        else
            return RequestPlayMode();
#else
        return false;
#endif
    }

    /// <summary>
    /// Editor'da sahne açma isteğinde bulunur. Açık olan sahneyi kaydeder ve yeni sahneyi açar.
    /// </summary>
    /// <param name="scenePath">Açılacak sahnenin yolu (Örn: "Assets/Scenes/EvMarket.unity" veya sahne adı)</param>
    /// <param name="saveCurrentModifiedScenes">Mevcut değiştirilmiş sahnelerin kaydedilmesini isteyip istemediği</param>
    public static bool RequestOpenSceneInEditor(string scenePath, bool saveCurrentModifiedScenes = true)
    {
#if UNITY_EDITOR
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[SceneCoordinator] Editor Play modundayken Edit-modu sahne açma işlemi yapılamaz! Önce RequestStopPlayMode() çağrılmalı.");
            return false;
        }

        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogError("[SceneCoordinator] Açılacak sahne yolu belirtilmedi!");
            return false;
        }

        // Eğer sadece sahne adı verildiyse "Assets/Scenes/<Name>.unity" olarak tamamla
        if (!scenePath.EndsWith(".unity"))
        {
            scenePath = $"Assets/Scenes/{scenePath}.unity";
        }

        if (saveCurrentModifiedScenes)
        {
            bool saveOk = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            if (!saveOk)
            {
                Debug.LogWarning("[SceneCoordinator] Kullanıcı veya sistem mevcut sahneyi kaydetmeyi reddetti. Sahne açma iptal edildi.");
                return false;
            }
        }

        Debug.Log($"[SceneCoordinator] Editörde sahne açılıyor: '{scenePath}'");
        try
        {
            var openedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (openedScene.IsValid())
            {
                Instance.NotifyEditorSceneOpened(scenePath);
                Debug.Log($"[SceneCoordinator] Sahne editörde başarıyla açıldı: '{scenePath}'");
                return true;
            }
            else
            {
                Debug.LogError($"[SceneCoordinator] Sahne açılamadı: '{scenePath}'");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SceneCoordinator] Sahne açılırken hata: {ex.Message}");
            return false;
        }
#else
        Debug.LogWarning("[SceneCoordinator] RequestOpenSceneInEditor sadece Unity Editor ortamında kullanılabilir.");
        return false;
#endif
    }

    /// <summary>
    /// Editörde aktif sahneyi kaydeder.
    /// </summary>
    public static bool RequestSaveActiveSceneInEditor()
    {
#if UNITY_EDITOR
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            bool success = EditorSceneManager.SaveScene(activeScene);
            Debug.Log($"[SceneCoordinator] Aktif sahne kaydedildi: {activeScene.path} (Sonuç: {success})");
            return success;
        }
        return false;
#else
        return false;
#endif
    }

    private void NotifyPlayModeChanged(bool isEnteringPlay)
    {
        try
        {
            OnPlayModeChanged?.Invoke(isEnteringPlay);
            PlayModeChanged?.Invoke(isEnteringPlay);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SceneCoordinator] OnPlayModeChanged bildirim hatası: {ex}");
        }
    }

    private void NotifyEditorSceneOpened(string path)
    {
        try
        {
            OnEditorSceneOpened?.Invoke(path);
            EditorSceneOpened?.Invoke(path);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SceneCoordinator] OnEditorSceneOpened bildirim hatası: {ex}");
        }
    }

    #endregion

    #region Asenkron Geçiş Rutini

    private IEnumerator TransitionRoutine(string sceneName, float fadeDuration, Action onComplete)
    {
        Debug.Log($"[SceneCoordinator] Sahne geçiş süreci başladı -> Hedef: '{sceneName}', Fade Süresi: {fadeDuration:F2}s");

        try
        {
            // 1. Geçiş Başlangıç Event'ini Tetikle
            try
            {
                OnBeforeSceneChange?.Invoke(sceneName);
                BeforeSceneChange?.Invoke(sceneName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneCoordinator] OnBeforeSceneChange listener hatası: {ex}");
            }

            ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();

            // 2. Ekranı Yumuşakça Karart (Fade to Black)
            if (fader != null && fadeDuration > 0.001f)
            {
                // Eğer ekran zaten önceden karartılmışsa ek bekleme yapma
                if (!fader.IsFadedToBlack)
                {
                    bool fadeComplete = false;
                    fader.FadeToBlack(fadeDuration, () => fadeComplete = true);

                    // Fade rutini dışarıdan iptal edilse bile takılı kalmamak için süre sınırıyla bekle.
                    float fadeWaited = 0f;
                    while (!fadeComplete && fadeWaited < fadeDuration + 0.5f)
                    {
                        fadeWaited += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
                else
                {
                    fader.SetBlackImmediate(true);
                }
            }
            else if (fader != null)
            {
                fader.SetBlackImmediate(true);
            }

            // 3. Sahne Uyumluluk Kontrolü
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"[SceneCoordinator] DİKKAT: '{sceneName}' sahnesi Build Settings (EditorBuildSettings) listesinde bulunamadı! Yine de yüklenmeye çalışılıyor...");
            }

            // 4. Asenkron Sahne Yükleme (LoadSceneAsync)
            AsyncOperation asyncOp = null;
            try
            {
                asyncOp = SceneManager.LoadSceneAsync(sceneName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneCoordinator] '{sceneName}' sahnesi LoadSceneAsync ile yüklenirken kritik hata: {ex.Message}");
            }

            if (asyncOp == null)
            {
                Debug.LogError($"[SceneCoordinator] '{sceneName}' sahnesi için AsyncOperation oluşturulamadı! Geçiş iptal ediliyor.");
                if (fader != null && fadeDuration > 0.001f)
                {
                    fader.FadeFromBlack(fadeDuration);
                }
                yield break;
            }

            // Yükleme ilerlemesini takip et
            while (!asyncOp.isDone)
            {
                TransitionProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);
                yield return null;
            }

            TransitionProgress = 1f;

            // Sahne nesnelerinin Awake/Start metotlarının çalışması için 1 frame bekle
            yield return null;

            Debug.Log($"[SceneCoordinator] Sahne yüklendi: '{sceneName}'. Ekran açılıyor...");

            // 5. Yeni Sahne Yüklendi Event'ini Tetikle
            try
            {
                OnSceneChanged?.Invoke(sceneName);
                SceneChanged?.Invoke(sceneName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneCoordinator] OnSceneChanged listener hatası: {ex}");
            }

            // 6. Ekranı Yumuşakça Aç (Fade from Black)
            if (fader != null && fadeDuration > 0.001f)
            {
                bool unfadeComplete = false;
                fader.FadeFromBlack(fadeDuration, () => unfadeComplete = true);

                // Yeni sahne başlarken perdeyi anında açan sistemler (SetBlackImmediate) fade callback'ini
                // iptal edebildiği için burada süre sınırıyla bekle; kilit asla takılı kalmasın.
                float unfadeWaited = 0f;
                while (!unfadeComplete && unfadeWaited < fadeDuration + 1f)
                {
                    unfadeWaited += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            else if (fader != null)
            {
                fader.SetBlackImmediate(false);
            }

            // 7. OnComplete Callback Çağrısı
            try
            {
                onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneCoordinator] onComplete callback yürütülürken hata oluştu: {ex}");
            }

            Debug.Log($"[SceneCoordinator] Sahne geçişi tamamen tamamlandı: '{sceneName}'.");
        }
        finally
        {
            // 8. Kilidi HER DURUMDA Aç (geçiş iptal edilse ya da takılsa bile kilit kalmasın).
            _isTransitioning = false;
            CurrentTargetScene = string.Empty;
            activeTransitionRoutine = null;
        }
    }

    #endregion
}
