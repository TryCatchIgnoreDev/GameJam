using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Etkileşim türleri. Hikaye akışındaki objeleri sınıflandırmak için kullanılır.
/// </summary>
public enum InteractableType
{
    Generic = 0,
    Poster = 1,
    Napkin = 2,
    HomeEntrance = 3,
    SafeTable = 4,
    MarketEntrance = 5
}

/// <summary>
/// Oyuncunun yaklaşıp kamerayla bakarak E tuşuyla etkileşime girebileceği modüler bileşen.
/// Hem Yeni Input System (Keyboard.current.eKey) hem eski Input.GetKeyDown(KeyCode.E) destekler.
/// </summary>
public class StoryInteractable : MonoBehaviour
{
    [Header("--- Temel Yapılandırma ---")]
    [Tooltip("Etkileşim objesinin mantıksal türü.")]
    public InteractableType interactableType = InteractableType.Generic;

    [Tooltip("Senaryoda kontrol edilecek benzersiz ID (Örn: 'kitchen_earthquake_poster').")]
    public string interactableId = "";

    [Tooltip("Ekranda belirecek etkileşim komut metni.")]
    public string promptText = "[E] İncele";

    [Tooltip("Etkileşime girilebilecek maksimum mesafe (metre).")]
    public float interactDistance = 3.2f;

    [Tooltip("Etkileşimin şu anda aktif olup olmadığı.")]
    public bool isInteractable = true;

    [Tooltip("Etkileşim için oyuncunun objeye doğru bakması zorunlu mu?")]
    public bool requireFacing = true;

    [Tooltip("Etkileşim ve alttaki prompt yazısı için kameradan doğrudan raycast isabeti şartı. Hiçbir engele çarpmadan doğrudan bu trigera çarpması gerekir.")]
    public bool requireRaycastDirectHit = false;

    [Tooltip("Açık olduğunda objenin üzerinde ufak bir sarı/turuncu hedef işareti veya GUI göstergesi çizer.")]
    public bool highlightMarker = false;

    [Tooltip("Bu objeye ait ek collider'lar (Örn: ilişkili kapı modeli). Raycast/LOS kontrolünde engel sayılmazlar.")]
    public Collider[] linkedColliders;

    [Header("--- Görsel Ofsetler ---")]
    [Tooltip("Marker göstergesinin objenin merkezine göre dikey ofseti.")]
    public float markerHeightOffset = 0.4f;

    // Olaylar (Events)
    public event Action<StoryInteractable> OnInteracted;
    public static event Action<StoryInteractable> OnAnyInteracted;

    // Statik Yönetim (Sahnedeki tüm aktif objeleri takip eder, en iyi hedefe odaklanır)
    private static readonly List<StoryInteractable> ActiveInteractables = new List<StoryInteractable>();
    public static StoryInteractable CurrentFocused { get; private set; }

    private Transform playerCam;
    private Collider cachedCollider;

    // GUI Stilleri (Tek seferlik başlatılır)
    private static GUIStyle promptBoxStyle;
    private static GUIStyle markerStyle;
    private static Texture2D boxBgTex;

    private void Awake()
    {
        cachedCollider = GetComponent<Collider>();
        if (cachedCollider == null)
        {
            cachedCollider = GetComponentInChildren<Collider>();
        }
        if (interactableType == InteractableType.Poster)
        {
            requireRaycastDirectHit = true;
        }
    }

    private void OnEnable()
    {
        if (!ActiveInteractables.Contains(this))
        {
            ActiveInteractables.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveInteractables.Remove(this);
        if (CurrentFocused == this)
        {
            CurrentFocused = null;
        }
    }

    private void Start()
    {
        FindPlayerCamera();
    }

    private void Update()
    {
        if (playerCam == null)
        {
            FindPlayerCamera();
            if (playerCam == null) return;
        }

        // Eğer FriendChoiceUI veya benzeri acil durum menüleri aktifse etkileşimi beklet
        if (FriendChoiceUI.Instance != null && (FriendChoiceUI.Instance.IsChoiceActive || FriendChoiceUI.Instance.IsRubblePromptActive))
        {
            if (CurrentFocused == this) CurrentFocused = null;
            return;
        }

        UpdateFocusTarget();

        // Sadece odaklanılmış olan obje E tuşunu dinler (çakışmaları önler)
        if (CurrentFocused == this && isInteractable)
        {
            if (IsEPressed())
            {
                TriggerInteraction();
            }
        }
    }

    /// <summary>
    /// Kameraya en uygun açıda ve mesafedeki objeyi odaklanan obje olarak belirler.
    /// Raycast kontrolü ile engellerin arkasından tetiklenmeyi ve prompt gösterilmesini önler.
    /// </summary>
    private void UpdateFocusTarget()
    {
        if (!isInteractable)
        {
            if (CurrentFocused == this) CurrentFocused = null;
            return;
        }

        Vector3 camPos = playerCam.position;
        if (cachedCollider == null)
        {
            cachedCollider = GetComponentInChildren<Collider>();
        }

        Vector3 centerPoint = (cachedCollider != null) ? cachedCollider.bounds.center : transform.position;
        Vector3 targetPoint = transform.position;
        if (cachedCollider != null)
        {
            if (cachedCollider is MeshCollider mc && !mc.convex)
            {
                targetPoint = cachedCollider.bounds.ClosestPoint(camPos);
            }
            else
            {
                targetPoint = cachedCollider.ClosestPoint(camPos);
            }
        }
        float dist = Vector3.Distance(camPos, targetPoint);

        if (dist > interactDistance)
        {
            if (CurrentFocused == this) CurrentFocused = null;
            return;
        }

        if (requireFacing)
        {
            Vector3 dirToTarget = (targetPoint - camPos).normalized;
            float dot = Vector3.Dot(playerCam.forward, dirToTarget);
            float minDot = (interactableType == InteractableType.HomeEntrance) ? 0.15f : 0.35f;
            if (dot < minDot)
            {
                if (CurrentFocused == this) CurrentFocused = null;
                return;
            }
        }

        // Raycast Kontrolü:
        bool useDirectRaycast = requireRaycastDirectHit || (interactableType == InteractableType.Poster);
        if (useDirectRaycast)
        {
            // Kamera merkezinden (crosshair) ileriye doğru doğrudan raycast fırlat
            Ray ray = new Ray(camPos, playerCam.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, ~0, QueryTriggerInteraction.Collide);
            bool directHitValid = false;

            if (hits != null && hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                for (int i = 0; i < hits.Length; i++)
                {
                    Collider hitCol = hits[i].collider;
                    if (hitCol == null) continue;

                    // Oyuncunun kendi collider'larını atla
                    if (IsPlayerCollider(hitCol))
                        continue;

                    // Bu StoryInteractable'a ait bir collider'a çarptıysa -> DOĞRUDAN ENGELSİZ İSABET!
                    if (IsMyCollider(hitCol))
                    {
                        directHitValid = true;
                        break;
                    }

                    // Görünmez sınır bloklarını (Cube 5 gibi Renderer'ı kapalı objeler) atla
                    MeshRenderer mr = hitCol.GetComponent<MeshRenderer>();
                    if (mr == null || !mr.enabled)
                        continue;

                    // Eğer StoryInteractable olmayan başka bir trigger ise (örneğin alan trigger'ı) es geç
                    if (hitCol.isTrigger && hitCol.GetComponentInParent<StoryInteractable>() == null)
                        continue;

                    // Katı bir engel (duvar, mobilya, kapı vs.) veya başka bir nesneye çarptı, görüş tıkandı!
                    directHitValid = false;
                    break;
                }
            }

            if (!directHitValid)
            {
                if (CurrentFocused == this) CurrentFocused = null;
                return;
            }
        }
        else
        {
            // Duvar arkasından etkileşimi kesin olarak engellemek için RaycastAll / Line-of-sight kontrolü
            bool hasLos = HasLineOfSight(camPos, targetPoint) || HasLineOfSight(camPos, centerPoint);
            if (!hasLos)
            {
                if (CurrentFocused == this) CurrentFocused = null;
                return;
            }
        }

        // Eğer mevcut odak boşsa veya bu obje mevcut odaktan daha yakın/odaklıysa
        if (CurrentFocused == null || CurrentFocused == this)
        {
            CurrentFocused = this;
        }
        else if (CurrentFocused != null && !CurrentFocused.isInteractable)
        {
            CurrentFocused = this;
        }
        else
        {
            // Eğer bu obje direct raycast ile tam ortadan vuruluyorsa ama mevcut odak direct raycast değilse, buna öncelik ver
            if (useDirectRaycast && !CurrentFocused.requireRaycastDirectHit && CurrentFocused.interactableType != InteractableType.Poster)
            {
                CurrentFocused = this;
            }
            else
            {
                float myDist = dist;
                Vector3 myDir = (centerPoint - camPos).normalized;
                float myDot = Vector3.Dot(playerCam.forward, myDir);

                Vector3 otherCenter = CurrentFocused.cachedCollider != null ? CurrentFocused.cachedCollider.bounds.center : CurrentFocused.transform.position;
                float otherDist = Vector3.Distance(camPos, CurrentFocused.cachedCollider != null ? CurrentFocused.cachedCollider.ClosestPoint(camPos) : otherCenter);
                Vector3 otherDir = (otherCenter - camPos).normalized;
                float otherDot = Vector3.Dot(playerCam.forward, otherDir);

                // Daha yüksek dot ve daha düşük mesafe önceliğe sahiptir
                float myScore = myDot * 1.5f - (myDist / interactDistance);
                float otherScore = otherDot * 1.5f - (otherDist / CurrentFocused.interactDistance);

                if (myScore > otherScore)
                {
                    CurrentFocused = this;
                }
            }
        }
    }

    /// <summary>
    /// Oyuncu kamerası ile hedef nokta arasında katı duvar/engel olup olmadığını kontrol eder.
    /// </summary>
    public bool HasLineOfSight(Vector3 camPos, Vector3 targetPoint)
    {
        Vector3 dir = targetPoint - camPos;
        float dist = dir.magnitude;
        if (dist <= 0.05f) return true;
        dir /= dist;

        RaycastHit[] hits = Physics.RaycastAll(camPos, dir, Mathf.Max(0.01f, dist - 0.05f), ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return true;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCol = hits[i].collider;
            if (hitCol == null) continue;

            if (IsPlayerCollider(hitCol)) continue;
            if (IsMyCollider(hitCol)) continue;
            if (hitCol.isTrigger) continue;

            // Görünmez sınır bloklarını (Cube 5 gibi Renderer'ı kapalı veya olmayan objeler) atla
            MeshRenderer mr = hitCol.GetComponent<MeshRenderer>();
            if (mr == null || !mr.enabled) continue;

            // Kapı ve bina girişlerinde (HomeEntrance) kapı kasasının kenarı veya hedef noktasının hemen arkasındaki yüzey görüşü engellemez
            if (interactableType == InteractableType.HomeEntrance)
            {
                // Eğer çarpma mesafesi hedefin çok yakınındaysa (son 35 cm) engel sayma
                if (hits[i].distance >= dist - 0.35f) continue;
            }

            // Katı bir duvar veya engel var, görüş engellendi!
            return false;
        }

        return true;
    }

    private bool IsPlayerCollider(Collider col)
    {
        if (col == null) return false;
        if (playerCam != null)
        {
            if (col.transform == playerCam || col.transform.IsChildOf(playerCam)) return true;
            if (col.transform.root == playerCam.root) return true;
        }
        if (col.CompareTag("Player")) return true;
        if (col.GetComponentInParent<FirstPersonMovement>() != null) return true;
        return false;
    }

    private bool IsMyCollider(Collider col)
    {
        if (col == null) return false;
        if (col == cachedCollider) return true;
        if (col.transform == transform || col.transform.IsChildOf(transform)) return true;
        if (col.GetComponentInParent<StoryInteractable>() == this) return true;
        if (linkedColliders != null)
        {
            for (int i = 0; i < linkedColliders.Length; i++)
            {
                if (linkedColliders[i] == col) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Yeni Input System ve Eski Input System ile güvenli E tuşu kontrolü.
    /// </summary>
    private bool IsEPressed()
    {
        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            pressed = true;
        if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
            pressed = true;
#endif
        try
        {
            if (Input.GetKeyDown(KeyCode.E))
                pressed = true;
        }
        catch { }
        return pressed;
    }

    /// <summary>
    /// Etkileşim metnini ve aktiflik durumunu dinamik olarak günceller.
    /// </summary>
    public void SetPrompt(string newPrompt, bool enabled = true)
    {
        promptText = newPrompt;
        isInteractable = enabled;
        if (!enabled && CurrentFocused == this)
        {
            CurrentFocused = null;
        }
    }

    /// <summary>
    /// Etkileşimi doğrudan kod üzerinden tetikler.
    /// </summary>
    public void TriggerInteraction()
    {
        Debug.Log($"[StoryInteractable] Tetiklendi -> Ad: {gameObject.name}, ID: '{interactableId}', Tür: {interactableType}");
        OnInteracted?.Invoke(this);
        OnAnyInteracted?.Invoke(this);
    }

    private void FindPlayerCamera()
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

    private void OnGUI()
    {
        EnsureStyles();

        // 1. Highlight Marker (Görsel hedef göstergesi)
        if (highlightMarker && isInteractable && playerCam != null)
        {
            DrawHighlightMarker();
        }

        // 2. Alt-Orta Prompt Kutusu (Yalnızca şu an odaklanılan obje çizer)
        if (CurrentFocused == this && isInteractable && !string.IsNullOrEmpty(promptText))
        {
            DrawPromptBox();
        }
    }

    private void DrawHighlightMarker()
    {
        Vector3 markerWorldPos = transform.position + Vector3.up * markerHeightOffset;
        Vector3 camPos = playerCam.position;
        float dist = Vector3.Distance(camPos, markerWorldPos);

        // 18 metreden uzaktaysa çizme
        if (dist > 18f) return;

        Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(markerWorldPos) : Vector3.zero;
        if (screenPos.z <= 0.1f) return; // Kameranın arkasında

        // GUI koordinatlarına dönüştür (Y ekseni ters)
        float guiY = Screen.height - screenPos.y;
        float size = Mathf.Lerp(26f, 16f, dist / 18f);

        // Nabız animasyonu
        float pulse = Mathf.Sin(Time.time * 5f) * 2f;
        Rect markerRect = new Rect(screenPos.x - (size + pulse) / 2f, guiY - (size + pulse) / 2f, size + pulse, size + pulse);

        Color prevColor = GUI.color;
        GUI.color = new Color(1f, 0.75f, 0.15f, Mathf.Clamp01(1f - (dist / 22f)));
        GUI.Label(markerRect, "◆", markerStyle);
        GUI.color = prevColor;
    }

    private void DrawPromptBox()
    {
        float width = Mathf.Max(260f, promptText.Length * 12.5f + 40f);
        float height = 44f;
        float x = (Screen.width - width) / 2f;
        float y = Screen.height - 110f;

        Rect boxRect = new Rect(x, y, width, height);

        // Arka plan kutusu
        GUI.Box(boxRect, GUIContent.none, promptBoxStyle);

        // İç metin
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.alignment = TextAnchor.MiddleCenter;
        textStyle.fontSize = 17;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.normal.textColor = Color.white;

        GUI.Label(boxRect, promptText, textStyle);
    }

    private static void EnsureStyles()
    {
        if (boxBgTex == null)
        {
            boxBgTex = new Texture2D(1, 1);
            boxBgTex.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.12f, 0.90f));
            boxBgTex.Apply();
        }

        if (promptBoxStyle == null)
        {
            promptBoxStyle = new GUIStyle(GUI.skin.box);
            promptBoxStyle.normal.background = boxBgTex;
            promptBoxStyle.border = new RectOffset(4, 4, 4, 4);
        }

        if (markerStyle == null)
        {
            markerStyle = new GUIStyle();
            markerStyle.alignment = TextAnchor.MiddleCenter;
            markerStyle.fontSize = 22;
            markerStyle.fontStyle = FontStyle.Bold;
            markerStyle.normal.textColor = new Color(1f, 0.72f, 0.1f, 0.95f);
        }
    }
}
