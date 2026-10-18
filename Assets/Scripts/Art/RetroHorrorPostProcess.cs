using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameJam.Art
{
    /// <summary>
    /// Manages retro-horror rendering aesthetics:
    /// - Low resolution render scale feel (0.65 - 0.75)
    /// - Very subtle vignette
    /// - Very subtle film grain / noise
    /// - Moody, cold/gloomy color grading
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class RetroHorrorPostProcess : MonoBehaviour
    {
        [Header("Low-Res / Retro Settings")]
        [Tooltip("Target render scale for retro horror pixel density feel (e.g., 0.72f)")]
        [Range(0.5f, 1.0f)]
        [SerializeField] private float retroRenderScale = 0.72f;
        [SerializeField] private bool applyRenderScale = true;

        [Header("Subtle Horror PostFX (Overrides Volume Profile)")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float vignetteIntensity = 0.22f;
        [Range(0.05f, 0.4f)]
        [SerializeField] private float filmGrainIntensity = 0.18f;
        [Range(0.0f, 0.3f)]
        [SerializeField] private float chromaticAberrationIntensity = 0.10f;

        [Header("Atmosphere Grading")]
        [Range(-1f, 1f)]
        [SerializeField] private float exposure = -0.15f;
        [Range(-30f, 50f)]
        [SerializeField] private float contrast = 18f;
        [Range(-50f, 20f)]
        [SerializeField] private float saturation = -12f;

        private Volume _volume;
        private VolumeProfile _profile;
        private Vignette _vignette;
        private FilmGrain _filmGrain;
        private ChromaticAberration _chromaticAberration;
        private ColorAdjustments _colorAdjustments;
        private ShadowsMidtonesHighlights _shadowsMidtones;

        private void OnEnable()
        {
            InitializeVolume();
            ApplySettings();
        }

        private void Start()
        {
            ApplySettings();
        }

        private void OnValidate()
        {
            ApplySettings();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ApplySettings();
            }
#endif
        }

        public void InitializeVolume()
        {
            if (_volume == null)
            {
                _volume = GetComponent<Volume>();
                if (_volume == null)
                {
                    _volume = gameObject.AddComponent<Volume>();
                    _volume.isGlobal = true;
                    _volume.priority = 100f;
                }
            }

            if (_volume.sharedProfile == null && _volume.profile == null)
            {
                _volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _volume.profile.name = "RetroHorror_RuntimeProfile";
            }

            _profile = _volume.profile != null ? _volume.profile : _volume.sharedProfile;

            if (_profile != null)
            {
                if (!_profile.TryGet(out _vignette))
                    _vignette = _profile.Add<Vignette>(true);

                if (!_profile.TryGet(out _filmGrain))
                    _filmGrain = _profile.Add<FilmGrain>(true);

                if (!_profile.TryGet(out _chromaticAberration))
                    _chromaticAberration = _profile.Add<ChromaticAberration>(true);

                if (!_profile.TryGet(out _colorAdjustments))
                    _colorAdjustments = _profile.Add<ColorAdjustments>(true);

                if (!_profile.TryGet(out _shadowsMidtones))
                    _shadowsMidtones = _profile.Add<ShadowsMidtonesHighlights>(true);
            }
        }

        public void ApplySettings()
        {
            // Apply Render Scale to active URP asset
            if (applyRenderScale)
            {
                UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (urpAsset == null)
                {
                    urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
                }

                if (urpAsset != null)
                {
                    urpAsset.renderScale = retroRenderScale;
                }
            }

            if (_profile == null)
            {
                InitializeVolume();
            }

            if (_profile == null) return;

            // 1. Very subtle Vignette
            if (_vignette != null)
            {
                _vignette.active = true;
                _vignette.intensity.overrideState = true;
                _vignette.intensity.value = vignetteIntensity;
                _vignette.smoothness.overrideState = true;
                _vignette.smoothness.value = 0.40f;
                _vignette.rounded.overrideState = true;
                _vignette.rounded.value = false;
            }

            // 2. Very subtle Film Grain
            if (_filmGrain != null)
            {
                _filmGrain.active = true;
                _filmGrain.type.overrideState = true;
                _filmGrain.type.value = FilmGrainLookup.Thin1;
                _filmGrain.intensity.overrideState = true;
                _filmGrain.intensity.value = filmGrainIntensity;
                _filmGrain.response.overrideState = true;
                _filmGrain.response.value = 0.85f;
            }

            // 3. Very subtle Chromatic Aberration
            if (_chromaticAberration != null)
            {
                _chromaticAberration.active = true;
                _chromaticAberration.intensity.overrideState = true;
                _chromaticAberration.intensity.value = chromaticAberrationIntensity;
            }

            // 4. Moody Gloomy Color Adjustments
            if (_colorAdjustments != null)
            {
                _colorAdjustments.active = true;
                _colorAdjustments.postExposure.overrideState = true;
                _colorAdjustments.postExposure.value = exposure;
                _colorAdjustments.contrast.overrideState = true;
                _colorAdjustments.contrast.value = contrast;
                _colorAdjustments.saturation.overrideState = true;
                _colorAdjustments.saturation.value = saturation;
            }

            // 5. Cold/Gloomy Shadows
            if (_shadowsMidtones != null)
            {
                _shadowsMidtones.active = true;
                _shadowsMidtones.shadows.overrideState = true;
                // Tint shadows cold navy/slate
                _shadowsMidtones.shadows.value = new Vector4(0.85f, 0.90f, 1.05f, 0f);
            }
        }
    }
}
