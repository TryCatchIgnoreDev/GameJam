using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Event 1 dinleyicisi (Event Listener):
/// GameEvents.OnEvent1_StartShaking tetiklendiğinde objeye rastgele yönlerde ufak kuvvetler ve tork uygulayarak sarsıntı başlatır.
/// GameEvents.OnEvent1_StopShaking tetiklendiğinde durdurur.
/// Hem Rigidbody fizik kuvvetlerini hem de Rigidbody olmayan objeler için transform titreşimini destekler.
/// </summary>
public class ObjectShaker : MonoBehaviour
{
    [Header("--- Event Listener Ayarları ---")]
    [Tooltip("GameEvents Event 1 (Sallanma) eventini dinlesin mi?")]
    public bool listenToEvent1 = true;

    [Tooltip("Oyun başlar başlamaz sarsıntı aktif olsun mu?")]
    public bool startImmediately = false;

    [Header("--- Opsiyonel Debug Tuşu (İsteğe Bağlı) ---")]
    [Tooltip("Test amaçlı klavye tuşu ile de tetiklensin mi?")]
    public bool enableDebugKey = false;

#if ENABLE_INPUT_SYSTEM
    public Key debugKey = Key.M;
#else
    public KeyCode debugKey = KeyCode.M;
#endif

    [Header("--- Kuvvet (Force) Ayarları ---")]
    [Tooltip("Uygulanacak minimum rastgele kuvvet miktarı.")]
    public float minForce = 1.5f;

    [Tooltip("Uygulanacak maksimum rastgele kuvvet miktarı.")]
    public float maxForce = 6f;

    [Tooltip("Kuvvet uygulanma türü (Force: Sürekli itme, Impulse: Anlık darbe).")]
    public ForceMode forceMode = ForceMode.Force;

    [Tooltip("Kuvvetlerin sadece yatay düzlemde (X/Z) mi yoksa yukarı/aşağı dahil 3 boyutta mı uygulanacağı.")]
    public bool allowVerticalForce = true;

    [Header("--- Tork / Dönme (Torque) Ayarları ---")]
    [Tooltip("Objeye rastgele dönme sarsıntısı (tork) uygulansın mı?")]
    public bool applyTorque = true;

    [Tooltip("Uygulanacak minimum tork miktarı.")]
    public float minTorque = 0.5f;

    [Tooltip("Uygulanacak maksimum tork miktarı.")]
    public float maxTorque = 3f;

    [Header("--- Zamanlama ve Frekans ---")]
    [Tooltip("Her FixedUpdate'te sürekli kuvvet uygulansın mı?")]
    public bool continuousForce = true;

    [Tooltip("continuousForce kapalıysa, kaç saniyede bir rastgele sarsıntı darbesi vurulsun?")]
    public float impulseInterval = 0.1f;

    [Header("--- Rigidbody Olmayan Objeler İçin Fallback ---")]
    [Tooltip("Objede Rigidbody yoksa bile Transform üzerinden ufak sallantı yapılsın mı?")]
    public bool fallbackToTransformShake = true;

    [Tooltip("Transform sallanma şiddeti (konum kayması).")]
    public float transformShakeMagnitude = 0.05f;

    // Durum Değişkenleri
    public bool IsShaking { get; private set; }

    private Rigidbody rb;
    private float timer = 0f;
    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;

    // Sahnedeki tüm shaker'ların listesi
    private static readonly HashSet<ObjectShaker> allShakers = new HashSet<ObjectShaker>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;
    }

    void OnEnable()
    {
        allShakers.Add(this);

        // EVENT 1 Dinleyicisine abone ol
        if (listenToEvent1)
        {
            GameEvents.OnEvent1_StartShaking += HandleEvent1_Start;
            GameEvents.OnEvent1_StopShaking += HandleEvent1_Stop;

            // Eğer event önceden zaten tetiklenmişse durumunu eşitle
            if (GameEvents.IsEvent1Active)
            {
                StartShaking();
            }
        }
    }

    void OnDisable()
    {
        allShakers.Remove(this);

        if (listenToEvent1)
        {
            GameEvents.OnEvent1_StartShaking -= HandleEvent1_Start;
            GameEvents.OnEvent1_StopShaking -= HandleEvent1_Stop;
        }
    }

    void Start()
    {
        if (startImmediately)
        {
            StartShaking();
        }
    }

    private void HandleEvent1_Start()
    {
        StartShaking();
    }

    private void HandleEvent1_Stop()
    {
        StopShaking();
    }

    void Update()
    {
        // Opsiyonel test tuşu açık ise
        if (enableDebugKey)
        {
            bool keyPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && debugKey != Key.None && debugKey != Key.IMESelected)
            {
                var keyControl = Keyboard.current[debugKey];
                if (keyControl != null && keyControl.wasPressedThisFrame)
                {
                    keyPressed = true;
                }
            }
#else
            if (Input.GetKeyDown(debugKey))
            {
                keyPressed = true;
            }
#endif
            if (keyPressed)
            {
                // Global Event 1'i tersine çevirerek tetikler
                GameEvents.ToggleEvent1();
            }
        }
    }

    void FixedUpdate()
    {
        if (!IsShaking) return;

        // Çalışma anında Rigidbody eklenmişse dinamik yakala (Örn: Event 2 devreye girdiğinde)
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb != null)
        {
            ApplyPhysicsShake();
        }
        else if (fallbackToTransformShake)
        {
            ApplyTransformShake();
        }
    }

    private void ApplyPhysicsShake()
    {
        if (continuousForce)
        {
            ApplyRandomForces();
        }
        else
        {
            timer += Time.fixedDeltaTime;
            if (timer >= impulseInterval)
            {
                timer = 0f;
                ApplyRandomForces();
            }
        }
    }

    private void ApplyRandomForces()
    {
        Vector3 randomDir = Random.insideUnitSphere.normalized;
        if (!allowVerticalForce)
        {
            randomDir.y = 0f;
            randomDir.Normalize();
        }

        float forceAmt = Random.Range(minForce, maxForce);
        rb.AddForce(randomDir * forceAmt, forceMode);

        if (applyTorque)
        {
            Vector3 randomTorqueDir = Random.insideUnitSphere;
            float torqueAmt = Random.Range(minTorque, maxTorque);
            rb.AddTorque(randomTorqueDir * torqueAmt, forceMode);
        }
    }

    private void ApplyTransformShake()
    {
        Vector3 shakeOffset = Random.insideUnitSphere * transformShakeMagnitude;
        if (!allowVerticalForce) shakeOffset.y = 0f;
        transform.localPosition = initialLocalPos + shakeOffset;
    }

    public void StartShaking()
    {
        IsShaking = true;
    }

    public void StopShaking()
    {
        IsShaking = false;
        if (rb == null && fallbackToTransformShake)
        {
            transform.localPosition = initialLocalPos;
            transform.localRotation = initialLocalRot;
        }
    }

    public void ToggleShaking()
    {
        if (IsShaking) StopShaking();
        else StartShaking();
    }

    #region Context Menüleri
    [ContextMenu("Event 1: Sallanmayı Başlat")]
    public void ContextStart() => GameEvents.TriggerEvent1(true);

    [ContextMenu("Event 1: Sallanmayı Durdur")]
    public void ContextStop() => GameEvents.TriggerEvent1(false);
    #endregion
}
