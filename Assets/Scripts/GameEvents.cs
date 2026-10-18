using System;
using UnityEngine;

/// <summary>
/// Oyun içi global event yöneticisi.
/// Tüm sistemlerin event listener üzerinden haberleşmesini sağlar.
/// - Event 1: Objelerin sallanmasını başlatır / durdurur (ObjectShaker).
/// - Event 2: Zemin dilimleme ve tüm ev objelerine Rigidbody atama işlemini başlatır (HouseRigidbodyAssigner).
/// - Event 3: Karakter / NPC koşma olayını tetikler (male01_2 & male02_2).
/// - Event 4: Sohbet balonu / diyalog akışını başlatır / durdurur (DialogueSequencer).
/// </summary>
public static class GameEvents
{
    // ==========================================
    // EVENT 1: Sallanma (ObjectShaker)
    // ==========================================
    public static event Action OnEvent1_StartShaking;
    public static event Action OnEvent1_StopShaking;
    public static bool IsEvent1Active { get; private set; }

    public static void TriggerEvent1(bool start = true)
    {
        IsEvent1Active = start;
        if (start)
        {
            Debug.Log("[GameEvents] Event 1 (Sallanma Başlat) tetiklendi.");
            OnEvent1_StartShaking?.Invoke();
        }
        else
        {
            Debug.Log("[GameEvents] Event 1 (Sallanma Durdur) tetiklendi.");
            OnEvent1_StopShaking?.Invoke();
        }
    }

    public static void ToggleEvent1()
    {
        TriggerEvent1(!IsEvent1Active);
    }


    // ==========================================
    // EVENT 2: Rigidbody Atama ve Zemin Dilimleme (HouseRigidbodyAssigner)
    // ==========================================
    public static event Action OnEvent2_AssignRigidbodies;
    public static bool IsEvent2Triggered { get; private set; }

    public static void TriggerEvent2()
    {
        IsEvent2Triggered = true;
        Debug.Log("[GameEvents] Event 2 (Tüm objelere Rigidbody ekleme & zemin dilimleme) tetiklendi.");
        OnEvent2_AssignRigidbodies?.Invoke();
    }


    // ==========================================
    // EVENT 3: NPC Koşma Olayı (Running)
    // ==========================================
    public static event Action<bool> OnEventRunning;
    public static bool IsRunningActive { get; private set; }

    public static void TriggerRunning(bool run = true)
    {
        IsRunningActive = run;
        Debug.Log($"[GameEvents] Event 3 (Running): {(run ? "KOŞMA BAŞLADI" : "KOŞMA DURDU/OTURMA")}");
        OnEventRunning?.Invoke(run);
    }

    public static void ToggleRunning()
    {
        TriggerRunning(!IsRunningActive);
    }


    // ==========================================
    // EVENT 4: Diyalog / Sohbet Balonu Sırası (DialogueSequencer)
    // ==========================================
    public static event Action<bool> OnEventDialogue;
    public static bool IsDialogueActive { get; private set; }

    public static void TriggerDialogue(bool start = true)
    {
        IsDialogueActive = start;
        Debug.Log($"[GameEvents] Event 4 (Diyalog): {(start ? "BAŞLADI" : "DURDURULDU")}");
        OnEventDialogue?.Invoke(start);
    }

    public static void ToggleDialogue()
    {
        TriggerDialogue(!IsDialogueActive);
    }


    // ==========================================
    // EVENT 5: Çömelme / Çök-Kapan-Tutun (Crouching)
    // ==========================================
    public static event Action<bool> OnEventCrouching;
    public static bool IsCrouchingActive { get; private set; }

    public static void TriggerCrouching(bool crouch = true)
    {
        IsCrouchingActive = crouch;
        Debug.Log($"[GameEvents] Event 5 (Crouching): {(crouch ? "ÇÖMELME BAŞLADI" : "ÇÖMELME DURDU")}");
        OnEventCrouching?.Invoke(crouch);
    }

    public static void ToggleCrouching()
    {
        TriggerCrouching(!IsCrouchingActive);
    }


    /// <summary>
    /// Gerektiğinde tüm event durumlarını sıfırlamak için kullanılır.
    /// </summary>
    public static void ResetEvents()
    {
        IsEvent1Active = false;
        IsEvent2Triggered = false;
        IsRunningActive = false;
        IsDialogueActive = false;
        IsCrouchingActive = false;
    }
}
