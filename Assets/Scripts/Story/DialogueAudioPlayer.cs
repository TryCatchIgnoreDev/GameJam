using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyun içindeki seslendirme ve diyalog ses dosyalarını yöneten yardımcı sınıf.
/// Assets/Resources/Sounds klasöründeki ses dosyalarını otomatik yükler ve oynatır.
/// </summary>
public static class DialogueAudioPlayer
{
    private static readonly Dictionary<string, AudioClip> cachedClips = new Dictionary<string, AudioClip>();
    private static AudioSource voiceSource;

    private static void EnsureAudioSource()
    {
        if (voiceSource == null)
        {
            GameObject go = GameObject.Find("GlobalVoiceAudioSource");
            if (go == null)
            {
                go = new GameObject("GlobalVoiceAudioSource");
                Object.DontDestroyOnLoad(go);
            }
            voiceSource = go.GetComponent<AudioSource>();
            if (voiceSource == null)
            {
                voiceSource = go.AddComponent<AudioSource>();
            }
            voiceSource.playOnAwake = false;
            voiceSource.spatialBlend = 0f; // 2D ses, diyaloglar her yerden net duyulsun
        }
    }

    /// <summary>
    /// İsmi verilen ses klibini (Örn: "1a", "2", "3", "7", "25") Resources/Sounds klasöründen yükler veya önbellekten alır.
    /// </summary>
    public static AudioClip GetClip(string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return null;

        // Uzantıyı kaldır (.mp3 varsa)
        if (clipName.EndsWith(".mp3"))
        {
            clipName = clipName.Substring(0, clipName.Length - 4);
        }

        if (cachedClips.TryGetValue(clipName, out AudioClip cached) && cached != null)
        {
            return cached;
        }

        // Resources/Sounds/{clipName} üzerinden yükle
        AudioClip loaded = Resources.Load<AudioClip>($"Sounds/{clipName}");
        if (loaded != null)
        {
            cachedClips[clipName] = loaded;
        }
        else
        {
            Debug.LogWarning($"[DialogueAudioPlayer] Ses klibi bulunamadı: Sounds/{clipName}");
        }

        return loaded;
    }

    /// <summary>
    /// İsmi verilen diyalog sesini çalar ve klip süresini döndürür.
    /// </summary>
    public static float PlayVoice(string clipName, float volume = 1.0f)
    {
        AudioClip clip = GetClip(clipName);
        if (clip != null)
        {
            EnsureAudioSource();
            voiceSource.PlayOneShot(clip, volume);
            return clip.length;
        }
        return 0f;
    }

    /// <summary>
    /// Ses efekti (siren, düdük vb.) çalar. Sahnede ses kaynağı atanmamış olabileceği için
    /// global AudioSource üzerinden çalınır; böylece efekt hiçbir zaman sessiz kalmaz.
    /// </summary>
    public static void PlaySfx(AudioClip clip, float volume = 1.0f)
    {
        if (clip == null) return;
        EnsureAudioSource();
        if (voiceSource != null)
        {
            voiceSource.PlayOneShot(clip, volume);
        }
    }

    /// <summary>
    /// O an oynatılan tüm diyalog seslerini durdurur.
    /// </summary>
    public static void StopVoice()
    {
        if (voiceSource != null && voiceSource.isPlaying)
        {
            voiceSource.Stop();
        }
    }
}
