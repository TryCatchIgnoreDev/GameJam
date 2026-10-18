using System;
using UnityEngine;

/// <summary>
/// End_UnderRubble sahnesinde oyuncunun enkaz altındaki hareketini ve fenerini yöneten kontrolcü.
/// Oyuncunun boyu normal insansı orandadır (CapsuleCollider height=1.75m, radius=0.35m, camera localPos.y=1.45m).
/// Oyuncu çökmüş tavanın altında ayakta durabilir ve dar alanda gezinebilir.
/// Gökyüzü tamamen çökmüş beton bloklarla kapalıdır.
/// Fener kameranın tam önünde, objelerin içine girmeden aydınlatır.
/// </summary>
public class PlayerCrawlController : MonoBehaviour
{
    public static PlayerCrawlController Instance { get; private set; }

    [Header("--- Referanslar ---")]
    public FirstPersonMovement movement;
    public FirstPersonLook look;
    public CapsuleCollider capsuleCollider;
    public CharacterController characterController;
    public Rigidbody characterRigidbody;
    public Camera playerCamera;
    public Light flashlight;

    [Header("--- Hareket Ayarları ---")]
    [Tooltip("Enkaz altındaki yürüme hızı (temkinli adımlar).")]
    public float exploreSpeed = 1.6f;

    [Tooltip("Normal ayakta durma kapsül yüksekliği.")]
    public float standingHeight = 1.75f;

    [Tooltip("Kapsül yarıçapı.")]
    public float standingRadius = 0.35f;

    [Tooltip("Kapsül merkez Y.")]
    public float standingCenterY = 0.88f;

    [Tooltip("Göz hizası kamera Y konumu.")]
    public float eyeLevelHeight = 1.45f;

    [Header("--- Durum ---")]
    [SerializeField]
    private bool isFrozen = false;
    public bool IsFrozen => isFrozen;

    private Func<float> speedOverrideDelegate;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        speedOverrideDelegate = GetExploreSpeed;
        AutoFindReferences();
        ApplyStandingMode();
    }

    private void Start()
    {
        AutoFindReferences();
        ApplyStandingMode();
    }

    private void LateUpdate()
    {
        if (playerCamera != null && !isFrozen)
        {
            Vector3 camLocal = playerCamera.transform.localPosition;
            if (Mathf.Abs(camLocal.y - eyeLevelHeight) > 0.02f)
            {
                playerCamera.transform.localPosition = new Vector3(camLocal.x, eyeLevelHeight, camLocal.z);
            }
        }
    }

    public void AutoFindReferences()
    {
        if (movement == null)
        {
            movement = GetComponent<FirstPersonMovement>() 
                    ?? GetComponentInParent<FirstPersonMovement>() 
                    ?? GetComponentInChildren<FirstPersonMovement>()
                    ?? FindFirstObjectByType<FirstPersonMovement>();
        }

        if (look == null)
        {
            look = GetComponent<FirstPersonLook>() 
                ?? GetComponentInChildren<FirstPersonLook>() 
                ?? GetComponentInParent<FirstPersonLook>()
                ?? FindFirstObjectByType<FirstPersonLook>();
        }

        if (capsuleCollider == null)
        {
            capsuleCollider = GetComponent<CapsuleCollider>() 
                           ?? GetComponentInChildren<CapsuleCollider>()
                           ?? (movement != null ? movement.GetComponentInChildren<CapsuleCollider>() : null);
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>() 
                                ?? GetComponentInChildren<CharacterController>();
        }

        if (characterRigidbody == null)
        {
            characterRigidbody = GetComponent<Rigidbody>() 
                              ?? GetComponentInParent<Rigidbody>()
                              ?? (movement != null ? movement.GetComponent<Rigidbody>() : null);
        }

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
        }

        if (flashlight == null && playerCamera != null)
        {
            flashlight = playerCamera.GetComponentInChildren<Light>(true);
        }
    }

    public void ApplyStandingMode()
    {
        // 1. Doğal 1:1 boyut
        transform.localScale = Vector3.one;

        // 2. Kapsül boyutları
        if (capsuleCollider != null)
        {
            capsuleCollider.height = standingHeight;
            capsuleCollider.radius = standingRadius;
            capsuleCollider.center = new Vector3(0f, standingCenterY, 0f);
        }

        if (characterController != null)
        {
            characterController.height = standingHeight;
            characterController.radius = standingRadius;
            characterController.center = new Vector3(0f, standingCenterY, 0f);
        }

        // 3. Kamera göz hizası
        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = new Vector3(0f, eyeLevelHeight, 0f);
            playerCamera.nearClipPlane = 0.05f;
        }

        // 4. Fener ayarları
        if (flashlight != null)
        {
            flashlight.transform.localPosition = new Vector3(0.15f, -0.15f, 0.25f);
            flashlight.transform.localRotation = Quaternion.identity;
            flashlight.range = 22f;
            flashlight.spotAngle = 60f;
            flashlight.innerSpotAngle = 38f;
            flashlight.intensity = 8.5f;
            flashlight.color = new Color(1.0f, 0.96f, 0.88f);
            flashlight.shadows = LightShadows.Soft;
        }

        // 5. Yürüme hızı ve kısıtlamalar
        if (movement != null)
        {
            movement.speed = exploreSpeed;
            movement.runSpeed = exploreSpeed;
            movement.canRun = false;

            if (speedOverrideDelegate != null && !movement.speedOverrides.Contains(speedOverrideDelegate))
            {
                movement.speedOverrides.Add(speedOverrideDelegate);
            }
        }

        // 6. Normal gövde mesh'i gizle (kamerayı bloke etmesin)
        Transform capsuleMesh = transform.Find("Capsule Mesh");
        if (capsuleMesh != null)
        {
            var rend = capsuleMesh.GetComponent<Renderer>();
            if (rend != null) rend.enabled = false;
        }

        // Normal zıplamayı kapat (tavana çarpmasın)
        MonoBehaviour jumpComp = GetComponent("Jump") as MonoBehaviour;
        if (jumpComp != null && jumpComp.enabled) jumpComp.enabled = false;

        Debug.Log("[PlayerCrawlController] Normal insan ölçeğinde ayakta durma modu uygulandı.");
    }

    private float GetExploreSpeed()
    {
        return exploreSpeed;
    }

    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;

        if (movement != null) movement.enabled = !frozen;
        if (look != null) look.enabled = !frozen;

        if (characterRigidbody != null)
        {
            if (frozen)
            {
                characterRigidbody.linearVelocity = Vector3.zero;
                characterRigidbody.angularVelocity = Vector3.zero;
                characterRigidbody.isKinematic = true;
            }
            else
            {
                characterRigidbody.isKinematic = false;
            }
        }
    }
}
