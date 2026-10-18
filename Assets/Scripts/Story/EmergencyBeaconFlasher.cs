using UnityEngine;

namespace GameJam.Story
{
    /// <summary>
    /// AFAD / İtfaiye / Polis acil durum çakar lambaları için ışık flaşörü.
    /// Kırmızı ve mavi ışıkları senkronize veya alternatif olarak flaşlatır.
    /// </summary>
    public class EmergencyBeaconFlasher : MonoBehaviour
    {
        [Header("Işıklar")]
        [SerializeField] private Light redLight;
        [SerializeField] private Light blueLight;

        [Header("Flaş Ayarları")]
        [SerializeField] private float flashSpeed = 3.5f;
        [SerializeField] private float maxIntensityRed = 5.0f;
        [SerializeField] private float maxIntensityBlue = 5.0f;
        [SerializeField] private bool strobeMode = true;

        private float timer;

        private void Awake()
        {
            if (redLight == null)
            {
                var redTransform = transform.Find("Red_Light");
                if (redTransform != null) redLight = redTransform.GetComponent<Light>();
            }

            if (blueLight == null)
            {
                var blueTransform = transform.Find("Blue_Light");
                if (blueTransform != null) blueLight = blueTransform.GetComponent<Light>();
            }
        }

        private void Update()
        {
            timer += Time.deltaTime * flashSpeed;

            if (strobeMode)
            {
                // Çift flaşlı acil durum stroboskop paterni
                float phase = Mathf.Repeat(timer, 2.0f);

                // 0.0 - 1.0 arası Kırmızı döngüsü
                if (phase < 1.0f)
                {
                    bool redOn = (phase < 0.2f) || (phase > 0.35f && phase < 0.55f);
                    if (redLight != null) redLight.intensity = redOn ? maxIntensityRed : 0f;
                    if (blueLight != null) blueLight.intensity = 0f;
                }
                // 1.0 - 2.0 arası Mavi döngüsü
                else
                {
                    float bluePhase = phase - 1.0f;
                    bool blueOn = (bluePhase < 0.2f) || (bluePhase > 0.35f && bluePhase < 0.55f);
                    if (redLight != null) redLight.intensity = 0f;
                    if (blueLight != null) blueLight.intensity = blueOn ? maxIntensityBlue : 0f;
                }
            }
            else
            {
                // Dönen döner tepe lambası simülasyonu
                float sin = Mathf.Sin(timer * Mathf.PI);
                if (redLight != null) redLight.intensity = Mathf.Max(0f, sin) * maxIntensityRed;
                if (blueLight != null) blueLight.intensity = Mathf.Max(0f, -sin) * maxIntensityBlue;
            }
        }
    }
}
