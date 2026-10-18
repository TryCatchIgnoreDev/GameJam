using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DoorInteract : MonoBehaviour
{
    [Header("--- Pivot ve Menteşe Ayarları ---")]
    [Tooltip("Özel bir pivot Transform'u atayabilirsiniz (Örn: kapının kenarındaki boş bir GameObject). Boş bırakılırsa aşağıdaki 'Pivot Offset' kullanılır.")]
    public Transform customPivot;

    [Tooltip("Kapının merkezine göre menteşenin yerel ofseti. Custom Pivot atanmamışsa bu değer kullanılır.")]
    public Vector3 pivotOffset = Vector3.zero;

    [Tooltip("Kapının etrafında döneceği yerel eksen (Varsayılan Y ekseni: 0, 1, 0).")]
    public Vector3 rotationAxis = Vector3.up;

    [Header("--- Kapı Hareket Ayarları ---")]
    [Tooltip("Açılma açısı (Derece cinsinden. Pozitif veya negatif vererek açılış yönünü değiştirebilirsiniz).")]
    public float openAngle = 90f;

    [Tooltip("Kapının açılma/kapanma hızı.")]
    public float speed = 3f;

    [Tooltip("Oyun başladığında kapı açık mı başlasın?")]
    public bool startOpen = false;

    [Header("--- Etkileşim Ayarları ---")]
    [Tooltip("Etkileşim mesafesi (metre).")]
    public float interactDistance = 3.5f;

    [Tooltip("Kapıyı açmak için oyuncunun kapıya doğru bakması zorunlu olsun mu?")]
    public bool requireFacing = true;

#if ENABLE_INPUT_SYSTEM
    [Tooltip("Etkileşim tuşu (Varsayılan: E).")]
    public Key interactKey = Key.E;
#else
    [Tooltip("Etkileşim tuşu (Varsayılan: E).")]
    public KeyCode interactKey = KeyCode.E;
#endif

    [Header("--- Ses Ayarları (Opsiyonel) ---")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioSource audioSource;

    [Header("--- Arayüz (UI) ---")]
    public bool showPrompt = true;
    public string openPrompt = "E - Kapıyı Aç";
    public string closePrompt = "E - Kapıyı Kapat";

    // Durum Değişkenleri
    public bool IsOpen { get; private set; }
    private float currentAngle = 0f;
    private float targetAngle = 0f;

    // Başlangıç referans pozisyon ve rotasyonları (Local Space - Kaymayı önler)
    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private Vector3 initialLocalPivotPos;
    private Vector3 initialLocalOffsetFromPivot;
    private Vector3 localRotationAxis;

    private Transform playerCam;

    // Olaylar (Events)
    public event System.Action<bool> OnDoorStateChanged;

    void Awake()
    {
#if ENABLE_INPUT_SYSTEM
        // Eski KeyCode değerinden (KeyCode.E = 101) otomatik geçiş kontrolü
        if ((int)interactKey == 101 || !System.Enum.IsDefined(typeof(Key), interactKey))
        {
            interactKey = Key.E;
        }
#endif
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        SetupInitialTransform();
    }

    void Start()
    {
        FindPlayerCamera();

        if (startOpen)
        {
            IsOpen = true;
            targetAngle = openAngle;
            currentAngle = openAngle;
            ApplyAngle(currentAngle);
        }
        else
        {
            IsOpen = false;
            targetAngle = 0f;
            currentAngle = 0f;
            ApplyAngle(currentAngle);
        }
    }

    /// <summary>
    /// Kapının kapalı referans konumunu ve pivot ofsetini hesaplayıp hafızaya alır.
    /// </summary>
    public void SetupInitialTransform()
    {
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;

        // Yerel pivot pozisyonu belirle
        if (customPivot != null)
        {
            if (transform.parent != null)
            {
                initialLocalPivotPos = transform.parent.InverseTransformPoint(customPivot.position);
                localRotationAxis = transform.parent.InverseTransformDirection(customPivot.TransformDirection(rotationAxis)).normalized;
            }
            else
            {
                initialLocalPivotPos = customPivot.position;
                localRotationAxis = customPivot.TransformDirection(rotationAxis).normalized;
            }
        }
        else
        {
            // Kapının başlangıç rotasyonu ile pivot ofsetini uygula
            initialLocalPivotPos = initialLocalPos + initialLocalRot * pivotOffset;
            localRotationAxis = (initialLocalRot * rotationAxis).normalized;
        }

        initialLocalOffsetFromPivot = initialLocalPos - initialLocalPivotPos;
    }

    void Update()
    {
        // Hedef açıya yumuşak geçiş
        if (!Mathf.Approximately(currentAngle, targetAngle))
        {
            currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, Time.deltaTime * Mathf.Abs(openAngle) * speed);
            ApplyAngle(currentAngle);
        }

        HandleInput();
    }

    void HandleInput()
    {
        if (playerCam == null)
        {
            FindPlayerCamera();
            if (playerCam == null) return;
        }

        float dist = Vector3.Distance(playerCam.position, transform.position);
        if (dist > interactDistance) return;

        if (requireFacing)
        {
            Vector3 dirToDoor = (transform.position - playerCam.position).normalized;
            if (Vector3.Dot(playerCam.forward, dirToDoor) < 0.25f) return;
        }

        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && interactKey != Key.None && interactKey != Key.IMESelected)
        {
            var keyControl = Keyboard.current[interactKey];
            if (keyControl != null && keyControl.wasPressedThisFrame)
            {
                pressed = true;
            }
        }
        if (!pressed && Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
        {
            pressed = true;
        }
#else
        if (Input.GetKeyDown(interactKey))
        {
            pressed = true;
        }
#endif

        if (pressed)
        {
            Toggle();
        }
    }

    /// <summary>
    /// Belirtilen açıyı pivot etrafında uygulayarak kapıyı konumlandırır ve döndürür.
    /// Sıfır kayma (zero-drift) garantisi verir.
    /// </summary>
    private void ApplyAngle(float angle)
    {
        Quaternion rotationDelta = Quaternion.AngleAxis(angle, localRotationAxis);
        transform.localRotation = rotationDelta * initialLocalRot;
        transform.localPosition = initialLocalPivotPos + rotationDelta * initialLocalOffsetFromPivot;
    }

    public void Toggle()
    {
        SetState(!IsOpen);
    }

    public void Open()
    {
        SetState(true);
    }

    public void Close()
    {
        SetState(false);
    }

    public void SetState(bool open)
    {
        if (IsOpen == open) return;
        IsOpen = open;
        targetAngle = IsOpen ? openAngle : 0f;

        PlayAudio(IsOpen ? openSound : closeSound);
        OnDoorStateChanged?.Invoke(IsOpen);
    }

    void PlayAudio(AudioClip clip)
    {
        if (clip != null)
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
            }
            audioSource.PlayOneShot(clip);
        }
    }

    void FindPlayerCamera()
    {
        if (Camera.main != null)
        {
            playerCam = Camera.main.transform;
        }
        else
        {
            Camera cam = FindAnyObjectByType<Camera>();
            if (cam != null) playerCam = cam.transform;
        }
    }

    void OnGUI()
    {
        if (!showPrompt) return;
        if (playerCam == null) return;

        float dist = Vector3.Distance(playerCam.position, transform.position);
        if (dist > interactDistance) return;

        if (requireFacing)
        {
            Vector3 dirToDoor = (transform.position - playerCam.position).normalized;
            if (Vector3.Dot(playerCam.forward, dirToDoor) < 0.25f) return;
        }

        string keyText = interactKey.ToString();
        string actionText = IsOpen ? closePrompt : openPrompt;
        string displayText = actionText.Replace("E -", keyText + " -");

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 16;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;

        int width = 250;
        int height = 38;
        GUI.Box(new Rect((Screen.width - width) / 2f, Screen.height - 120, width, height), displayText, style);
    }

    #region Editör Yardımcıları & Context Menüleri
    [ContextMenu("Menteşeyi Sol Kenara Ayarla (Bounds X/Z)")]
    public void SetPivotToLeftEdge()
    {
        Vector3 size = GetDoorSize();
        // Z ekseni genelde kapı genişliğidir, değilse X eksenidir
        if (size.z > size.x)
        {
            pivotOffset = new Vector3(0, 0, -size.z * 0.5f);
        }
        else
        {
            pivotOffset = new Vector3(-size.x * 0.5f, 0, 0);
        }
        customPivot = null;
    }

    [ContextMenu("Menteşeyi Sağ Kenara Ayarla (Bounds X/Z)")]
    public void SetPivotToRightEdge()
    {
        Vector3 size = GetDoorSize();
        if (size.z > size.x)
        {
            pivotOffset = new Vector3(0, 0, size.z * 0.5f);
        }
        else
        {
            pivotOffset = new Vector3(size.x * 0.5f, 0, 0);
        }
        customPivot = null;
    }

    [ContextMenu("Menteşeyi Merkeze Al (0,0,0)")]
    public void ResetPivotOffset()
    {
        pivotOffset = Vector3.zero;
        customPivot = null;
    }

    [ContextMenu("Menteşe (Pivot) GameObject'i Oluştur")]
    public void CreateHingePivotGameObject()
    {
        Vector3 worldPivotPos = GetCurrentPivotWorldPosition();

        GameObject pivotObj = new GameObject(gameObject.name + "_Pivot");
        if (transform.parent != null)
        {
            pivotObj.transform.SetParent(transform.parent, true);
        }
        pivotObj.transform.position = worldPivotPos;
        pivotObj.transform.rotation = transform.rotation;

        customPivot = pivotObj.transform;
        pivotOffset = Vector3.zero;
        Debug.Log($"'{pivotObj.name}' başarıyla oluşturuldu ve Custom Pivot olarak atandı. Scene ekranında tutamacı hareket ettirerek menteşeyi ayarlayabilirsiniz.", pivotObj);
    }

    private Vector3 GetDoorSize()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null) return box.size;

        Renderer rend = GetComponent<Renderer>();
        if (rend != null) return rend.localBounds.size;

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null) return mf.sharedMesh.bounds.size;

        return Vector3.one;
    }

    public Vector3 GetCurrentPivotWorldPosition()
    {
        if (customPivot != null)
        {
            return customPivot.position;
        }
        return transform.position + transform.rotation * pivotOffset;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 pivotPos = GetCurrentPivotWorldPosition();
        Vector3 axis = (customPivot != null ? customPivot.TransformDirection(rotationAxis) : transform.rotation * rotationAxis).normalized;

        // Menteşe Noktası
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pivotPos, 0.08f);
        Gizmos.DrawSphere(pivotPos, 0.03f);

        // Menteşe Dönme Ekseni (Dikey Çizgi)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pivotPos - axis * 0.6f, pivotPos + axis * 0.6f);

        // Kapı Merkezinden Menteşeye Bağlantı Çizgisi
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.75f);
        Gizmos.DrawLine(transform.position, pivotPos);

#if UNITY_EDITOR
        // Dönüş Yayı / Açılma Çemberi (Scene Görünümünde Yarıçap ve Açıyı Gösterir)
        Vector3 arm = transform.position - pivotPos;
        float radius = arm.magnitude;
        if (radius > 0.01f)
        {
            UnityEditor.Handles.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            UnityEditor.Handles.DrawWireDisc(pivotPos, axis, radius);

            Vector3 startDir = arm.normalized;
            UnityEditor.Handles.color = new Color(0f, 1f, 0.5f, 0.2f);
            UnityEditor.Handles.DrawSolidArc(pivotPos, axis, startDir, openAngle, radius);
        }
#endif
    }
    #endregion
}
