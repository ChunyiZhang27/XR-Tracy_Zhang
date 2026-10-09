using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class LadybirdExplorationProgress : MonoBehaviour
{
    [Header("Prototype2b opt-in")]
    public bool verifyTransitionReadiness;
    public AntennaInteraction antennaInteraction;
    public WingInteraction wingInteraction;
    public ElytraInteraction elytraInteraction;
    public UnityEvent environmentReady = new UnityEvent();
    public bool EnvironmentReady { get; private set; }
    public bool BodyPartsComplete => antennaExplored && elytraExplored && wingExplored;
    private Coroutine completionRoutine;

    [Header("Narration")]
    [SerializeField]
    private NarrationManager narrationManager;


    [Header("Completion Timing")]
    [SerializeField]
    private float completionDelay = 0.4f;


    // =========================
    // EXPLORATION STATE
    // =========================

    private bool antennaExplored = false;
    private bool elytraExplored = false;
    private bool wingExplored = false;

    private bool completionTriggered = false;


    // 每次这个 Ladybird Explore 对象重新启用时
    // 都重新开始记录探索进度
    private void OnEnable()
    {
        ResetProgress();
    }


    // =========================
    // ANTENNAE
    // =========================

    public void MarkAntennaExplored()
    {
        if (verifyTransitionReadiness && (antennaInteraction == null || !antennaInteraction.LastInteractionAccepted)) return;
        antennaExplored = true;

        CheckCompletion();
    }


    // =========================
    // ELYTRA
    // =========================

    public void MarkElytraExplored()
    {
        if (verifyTransitionReadiness && (elytraInteraction == null || !elytraInteraction.LastInteractionAccepted)) return;
        elytraExplored = true;

        CheckCompletion();
    }


    // =========================
    // FLIGHT WINGS
    // =========================

    public void MarkWingExplored()
    {
        if (verifyTransitionReadiness && (wingInteraction == null || !wingInteraction.LastInteractionAccepted)) return;
        wingExplored = true;

        CheckCompletion();
    }


    // =========================
    // CHECK COMPLETION
    // =========================

    private void CheckCompletion()
    {
        // 已经触发过完成语音，
        // 就不要再次触发。
        if (completionTriggered)
        {
            return;
        }


        if (
            antennaExplored &&
            elytraExplored &&
            wingExplored
        )
        {
            completionTriggered = true;

            completionRoutine = StartCoroutine(CompletionRoutine());
        }
    }


    // =========================
    // COMPLETION SEQUENCE
    // =========================

    private IEnumerator CompletionRoutine()
    {
        // 等一帧。
        //
        // 因为 Select Entered 同一帧里，
        // body-part narration 也会刚刚开始播放。
        yield return null;


        if (verifyTransitionReadiness && narrationManager != null)
            while (!narrationManager.BodyNarrationsComplete) yield return null;

        // 等最后一个身体部位的 narration
        // 完整播放结束。
        if (narrationManager != null)
        {
            while (narrationManager.IsNarrationPlaying)
            {
                yield return null;
            }
        }


        // 稍微停顿一下，
        // 避免两条 narration 紧紧连在一起。
        yield return new WaitForSeconds(
            completionDelay
        );


        // 播放 Ladybird 完成语音。
        if (narrationManager != null)
        {
            narrationManager
                .PlayLadybirdCompleteNarration();
        }
        if (verifyTransitionReadiness)
        {
            yield return null;
            if (narrationManager != null)
                while (!narrationManager.LadybirdCompletionNarrationComplete)
                {
                    // If another narration interrupts completion, retry when the source is idle.
                    if (!narrationManager.IsNarrationPlaying)
                    {
                        yield return new WaitForSeconds(0.1f);
                        if (!narrationManager.LadybirdCompletionNarrationComplete && !narrationManager.IsNarrationPlaying)
                            narrationManager.PlayLadybirdCompleteNarration();
                    }
                    yield return null;
                }
            EnvironmentReady = true;
            environmentReady.Invoke();
        }
        completionRoutine = null;
    }


    // =========================
    // RESET
    // =========================

    private void OnDisable()
    {
        if (!verifyTransitionReadiness) return;
        if (completionRoutine != null) StopCoroutine(completionRoutine);
        completionRoutine = null;
    }

    public void ResetProgress()
    {
        if (verifyTransitionReadiness && completionRoutine != null) StopCoroutine(completionRoutine);
        completionRoutine = null;
        EnvironmentReady = false;
        antennaExplored = false;
        elytraExplored = false;
        wingExplored = false;

        completionTriggered = false;
    }
}