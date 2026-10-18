using UnityEngine;

/// <summary>
/// Enkaz alanındaki acil durum işaret fişeği (flare) veya yangın/kıvılcım ışığının
/// gerçekçi şekilde titreşmesini (flicker) sağlayan hafif bileşen.
/// </summary>
[RequireComponent(typeof(Light))]
public class EmergencyFlickerLight : MonoBehaviour
{
    public float minIntensity = 1.8f;
    public float maxIntensity = 3.6f;
    public float flickerSpeed = 12.0f;

    private Light targetLight;
    private float seed;

    private void Awake()
    {
        targetLight = GetComponent<Light>();
        seed = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (targetLight == null) return;
        float noise = Mathf.PerlinNoise(seed, Time.time * flickerSpeed);
        targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}
