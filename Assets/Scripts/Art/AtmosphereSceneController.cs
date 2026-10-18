using UnityEngine;

namespace GameJam.Art
{
    public enum AtmosphereType
    {
        EvMarket_Aksham_Sisli,
        EvMarket_EvIci_Kasvetli,
        End_UnderRubble_Claustrophobic
    }

    /// <summary>
    /// Configures scene atmosphere, RenderSettings fog, and ambient lights
    /// according to the art direction.
    /// </summary>
    [ExecuteAlways]
    public class AtmosphereSceneController : MonoBehaviour
    {
        [SerializeField] private AtmosphereType atmosphereMode = AtmosphereType.EvMarket_Aksham_Sisli;
        [SerializeField] private bool applyOnStart = true;

        [Header("Outdoor Evening Fog")]
        [SerializeField] private Color outdoorFogColor = new Color(0.045f, 0.065f, 0.095f, 1f);
        [SerializeField] private float outdoorFogDensity = 0.032f;

        [Header("Indoor Gloomy Atmosphere")]
        [SerializeField] private Color indoorAmbientColor = new Color(0.12f, 0.11f, 0.10f, 1f);
        [SerializeField] private float indoorFogDensity = 0.008f;

        [Header("Rubble Deep Fog")]
        [SerializeField] private Color rubbleFogColor = new Color(0.02f, 0.028f, 0.045f, 1f);
        [SerializeField] private float rubbleFogDensity = 0.035f;

        private void OnEnable()
        {
            if (applyOnStart)
            {
                ApplyAtmosphere();
            }
        }

        private void Start()
        {
            if (applyOnStart)
            {
                ApplyAtmosphere();
            }
        }

        [ContextMenu("Apply Atmosphere Now")]
        public void ApplyAtmosphere()
        {
            switch (atmosphereMode)
            {
                case AtmosphereType.EvMarket_Aksham_Sisli:
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.ExponentialSquared;
                    RenderSettings.fogDensity = outdoorFogDensity;
                    RenderSettings.fogColor = outdoorFogColor;
                    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                    RenderSettings.ambientSkyColor = new Color(0.05f, 0.07f, 0.12f, 1f);
                    RenderSettings.ambientEquatorColor = new Color(0.03f, 0.04f, 0.06f, 1f);
                    RenderSettings.ambientGroundColor = new Color(0.015f, 0.018f, 0.025f, 1f);
                    break;

                case AtmosphereType.EvMarket_EvIci_Kasvetli:
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.ExponentialSquared;
                    RenderSettings.fogDensity = indoorFogDensity;
                    RenderSettings.fogColor = new Color(0.08f, 0.07f, 0.06f, 1f);
                    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                    RenderSettings.ambientLight = indoorAmbientColor;
                    break;

                case AtmosphereType.End_UnderRubble_Claustrophobic:
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.ExponentialSquared;
                    RenderSettings.fogDensity = rubbleFogDensity;
                    RenderSettings.fogColor = rubbleFogColor;
                    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                    RenderSettings.ambientSkyColor = new Color(0.03f, 0.04f, 0.06f, 1f);
                    RenderSettings.ambientEquatorColor = new Color(0.018f, 0.022f, 0.035f, 1f);
                    RenderSettings.ambientGroundColor = new Color(0.008f, 0.01f, 0.015f, 1f);
                    break;
            }
        }
    }
}
