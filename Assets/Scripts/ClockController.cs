using UnityEngine;

/// <summary>
/// (Prb)Clock saat kontrolcüsü.
/// Dijital olarak girilen başlangıç saat ve dakikasını (initial time) doğru kol açılarına (output angle)
/// dönüştürür ve oyun başladığında gerçek zamanlı (saniyede 1 saniye) akmasını sağlar.
/// </summary>
[ExecuteAlways]
public class ClockController : MonoBehaviour
{
    [Header("Clock Hands (Kollar)")]
    [Tooltip("Akrep (Saat kolu - clock_hour)")]
    public Transform hourHand;
    [Tooltip("Yelkovan (Dakika kolu - clock_minute)")]
    public Transform minuteHand;
    [Tooltip("Saniye kolu (clock_seconds)")]
    public Transform secondHand;

    [Header("Digital Time Input (Başlangıç Zamanı)")]
    [Tooltip("Başlangıç Saati (0 - 23)")]
    [Range(0, 23)]
    public int initialHour = 12;

    [Tooltip("Başlangıç Dakikası (0 - 59)")]
    [Range(0, 59)]
    public int initialMinute = 0;

    [Tooltip("Başlangıç Saniyesi (0 - 59)")]
    [Range(0, 59)]
    public int initialSecond = 0;

    [Tooltip("Zaman akış hızı (1 = gerçek zamanlı akış)")]
    public float timeScale = 1f;

    [Header("Output Angles (Hesaplanan Açılar - Derece)")]
    [Tooltip("Güncel Dijital Zaman (Saat:Dakika:Saniye)")]
    public string currentDigitalTime = "12:00:00";

    [Tooltip("Akrep Açısı (0° - 360°)")]
    public float hourAngle;

    [Tooltip("Yelkovan Açısı (0° - 360°)")]
    public float minuteAngle;

    [Tooltip("Saniye Kolu Açısı (0° - 360°)")]
    public float secondAngle;

    [Header("Model Rotation Settings")]
    [Tooltip("(Prb)Clock modeli için 12:00 referans rotasyonu (Y=90)")]
    public Vector3 baseEulerRotation = new Vector3(0f, 90f, 0f);

    [Tooltip("Saat yönü çarpanı ((Prb)Clock için +1 saat yönüdür)")]
    public bool invertDirection = false;

    private double _elapsedSeconds;

    void Reset()
    {
        AutoAssignHands();
        UpdateClockFromSeconds(GetInitialTotalSeconds());
    }

    void OnValidate()
    {
        AutoAssignHands();
        if (!Application.isPlaying)
        {
            UpdateClockFromSeconds(GetInitialTotalSeconds());
        }
    }

    void Start()
    {
        AutoAssignHands();
        _elapsedSeconds = GetInitialTotalSeconds();
        UpdateClockFromSeconds(_elapsedSeconds);
    }

    void Update()
    {
        if (!Application.isPlaying)
            return;

        _elapsedSeconds += (double)Time.deltaTime * timeScale;
        UpdateClockFromSeconds(_elapsedSeconds);
    }

    /// <summary>
    /// Dijital saat, dakika ve saniye girdisinden toplam saniyeyi hesaplar.
    /// </summary>
    public double GetInitialTotalSeconds()
    {
        return (initialHour % 24) * 3600.0 + initialMinute * 60.0 + initialSecond;
    }

    /// <summary>
    /// Dijital zamanı (saat, dakika, saniye) girdi olarak alır,
    /// akrep, yelkovan ve saniye kollarının açılarını (0-360 derece) output olarak döndürür.
    /// </summary>
    public static void CalculateAngles(int hour, int minute, float second, out float outHourAngle, out float outMinuteAngle, out float outSecondAngle)
    {
        float h12 = (hour % 12) + (minute / 60f) + (second / 3600f);
        float m60 = minute + (second / 60f);

        // Akrep: 12 saatte 360° -> saat başına 30°, dakika başına 0.5°
        outHourAngle = Mathf.Repeat(h12 * 30f, 360f);

        // Yelkovan: 60 dakikada 360° -> dakika başına 6°, saniye başına 0.1°
        outMinuteAngle = Mathf.Repeat(m60 * 6f, 360f);

        // Saniye: 60 saniyede 360° -> saniye başına 6°
        outSecondAngle = Mathf.Repeat(second * 6f, 360f);
    }

    /// <summary>
    /// Dijital zamanı dışarıdan ayarlamak ve açıları anında güncellemek için kullanılır.
    /// </summary>
    public void SetDigitalTime(int hour, int minute, int second = 0)
    {
        initialHour = Mathf.Clamp(hour, 0, 23);
        initialMinute = Mathf.Clamp(minute, 0, 59);
        initialSecond = Mathf.Clamp(second, 0, 59);
        _elapsedSeconds = GetInitialTotalSeconds();
        UpdateClockFromSeconds(_elapsedSeconds);
    }

    private void UpdateClockFromSeconds(double totalSeconds)
    {
        double wrappedDaySeconds = totalSeconds % 86400.0;
        if (wrappedDaySeconds < 0) wrappedDaySeconds += 86400.0;

        int hours24 = (int)(wrappedDaySeconds / 3600.0);
        int minutes = (int)((wrappedDaySeconds % 3600.0) / 60.0);
        float seconds = (float)(wrappedDaySeconds % 60.0);

        currentDigitalTime = $"{hours24:00}:{minutes:00}:{Mathf.FloorToInt(seconds):00}";

        CalculateAngles(hours24, minutes, seconds, out hourAngle, out minuteAngle, out secondAngle);
        ApplyAnglesToHands(hourAngle, minuteAngle, secondAngle);
    }

    private void ApplyAnglesToHands(float hAngle, float mAngle, float sAngle)
    {
        float dir = invertDirection ? -1f : 1f;

        if (hourHand != null)
            hourHand.localRotation = Quaternion.Euler(baseEulerRotation.x, baseEulerRotation.y, baseEulerRotation.z + hAngle * dir);

        if (minuteHand != null)
            minuteHand.localRotation = Quaternion.Euler(baseEulerRotation.x, baseEulerRotation.y, baseEulerRotation.z + mAngle * dir);

        if (secondHand != null)
            secondHand.localRotation = Quaternion.Euler(baseEulerRotation.x, baseEulerRotation.y, baseEulerRotation.z + sAngle * dir);
    }

    private void AutoAssignHands()
    {
        if (hourHand == null)
        {
            var t = transform.Find("clock/clock_hour") ?? transform.Find("clock_hour");
            if (t != null) hourHand = t;
        }
        if (minuteHand == null)
        {
            var t = transform.Find("clock/clock_minute") ?? transform.Find("clock_minute");
            if (t != null) minuteHand = t;
        }
        if (secondHand == null)
        {
            var t = transform.Find("clock/clock_seconds") ?? transform.Find("clock_seconds");
            if (t != null) secondHand = t;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DisableStaticOnHand(hourHand);
            DisableStaticOnHand(minuteHand);
            DisableStaticOnHand(secondHand);
        }
#endif
    }

#if UNITY_EDITOR
    private static void DisableStaticOnHand(Transform hand)
    {
        if (hand != null && hand.gameObject.isStatic)
        {
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(hand.gameObject, 0);
            hand.gameObject.isStatic = false;
            UnityEditor.EditorUtility.SetDirty(hand.gameObject);
        }
    }
#endif
}
