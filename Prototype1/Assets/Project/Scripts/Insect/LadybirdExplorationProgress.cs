using System.Collections;
using UnityEngine;

public class LadybirdExplorationProgress : MonoBehaviour
{
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
        antennaExplored = true;

        CheckCompletion();
    }


    // =========================
    // ELYTRA
    // =========================

    public void MarkElytraExplored()
    {
        elytraExplored = true;

        CheckCompletion();
    }


    // =========================
    // FLIGHT WINGS
    // =========================

    public void MarkWingExplored()
    {
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

            StartCoroutine(
                CompletionRoutine()
            );
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
    }


    // =========================
    // RESET
    // =========================

    public void ResetProgress()
    {
        antennaExplored = false;
        elytraExplored = false;
        wingExplored = false;

        completionTriggered = false;
    }
}