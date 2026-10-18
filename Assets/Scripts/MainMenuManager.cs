using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// "21:00" Ana Menü Sahnesi Yöneticisi.
/// Başlık animasyonu (yanıp sönen iki nokta), sahne geçişi (Intro'ya başlatma),
/// Ayarlar Paneli (Mouse Sensivity & FOV) ve Deprem Bilgilendirme Panelini yönetir.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Sahne Geçişi")]
    public string targetGameScene = "Intro";

    [Header("Paneller")]
    public GameObject mainButtonsContainer;
    public GameObject settingsPanel;
    public GameObject infoPanel;

    [Header("Başlık")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;

    [Header("Ayarlar Bileşenleri")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValueText;
    public Slider fovSlider;
    public TextMeshProUGUI fovValueText;

    [Header("Kamera Kontrolcüsü (Opsiyonel)")]
    public MainMenuCameraController cameraController;

    [Header("Butonlar (Boş bırakılırsa isimlerine göre otomatik bulunur)")]
    public Button startButton;
    public Button settingsButton;
    public Button infoButton;
    public Button backSettingsButton;
    public Button closeInfoButton;

    private float colonBlinkTimer = 0f;
    private bool colonVisible = true;
    private bool isTransitioning = false;

    private void Awake()
    {
        // Fare imlecini aç
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        InitializeSettingsUI();
        WireButtons();
        ShowMainButtons();
    }

    private void Update()
    {
        // "21:00" başlığındaki iki noktanın ':' dijital saat hissiyle yanıp sönmesi
        if (titleText != null)
        {
            colonBlinkTimer += Time.deltaTime;
            if (colonBlinkTimer >= 0.75f)
            {
                colonBlinkTimer = 0f;
                colonVisible = !colonVisible;
                titleText.text = colonVisible ? "21<color=#FF3333>:</color>00" : "21<color=#00000000>:</color>00";
            }
        }

        HandleKeyboardNavigation();
    }

    /// <summary>
    /// Klavyeden menü kullanımı: ESC açık paneli kapatır, Enter ana menüdeyken oyunu başlatır.
    /// </summary>
    private void HandleKeyboardNavigation()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
                return;
            }
            if (infoPanel != null && infoPanel.activeSelf)
            {
                CloseInfo();
                return;
            }
        }

        bool isMainMenuVisible = mainButtonsContainer == null || mainButtonsContainer.activeSelf;
        if (isMainMenuVisible &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            StartGame();
        }
#endif
    }

    #region Ayarlar Başlatma ve Dinleyiciler
    private void InitializeSettingsUI()
    {
        float currentSens = GameSettings.MouseSensitivity;
        float currentFov = GameSettings.CameraFOV;

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = GameSettings.MIN_MOUSE_SENSITIVITY;
            sensitivitySlider.maxValue = GameSettings.MAX_MOUSE_SENSITIVITY;
            sensitivitySlider.value = currentSens;
            sensitivitySlider.onValueChanged.RemoveAllListeners();
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        UpdateSensitivityDisplay(currentSens);

        if (fovSlider != null)
        {
            fovSlider.minValue = GameSettings.MIN_CAMERA_FOV;
            fovSlider.maxValue = GameSettings.MAX_CAMERA_FOV;
            fovSlider.value = currentFov;
            fovSlider.onValueChanged.RemoveAllListeners();
            fovSlider.onValueChanged.AddListener(OnFOVChanged);
        }

        UpdateFOVDisplay(currentFov);
    }

    private void OnSensitivityChanged(float val)
    {
        GameSettings.MouseSensitivity = val;
        UpdateSensitivityDisplay(val);
    }

    private void UpdateSensitivityDisplay(float val)
    {
        if (sensitivityValueText != null)
        {
            sensitivityValueText.text = val.ToString("F1");
        }
    }

    private void OnFOVChanged(float val)
    {
        GameSettings.CameraFOV = val;
        UpdateFOVDisplay(val);

        if (cameraController != null)
        {
            cameraController.RefreshFOV();
        }
        else if (Camera.main != null)
        {
            Camera.main.fieldOfView = val;
        }
    }

    private void UpdateFOVDisplay(float val)
    {
        if (fovValueText != null)
        {
            fovValueText.text = $"{Mathf.RoundToInt(val)}°";
        }
    }
    #endregion

    #region Buton Bağlama (Çalışma Anı)
    /// <summary>
    /// Buton tıklama olaylarını ÇALIŞMA ANINDA bağlar.
    /// Menü sahnesindeki dinleyiciler editör aracında Button.onClick.AddListener ile eklendiğinden
    /// serileştirilmez ve sahne tekrar yüklendiğinde kaybolur. Bu yüzden burada yeniden bağlanır.
    /// </summary>
    private void WireButtons()
    {
        if (startButton == null) startButton = FindButtonByName("Btn_StartGame");
        if (settingsButton == null) settingsButton = FindButtonByName("Btn_Settings");
        if (infoButton == null) infoButton = FindButtonByName("Btn_Info");
        if (backSettingsButton == null) backSettingsButton = FindButtonByName("Btn_BackSettings");
        if (closeInfoButton == null) closeInfoButton = FindButtonByName("Btn_CloseInfo");

        BindButton(startButton, StartGame);
        BindButton(settingsButton, OpenSettings);
        BindButton(infoButton, OpenInfo);
        BindButton(backSettingsButton, CloseSettings);
        BindButton(closeInfoButton, CloseInfo);
    }

    private Button FindButtonByName(string buttonName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button b in buttons)
        {
            if (b != null && b.name == buttonName) return b;
        }
        return null;
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null) return;
        // Mükerrer çağrıyı önlemek için aynı dinleyici varsa tekrar ekleme
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }
    #endregion

    #region Panel Navigasyonu
    public void ShowMainButtons()
    {
        if (mainButtonsContainer != null) mainButtonsContainer.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (mainButtonsContainer != null) mainButtonsContainer.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (infoPanel != null) infoPanel.SetActive(false);
    }

    public void CloseSettings()
    {
        PlayerPrefs.Save();
        ShowMainButtons();
    }

    public void OpenInfo()
    {
        if (mainButtonsContainer != null) mainButtonsContainer.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(true);
    }

    public void CloseInfo()
    {
        ShowMainButtons();
    }
    #endregion

    #region Oyuna Başla
    public void StartGame()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        Debug.Log($"[MainMenuManager] '{targetGameScene}' sahnesine geçiş başlatılıyor...");

        if (SceneCoordinator.Instance != null)
        {
            SceneCoordinator.Instance.RequestSceneChange(targetGameScene, 1.0f, () =>
            {
                isTransitioning = false;
            });
        }
        else
        {
            SceneManager.LoadScene(targetGameScene);
        }
    }
    #endregion
}
