using UnityEngine;

/// <summary>
/// Oyun genelindeki fare hassasiyeti ve kamera görüş açısı (FOV) gibi ayarları
/// PlayerPrefs ile yöneten merkezi ayar kontrolcüsü.
/// </summary>
public static class GameSettings
{
    public const string KEY_MOUSE_SENSITIVITY = "Settings_MouseSensitivity";
    public const string KEY_CAMERA_FOV = "Settings_CameraFOV";

    public const float DEFAULT_MOUSE_SENSITIVITY = 2.0f;
    public const float DEFAULT_CAMERA_FOV = 65.0f;

    public const float MIN_MOUSE_SENSITIVITY = 0.5f;
    public const float MAX_MOUSE_SENSITIVITY = 8.0f;

    public const float MIN_CAMERA_FOV = 50.0f;
    public const float MAX_CAMERA_FOV = 95.0f;

    /// <summary>
    /// Fare hassasiyeti (FirstPersonLook tarafından kullanılır).
    /// </summary>
    public static float MouseSensitivity
    {
        get => PlayerPrefs.GetFloat(KEY_MOUSE_SENSITIVITY, DEFAULT_MOUSE_SENSITIVITY);
        set
        {
            float clamped = Mathf.Clamp(value, MIN_MOUSE_SENSITIVITY, MAX_MOUSE_SENSITIVITY);
            PlayerPrefs.SetFloat(KEY_MOUSE_SENSITIVITY, clamped);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Kamera görüş açısı (FOV - Zoom ve Camera bileşenleri tarafından kullanılır).
    /// </summary>
    public static float CameraFOV
    {
        get => PlayerPrefs.GetFloat(KEY_CAMERA_FOV, DEFAULT_CAMERA_FOV);
        set
        {
            float clamped = Mathf.Clamp(value, MIN_CAMERA_FOV, MAX_CAMERA_FOV);
            PlayerPrefs.SetFloat(KEY_CAMERA_FOV, clamped);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Belirtilen kameraya güncel FOV ayarını uygular.
    /// </summary>
    public static void ApplyFOV(Camera cam)
    {
        if (cam != null)
        {
            cam.fieldOfView = CameraFOV;
        }
    }
}
