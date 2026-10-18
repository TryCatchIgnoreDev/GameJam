using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// EvMarket sahnesindeki tüm hikaye akışını, diyalogları, kamera efektlerini,
/// geri sayım sayacını ve alternatif zaman döngüsü (Time Loop) senaryolarını yöneten ana yönetici.
/// </summary>
public class EvMarketStoryManager : MonoBehaviour
{
    public static EvMarketStoryManager Instance { get; private set; }
    public static bool IsPosterCinematicActive { get; private set; } = false;

    public static EvMarketStoryManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        Instance = FindFirstObjectByType<EvMarketStoryManager>();
        if (Instance != null) return Instance;

        if (StoryLayoutDemoController.Instance != null)
        {
            Instance = StoryLayoutDemoController.Instance.gameObject.GetComponent<EvMarketStoryManager>();
            if (Instance == null)
            {
                Instance = StoryLayoutDemoController.Instance.gameObject.AddComponent<EvMarketStoryManager>();
            }
            return Instance;
        }

        GameObject go = new GameObject("EvMarketStoryManager");
        Instance = go.AddComponent<EvMarketStoryManager>();
        return Instance;
    }

    /// <summary>
    /// Hikaye aşamaları.
    /// </summary>
    public enum StoryStage
    {
        Stage0_WakeUpCoffeeSpill = 0,            // Kahve dökme uyanışı, Saat 20:00
        Stage1_GoToKitchenPoster = 1,            // Mutfağa iniş ve postere ilk bakış -> Saat 21:01 flashback -> Saat 20:00
        Stage2_TakeNapkin = 2,                   // Mutfaktan peçeteyi alma
        Stage3_TalkToFriendsUpstairs = 3,        // Arkadaşların yanına dönüş: İkizler burcu / Merkür gerilemesi & Maç için Kola-Cips isteği, merdivenden inerken müteahhit konuşması
        Stage4_SecondPosterLook = 4,             // Aşağı inerken postere 2. bakış: "O gün binlerce insan bana bi şey olmaz diye uyudu" -> 90 sn sayaç başlar!
        Stage5_MarketShopping = 5,               // Markete gidip Tetris çantasını hazırlama
        Stage6_ReturnHomeBefore2100 = 6,         // Süre bitmeden eve dönüş
        Stage7_EarthquakeAt2100_DirectFriends = 7,// Saat 21:00 depremi: Arkadaşları yönlendirme [Pencere vs Masa] + Masanın yanına Çök-Kapan-Tutun
        Stage8_UnderRubble = 8,                  // Enkaz altında uyanış: Fener yakma [F] ve Düdük çalma [E]
        Stage9_FinalRescueAndAwareness = 9       // AFAD kurtarışı, kameranın yıkık şehre uzaklaşması ve final farkındalık mesajı
    }

    [Header("--- Hikaye Durumu ---")]
    [Tooltip("Mevcut hikaye aşaması.")]
    public StoryStage currentStage = StoryStage.Stage0_WakeUpCoffeeSpill;

    [Tooltip("Otomatik hikaye başlasın mı?")]
    public bool autoStart = true;

    [Header("--- Diyalog Modu & Seslendirme ---")]
    [Tooltip("Yeni seslendirmeli diyalog sistemini kullanır. Kapatılırsa anında eski orijinal diyaloglara geri döner.")]
    public bool useNewDialoguesWithVoice = true;

    [Tooltip("Geri sayım süresi (saniye) - Varsayılan 90 sn (1.5 dakika)")]
    public float countdownDuration = 90f;

    // NOT: Oyunda GÖSTERİLEN isimler -> Arkadaş 1 = "Can", Arkadaş 2 = "Emre".
    // Kod, değişken adları ve yorumlar "Arkadaş 1/2" olarak bırakıldı ki eşleştirme unutulmasın.
    [Header("--- Sahne ve Karakter Referansları ---")]
    [Tooltip("Arkadaş 1 (male01_2) üzerindeki SpeechBubble bileşeni.")]
    public SpeechBubble friendA1;

    [Tooltip("Arkadaş 2 (male02_2) üzerindeki SpeechBubble bileşeni.")]
    public SpeechBubble friendA2;

    [Tooltip("Duvardaki saat (ClockController).")]
    public ClockController wallClock;

    [Tooltip("Deprem Farkındalık Posteri etkileşim objesi.")]
    public StoryInteractable posterInteractable;

    [Tooltip("Mutfaktaki peçete etkileşim objesi.")]
    public StoryInteractable napkinInteractable;

    [Tooltip("Deprem anında sığınılacak sağlam masa etkileşim objesi.")]
    public StoryInteractable safeTableInteractable;

    [Tooltip("Ev çıkış / giriş kapısı etkileşim objesi (İçerideki kapı).")]
    public StoryInteractable homeEntranceInteractable;

    [Tooltip("Alt kattaki fiziksel ahşap kapı modeli (whinedoor) etkileşim objesi.")]
    public StoryInteractable downstairsDoorMeshInteractable;

    [Tooltip("Sokaktan apartmana/eve geri giriş kapısı etkileşim objesi.")]
    public StoryInteractable streetReturnDoorInteractable;

    [Tooltip("Sokaktaki Market etkileşim objesi.")]
    public StoryInteractable marketEntranceInteractable;

    [Header("--- Tetikleyici & Işınlanma (Spawn) Noktaları ---")]
    [Tooltip("Apartmandan çıkınca oyuncunun SOKAKTA doğacağı nokta (Elle ayarlanabilir).")]
    public Transform streetSpawnPointOutside;

    [Tooltip("Sokaktan eve geri girince oyuncunun EV İÇİNDE doğacağı nokta.")]
    public Transform houseReturnSpawnPointInside;

    [Tooltip("Peçeteyi aldıktan sonra arkadaşların yanına dönüşü algılayan tetikleyici/hedef nokta.")]
    public Transform friendsRoomTriggerPoint;

    [Tooltip("Merdivenden inerken müteahhit konuşmasını tetikleyen nokta.")]
    public Transform stairsTriggerPoint;

    [Tooltip("2. kez enkazda kaldıktan sonra geçilecek 3. sahnenin adı.")]
    public string nextSceneName = "End_UnderRubble";

    [Tooltip("Final sinematik kamerasının bakacağı/gideceği tepe nokta (Opsiyonel).")]
    public Transform finalCameraViewPoint;

    [Header("--- Ses Efektleri (Opsiyonel) ---")]
    public AudioClip coffeeSpillClip;
    public AudioClip phoneRingClip;
    public AudioClip whistleSoundClip;
    public AudioClip sirenClip;
    public AudioSource audioSource;

    // Çalışma anı değişkenleri
    private Coroutine activeStoryRoutine = null;
    private bool isRestartingLoop = false;
    private bool hasInteractedWithSafeTable = false;
    private bool hasListenedStairsDialogue = false;
    private bool isPlayerOutsideOnStreet = false;
    private bool hasUsedApartmentExitDoor = false;
    private bool hasReturnedHomeInTime = false;
    private Transform playerTransform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }
        Instance = this;

        if (autoStart)
        {
            ScreenFaderUI.EnsureInstance()?.SetBlackImmediate(true);
        }
    }

    private void Start()
    {
        CrosshairUI.EnsureInstance();

        FindPlayer();
        AutoFindAndSetupReferences();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Dinamik etkileşim dinleyicilerini bağla
        StoryInteractable.OnAnyInteracted -= HandleAnyStoryInteraction;
        StoryInteractable.OnAnyInteracted += HandleAnyStoryInteraction;

        // Market tamamlanma dinleyicisi
        if (GameJam.Market.MarketUIManager.Instance != null)
        {
            GameJam.Market.MarketUIManager.Instance.OnShoppingCompleted -= HandleMarketShoppingCompleted;
            GameJam.Market.MarketUIManager.Instance.OnShoppingCompleted += HandleMarketShoppingCompleted;
        }

        // Sokağa açılan apartman kapı trigger'ı başlangıçta (Diyalogda dışarı çık kısmı gelene kadar) kapalı tutulur
        if (currentStage < StoryStage.Stage3_TalkToFriendsUpstairs)
        {
            SetHomeEntranceTriggerActive(false);
        }

        ResetCrouchPose();

        if (autoStart && activeStoryRoutine == null)
        {
            StartStage(currentStage);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        StoryInteractable.OnAnyInteracted -= HandleAnyStoryInteraction;
        if (GameJam.Market.MarketUIManager.Instance != null)
        {
            GameJam.Market.MarketUIManager.Instance.OnShoppingCompleted -= HandleMarketShoppingCompleted;
        }
    }

    private void Update()
    {
        // Oyuncu konumuna göre tetikleyici kontrolleri
        CheckProximityTriggers();

        // Hile tuşları kontrolü (F5 - F9)
        HandleDebugKeyboardCheats();
    }

    #region --- Dinamik Referans Kurulumu ---

    private void FindPlayer()
    {
        FirstPersonMovement fpm = FindFirstObjectByType<FirstPersonMovement>();
        if (fpm != null)
        {
            playerTransform = fpm.transform;
            return;
        }

        if (Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }
    }

    private void AutoFindAndSetupReferences()
    {
        // 1. Arkadaş 1 (male01_2)
        if (friendA1 == null || friendA1.transform.parent != null)
        {
            GameObject a1 = GameObject.Find("/male01_2") ?? GameObject.Find("male01_2") ?? GameObject.Find("male01");
            if (a1 != null)
            {
                if (a1.transform.parent != null && a1.transform.root != null && a1.transform.root.name.StartsWith("male01"))
                {
                    a1 = a1.transform.root.gameObject;
                }
                friendA1 = a1.GetComponent<SpeechBubble>() ?? a1.AddComponent<SpeechBubble>();
            }
        }

        if (friendA1 != null)
        {
            var rc1 = friendA1.GetComponent<NPCRunController>() ?? friendA1.gameObject.AddComponent<NPCRunController>();
            rc1.moveForwardWhenRunning = true;
            rc1.runMoveSpeed = 3.8f;
            rc1.rotationSpeed = 12.0f;
            rc1.SetEscapeKeyframes(
                new Vector3(7.5f, friendA1.transform.position.y, -6.5f),
                new Vector3(10.5f, friendA1.transform.position.y, -13.5f)
            );
        }

        // 2. Arkadaş 2 (male02_2) - Saate yakın olan arkadaş
        if (friendA2 == null || friendA2.transform.parent != null)
        {
            GameObject a2 = GameObject.Find("/male02_2") ?? GameObject.Find("male02_2") ?? GameObject.Find("male02");
            if (a2 != null)
            {
                if (a2.transform.parent != null && a2.transform.root != null && a2.transform.root.name.StartsWith("male02"))
                {
                    a2 = a2.transform.root.gameObject;
                }
                friendA2 = a2.GetComponent<SpeechBubble>() ?? a2.AddComponent<SpeechBubble>();
            }
        }

        if (friendA2 != null)
        {
            var rc2 = friendA2.GetComponent<NPCRunController>() ?? friendA2.gameObject.AddComponent<NPCRunController>();
            rc2.moveForwardWhenRunning = true;
            rc2.runMoveSpeed = 4.0f;
            rc2.rotationSpeed = 12.0f;
            rc2.SetEscapeKeyframes(
                new Vector3(5.5f, friendA2.transform.position.y, -9.5f),
                new Vector3(10.5f, friendA2.transform.position.y, -14.5f)
            );
        }

        // 3. Duvar Saati ((Prb)Clock)
        if (wallClock == null)
        {
            wallClock = FindFirstObjectByType<ClockController>();
            if (wallClock == null)
            {
                GameObject clockObj = GameObject.Find("(Prb)Clock") ?? GameObject.Find("Clock");
                if (clockObj != null) wallClock = clockObj.GetComponent<ClockController>() ?? clockObj.AddComponent<ClockController>();
            }
        }

        // 0. Öncelikli olarak [TRIGGER_PALETI_AL_KOY] içindeki veya sahnedeki numaralandırılmış objeleri ara
        GameObject pSpawn = GameObject.Find("01_Oyuncu_Uyanis_Spawn") ?? GameObject.Find("1_Oyuncu_Masa_Baslangic");
        if (pSpawn != null) houseReturnSpawnPointInside = pSpawn.transform;

        GameObject pPoster = GameObject.Find("02_Trigger_Mutfak_Posteri") ?? GameObject.Find("Earthquake_Awareness_Poster");
        if (pPoster != null) posterInteractable = pPoster.GetComponent<StoryInteractable>();

        GameObject pNapkin = GameObject.Find("03_Trigger_Mutfak_Pecetesi") ?? GameObject.Find("Kitchen_Napkin_Prop");
        if (pNapkin != null) napkinInteractable = pNapkin.GetComponent<StoryInteractable>();

        GameObject pFriendsTrig = GameObject.Find("04_AreaTrigger_Arkadaslar_Odasi") ?? GameObject.Find("Trigger_Arkadaslar_Odasi") ?? GameObject.Find("Trigger_FriendsRoom");
        if (pFriendsTrig != null) friendsRoomTriggerPoint = pFriendsTrig.transform;

        GameObject pStairsTrig = GameObject.Find("05_AreaTrigger_Merdiven_Muteahhit") ?? GameObject.Find("Trigger_Merdiven_Muteahhit_Konusmasi") ?? GameObject.Find("Trigger_Stairs");
        if (pStairsTrig != null) stairsTriggerPoint = pStairsTrig.transform;

        GameObject pExitDoor = GameObject.Find("06_Trigger_Apartman_Cikis_Kapisi");
        if (pExitDoor == null)
        {
            GameObject parent = GameObject.Find("[TRIGGER_PALETI_AL_KOY]") ?? GameObject.Find("/[TRIGGER_PALETI_AL_KOY]");
            if (parent != null)
            {
                Transform ch = parent.transform.Find("06_Trigger_Apartman_Cikis_Kapisi");
                if (ch != null) pExitDoor = ch.gameObject;
            }
        }
        if (pExitDoor == null)
        {
            StoryInteractable[] allInteractables = Resources.FindObjectsOfTypeAll<StoryInteractable>();
            foreach (var item in allInteractables)
            {
                if (item.gameObject.name == "06_Trigger_Apartman_Cikis_Kapisi" || item.interactableId == "apartment_exit_door")
                {
                    pExitDoor = item.gameObject;
                    break;
                }
            }
        }
        if (pExitDoor != null) homeEntranceInteractable = pExitDoor.GetComponent<StoryInteractable>();

        GameObject pStreetSpawn = GameObject.Find("07_Sokak_Spawn_Noktasi") ?? GameObject.Find("8_[ELLE_AYARLA]_Sokak_Spawn_Noktasi");
        if (pStreetSpawn != null) streetSpawnPointOutside = pStreetSpawn.transform;

        GameObject pMarket = GameObject.Find("08_Trigger_Market_Giris_Tetris") ?? GameObject.Find("[ELLE_AYARLA]_MarketSystem_Sokakta");
        if (pMarket != null) marketEntranceInteractable = pMarket.GetComponent<StoryInteractable>();

        GameObject pStreetDoor = GameObject.Find("09_Trigger_Sokaktan_Eve_Giris_Kapisi");
        if (pStreetDoor == null)
        {
            GameObject parent = GameObject.Find("[TRIGGER_PALETI_AL_KOY]") ?? GameObject.Find("/[TRIGGER_PALETI_AL_KOY]");
            if (parent != null)
            {
                Transform ch = parent.transform.Find("09_Trigger_Sokaktan_Eve_Giris_Kapisi");
                if (ch != null) pStreetDoor = ch.gameObject;
            }
        }
        if (pStreetDoor == null)
        {
            pStreetDoor = GameObject.Find("small Door frame.008");
        }
        if (pStreetDoor != null) streetReturnDoorInteractable = pStreetDoor.GetComponent<StoryInteractable>();

        GameObject pReturnSpawn = GameObject.Find("10_Eve_Geri_Donus_Spawn_Noktasi");
        if (pReturnSpawn == null)
        {
            GameObject parent = GameObject.Find("[TRIGGER_PALETI_AL_KOY]") ?? GameObject.Find("/[TRIGGER_PALETI_AL_KOY]");
            if (parent != null)
            {
                Transform ch = parent.transform.Find("10_Eve_Geri_Donus_Spawn_Noktasi");
                if (ch != null) pReturnSpawn = ch.gameObject;
            }
        }
        if (pReturnSpawn != null) houseReturnSpawnPointInside = pReturnSpawn.transform;

        GameObject pTable = GameObject.Find("11_Trigger_Saglam_Masa_CokKapanTutun");
        if (pTable != null) safeTableInteractable = pTable.GetComponent<StoryInteractable>();

        GameObject pOuttroCam = GameObject.Find("12_Final_Outtro_Kamera_Noktasi") ?? GameObject.Find("Final_Outtro_Kamera_Noktasi");
        if (pOuttroCam != null) finalCameraViewPoint = pOuttroCam.transform;

        // Sahnedeki bilinen ana objeleri tespit et (masa, kupa, merdiven, kapı)
        GameObject tableObj = GameObject.Find("chestnut plate") ?? GameObject.Find("rose wood  grande") ?? GameObject.Find("Table") ?? GameObject.Find("Masa");
        GameObject mugObj = GameObject.Find("(Prb)Mug") ?? GameObject.Find("Mug");
        GameObject stairsObj = GameObject.Find("rose wood tall stairs") ?? GameObject.Find("Stairs") ?? GameObject.Find("stairs");
        
        // Alt kattaki gerçek bina çıkış kapısını bul (Y < -70f)
        GameObject downstairsExitDoor = null;
        if (pExitDoor != null)
        {
            downstairsExitDoor = pExitDoor;
        }
        else
        {
            GameObject[] allSceneObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allSceneObjects)
            {
                if (go.name == "whinedoor" && go.transform.position.y < -70f)
                {
                    downstairsExitDoor = go;
                    break;
                }
            }
            if (downstairsExitDoor == null)
            {
                downstairsExitDoor = GameObject.Find("whinedoor") ?? GameObject.Find("Door") ?? GameObject.Find("Entrance");
            }
        }

        // 4. Sağlam Masa (SafeTable) Etkileşimi
        if (safeTableInteractable == null)
        {
            StoryInteractable found = FindInteractableByType(InteractableType.SafeTable);
            if (found != null)
            {
                safeTableInteractable = found;
            }
            else if (tableObj != null)
            {
                safeTableInteractable = tableObj.GetComponent<StoryInteractable>() ?? tableObj.AddComponent<StoryInteractable>();
                safeTableInteractable.interactableType = InteractableType.SafeTable;
                safeTableInteractable.promptText = "[E] Masanın Yanında ÇÖK - KAPAN - TUTUN!";
                safeTableInteractable.interactDistance = 4.0f;
                safeTableInteractable.requireFacing = false;
                safeTableInteractable.isInteractable = false;
                safeTableInteractable.highlightMarker = false;
            }
        }
        if (safeTableInteractable != null)
        {
            safeTableInteractable.requireFacing = false;
            safeTableInteractable.interactDistance = 4.0f;
        }

        // 5. Kapı / Bina Çıkış Kapısı (İçeriden Sokağa Çıkış)
        if (homeEntranceInteractable == null && downstairsExitDoor != null)
        {
            // Çakışan DoorInteract bileşeni varsa devre dışı bırak
            var di = downstairsExitDoor.GetComponent<DoorInteract>();
            if (di != null)
            {
                di.enabled = false;
                di.showPrompt = false;
            }

            homeEntranceInteractable = downstairsExitDoor.GetComponent<StoryInteractable>() ?? downstairsExitDoor.AddComponent<StoryInteractable>();
            homeEntranceInteractable.interactableType = InteractableType.HomeEntrance;
            homeEntranceInteractable.interactableId = "apartment_exit_door";
            homeEntranceInteractable.promptText = "[E] Sokağa Çık (Markete Git)";
            homeEntranceInteractable.interactDistance = 4.5f;
            homeEntranceInteractable.requireFacing = false;
            homeEntranceInteractable.isInteractable = false;
            homeEntranceInteractable.highlightMarker = false;
        }

        // Alt kattaki fiziksel ahşap kapı modelini (whinedoor Y < -70f) tespit et ve bağla
        GameObject downstairsDoorMesh = null;
        var doors = UnityEngine.Object.FindObjectsByType<DoorInteract>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var d in doors)
        {
            if (d.gameObject.name == "whinedoor" && d.transform.position.y < -70f)
            {
                downstairsDoorMesh = d.gameObject;
                break;
            }
        }
        if (downstairsDoorMesh == null)
        {
            var direct = GameObject.Find("whinedoor");
            if (direct != null && direct.transform.position.y < -70f) downstairsDoorMesh = direct;
        }
        if (downstairsDoorMesh != null)
        {
            var di = downstairsDoorMesh.GetComponent<DoorInteract>();
            if (di != null)
            {
                di.enabled = false;
                di.showPrompt = false;
            }

            downstairsDoorMeshInteractable = downstairsDoorMesh.GetComponent<StoryInteractable>() ?? downstairsDoorMesh.AddComponent<StoryInteractable>();
            downstairsDoorMeshInteractable.interactableType = InteractableType.HomeEntrance;
            downstairsDoorMeshInteractable.interactableId = "apartment_exit_door";
            downstairsDoorMeshInteractable.promptText = "[E] Sokağa Çık (Markete Git)";
            downstairsDoorMeshInteractable.interactDistance = 4.5f;
            downstairsDoorMeshInteractable.requireFacing = false;
            downstairsDoorMeshInteractable.isInteractable = false;
            downstairsDoorMeshInteractable.highlightMarker = false;

            Collider meshCol = downstairsDoorMesh.GetComponent<Collider>();
            if (meshCol != null && homeEntranceInteractable != null)
            {
                homeEntranceInteractable.linkedColliders = new Collider[] { meshCol };
                Collider trigCol = homeEntranceInteractable.GetComponent<Collider>();
                if (trigCol != null)
                {
                    downstairsDoorMeshInteractable.linkedColliders = new Collider[] { trigCol };
                }
            }
        }

        // 5b. Sokaktan Apartmana/Eve Geri Giriş Kapısı (small Door frame.008)
        if (streetReturnDoorInteractable == null)
        {
            EnsureStreetReturnDoorReference();
        }
        if (streetReturnDoorInteractable != null)
        {
            streetReturnDoorInteractable.interactableType = InteractableType.HomeEntrance;
            streetReturnDoorInteractable.interactableId = "street_return_door";
            streetReturnDoorInteractable.promptText = "[E] Apartmana / Eve Gir";
            streetReturnDoorInteractable.interactDistance = 4.5f;
            streetReturnDoorInteractable.requireFacing = false;
            streetReturnDoorInteractable.isInteractable = true;
            streetReturnDoorInteractable.highlightMarker = true;
        }

        // 6. Deprem Posteri (Poster)
        if (posterInteractable == null)
        {
            StoryInteractable found = FindInteractableByType(InteractableType.Poster);
            if (found != null)
            {
                posterInteractable = found;
            }
            else
            {
                Vector3 posterPos;
                Quaternion posterRot;
                if (downstairsExitDoor != null)
                {
                    posterPos = downstairsExitDoor.transform.position + downstairsExitDoor.transform.forward * 1.5f + Vector3.up * 1.4f;
                    posterRot = downstairsExitDoor.transform.rotation;
                }
                else if (stairsObj != null)
                {
                    posterPos = stairsObj.transform.position + Vector3.back * 2.5f + Vector3.up * 1.5f;
                    posterRot = Quaternion.identity;
                }
                else
                {
                    posterPos = new Vector3(5f, 1.5f, 2f);
                    posterRot = Quaternion.identity;
                }

                GameObject pObj = PosterGenerator.CreatePosterObject(posterPos, posterRot);
                posterInteractable = pObj.GetComponent<StoryInteractable>();
            }
        }

        if (posterInteractable != null)
        {
            PosterGenerator.EnsurePosterSetup(posterInteractable.gameObject);
            posterInteractable.promptText = "[E] Deprem Posterini İncele";
            posterInteractable.isInteractable = true;
        }


        // 7. Peçete (Napkin)
        if (napkinInteractable == null)
        {
            StoryInteractable found = FindInteractableByType(InteractableType.Napkin);
            if (found != null)
            {
                napkinInteractable = found;
            }
            else
            {
                GameObject napkinObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                napkinObj.name = "Kitchen_Napkin_Prop";
                Vector3 nPos;
                if (tableObj != null)
                {
                    nPos = tableObj.transform.position + Vector3.up * 0.82f + tableObj.transform.forward * 0.5f;
                }
                else if (mugObj != null)
                {
                    nPos = mugObj.transform.position + Vector3.right * 0.35f;
                }
                else
                {
                    nPos = new Vector3(5.5f, 1.0f, 1.5f);
                }
                napkinObj.transform.position = nPos;
                napkinObj.transform.localScale = new Vector3(0.22f, 0.05f, 0.22f);

                napkinInteractable = napkinObj.AddComponent<StoryInteractable>();
                napkinInteractable.interactableType = InteractableType.Napkin;
                napkinInteractable.promptText = "[E] Peçeteyi Al";
                napkinInteractable.interactDistance = 3.0f;
                napkinInteractable.isInteractable = false;
                napkinInteractable.highlightMarker = false;
            }
        }

        // 8. Tetikleyici Noktalar
        if (friendsRoomTriggerPoint == null)
        {
            GameObject trg = new GameObject("Trigger_FriendsRoom");
            Vector3 pos = (friendA1 != null) ? friendA1.transform.position : (tableObj != null ? tableObj.transform.position : Vector3.zero);
            trg.transform.position = pos;
            friendsRoomTriggerPoint = trg.transform;
        }

        if (stairsTriggerPoint == null)
        {
            GameObject trg = new GameObject("Trigger_Stairs");
            Vector3 pos = (friendsRoomTriggerPoint != null)
                ? new Vector3(10.10f, friendsRoomTriggerPoint.position.y - 1.2f, -15.5f)
                : new Vector3(10.10f, -71.2f, -15.5f);
            trg.transform.position = pos;
            stairsTriggerPoint = trg.transform;
        }
    }

    /// <summary>
    /// StoryAreaTrigger tarafından fiziksel temas anında tetiklenir
    /// </summary>
    public void OnAreaTriggerEnter(GameJam.Story.StoryAreaTrigger.AreaTriggerType triggerType)
    {
        if (triggerType == GameJam.Story.StoryAreaTrigger.AreaTriggerType.FriendsRoom)
        {
            if (currentStage == StoryStage.Stage3_TalkToFriendsUpstairs && activeStoryRoutine == null)
            {
                activeStoryRoutine = StartCoroutine(Stage3Routine());
            }
        }
        else if (triggerType == GameJam.Story.StoryAreaTrigger.AreaTriggerType.StairsContractor)
        {
            if (currentStage == StoryStage.Stage3_TalkToFriendsUpstairs)
            {
                hasListenedStairsDialogue = true;
            }
        }
        else if (triggerType == GameJam.Story.StoryAreaTrigger.AreaTriggerType.StreetReturnDoor)
        {
            // KAPIYA DOKUNMA İLE OTOMATİK IŞINLANMA YOK!
            // Sadece [E] tuşuna basılınca geçiş yapılır (CheckProximityTriggers / StoryInteractable).
            if (currentStage >= StoryStage.Stage6_ReturnHomeBefore2100)
            {
                ScreenFaderUI.EnsureInstance()?.ShowSubtitle("Görev", "[E] tuşuna basarak apartmana / eve gir.", 3.0f);
            }
            else
            {
                ScreenFaderUI.EnsureInstance()?.ShowSubtitle("Mert (Sen)", "Önce markete gidip deprem çantasını hazırlamalıyım!", 3.0f);
            }
        }
    }

    private StoryInteractable FindInteractableByType(InteractableType type)
    {
        StoryInteractable[] all = FindObjectsByType<StoryInteractable>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].interactableType == type) return all[i];
        }
        return null;
    }

    #endregion

    #region --- Tetikleyici Mesafe Kontrolleri ---

    private void CheckProximityTriggers()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        // Oyuncunun sokakta olup olmadığını Y pozisyonuna göre de senkronize et (Sokakta Y > -20, Evde Y < -50)
        if (playerTransform.position.y > -20f)
        {
            isPlayerOutsideOnStreet = true;
        }
        else if (playerTransform.position.y < -50f && !hasUsedApartmentExitDoor)
        {
            isPlayerOutsideOnStreet = false;
        }

        // Stage 3: Peçeteyi aldıktan sonra arkadaşların yanına dönüş
        if (currentStage == StoryStage.Stage3_TalkToFriendsUpstairs)
        {
            if (friendsRoomTriggerPoint != null && Vector3.Distance(playerTransform.position, friendsRoomTriggerPoint.position) <= 5.5f)
            {
                // Konuşmayı tetikle (eğer henüz başlamadıysa)
                if (activeStoryRoutine == null)
                {
                    activeStoryRoutine = StartCoroutine(Stage3Routine());
                }
            }
        }

        // Stage 6: Süre bitmeden eve dönüş kontrolü (Eve girdikten sonra odadaki masanın yanına varış)
        if (currentStage == StoryStage.Stage6_ReturnHomeBefore2100)
        {
            if (!isPlayerOutsideOnStreet)
            {
                bool isInsideHouse = false;
                if (friendsRoomTriggerPoint != null && Vector3.Distance(playerTransform.position, friendsRoomTriggerPoint.position) <= 6.0f)
                {
                    isInsideHouse = true;
                }
                if (isInsideHouse)
                {
                    MarkReturnedHomeInStage6();
                }
            }
        }

        // Sokağa açılan apartman çıkış kapısı kontrolü (Stage 4 poster bakışı sonrası veya Stage 5)
        if (!hasUsedApartmentExitDoor && !isPlayerOutsideOnStreet && currentStage >= StoryStage.Stage4_SecondPosterLook && currentStage <= StoryStage.Stage5_MarketShopping)
        {
            EnsureHomeEntranceReference();
            if (homeEntranceInteractable != null && homeEntranceInteractable.gameObject.activeInHierarchy && homeEntranceInteractable.isInteractable)
            {
                float distXZ = Vector2.Distance(new Vector2(playerTransform.position.x, playerTransform.position.z), new Vector2(homeEntranceInteractable.transform.position.x, homeEntranceInteractable.transform.position.z));
                float dist3D = Vector3.Distance(playerTransform.position, homeEntranceInteractable.transform.position);
                bool isEPressed = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) isEPressed = true;
                if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) isEPressed = true;
#endif
                try { if (Input.GetKeyDown(KeyCode.E)) isEPressed = true; } catch { }

                // Sadece [E] (veya gamepad batı) basılınca ışınlan. Kapıya dokunmak ışınlamaz!
                if (isEPressed && (dist3D <= 4.5f || distXZ <= 4.5f))
                {
                    TeleportPlayerToStreet();
                }
            }
        }

        // Sokaktan apartmana geri giriş kontrolü
        if (isPlayerOutsideOnStreet)
        {
            EnsureStreetReturnDoorReference();
            if (streetReturnDoorInteractable != null && streetReturnDoorInteractable.gameObject.activeInHierarchy)
            {
                float distXZ = Vector2.Distance(new Vector2(playerTransform.position.x, playerTransform.position.z), new Vector2(streetReturnDoorInteractable.transform.position.x, streetReturnDoorInteractable.transform.position.z));
                bool isEPressed = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) isEPressed = true;
                if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) isEPressed = true;
#endif
                try { if (Input.GetKeyDown(KeyCode.E)) isEPressed = true; } catch { }

                if (distXZ <= 4.5f && isEPressed)
                {
                    if (currentStage >= StoryStage.Stage6_ReturnHomeBefore2100)
                    {
                        TeleportPlayerBackInsideHouse();
                        MarkReturnedHomeInStage6();
                    }
                    else
                    {
                        ScreenFaderUI.EnsureInstance()?.ShowSubtitle("Mert (Sen)", "Önce markete gidip deprem çantasını hazırlamalıyım!", 3.0f);
                    }
                }
            }
        }

        // Stage 7: Masanın yanına çömelme veya E tuşuyla çök-kapan-tutun kontrolü
        if (currentStage == StoryStage.Stage7_EarthquakeAt2100_DirectFriends)
        {
            if (safeTableInteractable != null && Vector3.Distance(playerTransform.position, safeTableInteractable.transform.position) <= 3.5f)
            {
                var crouch = playerTransform.GetComponent<Crouch>() ?? playerTransform.GetComponentInChildren<Crouch>();
                bool isEPressed = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) isEPressed = true;
                if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) isEPressed = true;
#else
                if (Input.GetKeyDown(KeyCode.E)) isEPressed = true;
#endif
                if (isEPressed || (crouch != null && crouch.IsCrouched))
                {
                    hasInteractedWithSafeTable = true;
                    ApplyCokKapanTutunPose();
                }
            }
        }
    }

    private void HandleAnyStoryInteraction(StoryInteractable interactable)
    {
        if (interactable == null) return;

        // Stage 1: Postere ilk bakış
        if (currentStage == StoryStage.Stage1_GoToKitchenPoster && interactable.interactableType == InteractableType.Poster)
        {
            StartCoroutine(FirstPosterInteractionRoutine());
        }
        // Stage 2: Peçeteyi alma
        else if (currentStage == StoryStage.Stage2_TakeNapkin && interactable.interactableType == InteractableType.Napkin)
        {
            StartCoroutine(NapkinTakenRoutine());
        }
        // Stage 4: Postere 2. kez bakış
        else if (currentStage == StoryStage.Stage4_SecondPosterLook && interactable.interactableType == InteractableType.Poster)
        {
            if (activeStoryRoutine != null)
            {
                StopCoroutine(activeStoryRoutine);
                activeStoryRoutine = null;
            }
            currentStage = StoryStage.Stage4_SecondPosterLook;
            StartCoroutine(SecondPosterInteractionRoutine());
        }
        // Kapıdan sokağa çıkış veya sokaktan eve geri giriş
        else if (interactable.interactableType == InteractableType.HomeEntrance ||
                 interactable.interactableId == "apartment_exit_door" ||
                 interactable.interactableId == "street_return_door" ||
                 interactable == homeEntranceInteractable ||
                 interactable == streetReturnDoorInteractable)
        {
            bool isOutside = isPlayerOutsideOnStreet || (playerTransform != null && playerTransform.position.y > -20f);

            if (!isOutside && !hasUsedApartmentExitDoor)
            {
                // Evin içindeyken kapıya bakıp [E] ile basarsa sokağa ışınla! 1 kez kullan ve anında disable et!
                TeleportPlayerToStreet();
            }
            else if (isOutside)
            {
                if (currentStage >= StoryStage.Stage6_ReturnHomeBefore2100)
                {
                    // Sokaktayken giriş kapısına bakıp [E] ile basarsa doğrudan eve ışınla ve
                    // geri sayım bitince depremi başlat.
                    TeleportPlayerBackInsideHouse();
                    MarkReturnedHomeInStage6();
                }
                else
                {
                    ScreenFaderUI.EnsureInstance()?.ShowSubtitle("Mert (Sen)", "Önce markete gidip deprem çantasını hazırlamalıyım!", 3.0f);
                }
            }
        }
        // Sokaktaki Market objesiyle etkileşim
        else if (interactable.interactableType == InteractableType.MarketEntrance)
        {
            OpenMarketUI();
        }
        // Stage 7: Masanın yanına çök-kapan-tutun
        else if (currentStage == StoryStage.Stage7_EarthquakeAt2100_DirectFriends && interactable.interactableType == InteractableType.SafeTable)
        {
            hasInteractedWithSafeTable = true;
            ApplyCokKapanTutunPose();
        }
    }

    /// <summary>
    /// Çök-Kapan-Tutun pozunu uygular: Karakterin boyu yarıya iner (1.8m -> 0.9m)
    /// ve kamera göz hizası yarıya iner (~1.488m -> 0.74m).
    /// Hareketi yavaşlatıp güvenli kapanma pozisyonuna sokar.
    /// </summary>
    public void ApplyCokKapanTutunPose()
    {
        if (playerTransform == null) FindPlayer();
        if (playerTransform == null) return;

        Debug.Log("[EvMarketStoryManager] Çök-Kapan-Tutun pozisyonu uygulanıyor: Karakter boyu ve kamera yarıya indiriliyor.");

        // 1. Crouch bileşeni üzerinden zorla çömelme emri ver (Kamera yarı yüksekliğe insin: 0.74m)
        Crouch crouch = playerTransform.GetComponent<Crouch>() ?? playerTransform.GetComponentInChildren<Crouch>();
        if (crouch != null)
        {
            crouch.SetForceCrouch(true, 0.74f);
        }

        // 2. Kapsül collider yüksekliğini de doğrudan yarıya indir (1.8m -> 0.9m)
        CapsuleCollider col = playerTransform.GetComponent<CapsuleCollider>() ?? playerTransform.GetComponentInChildren<CapsuleCollider>();
        if (col != null)
        {
            col.height = 0.9f;
            col.center = new Vector3(col.center.x, 0.45f, col.center.z);
        }

        // 3. Kamera yerel yüksekliğini de doğrudan garantiye al
        Camera cam = playerTransform.GetComponentInChildren<Camera>();
        if (cam != null && crouch == null)
        {
            cam.transform.localPosition = new Vector3(cam.transform.localPosition.x, 0.74f, cam.transform.localPosition.z);
        }

        // 4. Masanın yanındayken ve deprem anında aşırı hızlı koşmayı engelle
        FirstPersonMovement fpm = playerTransform.GetComponent<FirstPersonMovement>() ?? playerTransform.GetComponentInChildren<FirstPersonMovement>();
        if (fpm != null)
        {
            fpm.canRun = false;
            fpm.speed = 1.0f;
        }
    }

    /// <summary>
    /// Çömelme pozunu sıfırlayıp karakteri normal boyuna (1.8m) ve normal kamera yüksekliğine (1.488m) döndürür.
    /// </summary>
    public void ResetCrouchPose()
    {
        if (playerTransform == null) FindPlayer();
        if (playerTransform == null) return;

        Debug.Log("[EvMarketStoryManager] Çökme pozisyonu sıfırlanıyor (Normal boya dönülüyor).");

        Crouch crouch = playerTransform.GetComponent<Crouch>() ?? playerTransform.GetComponentInChildren<Crouch>();
        if (crouch != null)
        {
            crouch.SetForceCrouch(false);
        }

        CapsuleCollider col = playerTransform.GetComponent<CapsuleCollider>() ?? playerTransform.GetComponentInChildren<CapsuleCollider>();
        if (col != null)
        {
            col.height = 1.8f;
            col.center = new Vector3(col.center.x, 0.9f, col.center.z);
        }

        Camera cam = playerTransform.GetComponentInChildren<Camera>();
        if (cam != null && crouch == null)
        {
            cam.transform.localPosition = new Vector3(cam.transform.localPosition.x, 1.488f, cam.transform.localPosition.z);
        }

        FirstPersonMovement fpm = playerTransform.GetComponent<FirstPersonMovement>() ?? playerTransform.GetComponentInChildren<FirstPersonMovement>();
        if (fpm != null)
        {
            fpm.canRun = true;
            fpm.speed = 5.0f;
        }
    }

    private void TeleportPlayerToStreet()
    {
        // Apartman çıkış kapısı dışarı çıkarken 1 kez kullanılır ve anında disable edilir!
        hasUsedApartmentExitDoor = true;
        SetHomeEntranceTriggerActive(false);

        if (playerTransform == null) FindPlayer();

        if (streetSpawnPointOutside == null)
        {
            GameObject sp = GameObject.Find("8_[ELLE_AYARLA]_Sokak_Spawn_Noktasi");
            if (sp != null) streetSpawnPointOutside = sp.transform;
            else if (StoryLayoutDemoController.Instance != null && StoryLayoutDemoController.Instance.streetSpawnPointOutside != null)
                streetSpawnPointOutside = StoryLayoutDemoController.Instance.streetSpawnPointOutside;
        }

        Vector3 targetPos = (streetSpawnPointOutside != null) ? streetSpawnPointOutside.position : new Vector3(-14.50f, 1.15f, -60.00f);
        Quaternion targetRot = (streetSpawnPointOutside != null) ? streetSpawnPointOutside.rotation : Quaternion.identity;

        if (playerTransform != null)
        {
            StoryLayoutDemoController.TeleportTransform(playerTransform, targetPos, targetRot);
            isPlayerOutsideOnStreet = true;
            Debug.Log("[EvMarketStoryManager] Oyuncu apartman kapısından sokağa spawn edildi: " + targetPos);
        }

        if (currentStage < StoryStage.Stage5_MarketShopping)
        {
            StartStage(StoryStage.Stage5_MarketShopping);
            if (!ScreenFaderUI.EnsureInstance().IsCountdownRunning)
            {
                ScreenFaderUI.EnsureInstance().StartCountdown(countdownDuration, OnCountdownExpired);
            }
        }

        if (homeEntranceInteractable != null)
        {
            homeEntranceInteractable.highlightMarker = false;
        }

        if (downstairsDoorMeshInteractable != null)
        {
            downstairsDoorMeshInteractable.highlightMarker = false;
            downstairsDoorMeshInteractable.isInteractable = false;
        }

        if (marketEntranceInteractable != null)
        {
            marketEntranceInteractable.isInteractable = true;
            marketEntranceInteractable.highlightMarker = true;
            marketEntranceInteractable.SetPrompt("[E] Markete Gir (Deprem Çantasını Hazırla)", true);
        }

        EnsureStreetReturnDoorReference();
        if (streetReturnDoorInteractable != null)
        {
            streetReturnDoorInteractable.gameObject.SetActive(true);
            streetReturnDoorInteractable.isInteractable = true;
            streetReturnDoorInteractable.highlightMarker = true;
            streetReturnDoorInteractable.requireFacing = false;
            streetReturnDoorInteractable.interactDistance = 4.5f;
            streetReturnDoorInteractable.SetPrompt("[E] Apartmana / Eve Gir", true);
        }

        ScreenFaderUI.EnsureInstance()?.ShowSubtitle("Görev", "Sokağa çıktın! Hemen sokağın ortasındaki Market noktasına koş!", 4.0f);
    }

    private void TeleportPlayerBackInsideHouse()
    {
        if (playerTransform == null) FindPlayer();

        if (houseReturnSpawnPointInside == null)
        {
            GameObject sp = GameObject.Find("10_Eve_Geri_Donus_Spawn_Noktasi");
            if (sp == null)
            {
                GameObject parent = GameObject.Find("[TRIGGER_PALETI_AL_KOY]") ?? GameObject.Find("/[TRIGGER_PALETI_AL_KOY]");
                if (parent != null)
                {
                    Transform ch = parent.transform.Find("10_Eve_Geri_Donus_Spawn_Noktasi");
                    if (ch != null) sp = ch.gameObject;
                }
            }
            if (sp != null) houseReturnSpawnPointInside = sp.transform;
            else if (friendsRoomTriggerPoint != null) houseReturnSpawnPointInside = friendsRoomTriggerPoint;
        }

        Vector3 returnPos = (houseReturnSpawnPointInside != null) ? houseReturnSpawnPointInside.position : new Vector3(5.64f, -75.17f, 0.13f);
        Quaternion returnRot = (houseReturnSpawnPointInside != null) ? houseReturnSpawnPointInside.rotation : Quaternion.Euler(0f, 90f, 0f);

        if (playerTransform != null)
        {
            StoryLayoutDemoController.TeleportTransform(playerTransform, returnPos, returnRot);
            isPlayerOutsideOnStreet = false;
            Debug.Log("[EvMarketStoryManager] Oyuncu sokaktan eve (girişe) ışınlandı: " + returnPos);
        }

        if (streetReturnDoorInteractable != null)
        {
            streetReturnDoorInteractable.highlightMarker = false;
        }
    }

    /// <summary>
    /// Oyuncu Stage 6'da (süre bitmeden) eve döndüğünde çağrılır.
    /// Depremi HEMEN başlatmaz; geri sayım dolduğunda (OnCountdownExpired) başlatılmasını sağlar.
    /// </summary>
    private void MarkReturnedHomeInStage6()
    {
        if (currentStage != StoryStage.Stage6_ReturnHomeBefore2100) return;
        if (hasReturnedHomeInTime) return;

        hasReturnedHomeInTime = true;

        ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();

        // Güvenlik: herhangi bir sebeple sayaç çalışmıyorsa (hile / sahne atlama) oyun kilitlenmesin, hemen başlat.
        if (fader == null || !fader.IsCountdownRunning || fader.RemainingTime <= 0.05f)
        {
            StartStage(StoryStage.Stage7_EarthquakeAt2100_DirectFriends);
            return;
        }

        fader.ShowSubtitle("Görev", "Eve yetiştin! Saat 21:00 olana kadar hazırlan ve güvenli bir yerde bekle...", 4.0f);
    }

    private void HandleMarketShoppingCompleted()
    {
        if (currentStage == StoryStage.Stage5_MarketShopping)
        {
            StartStage(StoryStage.Stage6_ReturnHomeBefore2100);
        }
    }

    /// <summary>
    /// Sokağa açılan apartman çıkış kapısı trigger'ının referansını garantiye alır.
    /// Obje pasif olsa bile parent ([TRIGGER_PALETI_AL_KOY]) üzerinden veya sahnedeki objelerden bulur.
    /// </summary>
    public StoryInteractable EnsureHomeEntranceReference()
    {
        if (homeEntranceInteractable != null) return homeEntranceInteractable;

        GameObject exitObj = GameObject.Find("06_Trigger_Apartman_Cikis_Kapisi");
        if (exitObj == null)
        {
            GameObject parent = GameObject.Find("[TRIGGER_PALETI_AL_KOY]") ?? GameObject.Find("/[TRIGGER_PALETI_AL_KOY]");
            if (parent != null)
            {
                Transform child = parent.transform.Find("06_Trigger_Apartman_Cikis_Kapisi");
                if (child != null) exitObj = child.gameObject;
            }
        }
        if (exitObj == null)
        {
            var allSceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allSceneObjects)
            {
                if ((go.name == "06_Trigger_Apartman_Cikis_Kapisi") && go.scene.isLoaded)
                {
                    exitObj = go;
                    break;
                }
            }
        }

        if (exitObj != null)
        {
            homeEntranceInteractable = exitObj.GetComponent<StoryInteractable>() ?? exitObj.AddComponent<StoryInteractable>();
            homeEntranceInteractable.interactableType = InteractableType.HomeEntrance;
            homeEntranceInteractable.interactableId = "apartment_exit_door";
            homeEntranceInteractable.promptText = "[E] Apartmandan Sokağa Çık (Markete Git)";
            homeEntranceInteractable.interactDistance = 4.5f;
            homeEntranceInteractable.requireFacing = false;
        }

        return homeEntranceInteractable;
    }

    /// <summary>
    /// Sokaktan apartmana geri giriş kapısı trigger'ının referansını garantiye alır.
    /// Obje pasif olsa bile parent ([TRIGGER_PALETI_AL_KOY]) üzerinden veya sahnedeki objelerden bulur.
    /// </summary>
    public StoryInteractable EnsureStreetReturnDoorReference()
    {
        if (streetReturnDoorInteractable != null) return streetReturnDoorInteractable;

        GameObject returnObj = GameObject.Find("09_Trigger_Sokaktan_Eve_Giris_Kapisi");
        if (returnObj == null)
        {
            GameObject parent = GameObject.Find("[TRIGGER_PALETI_AL_KOY]") ?? GameObject.Find("/[TRIGGER_PALETI_AL_KOY]");
            if (parent != null)
            {
                Transform child = parent.transform.Find("09_Trigger_Sokaktan_Eve_Giris_Kapisi");
                if (child != null) returnObj = child.gameObject;
            }
        }
        if (returnObj == null)
        {
            var allSceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allSceneObjects)
            {
                if (go.name == "09_Trigger_Sokaktan_Eve_Giris_Kapisi" && go.scene.isLoaded)
                {
                    returnObj = go;
                    break;
                }
            }
        }
        if (returnObj == null)
        {
            returnObj = GameObject.Find("small Door frame.008");
        }

        if (returnObj != null)
        {
            streetReturnDoorInteractable = returnObj.GetComponent<StoryInteractable>() ?? returnObj.AddComponent<StoryInteractable>();
            streetReturnDoorInteractable.interactableType = InteractableType.HomeEntrance;
            streetReturnDoorInteractable.interactableId = "street_return_door";
            streetReturnDoorInteractable.promptText = "[E] Apartmana / Eve Gir";
            streetReturnDoorInteractable.interactDistance = 4.5f;
            streetReturnDoorInteractable.requireFacing = false;
            streetReturnDoorInteractable.isInteractable = true;
            streetReturnDoorInteractable.highlightMarker = true;
        }

        return streetReturnDoorInteractable;
    }

    /// <summary>
    /// Sokağa açılan apartman çıkış kapısı trigger'ını (06_Trigger_Apartman_Cikis_Kapisi) aktif veya pasif yapar.
    /// Dışarı çıkış için 1 kez kullanıldıktan sonra tekrar aktif edilemez.
    /// </summary>
    public void SetHomeEntranceTriggerActive(bool active)
    {
        EnsureHomeEntranceReference();

        // Eğer dışarı çıkış için 1 kez kullanıldıysa, tekrar aktif edilmesine izin verme!
        if (active && hasUsedApartmentExitDoor)
        {
            active = false;
        }

        if (homeEntranceInteractable != null)
        {
            homeEntranceInteractable.gameObject.SetActive(active);
            homeEntranceInteractable.isInteractable = active;
            homeEntranceInteractable.highlightMarker = active;
            homeEntranceInteractable.requireFacing = false;
            homeEntranceInteractable.interactDistance = 4.5f;
            if (active)
            {
                homeEntranceInteractable.SetPrompt("[E] Apartmandan Sokağa Çık (Markete Git)", true);
            }
            else
            {
                if (StoryInteractable.CurrentFocused == homeEntranceInteractable)
                {
                    homeEntranceInteractable.SetPrompt("", false);
                }
            }

            Collider col = homeEntranceInteractable.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = active;
                col.isTrigger = true;
            }

            Debug.Log($"[EvMarketStoryManager] Sokağa açılan kapı triggeri: {(active ? "AKTİF EDİLDİ" : "PASİF (GİZLİ/DEVRE DIŞI)")}");
        }
        else
        {
            Debug.LogWarning("[EvMarketStoryManager] SetHomeEntranceTriggerActive: 06_Trigger_Apartman_Cikis_Kapisi bulunamadı!");
        }

        if (downstairsDoorMeshInteractable != null)
        {
            downstairsDoorMeshInteractable.isInteractable = active;
            downstairsDoorMeshInteractable.highlightMarker = active;
            downstairsDoorMeshInteractable.requireFacing = false;
            downstairsDoorMeshInteractable.interactDistance = 4.5f;
            if (active)
            {
                downstairsDoorMeshInteractable.SetPrompt("[E] Sokağa Çık (Markete Git)", true);
            }
            else
            {
                if (StoryInteractable.CurrentFocused == downstairsDoorMeshInteractable)
                {
                    downstairsDoorMeshInteractable.SetPrompt("", false);
                }
            }
        }
    }

    #endregion

    #region --- Hikaye Aşamaları Akışı ---

    public void StartStage(StoryStage stage)
    {
        currentStage = stage;
        if (activeStoryRoutine != null)
        {
            StopCoroutine(activeStoryRoutine);
            activeStoryRoutine = null;
        }

        Debug.Log($"[EvMarketStoryManager] AŞAMA BAŞLADI: {stage}");

        switch (stage)
        {
            case StoryStage.Stage0_WakeUpCoffeeSpill:
                activeStoryRoutine = StartCoroutine(Stage0Routine());
                break;
            case StoryStage.Stage1_GoToKitchenPoster:
                activeStoryRoutine = StartCoroutine(Stage1SetupRoutine());
                break;
            case StoryStage.Stage2_TakeNapkin:
                activeStoryRoutine = StartCoroutine(Stage2SetupRoutine());
                break;
            case StoryStage.Stage3_TalkToFriendsUpstairs:
                // Oyuncu yukarı çıkınca tetiklenecek
                activeStoryRoutine = null;
                break;
            case StoryStage.Stage4_SecondPosterLook:
                activeStoryRoutine = StartCoroutine(Stage4SetupRoutine());
                break;
            case StoryStage.Stage5_MarketShopping:
                activeStoryRoutine = StartCoroutine(Stage5Routine());
                break;
            case StoryStage.Stage6_ReturnHomeBefore2100:
                activeStoryRoutine = StartCoroutine(Stage6Routine());
                break;
            case StoryStage.Stage7_EarthquakeAt2100_DirectFriends:
                activeStoryRoutine = StartCoroutine(Stage7Routine());
                break;
            case StoryStage.Stage8_UnderRubble:
                activeStoryRoutine = StartCoroutine(Stage8Routine());
                break;
            case StoryStage.Stage9_FinalRescueAndAwareness:
                activeStoryRoutine = StartCoroutine(Stage9Routine());
                break;
        }
    }

    // --- STAGE 0: Kahve Dökme Uyanışı (Saat 20:00) ---
    private IEnumerator Stage0Routine()
    {
        SetHomeEntranceTriggerActive(false);

        if (wallClock != null) wallClock.SetDigitalTime(20, 0, 0);

        ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();
        fader.SetBlackImmediate(true);
        yield return new WaitForSeconds(0.8f);

        fader.FadeFromBlack(1.6f);
        yield return new WaitForSeconds(1.2f);

        PlayAudio(coffeeSpillClip, 0.8f);

        if (useNewDialoguesWithVoice)
        {
            // 1. Arkadaş 2: "Oha kanka iyi misin? Resmen kahveyi üzerine fırlattın." {7.mp3}
            Speak(friendA2, "Emre", "Oha kanka iyi misin? Resmen kahveyi üzerine fırlattın.", 3.4f, "7");
            yield return new WaitForSeconds(3.6f);

            // 2. Mert (Sen): "Ben... Bi an dalmışım. Üstümü silip geliyorum." {8.mp3}
            Speak(null, "Mert (Sen)", "Ben... Bi an dalmışım. Üstümü silip geliyorum.", 3.0f, "8");
            yield return new WaitForSeconds(3.2f);

            // 3. Arkadaş 2: "Peçete mutfakta bi de Mert mutfak girişindeki poster biraz eskimedi mi bi baksana" {9.mp3}
            Speak(friendA2, "Emre", "Peçete mutfakta bi de Mert mutfak girişindeki poster biraz eskimedi mi bi baksana", 4.2f, "9");
            yield return new WaitForSeconds(4.4f);

            fader.ShowSubtitle("Görev", "Aşağı mutfağa ilerle (Mutfak girişindeki deprem posterine bak).", 4.0f);
        }
        else
        {
            // ESKİ ORİJİNAL DİYALOGLAR
            Speak(friendA2, "Emre", "Mert iyi misin? Az önce kahveni üzerine döktün!", 3.2f);
            yield return new WaitForSeconds(3.4f);

            fader.ShowSubtitle("Mert (Sen)", "Bi an daldım... Üstümü silip geleyim.", 2.8f);
            yield return new WaitForSeconds(3.0f);

            fader.ShowSubtitle("Görev", "Aşağı mutfağa ilerle (Mutfak girişindeki deprem posterine bak).", 4.0f);
        }

        StartStage(StoryStage.Stage1_GoToKitchenPoster);
    }

    // --- STAGE 1: Mutfağa İniş ve Postere İlk Bakış ---
    private IEnumerator Stage1SetupRoutine()
    {
        SetHomeEntranceTriggerActive(false);

        if (posterInteractable != null)
        {
            posterInteractable.isInteractable = true;
            posterInteractable.highlightMarker = true;
            posterInteractable.SetPrompt("[E] Deprem Posterini İncele", true);
        }
        yield return null;
    }

    private IEnumerator FirstPosterInteractionRoutine()
    {
        if (posterInteractable != null)
        {
            posterInteractable.highlightMarker = false;
            posterInteractable.isInteractable = false;
        }

        IsPosterCinematicActive = true;
        CameraEffectsController camFx = CameraEffectsController.EnsureInstance();

        // 1. ve 2. bakıştaki şiddetli ekran sarsıntıları (shake) tamamen kaldırıldı!
        // Sarsıntı yerine postere doğru yumuşak, temiz bir normal yakınlaştırma (smooth zoom-in FOV/kamera hareketi)
        Transform posterTr = posterInteractable != null ? posterInteractable.transform : transform;
        yield return camFx.FocusOnPosterRoutine(posterTr, focusOnBottomRight: false, duration: 0f);

        // Flashback anı: Saat 21:01
        if (wallClock != null) wallClock.SetDigitalTime(21, 1, 0);

        ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();
        // Flashback anında siren sesi ({siren})
        PlayAudio(sirenClip, 0.7f);
        fader.ShowCenterMessage("SAAT 21:01", "(Aklına yıkılan ev ve bağrışmalar gelir...)", 2.4f);
        yield return new WaitForSeconds(2.5f);

        fader.ShowSubtitle("Mert (Sen)", "Merdiven.. eğilme.. kapanma.. pencere...", 3.0f);
        yield return new WaitForSeconds(3.2f);

        // Saat normale (20:00) döner
        if (wallClock != null) wallClock.SetDigitalTime(20, 0, 0);

        if (useNewDialoguesWithVoice)
        {
            // 7. Mert: "(Saate bakar: 20:00) Ya gerçekleşirse? Anneannem de böyle derdi bana, bu posteri o göndermişti..." {10.mp3}
            Speak(null, "Mert (Sen)", "(Saate bakar: 20:00) Ya gerçekleşirse? Anneannem de böyle derdi bana, bu posteri o göndermişti...", 4.2f, "10");
            yield return new WaitForSeconds(4.4f);
        }
        else
        {
            fader.ShowSubtitle("Mert (Sen)", "(Saate bakar: 20:00) Ya gerçekleşirse? Anneannem de böyle derdi bana, bu posteri o göndermişti...", 4.0f);
            yield return new WaitForSeconds(4.0f);
        }

        // Kamerayı yumuşakça orijinal görüşe döndür
        yield return camFx.ReturnFromPosterFocusRoutine(0.8f);
        IsPosterCinematicActive = false;

        StartStage(StoryStage.Stage2_TakeNapkin);
    }


    // --- STAGE 2: Peçeteyi Alma ---
    private IEnumerator Stage2SetupRoutine()
    {
        SetHomeEntranceTriggerActive(false);

        if (napkinInteractable != null)
        {
            napkinInteractable.gameObject.SetActive(true);
            napkinInteractable.isInteractable = true;
            napkinInteractable.highlightMarker = true;
            napkinInteractable.SetPrompt("[E] Peçeteyi Al", true);
        }
        ScreenFaderUI.EnsureInstance()?.ShowSubtitle("Görev", "Mutfaktan peçeteyi al ve arkadaşlarının yanına git.", 3.5f);
        yield return null;
    }

    private IEnumerator NapkinTakenRoutine()
    {
        if (napkinInteractable != null)
        {
            napkinInteractable.highlightMarker = false;
            napkinInteractable.isInteractable = false;
            napkinInteractable.gameObject.SetActive(false);
        }

        ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();
        if (useNewDialoguesWithVoice)
        {
            // 10. Mert: "Peçeteyi aldım. Arkadaşlarımın yanına gidip onları uyarmalıyım." {11.mp3}
            Speak(null, "Mert (Sen)", "Peçeteyi aldım. Arkadaşlarımın yanına gidip onları uyarmalıyım.", 3.2f, "11");
            yield return new WaitForSeconds(3.2f);
        }
        else
        {
            fader.ShowSubtitle("Mert (Sen)", "Peçeteyi aldım. Arkadaşların yanına gidip onları uyarmalıyım.", 2.8f);
            yield return new WaitForSeconds(2.8f);
        }

        fader.ShowSubtitle("Görev", "Yukarı çık ve arkadaşlarının yanına git.", 3.5f);
        StartStage(StoryStage.Stage3_TalkToFriendsUpstairs);
    }

    // --- STAGE 3: Arkadaşların Yanına Dönüş & Müteahhit Konuşması ---
    private IEnumerator Stage3Routine()
    {
        // Konuşma öncesi kapı triggerinin hala kapalı olduğundan emin ol
        SetHomeEntranceTriggerActive(false);

        ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();

        if (useNewDialoguesWithVoice)
        {
            // 12. Mert: "İçimde kötü bi his var, sanki deprem çok yakın... Parkta mı otursak?" {12.mp3}
            Speak(null, "Mert (Sen)", "İçimde kötü bi his var, sanki deprem çok yakın... Parkta mı otursak?", 3.8f, "12");
            yield return new WaitForSeconds(4.0f);

            // 13. Arkadaş 1: "Oğlum senin yükselenin İkizler değil mi?..." {13.mp3}
            Speak(friendA1, "Can", "Oğlum senin yükselenin İkizler değil mi? Sabah Twitter'da gördüm, Merkür gerilemen varmış aşırı paranoyaklık yaparmış, dinlen bence.", 5.5f, "13");
            yield return new WaitForSeconds(5.7f);

            // 14. Arkadaş 2: "Kanka birazdan maç başlayacak..." {14.mp3}
            Speak(friendA2, "Emre", "Kanka birazdan maç başlayacak, dışarı çıkıp kola ve cips alsana hem biraz hava alırsın.", 4.2f, "14");
            yield return new WaitForSeconds(4.4f);

            // 15. Mert: "Tamam..." {15.mp3}
            Speak(null, "Mert (Sen)", "Tamam...", 2.0f, "15");
            yield return new WaitForSeconds(2.2f);
        }
        else
        {
            fader.ShowSubtitle("Mert (Sen)", "İçimde kötü bi his var, sanki deprem çok yakın... Parkta mı otursak?", 3.6f);
            yield return new WaitForSeconds(3.8f);

            Speak(friendA1, "Can", "Oğlum senin yükselenin İkizler değil mi? Sabah Twitter'da gördüm, Merkür gerilemen varmış aşırı paranoyaklık yaparmış, dinlen bence.", 4.8f);
            yield return new WaitForSeconds(5.0f);

            Speak(friendA2, "Emre", "Kanka birazdan maç başlayacak, dışarı çıkıp kola ve cips alsana hem biraz hava alırsın.", 3.8f);
            yield return new WaitForSeconds(4.0f);

            fader.ShowSubtitle("Mert (Sen)", "Tamam...", 2.0f);
            yield return new WaitForSeconds(2.2f);
        }

        // Postere 2. kez bakılana kadar apartman kapısı kapalı tutulur
        SetHomeEntranceTriggerActive(false);

        fader.ShowSubtitle("Görev", "Merdivenlerden aşağı in ve deprem posterine tekrar bak.", 3.5f);

        // Posteri şimdiden etkileşime hazırla ki oyuncu hızlıca aşağı inerse beklemeden bakabilsin
        if (posterInteractable != null)
        {
            posterInteractable.gameObject.SetActive(true);
            posterInteractable.isInteractable = true;
            posterInteractable.highlightMarker = true;
            posterInteractable.SetPrompt("[E] Postere Tekrar Bak", true);
        }

        // Merdiven tetikleyicisi kontrolü: odadan koridora/merdivene çıktığı veya aşağı inmeye başladığı an tetiklenir
        hasListenedStairsDialogue = false;
        while (!hasListenedStairsDialogue)
        {
            if (playerTransform != null)
            {
                bool nearStairs = stairsTriggerPoint != null && Vector3.Distance(playerTransform.position, stairsTriggerPoint.position) <= 5.5f;
                bool leftFriendsRoom = friendsRoomTriggerPoint != null && Vector3.Distance(playerTransform.position, friendsRoomTriggerPoint.position) >= 5.2f;
                bool steppedDownStairs = friendsRoomTriggerPoint != null && playerTransform.position.y < (friendsRoomTriggerPoint.position.y - 0.5f);

                if (nearStairs || leftFriendsRoom || steppedDownStairs)
                {
                    hasListenedStairsDialogue = true;
                    break;
                }
            }
            yield return null;
        }

        // Merdivenden inerken arkadan 2 NPC konuşur
        if (useNewDialoguesWithVoice)
        {
            // 17. Arkadaş 1: "Müteahhit dedi zaten..." {16.mp3}
            Speak(friendA1, "Can", "Müteahhit dedi zaten bu bina 9'a bile dayanır test etmişler! Hem binada oğlu oturuyor, yoksa oturtur muydu burda?", 5.0f, "16");
            yield return new WaitForSeconds(5.2f);

            // 18. Arkadaş 2: "Aynen kanka bence de." {17.mp3}
            Speak(friendA2, "Emre", "Aynen kanka bence de.", 2.5f, "17");
            yield return new WaitForSeconds(2.6f);
        }
        else
        {
            Speak(friendA1, "Can", "Müteahhit dedi zaten bu bina 9'a bile dayanır test etmişler! Hem binada oğlu oturuyor, yoksa oturtur muydu burda?", 4.8f);
            yield return new WaitForSeconds(5.0f);

            Speak(friendA2, "Emre", "Aynen kanka bence de.", 2.5f);
            yield return new WaitForSeconds(2.6f);
        }

        StartStage(StoryStage.Stage4_SecondPosterLook);
    }

    // --- STAGE 4: Postere 2. Bakış & 90 Saniye Sayacının Başlaması ---
    private IEnumerator Stage4SetupRoutine()
    {
        // Postere 2. kez bakılana kadar kapı kapalı tutulur
        SetHomeEntranceTriggerActive(false);
        if (posterInteractable != null)
        {
            posterInteractable.gameObject.SetActive(true);
            posterInteractable.isInteractable = true;
            posterInteractable.highlightMarker = true;
            posterInteractable.SetPrompt("[E] Postere Tekrar Bak", true);
        }
        ScreenFaderUI.EnsureInstance()?.ShowSubtitle("Görev", "Aşağı in ve deprem posterine tekrar bak.", 4.0f);
        yield return null;
    }

    private IEnumerator SecondPosterInteractionRoutine()
    {
        if (posterInteractable != null)
        {
            posterInteractable.highlightMarker = false;
            posterInteractable.isInteractable = false;
        }

        IsPosterCinematicActive = true;
        CameraEffectsController camFx = CameraEffectsController.EnsureInstance();

        // 1. ve 2. bakıştaki şiddetli ekran sarsıntıları (shake) tamamen kaldırıldı!
        // 2. kez postere bakıldığında posterin sağ altındaki yazıya ("O gün binlerce insan...") EKSTRA ODAKLANAN ZOOM / DETAY KADRAJI
        Transform posterTr = posterInteractable != null ? posterInteractable.transform : transform;
        yield return camFx.FocusOnPosterRoutine(posterTr, focusOnBottomRight: true, duration: 0f);

        ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();
        fader.ShowCenterMessage("DEPREM FARKINDALIK POSTERİ", "\"O gün binlerce insan bana bi şey olmaz diye uyudu.\"", 3.6f);
        yield return new WaitForSeconds(3.8f);

        if (useNewDialoguesWithVoice)
        {
            // 22. Mert: "O ana az kaldı, ya gerçekse harekete geçmeliyim!" {18.mp3}
            Speak(null, "Mert (Sen)", "O ana az kaldı, ya gerçekse harekete geçmeliyim!", 2.8f, "18");
            yield return new WaitForSeconds(2.8f);

            // Kamerayı yumuşakça orijinal görüşe döndür
            yield return camFx.ReturnFromPosterFocusRoutine(0.8f);
            IsPosterCinematicActive = false;

            // 90 SANİYE (1.5 DK) SAYACI BAŞLAR!
            fader.StartCountdown(countdownDuration, OnCountdownExpired);

            // 23. Mert: "Markete gitmeliyim ve düdük, su, fener, konserve, ilk yardım kiti, battaniye almalıyım..." {19.mp3}
            Speak(null, "Mert (Sen)", "Markete gitmeliyim ve düdük, su, fener, konserve, ilk yardım kiti, battaniye almalıyım. Hızlı olmalıyım az kaldı!", 4.8f, "19");
        }
        else
        {
            fader.ShowSubtitle("Mert (Sen)", "O ana az kaldı, ya gerçekse harekete geçmeliyim!", 2.8f);
            yield return new WaitForSeconds(2.8f);

            // Kamerayı yumuşakça orijinal görüşe döndür
            yield return camFx.ReturnFromPosterFocusRoutine(0.8f);
            IsPosterCinematicActive = false;

            // 90 SANİYE (1.5 DK) SAYACI BAŞLAR!
            fader.StartCountdown(countdownDuration, OnCountdownExpired);

            fader.ShowSubtitle("Mert (Sen)", "Markete gitmeliyim ve düdük, su, fener, konserve, ilk yardım kiti, battaniye almalıyım. Hızlı olmalıyım az kaldı!", 4.5f);
        }

        SetHomeEntranceTriggerActive(true);

        StartStage(StoryStage.Stage5_MarketShopping);
    }


    // --- STAGE 5: Markette Tetris Çantasını Hazırlama ---
    private IEnumerator Stage5Routine()
    {
        SetHomeEntranceTriggerActive(true);
        ScreenFaderUI.EnsureInstance()?.ShowSubtitle("Görev", "Apartman çıkış kapısından sokağa çık, Markete koş ve çantayı hazırla!", 4.0f);
        yield return null;
    }

    public void OpenMarketUI()
    {
        if (GameJam.Market.MarketUIManager.Instance != null)
        {
            GameJam.Market.MarketUIManager.Instance.OpenMarket();
        }
        else
        {
            // Eğer sahne test ediliyorsa ve MarketUIManager yoksa doğrudan tamamla
            Debug.LogWarning("[EvMarketStoryManager] MarketUIManager bulunamadı, alışveriş tamamlandı sayılıyor.");
            HandleMarketShoppingCompleted();
        }
    }

    // --- STAGE 6: Süre Bitmeden Eve Dönüş ---
    private IEnumerator Stage6Routine()
    {
        hasReturnedHomeInTime = false;

        ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();
        if (useNewDialoguesWithVoice)
        {
            // 27. Mert: "Eşyaları aldım! Deprem olmadan acilen apartmana dönüp arkadaşların yanına çıkmalıyım!" {20.mp3}
            Speak(null, "Mert (Sen)", "Eşyaları aldım! Deprem olmadan acilen apartmana dönüp arkadaşların yanına çıkmalıyım!", 4.0f, "20");
        }
        else
        {
            fader.ShowSubtitle("Mert (Sen)", "Eşyaları aldım! Deprem olmadan acilen apartmana dönüp arkadaşların yanına çıkmalıyım!", 4.0f);
        }

        if (marketEntranceInteractable != null)
        {
            marketEntranceInteractable.highlightMarker = false;
        }

        EnsureStreetReturnDoorReference();
        if (streetReturnDoorInteractable != null)
        {
            streetReturnDoorInteractable.gameObject.SetActive(true);
            streetReturnDoorInteractable.isInteractable = true;
            streetReturnDoorInteractable.highlightMarker = true;
            streetReturnDoorInteractable.requireFacing = false;
            streetReturnDoorInteractable.interactDistance = 4.5f;
            streetReturnDoorInteractable.SetPrompt("[E] Apartmana / Eve Gir", true);
        }

        if (homeEntranceInteractable != null)
        {
            homeEntranceInteractable.isInteractable = true;
            homeEntranceInteractable.highlightMarker = true;
            homeEntranceInteractable.SetPrompt("[E] Eve Gir", true);
        }
        yield return null;
    }

    // --- STAGE 7: Saat 21:00 Depremi, Yönlendirme ve Masanın Yanına Çökme ---
    private IEnumerator Stage7Routine()
    {
        ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();
        fader.StopCountdown();

        if (wallClock != null) wallClock.SetDigitalTime(21, 0, 0);

        if (useNewDialoguesWithVoice)
        {
            // 29. Mert: "Yetiştim! Saat tam 21:00..." {21.mp3}
            Speak(null, "Mert (Sen)", "Yetiştim! Saat tam 21:00...", 2.4f, "21");
            yield return new WaitForSeconds(2.6f);
        }
        else
        {
            fader.ShowSubtitle("Mert (Sen)", "Yetiştim! Saat tam 21:00...", 2.2f);
            yield return new WaitForSeconds(2.4f);
        }

        // DEPREM BAŞLAR!
        Debug.Log("[EvMarketStoryManager] Saat 21:00 Depremi Başladı!");
        GameEvents.TriggerEvent1(true);
        CameraEffectsController.EnsureInstance()?.StartEarthquakeShake(0.18f);

        if (useNewDialoguesWithVoice)
        {
            // 30. Arkadaş 1: "DEPREM OLUYOR! ÇILDIRACAĞIM, BİNA ÇOK FENA SALLANIYOR!" {22.mp3}
            Speak(friendA1, "Can", "DEPREM OLUYOR! ÇILDIRACAĞIM, BİNA ÇOK FENA SALLANIYOR!", 3.0f, "22");
            yield return new WaitForSeconds(2.2f);

            // 31. Arkadaş 2: "MERDİVENLERE Mİ KOŞALIM YOKSA PENCEREYE Mİ?! NE YAPACAĞIZ?!" {23.mp3}
            Speak(friendA2, "Emre", "MERDİVENLERE Mİ KOŞALIM YOKSA PENCEREYE Mİ?! NE YAPACAĞIZ?!", 3.0f, "23");
            yield return new WaitForSeconds(1.8f);
        }
        else
        {
            Speak(friendA1, "Can", "DEPREM OLUYOR! ÇILDIRACAĞIM, BİNA ÇOK FENA SALLANIYOR!", 2.8f);
            yield return new WaitForSeconds(2.0f);

            Speak(friendA2, "Emre", "MERDİVENLERE Mİ KOŞALIM YOKSA PENCEREYE Mİ?! NE YAPACAĞIZ?!", 2.8f);
            yield return new WaitForSeconds(1.5f);
        }

        // Arkadaşları Yönlendirme Seçimi Açılır
        FriendChoiceUI choiceUI = FriendChoiceUI.EnsureInstance();
        choiceUI.ShowFriendDirectionChoice(OnFriendDirectionChoiceMade);
    }

    private void OnFriendDirectionChoiceMade(bool choseSafeTable)
    {
        if (choseSafeTable)
        {
            // DOĞRU SEÇİM: Sağlam masanın yanına çök-kapan-tutun
            StartCoroutine(CorrectChoiceRoutine());
        }
        else
        {
            // SENARYO B: Yanlış seçim (Pencere kenarı / merdivenler)
            StartCoroutine(WrongChoiceRoutine());
        }
    }

    private IEnumerator WrongChoiceRoutine()
    {
        // Arkadaşlar merdivene ve pencereye panikle koşar (2 belirgin kaçış keyframe'i ile)
        if (friendA1 != null)
        {
            var rc1 = friendA1.GetComponent<NPCRunController>() ?? friendA1.gameObject.AddComponent<NPCRunController>();
            rc1.moveForwardWhenRunning = true;
            rc1.runMoveSpeed = 4.0f;
            rc1.rotationSpeed = 12.0f;
            rc1.SetEscapeKeyframes(
                new Vector3(7.5f, friendA1.transform.position.y, -6.5f),  // 1. Keyframe: Masa çevresinden koridora açıl
                new Vector3(10.5f, friendA1.transform.position.y, -13.5f) // 2. Keyframe: Merdiven boşluğuna doğru koş
            );
        }
        if (friendA2 != null)
        {
            var rc2 = friendA2.GetComponent<NPCRunController>() ?? friendA2.gameObject.AddComponent<NPCRunController>();
            rc2.moveForwardWhenRunning = true;
            rc2.runMoveSpeed = 4.2f;
            rc2.rotationSpeed = 12.0f;
            rc2.SetEscapeKeyframes(
                new Vector3(5.5f, friendA2.transform.position.y, -9.5f),  // 1. Keyframe: Masa çevresinden koridora açıl
                new Vector3(10.5f, friendA2.transform.position.y, -14.5f) // 2. Keyframe: Merdiven boşluğuna doğru koş
            );
        }

        GameEvents.TriggerRunning(true);
        if (useNewDialoguesWithVoice)
        {
            // 32. Arkadaş 1: "Merdivenlere koşuuun!!" {24.mp3}
            Speak(friendA1, "Can", "Merdivenlere koşuuun!!", 2.2f, "24");
        }
        else
        {
            Speak(friendA1, "Can", "Merdivenlere koşuuun!!", 2.0f);
        }
        yield return new WaitForSeconds(1.5f);

        // Bina çöker (Event 2)
        GameEvents.TriggerEvent2();

        // En son yıkılma / çökme anında bu arkadaşlara Rigidbody ve Collider ekle / savrulma fiziğini aktif et
        friendA1?.GetComponent<NPCRunController>()?.EnableCollapsePhysics();
        friendA2?.GetComponent<NPCRunController>()?.EnableCollapsePhysics();

        yield return new WaitForSeconds(1.0f);

        TriggerLoopRestart("Merdivenler ve pencere kenarı çöktü! Arkadaşların kurtulamadı...");
    }

    private IEnumerator CorrectChoiceRoutine()
    {
        if (useNewDialoguesWithVoice)
        {
            // 34. Arkadaş 1: "Tamam, masanın yanına çöküyoruz!" {25.mp3}
            Speak(friendA1, "Can", "Tamam, masanın yanına çöküyoruz!", 2.5f, "25");
        }
        else
        {
            Speak(friendA1, "Can", "Tamam, masanın yanına çöküyoruz!", 2.5f);
        }

        // Arkadaşlar masanın yanına 2 keyframe ile geçip ÇÖK-KAPAN-TUTUN pozisyonuna geçer
        Transform tableTarget = safeTableInteractable != null ? safeTableInteractable.transform : null;

        if (friendA1 != null)
        {
            var rc1 = friendA1.GetComponent<NPCRunController>() ?? friendA1.gameObject.AddComponent<NPCRunController>();
            rc1.moveForwardWhenRunning = true;
            rc1.runMoveSpeed = 3.6f;
            rc1.rotationSpeed = 12.0f;
            rc1.SetEscapeKeyframes(
                new Vector3(4.5f, friendA1.transform.position.y, -6.6f), // 1. Keyframe: Masanın yanına yaklaş
                new Vector3(4.5f, friendA1.transform.position.y, -7.2f), // 2. Keyframe: Masanın güvenli yaşam üçgenine otur
                crouchAtEnd: true, 
                lookAtTarget: tableTarget
            );
            rc1.StartRunning();
        }
        else
        {
            GameEvents.TriggerCrouching(true);
        }

        if (friendA2 != null)
        {
            var rc2 = friendA2.GetComponent<NPCRunController>() ?? friendA2.gameObject.AddComponent<NPCRunController>();
            rc2.moveForwardWhenRunning = true;
            rc2.runMoveSpeed = 3.8f;
            rc2.rotationSpeed = 12.0f;
            rc2.SetEscapeKeyframes(
                new Vector3(3.5f, friendA2.transform.position.y, -7.0f), // 1. Keyframe: Masanın yanına yaklaş
                new Vector3(3.5f, friendA2.transform.position.y, -7.8f), // 2. Keyframe: Masanın güvenli yaşam üçgenine otur
                crouchAtEnd: true, 
                lookAtTarget: tableTarget
            );
            rc2.StartRunning();
        }
        else
        {
            GameEvents.TriggerCrouching(true);
        }

        ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();
        if (useNewDialoguesWithVoice)
        {
            // 35. Mert: "Çabuk! 12 saniye içinde sağlam masanın yanına geç ve ÇÖK-KAPAN-TUTUN yap!" {25.mp3}
            Speak(null, "Mert (Sen)", "Çabuk! 12 saniye içinde sağlam masanın yanına geç ve ÇÖK-KAPAN-TUTUN yap!", 3.2f, "25");
        }
        else
        {
            fader.ShowSubtitle("Mert (Sen)", "Çabuk! 12 saniye içinde sağlam masanın yanına geç ve ÇÖK-KAPAN-TUTUN yap!", 3.2f);
        }

        if (safeTableInteractable != null)
        {
            safeTableInteractable.gameObject.SetActive(true);
            safeTableInteractable.isInteractable = true;
            safeTableInteractable.highlightMarker = true;
            safeTableInteractable.requireFacing = false;
            safeTableInteractable.interactDistance = 4.0f;
            safeTableInteractable.SetPrompt("[E] Masanın Yanında ÇÖK - KAPAN - TUTUN!", true);
        }

        hasInteractedWithSafeTable = false;
        float remainingCrouchTime = 12.0f;

        while (remainingCrouchTime > 0f)
        {
            remainingCrouchTime -= Time.deltaTime;

            if (hasInteractedWithSafeTable)
            {
                break;
            }

            yield return null;
        }

        // Eğer 12 sn içinde masanın yanına geçip kapanmadıysa -> SENARYO C
        if (!hasInteractedWithSafeTable)
        {
            if (safeTableInteractable != null) safeTableInteractable.highlightMarker = false;
            GameEvents.TriggerEvent2();
            friendA1?.GetComponent<NPCRunController>()?.EnableCollapsePhysics();
            friendA2?.GetComponent<NPCRunController>()?.EnableCollapsePhysics();
            TriggerLoopRestart("Evin tavanı çöktü! Masanın yanına Çök-Kapan-Tutun yapmadığın için kurtulamadın...");
            yield break;
        }

        // Başarılı Çök-Kapan-Tutun
        if (safeTableInteractable != null)
        {
            safeTableInteractable.highlightMarker = false;
            safeTableInteractable.isInteractable = false;
        }

        ApplyCokKapanTutunPose();

        if (useNewDialoguesWithVoice)
        {
            // 37. Mert: "Masanın yanına cenin pozisyonunda kapandım, başımı koruyorum!" {27.mp3}
            Speak(null, "Mert (Sen)", "Masanın yanına cenin pozisyonunda kapandım, başımı koruyorum!", 3.0f, "27");
        }
        else
        {
            fader.ShowSubtitle("Mert (Sen)", "Masanın yanına cenin pozisyonunda kapandım, başımı koruyorum!", 3.0f);
        }
        yield return new WaitForSeconds(1.4f);

        // Bina çöker! (2. kez çöküş)
        GameEvents.TriggerEvent2();
        friendA1?.GetComponent<NPCRunController>()?.EnableCollapsePhysics();
        friendA2?.GetComponent<NPCRunController>()?.EnableCollapsePhysics();

        // Ekran yavaşça kararır
        fader.FadeToBlack(2.5f);
        yield return new WaitForSeconds(1.5f);

        GameEvents.TriggerEvent1(false);
        CameraEffectsController.EnsureInstance()?.StopEarthquakeShake();

        // Oyuncunun hayatta kalma ve çanta verilerini 3. sahneye aktarmak için kaydet
        if (GameJam.Market.MarketUIManager.Instance != null)
        {
            var market = GameJam.Market.MarketUIManager.Instance;
            PlayerSurvivalData.HasCompletedShopping = market.HasCompletedShopping;
            PlayerSurvivalData.HasFlashlight = market.HasFlashlight();
            PlayerSurvivalData.HasWhistle = market.HasWhistle();
            PlayerSurvivalData.HasWater = market.HasWater();
            PlayerSurvivalData.HasFirstAid = market.HasFirstAid();
            PlayerSurvivalData.HasBlanket = market.HasSleepingBag();
            PlayerSurvivalData.HasFood = market.HasCannedFood();
            PlayerSurvivalData.EssentialItemCount = market.GetPlacedEssentialCount();

            PlayerSurvivalData.PlacedItemIds.Clear();
            if (market.Grid != null && market.Grid.PlacedItems != null)
            {
                foreach (var pi in market.Grid.PlacedItems)
                {
                    if (pi != null && pi.data != null && !string.IsNullOrEmpty(pi.data.id))
                    {
                        PlayerSurvivalData.PlacedItemIds.Add(pi.data.id);
                    }
                }
            }
        }
        else
        {
            PlayerSurvivalData.HasCompletedShopping = true;
            PlayerSurvivalData.HasFlashlight = true;
            PlayerSurvivalData.HasWhistle = true;
            PlayerSurvivalData.HasWater = true;
            PlayerSurvivalData.HasFirstAid = true;
            PlayerSurvivalData.HasBlanket = true;
            PlayerSurvivalData.HasFood = true;
            PlayerSurvivalData.EssentialItemCount = 6;
        }
        PlayerSurvivalData.FriendsSavedAtTable = true;

        // Siren sesleri ve enkaz geçiş mesajı (Intro -> EvMarket geçişindeki gibi)
        PlayAudio(sirenClip, 0.85f);
        fader.ShowCenterMessage(
            "[ Enkaz Altı ]",
            "Masanın oluşturduğu yaşam üçgeninde hayattasınız...\nUzaktan arama kurtarma sesleri duyuluyor...",
            4.0f
        );
        yield return new WaitForSeconds(4.2f);

        GameEvents.ResetEvents();

        // 3. Sahneye Geçiş (Stage3)
        Debug.Log($"[EvMarketStoryManager] 2. kez enkaz tamamlandı. 3. Sahneye geçiliyor: '{nextSceneName}'");
        if (Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            RequestSceneTransition(nextSceneName);
        }
        else
        {
            Debug.LogWarning($"[EvMarketStoryManager] '{nextSceneName}' sahnesi Build Settings'te bulunamadı!");
        }
    }

    // --- STAGE 8: Enkaz Altında Uyanış & Hayatta Kalma ---
    private IEnumerator Stage8Routine()
    {
        ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();
        fader.SetBlackImmediate(true);

        // 39. Mert: "Gözlerimi açtığımda her yer zifiri karanlıktı... Molozların altındayız."
        fader.ShowSubtitle("Mert (Sen)", "Gözlerimi açtığımda her yer zifiri karanlıktı... Molozların altındayız.", 3.5f);
        yield return new WaitForSeconds(3.7f);

        if (useNewDialoguesWithVoice)
        {
            // 40. Mert: "Arkadaşlarımdan ses gelmiyor... Çantam yanımda olmalı!" {29.mp3}
            Speak(null, "Mert (Sen)", "Arkadaşlarımdan ses gelmiyor... Çantam yanımda olmalı!", 3.2f, "29");
            yield return new WaitForSeconds(3.4f);
        }
        else
        {
            fader.ShowSubtitle("Mert (Sen)", "Arkadaşlarımdan ses gelmiyor... Çantam yanımda olmalı!", 3.0f);
            yield return new WaitForSeconds(3.2f);
        }

        // Çantada tüm hayati eşyalar var mı kontrol et
        if (GameJam.Market.MarketUIManager.Instance != null)
        {
            var market = GameJam.Market.MarketUIManager.Instance;
            PlayerSurvivalData.HasFlashlight = market.HasFlashlight();
            PlayerSurvivalData.HasWhistle = market.HasWhistle();
            PlayerSurvivalData.HasWater = market.HasWater();
            PlayerSurvivalData.HasFirstAid = market.HasFirstAid();
            PlayerSurvivalData.HasBlanket = market.HasSleepingBag();
            PlayerSurvivalData.HasFood = market.HasCannedFood();
            PlayerSurvivalData.EssentialItemCount = market.GetPlacedEssentialCount();
        }

        if (!PlayerSurvivalData.HasAllEssentialItems())
        {
            string failReason = PlayerSurvivalData.GetPrimaryDeathReason();
            var missingList = PlayerSurvivalData.GetMissingEssentialItemNames();
            if (missingList != null && missingList.Count > 0)
            {
                failReason += $"\n(Eksik Eşyalar: {string.Join(", ", missingList)})";
            }
            TriggerLoopRestart(failReason);
            yield break;
        }

        // Fener ve Düdük Prompt'u Göster
        FriendChoiceUI choiceUI = FriendChoiceUI.EnsureInstance();
        choiceUI.ShowRubbleSurvivalPrompt(OnRubbleFlashlightUsed, OnRubbleWhistleUsed);
    }

    private void OnRubbleFlashlightUsed()
    {
        if (useNewDialoguesWithVoice)
        {
            // 41. Mert: "Feneri yaktım! Masanın oluşturduğu yaşam üçgeni bizi korumuş..." {30.mp3}
            Speak(null, "Mert (Sen)", "Feneri yaktım! Masanın oluşturduğu yaşam üçgeni bizi korumuş, arkadaşlarım baygın ama nefes alıyorlar!", 4.0f, "30");
        }
        else
        {
            ScreenFaderUI.EnsureInstance()?.ShowSubtitle("Mert (Sen)", "Feneri yaktım! Masanın oluşturduğu yaşam üçgeni bizi korumuş, arkadaşlarım baygın ama nefes alıyorlar!", 3.8f);
        }
    }

    private void OnRubbleWhistleUsed()
    {
        StartCoroutine(RubbleWhistleSuccessRoutine());
    }

    private IEnumerator RubbleWhistleSuccessRoutine()
    {
        PlayAudio(whistleSoundClip, 1.0f);

        ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();

        if (useNewDialoguesWithVoice)
        {
            // 42. Mert: "Düdüğü var gücümle çalıyorum: TİİİİİTTT! TİİİİİTTT!" {31.mp3}
            Speak(null, "Mert (Sen)", "Düdüğü var gücümle çalıyorum: TİİİİİTTT! TİİİİİTTT!", 3.2f, "31");
            yield return new WaitForSeconds(3.4f);

            PlayAudio(sirenClip, 0.85f);
            // 43. AFAD Arama Kurtarma: "SESİ DUYDUNUZ MU?! AŞAĞIDA BİRİLERİ VAR! SESİME GEL SESİME!" {32.mp3}
            Speak(null, "AFAD Arama Kurtarma", "SESİ DUYDUNUZ MU?! AŞAĞIDA BİRİLERİ VAR! SESİME GEL SESİME!", 4.0f, "32");
            yield return new WaitForSeconds(4.2f);
        }
        else
        {
            fader.ShowSubtitle("Mert (Sen)", "Düdüğü var gücümle çalıyorum: TİİİİİTTT! TİİİİİTTT!", 3.2f);
            yield return new WaitForSeconds(3.4f);

            PlayAudio(sirenClip, 0.85f);
            fader.ShowSubtitle("AFAD Arama Kurtarma", "SESİ DUYDUNUZ MU?! AŞAĞIDA BİRİLERİ VAR! SESİME GEL SESİME!", 4.0f);
            yield return new WaitForSeconds(4.2f);
        }

        StartStage(StoryStage.Stage9_FinalRescueAndAwareness);
    }

    // --- STAGE 9: 2. Kez Enkaz Altı Kurtarılış & 3. Sahneye Geçiş ---
    private IEnumerator Stage9Routine()
    {
        FriendChoiceUI.EnsureInstance()?.HideRubblePrompt();

        ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();
        fader.FadeFromBlack(2.5f);

        if (useNewDialoguesWithVoice)
        {
            // 44. AFAD Görevlisi: "Sakin olun gençler, sizi çıkarıyoruz! Yaşam üçgeni ve deprem düdüğü hayatınızı kurtardı!" {33.mp3}
            Speak(null, "AFAD Görevlisi", "Sakin olun gençler, sizi çıkarıyoruz! Yaşam üçgeni ve deprem düdüğü hayatınızı kurtardı!", 4.2f, "33");
            yield return new WaitForSeconds(4.4f);
        }
        else
        {
            fader.ShowSubtitle("AFAD Görevlisi", "Sakin olun gençler, sizi çıkarıyoruz! Yaşam üçgeni ve deprem düdüğü hayatınızı kurtardı!", 4.0f);
            yield return new WaitForSeconds(4.2f);
        }

        fader.FadeToBlack(2.0f);
        yield return new WaitForSeconds(2.0f);

        fader.ShowCenterMessage("[ Enkazdan Çıkarıldınız ]", "Arama kurtarma ekipleri sizi güvenli bölgeye taşıyor...", 3.0f);
        yield return new WaitForSeconds(3.0f);

        // 3. Sahneye Geçiş
        Debug.Log($"[EvMarketStoryManager] 2. kez enkaz tamamlandı. Sonraki sahneye geçiliyor: '{nextSceneName}'");
        if (Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            RequestSceneTransition(nextSceneName);
        }
        else
        {
            Debug.LogWarning($"[EvMarketStoryManager] '{nextSceneName}' sahnesi Build Settings'te bulunamadı!");
        }
    }

    #endregion

    #region --- Fail / Time Loop Sistemi (Alternatif Senaryolar) ---

    private void OnCountdownExpired()
    {
        // Oyuncu süre dolmadan eve dönmüşse deprem ŞİMDİ (saat 21:00'de) başlar.
        if (currentStage == StoryStage.Stage6_ReturnHomeBefore2100 && hasReturnedHomeInTime)
        {
            StartStage(StoryStage.Stage7_EarthquakeAt2100_DirectFriends);
            return;
        }

        // SENARYO A: 90 sn doldu ama eve yetişemedi veya hazırlık yapmadı
        if (currentStage == StoryStage.Stage4_SecondPosterLook ||
            currentStage == StoryStage.Stage5_MarketShopping ||
            currentStage == StoryStage.Stage6_ReturnHomeBefore2100)
        {
            TriggerLoopRestart("Molozların altında arkadaşların kurtulamadı...\n(Zamanında hazırlık yapıp eve yetişemedin)");
        }
    }

    /// <summary>
    /// Bir başarısızlık durumunda ekranı karartır, sebep mesajını gösterir ve sahneyi başa alarak döngüyü yeniden başlatır.
    /// </summary>
    public void TriggerLoopRestart(string failReason)
    {
        if (isRestartingLoop) return;
        isRestartingLoop = true;
        SetHomeEntranceTriggerActive(false);

        if (activeStoryRoutine != null)
        {
            StopCoroutine(activeStoryRoutine);
            activeStoryRoutine = null;
        }

        StartCoroutine(LoopRestartRoutine(failReason));
    }

    private IEnumerator LoopRestartRoutine(string failReason)
    {
        Debug.LogWarning($"[EvMarketStoryManager] ZAMAN DÖNGÜSÜ TETİKLENDİ: {failReason}");

        // Deprem ve sarsıntı
        CameraEffectsController.EnsureInstance()?.StartEarthquakeShake(0.20f);
        GameEvents.TriggerEvent1(true);

        ScreenFaderUI fader = ScreenFaderUI.EnsureInstance();
        fader.StopCountdown();
        FriendChoiceUI.EnsureInstance()?.HideChoice();
        FriendChoiceUI.EnsureInstance()?.HideRubblePrompt();

        fader.FadeToBlack(1.6f);
        yield return new WaitForSeconds(1.6f);

        GameEvents.TriggerEvent1(false);
        CameraEffectsController.EnsureInstance()?.StopEarthquakeShake();

        // Dramatik Başarısızlık Mesajı
        fader.ShowCenterMessage("[ ZAMAN DÖNGÜSÜ BAŞA DÖNDÜ ]", failReason, 4.0f);
        yield return new WaitForSeconds(4.0f);

        GameEvents.ResetEvents();
        ResetCrouchPose();
        PlayerSurvivalData.ResetData();

        // Sahneyi yeniden yükle -> Kahve dökme sahnesinden tekrar uyanış!
        string sceneName = SceneManager.GetActiveScene().name;
        RequestSceneTransition(sceneName);
    }

    /// <summary>
    /// Merkezi sahne koordinatörü (SceneCoordinator) mimarisi üzerinden sahne geçişi yapar.
    /// </summary>
    public static void RequestSceneTransition(string targetScene)
    {
        if (SceneCoordinator.Instance != null)
        {
            Debug.Log($"[EvMarketStoryManager] SceneCoordinator.Instance.RequestSceneChange('{targetScene}') çağrılıyor.");
            SceneCoordinator.Instance.RequestSceneChange(targetScene, 1.0f);
        }
        else
        {
            Debug.Log($"[EvMarketStoryManager] Doğrudan SceneManager ile sahne yükleniyor: '{targetScene}'");
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
        }
    }

    #endregion

    #region --- Test & Hile Yöntemleri (SkipToStage) ---

    /// <summary>
    /// Testler için doğrudan istenilen hikaye aşamasına atlar.
    /// </summary>
    public void SkipToStage(StoryStage targetStage)
    {
        Debug.Log($"[EvMarketStoryManager] HİLE: Aşama {targetStage}'e atlanıyor...");

        if (activeStoryRoutine != null)
        {
            StopCoroutine(activeStoryRoutine);
            activeStoryRoutine = null;
        }

        GameEvents.ResetEvents();
        CameraEffectsController.EnsureInstance()?.StopEarthquakeShake();
        ScreenFaderUI.EnsureInstance()?.StopCountdown();
        ScreenFaderUI.EnsureInstance()?.HideSubtitle();
        ScreenFaderUI.EnsureInstance()?.HideCenterMessage();
        FriendChoiceUI.EnsureInstance()?.HideChoice();
        FriendChoiceUI.EnsureInstance()?.HideRubblePrompt();

        // Hile ile aşama atlarken "eve döndü" bayrağını sıfırla (eski durum sızmasın)
        hasReturnedHomeInTime = false;

        if (targetStage != StoryStage.Stage7_EarthquakeAt2100_DirectFriends)
        {
            ResetCrouchPose();
        }

        if (targetStage < StoryStage.Stage7_EarthquakeAt2100_DirectFriends)
        {
            wallClock?.SetDigitalTime(20, 0, 0);
        }
        else
        {
            wallClock?.SetDigitalTime(21, 0, 0);
        }

        if (targetStage < StoryStage.Stage5_MarketShopping)
        {
            hasUsedApartmentExitDoor = false;
            SetHomeEntranceTriggerActive(false);
        }
        else
        {
            SetHomeEntranceTriggerActive(true);
        }

        StartStage(targetStage);
    }

    private void HandleDebugKeyboardCheats()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return;
        if (Keyboard.current.f5Key.wasPressedThisFrame) SkipToStage(StoryStage.Stage4_SecondPosterLook);
        if (Keyboard.current.f6Key.wasPressedThisFrame) SkipToStage(StoryStage.Stage5_MarketShopping);
        if (Keyboard.current.f7Key.wasPressedThisFrame) SkipToStage(StoryStage.Stage7_EarthquakeAt2100_DirectFriends);
        if (Keyboard.current.f8Key.wasPressedThisFrame) SkipToStage(StoryStage.Stage8_UnderRubble);
        if (Keyboard.current.f9Key.wasPressedThisFrame) SkipToStage(StoryStage.Stage9_FinalRescueAndAwareness);
#else
        if (Input.GetKeyDown(KeyCode.F5)) SkipToStage(StoryStage.Stage4_SecondPosterLook);
        if (Input.GetKeyDown(KeyCode.F6)) SkipToStage(StoryStage.Stage5_MarketShopping);
        if (Input.GetKeyDown(KeyCode.F7)) SkipToStage(StoryStage.Stage7_EarthquakeAt2100_DirectFriends);
        if (Input.GetKeyDown(KeyCode.F8)) SkipToStage(StoryStage.Stage8_UnderRubble);
        if (Input.GetKeyDown(KeyCode.F9)) SkipToStage(StoryStage.Stage9_FinalRescueAndAwareness);
#endif
    }

    [ContextMenu("Skip To Stage 0 (Uyanış)")]
    public void ContextSkipStage0() => SkipToStage(StoryStage.Stage0_WakeUpCoffeeSpill);

    [ContextMenu("Skip To Stage 1 (Mutfak & Poster)")]
    public void ContextSkipStage1() => SkipToStage(StoryStage.Stage1_GoToKitchenPoster);

    [ContextMenu("Skip To Stage 2 (Peçete Al)")]
    public void ContextSkipStage2() => SkipToStage(StoryStage.Stage2_TakeNapkin);

    [ContextMenu("Skip To Stage 3 (Arkadaşlarla Konuş)")]
    public void ContextSkipStage3() => SkipToStage(StoryStage.Stage3_TalkToFriendsUpstairs);

    [ContextMenu("Skip To Stage 4 (Postere 2. Bakış & Sayaç)")]
    public void ContextSkipStage4() => SkipToStage(StoryStage.Stage4_SecondPosterLook);

    [ContextMenu("Skip To Stage 5 (Market Alışverişi)")]
    public void ContextSkipStage5() => SkipToStage(StoryStage.Stage5_MarketShopping);

    [ContextMenu("Skip To Stage 6 (Eve Dönüş)")]
    public void ContextSkipStage6() => SkipToStage(StoryStage.Stage6_ReturnHomeBefore2100);

    [ContextMenu("Skip To Stage 7 (Saat 21:00 Depremi & Seçim)")]
    public void ContextSkipStage7() => SkipToStage(StoryStage.Stage7_EarthquakeAt2100_DirectFriends);

    [ContextMenu("Skip To Stage 8 (Enkaz Altı)")]
    public void ContextSkipStage8() => SkipToStage(StoryStage.Stage8_UnderRubble);

    [ContextMenu("Skip To Stage 9 (AFAD Kurtarma & Final)")]
    public void ContextSkipStage9() => SkipToStage(StoryStage.Stage9_FinalRescueAndAwareness);

    #endregion

    #region --- Yardımcı Metodlar ---

    private void Speak(SpeechBubble speaker, string speakerName, string text, float duration, string voiceClipName = null)
    {
        if (speaker != null)
        {
            speaker.Say(text, duration, speakerName);
        }
        ScreenFaderUI.EnsureInstance()?.ShowSubtitle(speakerName, text, duration);

        if (!string.IsNullOrEmpty(voiceClipName) && useNewDialoguesWithVoice)
        {
            DialogueAudioPlayer.PlayVoice(voiceClipName);
        }
    }

    private void PlayAudio(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        // Sahne ses kaynağı atanmamışsa (siren/düdük seslerinin kaybolmaması için) global kaynağa düş.
        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
        else
        {
            DialogueAudioPlayer.PlaySfx(clip, volume);
        }
    }

    #endregion
}
