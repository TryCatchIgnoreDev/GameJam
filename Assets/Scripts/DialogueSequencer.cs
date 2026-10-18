using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tamamen modüler Diyalog & Sohbet Balonu Zamanlayıcısı.
/// Inspector üzerinden kolayca konuşma adımları eklenebilir, hangi karakterin konuşacağı,
/// kaç saniye bekleyeceği, ne kadar ekranda kalacağı ve adım sonunda hangi Event'in (Sallanma, Yıkım, Koşma)
/// tetikleneceği seçilebilir.
/// </summary>
public class DialogueSequencer : MonoBehaviour
{
    public enum StepEventAction
    {
        None = 0,
        Event1_StartShake = 1,
        Event1_StopShake = 2,
        Event2_Destruction = 3,
        Event3_StartRun = 4,
        Event3_StopRun = 5
    }

    [Serializable]
    public class DialogueStep
    {
        [Tooltip("Inspector'da adımı kolay tanımak için ufak başlık (Opsiyonel).")]
        public string label = "Konuşma Adımı";

        [Tooltip("Konuşacak karakterin SpeechBubble bileşeni (male01_2 veya male02_2'yi sürükleyin).")]
        public SpeechBubble speaker;

        [Tooltip("Sohbet balonunda yazacak metin.")]
        [TextArea(2, 4)]
        public string message = "Merhaba!";

        [Tooltip("Bu konuşma başlamadan ÖNCE kaç saniye beklensin?")]
        public float delayBefore = 0f;

        [Tooltip("Balon ekranda kaç saniye açık kalsın?")]
        public float duration = 3f;

        [Tooltip("Sıradaki konuşmaya geçmeden önce bu balonun kapanmasını beklesin mi? (Kapalıysa iki kişi aynı anda konuşabilir)")]
        public bool waitForBubbleToFinish = true;

        [Tooltip("Bu konuşma BAŞLADIĞINDA tetiklenecek oyun olayı (Opsiyonel).")]
        public StepEventAction eventOnStart = StepEventAction.None;

        [Tooltip("Bu konuşma BİTTİĞİNDE tetiklenecek oyun olayı (Opsiyonel).")]
        public StepEventAction eventOnEnd = StepEventAction.None;

        [Tooltip("Bu adım tamamlandığında çalışacak ekstra özel UnityEvent.")]
        public UnityEvent onStepCompleted;
    }

    [Header("--- Genel Zamanlama Ayarları ---")]
    [Tooltip("Oyun başlar başlamaz diyalog otomatik başlasın mı?")]
    public bool playOnStart = false;

    [Tooltip("playOnStart açıksa, oyun başladıktan kaç saniye sonra ilk balon çıksın?")]
    public float initialDelay = 1.0f;

    [Tooltip("Diyalog bittiğinde başa sarıp tekrar etsin mi?")]
    public bool loopSequence = false;

    [Header("--- Konuşma Sırası (Modüler Liste) ---")]
    public List<DialogueStep> steps = new List<DialogueStep>();

    [Header("--- Diyalog Sonu Olayı ---")]
    public UnityEvent onSequenceCompleted;

    public bool IsPlaying { get; private set; }
    private Coroutine sequenceRoutine;

    void OnEnable()
    {
        GameEvents.OnEventDialogue += HandleDialogueEvent;
    }

    void OnDisable()
    {
        GameEvents.OnEventDialogue -= HandleDialogueEvent;
    }

    void Start()
    {
        if (playOnStart)
        {
            PlaySequence(initialDelay);
        }
    }

    private void HandleDialogueEvent(bool start)
    {
        if (start) PlaySequence(0f);
        else StopSequence();
    }

    /// <summary>
    /// Diyalog dizisini başlatır.
    /// </summary>
    public void PlaySequence(float customInitialDelay = 0f)
    {
        if (sequenceRoutine != null) StopCoroutine(sequenceRoutine);
        sequenceRoutine = StartCoroutine(RunSequenceRoutine(customInitialDelay));
    }

    /// <summary>
    /// Aktif diyaloğu durdurur ve açık olan tüm balonları kapatır.
    /// </summary>
    public void StopSequence()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }
        IsPlaying = false;

        foreach (var step in steps)
        {
            if (step.speaker != null)
            {
                step.speaker.Hide();
            }
        }
    }

    /// <summary>
    /// Kod üzerinden tek satırla hızlıca yeni bir konuşma adımı eklemek için yardımcı metod.
    /// </summary>
    public void AddStep(SpeechBubble speaker, string message, float duration = 3f, float delayBefore = 0f, bool wait = true)
    {
        steps.Add(new DialogueStep
        {
            label = $"{speaker?.name}: {message}",
            speaker = speaker,
            message = message,
            duration = duration,
            delayBefore = delayBefore,
            waitForBubbleToFinish = wait
        });
    }

    private IEnumerator RunSequenceRoutine(float startWait)
    {
        IsPlaying = true;

        if (startWait > 0f)
        {
            yield return new WaitForSeconds(startWait);
        }

        do
        {
            for (int i = 0; i < steps.Count; i++)
            {
                DialogueStep step = steps[i];
                if (step == null) continue;

                // 1. Konuşma öncesi bekleme
                if (step.delayBefore > 0f)
                {
                    yield return new WaitForSeconds(step.delayBefore);
                }

                // 2. Başlangıç Event'i varsa tetikle
                ExecuteStepEvent(step.eventOnStart);

                // 3. Balonu göster
                if (step.speaker != null && !string.IsNullOrEmpty(step.message))
                {
                    step.speaker.Say(step.message, step.duration);
                }

                // 4. Balonun bitmesini bekle (Eğer waitForBubbleToFinish açıksa)
                if (step.waitForBubbleToFinish && step.duration > 0f)
                {
                    // Daktilo süresini de hesaba kat
                    float extraTypeTime = 0f;
                    if (step.speaker != null && step.speaker.useTypewriter && step.speaker.charsPerSecond > 0f)
                    {
                        extraTypeTime = step.message.Length / step.speaker.charsPerSecond;
                    }
                    yield return new WaitForSeconds(step.duration + extraTypeTime + 0.2f);
                }

                // 5. Bitiş Event'i varsa tetikle
                ExecuteStepEvent(step.eventOnEnd);
                step.onStepCompleted?.Invoke();
            }
        }
        while (loopSequence && steps.Count > 0);

        IsPlaying = false;
        onSequenceCompleted?.Invoke();
    }

    private void ExecuteStepEvent(StepEventAction action)
    {
        switch (action)
        {
            case StepEventAction.Event1_StartShake:
                GameEvents.TriggerEvent1(true);
                break;
            case StepEventAction.Event1_StopShake:
                GameEvents.TriggerEvent1(false);
                break;
            case StepEventAction.Event2_Destruction:
                GameEvents.TriggerEvent2();
                break;
            case StepEventAction.Event3_StartRun:
                GameEvents.TriggerRunning(true);
                break;
            case StepEventAction.Event3_StopRun:
                GameEvents.TriggerRunning(false);
                break;
        }
    }

    #region Context Menüleri
    [ContextMenu("Diyaloğu Şimdi Başlat")]
    public void ContextPlay() => PlaySequence(0f);

    [ContextMenu("Diyaloğu Durdur")]
    public void ContextStop() => StopSequence();
    #endregion
}
