using UnityEngine;

/// <summary>
/// male02_2 (veya diğer NPC'ler) için koşma ve oturma animasyonu durum yöneticisi.
/// GameEvents.OnEventRunning olayını dinler.
/// Event tetiklendiğinde Animator üzerindeki "isRunning" parametresini ve "Run" / "Sit" trigger'larını yönetir.
/// </summary>
[RequireComponent(typeof(Animator))]
public class NPCRunController : MonoBehaviour
{
    [Header("--- Animasyon Ayarları ---")]
    [Tooltip("Animator içindeki bool parametre adı.")]
    public string isRunningParam = "isRunning";

    [Tooltip("Animator içindeki koşma trigger adı.")]
    public string runTrigger = "Run";

    [Tooltip("Animator içindeki oturma trigger adı.")]
    public string sitTrigger = "Sit";

    [Header("--- Hareket Ayarları (Opsiyonel) ---")]
    [Tooltip("Koşma başladığında karakter ileri doğru fiziksel olarak da hareket etsin mi?")]
    public bool moveForwardWhenRunning = false;

    [Tooltip("İleri hareket hızı.")]
    public float runMoveSpeed = 3.5f;

    private Animator animator;
    public bool IsRunning { get; private set; }

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        GameEvents.OnEventRunning += HandleRunningEvent;

        // Sahne açıldığında event zaten aktifse durumu eşitle
        if (GameEvents.IsRunningActive)
        {
            StartRunning();
        }
    }

    void OnDisable()
    {
        GameEvents.OnEventRunning -= HandleRunningEvent;
    }

    private void HandleRunningEvent(bool run)
    {
        if (run)
        {
            StartRunning();
        }
        else
        {
            StopRunning();
        }
    }

    void Update()
    {
        if (IsRunning && moveForwardWhenRunning)
        {
            transform.Translate(Vector3.forward * (runMoveSpeed * Time.deltaTime));
        }
    }

    /// <summary>
    /// Karakteri koşma animasyonuna geçirir.
    /// </summary>
    public void StartRunning()
    {
        IsRunning = true;
        if (animator != null)
        {
            animator.SetBool(isRunningParam, true);
            animator.SetTrigger(runTrigger);
        }
    }

    /// <summary>
    /// Karakteri oturma (Sitting Idle) animasyonuna geri döndürür.
    /// </summary>
    public void StopRunning()
    {
        IsRunning = false;
        if (animator != null)
        {
            animator.SetBool(isRunningParam, false);
            animator.SetTrigger(sitTrigger);
        }
    }

    public void ToggleRunning()
    {
        if (IsRunning) StopRunning();
        else StartRunning();
    }

    #region Context Menüleri
    [ContextMenu("Koşmayı Başlat (Run)")]
    public void ContextStartRunning() => GameEvents.TriggerRunning(true);

    [ContextMenu("Oturmaya Dön (Sit)")]
    public void ContextStopRunning() => GameEvents.TriggerRunning(false);
    #endregion
}
