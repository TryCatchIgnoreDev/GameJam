using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Sahnedeki tüm kritik hikaye objelerinin (Oyuncu masa başlangıcı, NPC konumları, Mutfak, Poster, Peçete,
/// Apartman Çıkış Kapısı -> Sokak Spawn Noktası ve Sokak Ortasındaki Market Prefabı) tek bir kök obje altından
/// kolayca görülmesini, elle taşınmasını veya tek tıkla kurulup silinmesini sağlayan Demo Yerleşim Yöneticisi.
/// </summary>
[ExecuteAlways]
public class StoryLayoutDemoController : MonoBehaviour
{
    public static StoryLayoutDemoController Instance { get; private set; }

    [Header("=== HIZLI TEK TIK İŞLEMLERİ (Inspector Checkbox) ===")]
    [Tooltip("İşaretlediğinizde tüm anchor konumlarını sahnedeki gerçek objelere anında uygular.")]
    public bool snapObjectsToAnchorsNow = false;

    [Tooltip("İşaretlediğinizde sahnede elle taşıdığınız gerçek objelerin konumlarını anchor'lara kaydeder.")]
    public bool saveObjectPositionsToAnchorsNow = false;

    [Header("=== 1. MASA BAŞI & NPC KONUMLARI ===")]
    [Tooltip("Oyuncunun (First Person Controller) oyun başında masanın yanında başlayacağı nokta.")]
    public Transform playerTableStartAnchor;

    [Tooltip("Arkadaş 1 (male01_2 - Sarı Tişörtlü) konumu.")]
    public Transform friendA1Anchor;

    [Tooltip("Arkadaş 2 (male02_2 - Mavi Tişörtlü) konumu.")]
    public Transform friendA2Anchor;

    [Header("=== 2. ALT KAT MUTFAK, POSTER & PEÇETE ===")]
    [Tooltip("Mutfak tezgahı ((Prb)KitchenModule) konumu.")]
    public Transform kitchenModuleAnchor;

    [Tooltip("Mutfak girişindeki Deprem Farkındalık Posteri konumu.")]
    public Transform posterWallAnchor;

    [Tooltip("Mutfak tezgahı üzerindeki Peçete konumu.")]
    public Transform napkinPropAnchor;

    [Header("=== 3. SOKAĞA ÇIKIŞ SPAWN & MARKET KONUMLARI (ELLE AYARLANABİLİR) ===")]
    [Tooltip("Apartmanın içindeki çıkış kapısı etkileşim noktası (Buna basınca oyuncu streetSpawnPointOutside noktasına ışınlanır).")]
    public Transform apartmentExitDoorInsideAnchor;

    [Tooltip("ÖNEMLİ: Apartmandan çıkınca oyuncunun SOKAKTA SPAWN OLACAĞI NOKTA! Bunu sahnede istediğin yere elle taşıyabilirsin.")]
    public Transform streetSpawnPointOutside;

    [Tooltip("ÖNEMLİ: Sokaktan tekrar apartmana/eve geri giriş kapısı tetikleyicisi.")]
    public Transform streetReturnDoorTriggerAnchor;

    [Tooltip("Sokaktan eve geri girince oyuncunun ev içinde doğacağı nokta (masa/salon yanı).")]
    public Transform houseReturnSpawnPointInside;

    [Tooltip("ÖNEMLİ: Sokağın ortasındaki Market Prefabı (MarketSystem). Doğrudan sahnede elle istediğin yere taşıyabilirsin.")]
    public Transform marketPrefabTransform;

    [Header("=== SAHNEDEKİ GERÇEK OBJE REFERANSLARI ===")]
    public Transform playerTransform;
    public Transform friendA1Transform;
    public Transform friendA2Transform;
    public GameObject spawnedKitchenModule;
    public GameObject spawnedPosterObject;
    public GameObject spawnedNapkinObject;
    public GameObject spawnedMarketSystem;
    public GameObject spawnedStreetReturnTrigger;

    [Header("=== GİZMO GÖRÜNÜM AYARLARI ===")]
    public bool showSceneGizmos = true;
    public float gizmoSphereRadius = 0.45f;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        Instance = this;
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            EnsureStoryManagerWired();
        }
    }

    /// <summary>
    /// Oyun başladığında (Play Mode) veya sahne geçişinde EvMarketStoryManager'ın var olduğundan
    /// ve tüm demo anchor/interactable referanslarının eksiksiz bağlandığından emin olur.
    /// </summary>
    public void EnsureStoryManagerWired()
    {
        EvMarketStoryManager mgr = EvMarketStoryManager.EnsureInstance();
        if (mgr == null) return;

        if (friendA1Transform != null && mgr.friendA1 == null)
            mgr.friendA1 = friendA1Transform.GetComponent<SpeechBubble>() ?? friendA1Transform.gameObject.AddComponent<SpeechBubble>();
        if (friendA2Transform != null && mgr.friendA2 == null)
            mgr.friendA2 = friendA2Transform.GetComponent<SpeechBubble>() ?? friendA2Transform.gameObject.AddComponent<SpeechBubble>();

        if (spawnedPosterObject != null && mgr.posterInteractable == null)
            mgr.posterInteractable = spawnedPosterObject.GetComponent<StoryInteractable>();
        if (spawnedNapkinObject != null && mgr.napkinInteractable == null)
            mgr.napkinInteractable = spawnedNapkinObject.GetComponent<StoryInteractable>();
        if (spawnedMarketSystem != null && mgr.marketEntranceInteractable == null)
            mgr.marketEntranceInteractable = spawnedMarketSystem.GetComponent<StoryInteractable>();
        if (spawnedStreetReturnTrigger != null && mgr.streetReturnDoorInteractable == null)
            mgr.streetReturnDoorInteractable = spawnedStreetReturnTrigger.GetComponent<StoryInteractable>();

        if (streetSpawnPointOutside != null && mgr.streetSpawnPointOutside == null)
            mgr.streetSpawnPointOutside = streetSpawnPointOutside;
        if (houseReturnSpawnPointInside != null && mgr.houseReturnSpawnPointInside == null)
            mgr.houseReturnSpawnPointInside = houseReturnSpawnPointInside;
    }

    private void Update()
    {
        if (Application.isPlaying && EvMarketStoryManager.Instance == null)
        {
            EnsureStoryManagerWired();
        }

        if (snapObjectsToAnchorsNow)
        {
            snapObjectsToAnchorsNow = false;
            ApplyAnchorsToSceneObjects();
        }

        if (saveObjectPositionsToAnchorsNow)
        {
            saveObjectPositionsToAnchorsNow = false;
            SyncAnchorsFromSceneObjects();
        }
    }

    /// <summary>
    /// Anchor noktalarındaki pozisyon ve rotasyonları sahnedeki gerçek objelere uygular.
    /// </summary>
    [ContextMenu("1. Anchor Konumlarını Sahnedeki Objelere Uygula (Snap)")]
    public void ApplyAnchorsToSceneObjects()
    {
        if (playerTransform != null && playerTableStartAnchor != null)
        {
            TeleportTransform(playerTransform, playerTableStartAnchor.position, playerTableStartAnchor.rotation);
        }

        if (friendA1Transform != null && friendA1Anchor != null)
        {
            friendA1Transform.position = friendA1Anchor.position;
            friendA1Transform.rotation = friendA1Anchor.rotation;
        }

        if (friendA2Transform != null && friendA2Anchor != null)
        {
            friendA2Transform.position = friendA2Anchor.position;
            friendA2Transform.rotation = friendA2Anchor.rotation;
        }

        if (spawnedKitchenModule != null && kitchenModuleAnchor != null)
        {
            spawnedKitchenModule.transform.position = kitchenModuleAnchor.position;
            spawnedKitchenModule.transform.rotation = kitchenModuleAnchor.rotation;
        }

        if (spawnedPosterObject != null && posterWallAnchor != null)
        {
            spawnedPosterObject.transform.position = posterWallAnchor.position;
            spawnedPosterObject.transform.rotation = posterWallAnchor.rotation;
        }

        if (spawnedNapkinObject != null && napkinPropAnchor != null)
        {
            spawnedNapkinObject.transform.position = napkinPropAnchor.position;
            spawnedNapkinObject.transform.rotation = napkinPropAnchor.rotation;
        }

        if (spawnedStreetReturnTrigger != null && streetReturnDoorTriggerAnchor != null)
        {
            spawnedStreetReturnTrigger.transform.position = streetReturnDoorTriggerAnchor.position;
            spawnedStreetReturnTrigger.transform.rotation = streetReturnDoorTriggerAnchor.rotation;
        }

        Debug.Log("[StoryLayoutDemoController] Tüm demo anchor konumları sahnedeki objelere uygulandı!");
    }

    /// <summary>
    /// Sahnede elle taşınan gerçek objelerin konumlarını anchor objelerine kaydeder.
    /// </summary>
    [ContextMenu("2. Elle Taşınan Obje Konumlarını Anchor'lara Kaydet")]
    public void SyncAnchorsFromSceneObjects()
    {
        if (playerTransform != null && playerTableStartAnchor != null)
        {
            playerTableStartAnchor.position = playerTransform.position;
            playerTableStartAnchor.rotation = playerTransform.rotation;
        }

        if (friendA1Transform != null && friendA1Anchor != null)
        {
            friendA1Anchor.position = friendA1Transform.position;
            friendA1Anchor.rotation = friendA1Transform.rotation;
        }

        if (friendA2Transform != null && friendA2Anchor != null)
        {
            friendA2Anchor.position = friendA2Transform.position;
            friendA2Anchor.rotation = friendA2Transform.rotation;
        }

        if (spawnedKitchenModule != null && kitchenModuleAnchor != null)
        {
            kitchenModuleAnchor.position = spawnedKitchenModule.transform.position;
            kitchenModuleAnchor.rotation = spawnedKitchenModule.transform.rotation;
        }

        if (spawnedPosterObject != null && posterWallAnchor != null)
        {
            posterWallAnchor.position = spawnedPosterObject.transform.position;
            posterWallAnchor.rotation = spawnedPosterObject.transform.rotation;
        }

        if (spawnedNapkinObject != null && napkinPropAnchor != null)
        {
            napkinPropAnchor.position = spawnedNapkinObject.transform.position;
            napkinPropAnchor.rotation = spawnedNapkinObject.transform.rotation;
        }

        Debug.Log("[StoryLayoutDemoController] Sahnedeki objelerin güncel konumları anchor'lara kaydedildi!");
    }

    /// <summary>
    /// Demo olarak oluşturulan mutfak, poster, peçete ve sokak geri dönüş tetikleyicisini tek tıkla siler.
    /// </summary>
    [ContextMenu("3. Oluşturulan Demo Objelerini Sil (Temizle)")]
    public void ClearSpawnedDemoObjects()
    {
        if (spawnedKitchenModule != null) DestroyImmediate(spawnedKitchenModule);
        if (spawnedPosterObject != null) DestroyImmediate(spawnedPosterObject);
        if (spawnedNapkinObject != null) DestroyImmediate(spawnedNapkinObject);
        if (spawnedStreetReturnTrigger != null) DestroyImmediate(spawnedStreetReturnTrigger);

        Debug.Log("[StoryLayoutDemoController] Oluşturulan demo objeleri temizlendi.");
    }

    public static void TeleportTransform(Transform target, Vector3 worldPos, Quaternion worldRot)
    {
        if (target == null) return;

        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = worldPos;
            rb.rotation = worldRot;
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        CharacterController cc = target.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        target.position = worldPos;
        target.rotation = worldRot;

        if (cc != null) cc.enabled = true;
    }

    private void OnDrawGizmos()
    {
        if (!showSceneGizmos) return;

        DrawAnchorGizmo(playerTableStartAnchor, new Color(0.1f, 0.9f, 1.0f, 0.9f), "OYUNCU BASLANGIC (Masa Basi)");
        DrawAnchorGizmo(friendA1Anchor, new Color(1.0f, 0.85f, 0.1f, 0.9f), "ARKADAS 1 (male01_2 - Sari)");
        DrawAnchorGizmo(friendA2Anchor, new Color(0.2f, 0.5f, 1.0f, 0.9f), "ARKADAS 2 (male02_2 - Mavi)");
        DrawAnchorGizmo(kitchenModuleAnchor, new Color(0.8f, 0.5f, 0.2f, 0.9f), "MUTFAK TEZGAHI");
        DrawAnchorGizmo(posterWallAnchor, new Color(1.0f, 0.25f, 0.25f, 0.9f), "DEPREM POSTERI (Duvar)");
        DrawAnchorGizmo(napkinPropAnchor, new Color(0.95f, 0.95f, 0.95f, 0.9f), "MUTFAK PECETE");
        DrawAnchorGizmo(apartmentExitDoorInsideAnchor, new Color(1.0f, 0.55f, 0.0f, 0.9f), "IC CIKIS KAPISI -> Sokaga Isinlar");
        DrawAnchorGizmo(streetSpawnPointOutside, new Color(0.1f, 1.0f, 0.25f, 0.95f), "★ SOKAK SPAWN NOKTASI (Elle Tasi!) ★", 0.85f);
        DrawAnchorGizmo(streetReturnDoorTriggerAnchor, new Color(0.2f, 0.9f, 0.6f, 0.9f), "★ SOKAKTAN EVE GIRIS KAPISI (Elle Tasi!) ★", 0.7f);
        DrawAnchorGizmo(marketPrefabTransform, new Color(1.0f, 0.2f, 0.85f, 0.95f), "★ MARKET PREFAB (Elle Tasi!) ★", 1.1f);
    }

    private void DrawAnchorGizmo(Transform anchor, Color col, string label, float scaleMult = 1f)
    {
        if (anchor == null) return;

        Gizmos.color = col;
        float r = gizmoSphereRadius * scaleMult;
        Gizmos.DrawWireSphere(anchor.position, r);
        Gizmos.DrawSphere(anchor.position, r * 0.35f);

        // Yön oku
        Vector3 fwd = anchor.forward * (r * 2.5f);
        Gizmos.DrawLine(anchor.position, anchor.position + fwd);
        Gizmos.DrawSphere(anchor.position + fwd, r * 0.2f);

#if UNITY_EDITOR
        GUIStyle style = new GUIStyle();
        style.normal.textColor = col;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        Handles.Label(anchor.position + Vector3.up * (r + 0.35f), label, style);
#endif
    }
}
