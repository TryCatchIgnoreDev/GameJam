using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Merkezi Hile ve Debug Sistemi (Cheat Codes).
/// Tüm test ve hile kodları bu script altında toplanır.
/// - 1 Tuşu: Event 1 (Objelerin Sallanması - ObjectShaker) tetikler/durdurur.
/// - 2 Tuşu: Event 2 (Zemin Dilimleme ve Tüm Evlere Rigidbody Atama) tetikler.
/// - 3 Tuşu: Event 3 (male01_2 & male02_2 Koşma Olayı) tetikler/durdurur.
/// - 4 Tuşu: Event 4 (Sohbet Balonu / Diyalog Sırası) başlatır/durdurur.
/// Gelecekte eklenecek tüm yeni hileler buraya eklenecektir.
/// </summary>
public class CheatController : MonoBehaviour
{
    [Header("--- Hile Sistemi Genel Ayarlar ---")]
    [Tooltip("Hile sistemini tamamen açıp kapatır.")]
    public bool enableCheats = true;

    [Tooltip("Ekranın sol üstünde hile tuşlarını gösteren küçük bir rehber arayüzü çizsin mi?")]
    public bool showCheatOverlay = true;

    [Header("--- Event Tetikleme Tuşları (Trigger Cheats) ---")]
#if ENABLE_INPUT_SYSTEM
    [Tooltip("Event 1 (Sallanma) Tetikleme Tuşu (Varsayılan: 1)")]
    public Key triggerEvent1Key = Key.Digit1;

    [Tooltip("Event 2 (Rigidbody & Zemin Yıkımı) Tetikleme Tuşu (Varsayılan: 2)")]
    public Key triggerEvent2Key = Key.Digit2;

    [Tooltip("Event 3 (NPC Koşma) Tetikleme Tuşu (Varsayılan: 3)")]
    public Key triggerEvent3Key = Key.Digit3;

    [Tooltip("Event 4 (Sohbet Balonu / Diyalog) Tetikleme Tuşu (Varsayılan: 4)")]
    public Key triggerEvent4Key = Key.Digit4;
#else
    [Tooltip("Event 1 (Sallanma) Tetikleme Tuşu (Varsayılan: 1)")]
    public KeyCode triggerEvent1Key = KeyCode.Alpha1;

    [Tooltip("Event 2 (Rigidbody & Zemin Yıkımı) Tetikleme Tuşu (Varsayılan: 2)")]
    public KeyCode triggerEvent2Key = KeyCode.Alpha2;

    [Tooltip("Event 3 (NPC Koşma) Tetikleme Tuşu (Varsayılan: 3)")]
    public KeyCode triggerEvent3Key = KeyCode.Alpha3;

    [Tooltip("Event 4 (Sohbet Balonu / Diyalog) Tetikleme Tuşu (Varsayılan: 4)")]
    public KeyCode triggerEvent4Key = KeyCode.Alpha4;
#endif

    // Singleton (Tekil Örnek)
    public static CheatController Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (!enableCheats) return;

        // Eğer hikaye seçim ekranı (1/2 tuşları) veya enkaz ekranı açıksa cheat'leri engelle
        if (FriendChoiceUI.Instance != null && (FriendChoiceUI.Instance.IsChoiceActive || FriendChoiceUI.Instance.IsRubblePromptActive)) return;

        HandleTriggerCheats();
    }

    #region Event Trigger Cheats (1, 2, 3, 4)
    private void HandleTriggerCheats()
    {
        // 1 Tuşu: Event 1 (Sallanma)
        if (IsKeyPressed(triggerEvent1Key))
        {
            ToggleEvent1_Shaking();
        }

        // 2 Tuşu: Event 2 (Rigidbody & Zemin Yıkımı)
        if (IsKeyPressed(triggerEvent2Key))
        {
            TriggerEvent2_Destruction();
        }

        // 3 Tuşu: Event 3 (NPC Koşma)
        if (IsKeyPressed(triggerEvent3Key))
        {
            ToggleEvent3_Running();
        }

        // 4 Tuşu: Event 4 (Sohbet Balonu / Diyalog)
        if (IsKeyPressed(triggerEvent4Key))
        {
            ToggleEvent4_Dialogue();
        }
    }

    public void ToggleEvent1_Shaking()
    {
        GameEvents.ToggleEvent1();
        Debug.Log($"<color=cyan>[CHEAT] 1 Tuşuna Basıldı -> Event 1 (Sallanma): {(GameEvents.IsEvent1Active ? "AÇIK" : "KAPALI")}</color>");
    }

    public void TriggerEvent2_Destruction()
    {
        GameEvents.TriggerEvent2();
        Debug.Log("<color=orange>[CHEAT] 2 Tuşuna Basıldı -> Event 2 (Rigidbody & Yıkım) Tetiklendi!</color>");
    }

    public void ToggleEvent3_Running()
    {
        GameEvents.ToggleRunning();
        Debug.Log($"<color=yellow>[CHEAT] 3 Tuşuna Basıldı -> Event 3 (Koşma): {(GameEvents.IsRunningActive ? "KOŞUYOR" : "DURDU/OTURUYOR")}</color>");
    }

    public void ToggleEvent4_Dialogue()
    {
        GameEvents.ToggleDialogue();
        Debug.Log($"<color=green>[CHEAT] 4 Tuşuna Basıldı -> Event 4 (Diyalog): {(GameEvents.IsDialogueActive ? "BAŞLADI" : "DURDURULDU")}</color>");
    }
    #endregion

    #region Input Yardımcısı
#if ENABLE_INPUT_SYSTEM
    private bool IsKeyPressed(Key key)
    {
        if (Keyboard.current == null || key == Key.None || key == Key.IMESelected) return false;
        var control = Keyboard.current[key];
        return control != null && control.wasPressedThisFrame;
    }
#else
    private bool IsKeyPressed(KeyCode key)
    {
        return Input.GetKeyDown(key);
    }
#endif
    #endregion

    #region Ekran Arayüzü (Cheat Overlay)
    void OnGUI()
    {
        if (!enableCheats || !showCheatOverlay) return;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.alignment = TextAnchor.UpperLeft;
        boxStyle.fontSize = 13;
        boxStyle.normal.textColor = Color.white;

        string event1State = GameEvents.IsEvent1Active ? "<color=green>AKTIF</color>" : "<color=gray>KAPALI</color>";
        string event2State = GameEvents.IsEvent2Triggered ? "<color=yellow>TETIKLENDI</color>" : "<color=gray>BEKLIYOR</color>";
        string event3State = GameEvents.IsRunningActive ? "<color=green>KOŞUYOR</color>" : "<color=gray>OTURUYOR</color>";
        string event4State = GameEvents.IsDialogueActive ? "<color=green>KONUŞUYOR</color>" : "<color=gray>HAZIR</color>";
        string storyState = EvMarketStoryManager.Instance != null ? $"<color=cyan>{EvMarketStoryManager.Instance.currentStage}</color>" : "<color=gray>Yok</color>";

        string content = $"<b>[CHEAT CODES]</b>\n" +
                         $"[1] Event 1 (Sallanma): {event1State}\n" +
                         $"[2] Event 2 (Yıkım / Rigidbody): {event2State}\n" +
                         $"[3] Event 3 (Koşma): {event3State}\n" +
                         $"[4] Event 4 (Sohbet Balonu): {event4State}\n" +
                         $"[F5-F9] Hikaye Aşaması: {storyState}";

        GUI.Box(new Rect(15, 15, 290, 130), content, boxStyle);
    }
    #endregion

    #region Context Menüleri (Sağ Tık ile Tetikleme)
    [ContextMenu("Hile 1: Event 1 (Sallanma) Aç / Kapat")]
    public void ContextToggleEvent1() => ToggleEvent1_Shaking();

    [ContextMenu("Hile 2: Event 2 (Rigidbody Atama) Tetikle")]
    public void ContextTriggerEvent2() => TriggerEvent2_Destruction();

    [ContextMenu("Hile 3: Event 3 (Koşma) Başlat / Durdur")]
    public void ContextToggleEvent3() => ToggleEvent3_Running();

    [ContextMenu("Hile 4: Event 4 (Diyalog) Başlat / Durdur")]
    public void ContextToggleEvent4() => ToggleEvent4_Dialogue();

    [ContextMenu("Hikaye Hilesi: Aşama 4 (Poster & 90s Sayaç)")]
    public void CheatStoryStage4() => EvMarketStoryManager.Instance?.SkipToStage(EvMarketStoryManager.StoryStage.Stage4_SecondPosterLook);

    [ContextMenu("Hikaye Hilesi: Aşama 5 (Market Tetris)")]
    public void CheatStoryStage5() => EvMarketStoryManager.Instance?.SkipToStage(EvMarketStoryManager.StoryStage.Stage5_MarketShopping);

    [ContextMenu("Hikaye Hilesi: Aşama 7 (Saat 21:00 Depremi)")]
    public void CheatStoryStage7() => EvMarketStoryManager.Instance?.SkipToStage(EvMarketStoryManager.StoryStage.Stage7_EarthquakeAt2100_DirectFriends);

    [ContextMenu("Hikaye Hilesi: Aşama 8 (Enkaz Altı)")]
    public void CheatStoryStage8() => EvMarketStoryManager.Instance?.SkipToStage(EvMarketStoryManager.StoryStage.Stage8_UnderRubble);

    [ContextMenu("Hikaye Hilesi: Aşama 9 (AFAD Kurtarma & Outtro)")]
    public void CheatStoryStage9() => EvMarketStoryManager.Instance?.SkipToStage(EvMarketStoryManager.StoryStage.Stage9_FinalRescueAndAwareness);
    #endregion
}
