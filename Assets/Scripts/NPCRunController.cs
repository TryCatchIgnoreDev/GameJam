using System;
using UnityEngine;

/// <summary>
/// male01_2, male02_2 (veya diğer NPC'ler) için koşma, oturma, çömelme (Çök-Kapan-Tutun)
/// ve bina çöküşü anında fiziksel savrulma / düşme yöneticisi.
/// GameEvents.OnEventRunning, GameEvents.OnEventCrouching ve GameEvents.OnEvent2_AssignRigidbodies olaylarını dinler.
/// Animator üzerindeki isRunning, isCrouching parametrelerini ve Run / Sit / Crouch trigger'larını yönetir.
/// 2 adet belirgin kaçış keyframe'i (waypoint) üzerinden sırasıyla hedeflere yönelip koşmayı sağlar.
/// Yıkılma (Event 2) anında Animator'ı devre dışı bırakıp Rigidbody ve Collider ekleyerek
/// karakterin fiziksel olarak yere savrulup düşmesini sağlar.
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
    public float rotationSpeed = 12.0f;

    [Tooltip("Bir keyframe / waypoint noktasına ulaşıldı sayılma mesafesi (metre).")]
    public float waypointReachDistance = 0.35f;

    [Header("--- Rastgele Kaçış Koşusu (XZ Düzlemi) ---")]
    [Tooltip("Koşarken waypoint takibi yerine XZ düzleminde rastgele yönlere koş. (Panik kaçışı)")]
    public bool randomWanderOnRun = true;

    [Tooltip("Kaç saniyede bir yeni rastgele yön seçileceği.")]
    public float randomTurnInterval = 0.5f;

    [Tooltip("Rastgele koşarken başlangıç noktasından en fazla kaç metre uzaklaşılabileceği (0 = sınırsız).")]
    public float randomWanderRadius = 6f;

    [Header("--- 2 Belirgin Kaçış Keyframe Hedefi ---")]
    [Tooltip("1. Kaçış Keyframe Hedefi (İlk bakılacak ve koşulacak ara nokta, örn. masa önü açıklığı)")]
    public Vector3 keyframe1;

    [Tooltip("2. Kaçış Keyframe Hedefi (İkinci bakılacak ve koşulacak ana hedef, örn. dış kapı / koridor)")]
    public Vector3 keyframe2;

    [Tooltip("Belirlenmiş 2 keyframe aktif olarak kullanılsın mı?")]
    public bool useKeyframes = false;

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

    [Header("--- Yıkılma / Çökme Fiziği Ayarları (Event 2) ---")]
    [Tooltip("Event 2 (Yıkılma / Rigidbody Atama) tetiklendiğinde otomatik olarak fizik aktif olsun mu?")]
    public bool enableCollapsePhysicsOnEvent2 = true;

    [Tooltip("Fiziksel gövde kütlesi (kg).")]
    public float ragdollMass = 70.0f;

    [Tooltip("Çökme anında uygulanacak savrulma / itme kuvveti çarpanı.")]
    public float collapseImpulse = 4.0f;

    [Tooltip("Çökme anında uygulanacak rastgele dönme torku çarpanı.")]
    public float collapseTorque = 15.0f;

    private Animator animator;
    private int currentWaypointIndex = 0;
    private bool hasCollapsePhysicsEnabled = false;
    private float randomTurnTimer = 0f;
    private float randomTargetYaw = 0f;
    private Vector3 randomWanderOrigin = Vector3.zero;

    public bool IsRunning { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool HasCollapsePhysicsEnabled => hasCollapsePhysicsEnabled;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Yerinde saymayı engellemek için Animator Root Motion'ı devre dışı bırak
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // Koşma hareket bayrağının varsayılan olarak açık olduğundan emin ol
        moveForwardWhenRunning = true;
    }

    void OnEnable()
    {
        GameEvents.OnEventRunning += HandleRunningEvent;
        GameEvents.OnEventCrouching += HandleCrouchingEvent;

        if (enableCollapsePhysicsOnEvent2)
        {
            GameEvents.OnEvent2_AssignRigidbodies += HandleCollapseEvent2;
        }

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

        if (enableCollapsePhysicsOnEvent2)
        {
            GameEvents.OnEvent2_AssignRigidbodies -= HandleCollapseEvent2;
        }
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

    private void HandleCollapseEvent2()
    {
        EnableCollapsePhysics();
    }

    void Update()
    {
        // Yıkılma gerçekleştiyse hareket kontrolünü tamamen devre dışı bırak
        if (hasCollapsePhysicsEnabled) return;

        if (!IsRunning) return;

        // XZ düzleminde her randomTurnInterval saniyede rastgele bir yöne dönüp ilerle
        if (randomWanderOnRun)
        {
            RandomWanderUpdate();
            return;
        }

        if (!moveForwardWhenRunning) return;

        Vector3 targetPos;
        bool hasTarget = GetCurrentTargetPosition(out targetPos);

        if (hasTarget)
        {
            // Y eksenini karakterin mevcut yüksekliğinde tutarak zeminin altına gömülmesini engelle
            targetPos.y = transform.position.y;
            Vector3 diff = targetPos - transform.position;
            diff.y = 0f;

            float distSqr = diff.sqrMagnitude;
            float reachThresholdSqr = waypointReachDistance * waypointReachDistance;

            if (distSqr > reachThresholdSqr)
            {
                // 1. Hedefe doğru yönel ve pürüzsüz bak
                if (diff.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(diff.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }

                // 2. Hedefe doğru ilerle
                transform.position = Vector3.MoveTowards(transform.position, targetPos, runMoveSpeed * Time.deltaTime);
            }
            else
            {
                // Mevcut keyframe / waypoint hedefine ulaşıldı!
                Debug.Log($"[{gameObject.name}] Keyframe {currentWaypointIndex + 1} hedefine ulaşıldı: {targetPos}");
                currentWaypointIndex++;

                if (!HasMoreWaypoints())
                {
                    // Belirlenen 2 keyframe (tüm rota) tamamlandı
                    Debug.Log($"[{gameObject.name}] Tüm kaçış keyframe rotasını tamamladı!");

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
                else
                {
                    // Bir sonraki keyframe'e geçerken anında yönelmeye başla
                    Vector3 nextPos;
                    if (GetCurrentTargetPosition(out nextPos))
                    {
                        Vector3 nextDiff = nextPos - transform.position;
                        nextDiff.y = 0f;
                        if (nextDiff.sqrMagnitude > 0.001f)
                        {
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(nextDiff.normalized), rotationSpeed * Time.deltaTime * 2.0f);
                        }
                    }
                }
            }
        }
        else
        {
            // Belirlenmiş rota bittiyse ve çömelme verilmediyse son baktığı yöne doğru ilerlemeye devam et
            if (!IsCrouching)
            {
                transform.Translate(Vector3.forward * (runMoveSpeed * Time.deltaTime), Space.Self);
            }
        }
    }

    /// <summary>
    /// XZ düzleminde rastgele kaçış koşusu.
    /// Her randomTurnInterval saniyede yeni bir rastgele yön (yaw) seçer, o yöne döner
    /// ve ileri doğru (sadece XZ düzleminde) ilerler. Y ekseni sabit tutulur.
    /// </summary>
    private void RandomWanderUpdate()
    {
        randomTurnTimer -= Time.deltaTime;
        if (randomTurnTimer <= 0f)
        {
            randomTurnTimer = Mathf.Max(0.05f, randomTurnInterval);
            randomTargetYaw = UnityEngine.Random.Range(0f, 360f);
        }

        // Başlangıç noktasından çok uzaklaşırsa merkeze geri dönmeye yönel
        Vector3 offset = transform.position - randomWanderOrigin;
        offset.y = 0f;
        if (randomWanderRadius > 0.1f && offset.sqrMagnitude > randomWanderRadius * randomWanderRadius)
        {
            randomTargetYaw = Quaternion.LookRotation(-offset.normalized).eulerAngles.y;
            randomTurnTimer = Mathf.Max(0.05f, randomTurnInterval);
        }

        // 1. Rastgele hedef yöne pürüzsüzce dön
        Quaternion targetRot = Quaternion.Euler(0f, randomTargetYaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

        // 2. Baktığı yönde XZ düzleminde ilerle (Y değişmez)
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f)
        {
            forward.Normalize();
            transform.position += forward * (runMoveSpeed * Time.deltaTime);
        }
    }

    private bool GetCurrentTargetPosition(out Vector3 pos)
    {
        if (customWaypoints != null && customWaypoints.Length > 0 && currentWaypointIndex < customWaypoints.Length)
        {
            pos = customWaypoints[currentWaypointIndex];
            return true;
        }

        if (waypoints != null && waypoints.Length > 0 && currentWaypointIndex < waypoints.Length)
        {
            if (waypoints[currentWaypointIndex] != null)
            {
                pos = waypoints[currentWaypointIndex].position;
                return true;
            }
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
        if (customWaypoints != null && customWaypoints.Length > 0 && currentWaypointIndex < customWaypoints.Length) return true;
        if (waypoints != null && waypoints.Length > 0 && currentWaypointIndex < waypoints.Length) return true;
        return false;
    }

    /// <summary>
    /// Karakter için 2 adet belirgin kaçış keyframe'i tanımlar.
    /// Karakter deprem anında sırasıyla bu iki noktaya yönelir ve koşar.
    /// </summary>
    public void SetEscapeKeyframes(Vector3 firstKeyframe, Vector3 secondKeyframe, bool crouchAtEnd = false, Transform lookAtTarget = null)
    {
        keyframe1 = firstKeyframe;
        keyframe2 = secondKeyframe;
        useKeyframes = true;
        crouchOnDestinationReached = crouchAtEnd;
        lookTargetOnCrouch = lookAtTarget;

        customWaypoints = new Vector3[] { firstKeyframe, secondKeyframe };
        currentWaypointIndex = 0;
        moveForwardWhenRunning = true;

        Debug.Log($"[{gameObject.name}] 2 Kaçış Keyframe'i tanımlandı: 1={keyframe1}, 2={keyframe2}, ÇökmeSonu={crouchAtEnd}");
    }

    /// <summary>
    /// Eğer karakterin waypoints veya keyframes listesi boşsa, bulunduğu konuma ve sahneye göre
    /// 2 adet belirgin kaçış keyframe'i oluşturur.
    /// </summary>
    public void EnsureEscapeKeyframesExist()
    {
        if (customWaypoints != null && customWaypoints.Length >= 2) return;
        if (waypoints != null && waypoints.Length >= 2) return;

        if (useKeyframes && keyframe1 != Vector3.zero && keyframe2 != Vector3.zero)
        {
            customWaypoints = new Vector3[] { keyframe1, keyframe2 };
            return;
        }

        float curY = transform.position.y;
        float curZ = transform.position.z;
        string objName = gameObject.name.ToLower();

        if (curZ < -1000f) // Intro.unity sahnesi
        {
            if (objName.Contains("male01"))
            {
                keyframe1 = new Vector3(27.5f, curY, -1862.5f);
                keyframe2 = new Vector3(29.8f, curY, -1859.3f);
            }
            else // male02 veya diğer
            {
                keyframe1 = new Vector3(29.8f, curY, -1864.0f);
                keyframe2 = new Vector3(29.8f, curY, -1859.3f);
            }
        }
        else // EvMarket.unity sahnesi
        {
            if (objName.Contains("male01"))
            {
                keyframe1 = new Vector3(7.5f, curY, -6.5f);
                keyframe2 = new Vector3(10.5f, curY, -13.5f);
            }
            else // male02 veya diğer
            {
                keyframe1 = new Vector3(5.5f, curY, -9.5f);
                keyframe2 = new Vector3(10.5f, curY, -14.5f);
            }
        }

        useKeyframes = true;
        customWaypoints = new Vector3[] { keyframe1, keyframe2 };
        Debug.Log($"[{gameObject.name}] Otomatik 2 Kaçış Keyframe'i sağlandı: 1={keyframe1}, 2={keyframe2}");
    }

    /// <summary>
    /// Karakteri koşma animasyonuna geçirir ve 2 keyframe rotasını başlatır.
    /// </summary>
    public void StartRunning()
    {
        if (hasCollapsePhysicsEnabled) return;

        IsRunning = true;
        IsCrouching = false;
        moveForwardWhenRunning = true;
        currentWaypointIndex = 0;

        // Rastgele kaçış koşusu için ilk yönü seç
        randomTurnTimer = 0f;
        randomTargetYaw = UnityEngine.Random.Range(0f, 360f);
        randomWanderOrigin = transform.position;

        EnsureEscapeKeyframesExist();

        if (animator == null) animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
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
        moveForwardWhenRunning = true;
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

    /// <summary>
    /// En son yıkılma / çökme anında bu arkadaşa Rigidbody ve Collider ekler,
    /// Animator'ı devre dışı bırakır ve rastgele tork ve itme kuvveti uygulayarak
    /// binanın yıkılmasıyla beraber fiziksel olarak yere savrulup düşmesini sağlar.
    /// </summary>
    public void EnableCollapsePhysics()
    {
        if (hasCollapsePhysicsEnabled) return;
        hasCollapsePhysicsEnabled = true;

        Debug.Log($"[{gameObject.name}] YIKILMA / ÇÖKME FİZİĞİ AKTİF EDİLDİ: Rigidbody ve Collider ekleniyor, yere savruluyor!");

        // 1. Koşma durumunu sonlandır
        IsRunning = false;
        moveForwardWhenRunning = false;

        // 2. Animator'ı devre dışı bırak (Animasyonun karakter pozunu kitlemesini önler)
        if (animator == null) animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
        }

        // 3. Varsa CharacterController veya NavMeshAgent'ı devre dışı bırak
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 4. Collider ekle veya ayarla
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<CapsuleCollider>();
        }
        col.enabled = true;
        col.isTrigger = false;
        col.height = 1.75f;
        col.radius = 0.35f;
        col.center = new Vector3(0f, 0.88f, 0f);

        // 5. Rigidbody ekle veya ayarla
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.mass = ragdollMass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.None; // Serbest yuvarlanma / düşme

        // 6. Deprem ve tavan çöküşü etkisiyle organik olarak savrulması için tork ve itme kuvveti uygula
        Vector3 randomToss = new Vector3(
            UnityEngine.Random.Range(-collapseImpulse, collapseImpulse),
            UnityEngine.Random.Range(-1.0f, 1.8f),
            UnityEngine.Random.Range(-collapseImpulse, collapseImpulse)
        );
        rb.AddForce(randomToss, ForceMode.Impulse);
        rb.AddTorque(UnityEngine.Random.insideUnitSphere * collapseTorque, ForceMode.Impulse);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 p1 = (customWaypoints != null && customWaypoints.Length > 0) ? customWaypoints[0] : keyframe1;
        Vector3 p2 = (customWaypoints != null && customWaypoints.Length > 1) ? customWaypoints[1] : keyframe2;

        if (p1 != Vector3.zero || p2 != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(p1, 0.35f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(p2, 0.35f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, p1);
            Gizmos.DrawLine(p1, p2);
        }
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

    [ContextMenu("Çöküş Fiziğini Aktif Et (Ragdoll / Rigidbody)")]
    public void ContextTriggerCollapsePhysics() => EnableCollapsePhysics();
    #endregion
}
