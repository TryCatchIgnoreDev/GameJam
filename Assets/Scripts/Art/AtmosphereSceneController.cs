using UnityEngine;

namespace GameJam.Art
{
    public enum AtmosphereType
    {
        EvMarket_Aksham_Sisli,
        EvMarket_EvIci_Kasvetli,
        Intro_Kabus_Kasvetli,
        End_UnderRubble_Claustrophobic
    }

    /// <summary>
    /// Configures scene atmosphere, RenderSettings fog, and ambient lights
    /// according to the art direction:
    /// - EvMarket (Street): Evening, illuminated streetlights, tangible misty linear fog
    /// - EvMarket (Indoor): Gloomy, tense, dim chandelier/table warmth
    /// - Intro: Indoor nightmare premonition, gloomy misty linear fog
    /// - End_UnderRubble: Exact EvMarket fog & lighting match, linear mist wrapping rubble
    /// </summary>
    [ExecuteAlways]
    public class AtmosphereSceneController : MonoBehaviour
    {
        [SerializeField] public AtmosphereType atmosphereMode = AtmosphereType.EvMarket_Aksham_Sisli;
        [SerializeField] private bool applyOnStart = true;

        [Header("Outdoor Evening Fog (EvMarket & End_UnderRubble)")]
        [Tooltip("Soft atmospheric misty slate-blue fog color that reflects ambient evening sky")]
        [SerializeField] private Color outdoorFogColor = new Color(0.32f, 0.38f, 0.48f, 1.0f);
        [SerializeField] private float outdoorFogStart = 4.0f;
        [SerializeField] private float outdoorFogEnd = 55.0f;

        [Header("Indoor Gloomy Atmosphere (EvMarket)")]
        [SerializeField] private Color indoorAmbientColor = new Color(0.12f, 0.11f, 0.10f, 1f);
        [SerializeField] private float indoorFogDensity = 0.008f;

        [Header("Intro Indoor Fog (Intro)")]
        [SerializeField] private Color introFogColor = new Color(0.18f, 0.15f, 0.18f, 1.0f);
        [SerializeField] private float introFogStart = 1.0f;
        [SerializeField] private float introFogEnd = 18.0f;

        [Header("Rubble Atmosphere (End_UnderRubble)")]
        [Tooltip("Exact EvMarket fog & lighting match for rubble scenes with tuned distance for crawl & aerial view.")]
        [SerializeField] private bool rubbleUseRenderSettingsFog = true;
        [SerializeField] private Color rubbleFogColor = new Color(0.32f, 0.38f, 0.48f, 1.0f);
        [SerializeField] private float rubbleFogStart = 0.5f;
        [SerializeField] private float rubbleFogEnd = 28.0f;

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
                    RenderSettings.fogMode = FogMode.Linear;
                    RenderSettings.fogStartDistance = outdoorFogStart;
                    RenderSettings.fogEndDistance = outdoorFogEnd;
                    RenderSettings.fogColor = outdoorFogColor;

                    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                    RenderSettings.ambientSkyColor = new Color(0.24f, 0.28f, 0.38f, 1f);
                    RenderSettings.ambientEquatorColor = new Color(0.16f, 0.20f, 0.28f, 1f);
                    RenderSettings.ambientGroundColor = new Color(0.08f, 0.10f, 0.14f, 1f);

                    UpdateEveningSkybox();
                    break;

                case AtmosphereType.EvMarket_EvIci_Kasvetli:
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.ExponentialSquared;
                    RenderSettings.fogDensity = indoorFogDensity;
                    RenderSettings.fogColor = new Color(0.08f, 0.07f, 0.06f, 1f);
                    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                    RenderSettings.ambientLight = indoorAmbientColor;
                    break;

                case AtmosphereType.Intro_Kabus_Kasvetli:
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.Linear;
                    RenderSettings.fogStartDistance = introFogStart;
                    RenderSettings.fogEndDistance = introFogEnd;
                    RenderSettings.fogColor = introFogColor;

                    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                    RenderSettings.ambientLight = new Color(0.12f, 0.10f, 0.12f, 1f);
                    break;

                case AtmosphereType.End_UnderRubble_Claustrophobic:
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.Linear;
                    RenderSettings.fogStartDistance = rubbleFogStart;
                    RenderSettings.fogEndDistance = rubbleFogEnd;
                    RenderSettings.fogColor = rubbleFogColor;

                    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                    RenderSettings.ambientSkyColor = new Color(0.24f, 0.28f, 0.38f, 1f);
                    RenderSettings.ambientEquatorColor = new Color(0.16f, 0.20f, 0.28f, 1f);
                    RenderSettings.ambientGroundColor = new Color(0.08f, 0.10f, 0.14f, 1f);

                    UpdateEveningSkybox();
                    break;
            }
        }

        private void UpdateEveningSkybox()
        {
            Material sky = RenderSettings.skybox;
            if (sky != null && sky.shader != null && sky.shader.name == "Skybox/Procedural")
            {
                sky.SetColor("_SkyTint", outdoorFogColor);
                sky.SetColor("_GroundColor", new Color(0.14f, 0.18f, 0.24f, 1f));
                sky.SetFloat("_Exposure", 0.55f);
            }
        }
    }
}
