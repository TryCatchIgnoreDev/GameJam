using System;
using UnityEngine;

/// <summary>
/// male01_2, male02_2 (veya diğer NPC'ler) için koşma, oturma ve çömelme (Çök-Kapan-Tutun) durum yöneticisi.
/// GameEvents.OnEventRunning ve GameEvents.OnEventCrouching olaylarını dinler.
/// Animator üzerindeki isRunning, isCrouching parametrelerini ve Run / Sit / Crouch trigger'larını yönetir.
/// Waypoint ve hedef rotalama desteği ile NPC'lerin masa/sandalye engellerine takılmadan güvenli noktalara veya kapıya koşmasını sağlar.
/// </summary>
[RequireComponent(typeof(Animator))]
public class NPCRunController : MonoBehaviour
{
    [Header("--- Animasyon Parametreleri ---")]
    [Tooltip("Animator içindeki koşma bool parametre adı.")]
    public string isRunningParam = "isRunning";

    [Tooltip("Animator içindeki koşma trigger adı.")]
    public string runTrigger = "Run";

    [Tooltip("Animator içindeki oturma trigger adı.")]
    public string sitTrigger = "Sit";

    [Tooltip("Animator içindeki çömelme (Crouch / Çök-Kapan) bool parametre adı.")]
    public string isCrouchingParam = "isCrouching";

    [Tooltip("Animator içindeki çömelme trigger adı.")]
    public string crouchTrigger = "Crouch";

    [Tooltip("Animator içindeki ayağa kalkma trigger adı.")]
    public string standTrigger = "Stand";

    [Header("--- Hareket ve Rotalama Ayarları ---")]
    [Tooltip("Koşma başladığında karakter fiziksel / dönüşlü olarak hareket etsin mi?")]
    public bool moveForwardWhenRunning = true;

    [Tooltip("Koşma hızı.")]
    public float runMoveSpeed = 3.8f;

    [Tooltip("Koşarken hedefe dönme hızı.")]
    public float rotationSpeed = 10.0f;

    [Tooltip("Takip edilecek Transform hedefleri / waypoints.")]
    public Transform[] waypoints;

    [Tooltip("Takip edilecek koordinat listesi (Transform yoksa).")]
    public Vector3[] customWaypoints;

    [Tooltip("Koşulacak tekil ana hedef nokta (opsiyonel).")]
    public Transform runTarget;

    [Tooltip("Hedefe ulaşıldığında otomatik olarak Çömelme (Crouch / Çök-Kapan) durumuna geçilsin mi?")]
    public bool crouchOnDestinationReached = false;

    [Tooltip("Çömelme pozisyonuna geçince bakılacak hedef yön veya obje (örn. Sağlam Masa).")]
    public Transform lookTargetOnCrouch;

    private Animator animator;
    private int currentWaypointIndex = 0;

    public bool IsRunning { get; private set; }
    public bool IsCrouching { get; private set; }

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        GameEvents.OnEventRunning += HandleRunningEvent;
        GameEvents.OnEventCrouching += HandleCrouchingEvent;

        // Sahne açıldığında event zaten aktifse durumu eşitle
        if (GameEvents.IsRunningActive)
        {
            StartRunning();
        }
        else if (GameEvents.IsCrouchingActive)
        {
            StartCrouching();
        }
    }

    void OnDisable()
    {
        GameEvents.OnEventRunning -= HandleRunningEvent;
        GameEvents.OnEventCrouching -= HandleCrouchingEvent;
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

    private void HandleCrouchingEvent(bool crouch)
    {
        if (crouch)
        {
            StartCrouching();
        }
        else
        {
            StopCrouching();
        }
    }

    void Update()
    {
        if (!IsRunning || !moveForwardWhenRunning) return;

        Vector3 targetPos;
        bool hasTarget = GetCurrentTargetPosition(out targetPos);

        if (hasTarget)
        {
            // Y eksenini karakterin mevcut yüksekliğinde tutarak yerin altına girmesini engelle
            targetPos.y = transform.position.y;
            Vector3 diff = targetPos - transform.position;
            diff.y = 0f;

            if (diff.sqrMagnitude > 0.05f)
            {
                // Hedefe doğru pürüzsüz dön
                Quaternion targetRot = Quaternion.LookRotation(diff.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

                // Hedefe doğru ilerle
                transform.position = Vector3.MoveTowards(transform.position, targetPos, runMoveSpeed * Time.deltaTime);
            }
            else
            {
                // Mevcut noktaya ulaşıldı, bir sonraki noktaya geç
                currentWaypointIndex++;
                if (!HasMoreWaypoints())
                {
                    // Tüm rotayı tamamladı
                    if (crouchOnDestinationReached)
                    {
                        StartCrouching();

                        if (lookTargetOnCrouch != null)
                        {
                            Vector3 lookDir = (lookTargetOnCrouch.position - transform.position);
                            lookDir.y = 0;
                            if (lookDir.sqrMagnitude > 0.01f)
                            {
                                transform.rotation = Quaternion.LookRotation(lookDir.normalized);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            // Belirlenmiş rota yoksa kendi baktığı yöne doğru düz koş
            transform.Translate(Vector3.forward * (runMoveSpeed * Time.deltaTime), Space.Self);
        }
    }

    private bool GetCurrentTargetPosition(out Vector3 pos)
    {
        if (waypoints != null && waypoints.Length > 0 && currentWaypointIndex < waypoints.Length)
        {
            if (waypoints[currentWaypointIndex] != null)
            {
                pos = waypoints[currentWaypointIndex].position;
                return true;
            }
        }

        if (customWaypoints != null && customWaypoints.Length > 0 && currentWaypointIndex < customWaypoints.Length)
        {
            pos = customWaypoints[currentWaypointIndex];
            return true;
        }

        if (runTarget != null)
        {
            pos = runTarget.position;
            return true;
        }

        pos = Vector3.zero;
        return false;
    }

    private bool HasMoreWaypoints()
    {
        if (waypoints != null && waypoints.Length > 0 && currentWaypointIndex < waypoints.Length) return true;
        if (customWaypoints != null && customWaypoints.Length > 0 && currentWaypointIndex < customWaypoints.Length) return true;
        return false;
    }

    /// <summary>
    /// Karakteri koşma animasyonuna geçirir ve rotasını başlatır.
    /// </summary>
    public void StartRunning()
    {
        IsRunning = true;
        IsCrouching = false;
        currentWaypointIndex = 0;

        if (animator != null)
        {
            animator.SetBool(isCrouchingParam, false);
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
            if (!IsCrouching)
            {
                animator.SetTrigger(sitTrigger);
            }
        }
    }

    /// <summary>
    /// Karakteri çömelme (Crouch / Çök-Kapan) animasyonuna geçirir.
    /// </summary>
    public void StartCrouching()
    {
        IsCrouching = true;
        IsRunning = false;

        if (animator != null)
        {
            animator.SetBool(isRunningParam, false);
            animator.SetBool(isCrouchingParam, true);
            animator.SetTrigger(crouchTrigger);
        }
    }

    /// <summary>
    /// Çömelme durumunu sonlandırır ve oturma durumuna geçer.
    /// </summary>
    public void StopCrouching()
    {
        IsCrouching = false;
        if (animator != null)
        {
            animator.SetBool(isCrouchingParam, false);
            animator.SetTrigger(sitTrigger);
        }
    }

    public void SetCustomWaypoints(Vector3[] points, bool crouchAtEnd = false, Transform lookAtTarget = null)
    {
        customWaypoints = points;
        currentWaypointIndex = 0;
        crouchOnDestinationReached = crouchAtEnd;
        lookTargetOnCrouch = lookAtTarget;
    }

    public void ToggleRunning()
    {
        if (IsRunning) StopRunning();
        else StartRunning();
    }

    public void ToggleCrouching()
    {
        if (IsCrouching) StopCrouching();
        else StartCrouching();
    }

    #region Context Menüleri
    [ContextMenu("Koşmayı Başlat (Run)")]
    public void ContextStartRunning() => GameEvents.TriggerRunning(true);

    [ContextMenu("Oturmaya Dön (Sit)")]
    public void ContextStopRunning() => GameEvents.TriggerRunning(false);

    [ContextMenu("Çömelmeyi Başlat (Crouch)")]
    public void ContextStartCrouch() => GameEvents.TriggerCrouching(true);

    [ContextMenu("Çömelmeyi Bitir (Stand/Sit)")]
    public void ContextStopCrouch() => GameEvents.TriggerCrouching(false);
    #endregion
}
