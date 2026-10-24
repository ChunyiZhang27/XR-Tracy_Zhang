using System.Collections;
using UnityEngine;

/// <summary>Optional Prototype2b narration. Gameplay owns stage entry; this component only plays audio.</summary>
[RequireComponent(typeof(AudioSource))]
public sealed class Prototype2bVoiceGuidance : MonoBehaviour
{
    // ===== 1. Inspector 音频引用 =====
    // AudioClip 保存录音内容；同物体 AudioSource 负责播放，本组件只协调时机，不推进游戏。
    public AudioClip voiceStartExploration, voiceHabitatFound, voiceStartFoodSearch,
        voiceWrongInsect, voiceMissionComplete;
    public AudioClip voiceFoodLearning1;
    [Tooltip("Catch count spoken in the assigned Start Food Search clip.")]
    [Min(1)] public int recordedStartFoodSearchAphidCount = 10;
    [Tooltip("Legacy authoring metadata retained for compatibility. Does not gate the assigned Mission Complete clip.")]
    [Min(1)] public int recordedFoodMissionAphidCount = 5;
    [Tooltip("Must match this scene's FoodSearchActivity.Required Aphids. Used to validate Start Food Search recordings.")]
    [Min(1)] public int foodMissionAphidCount = 10;
    public NarrationManager existingNarration;
    public GameObject videoCard;
    [Tooltip("Optional dedicated catch SFX source. Completion narration waits for its final one-shot to finish.")]
    public AudioSource completionFeedbackSource;
    [Range(0f, 1f)] public float volume = 1f;
    [Min(0f)] public float incorrectGrabCooldown = 4f;
    public bool logCompletionNarration = true;
    public enum Stage { Silent, Exploring, Habitat, FoodSearch, Complete, FoodLearning1 }
    public Stage CurrentStage { get; private set; }
    public string CurrentGuidance { get; private set; } = "";
    AudioSource source;
    Coroutine playback;
    float nextIncorrectTime = float.NegativeInfinity;
    public const string StartExplorationText = "Great job! Now it's time to explore! Use your left thumbstick to guide your ladybird towards its habitat.";
    public const string HabitatFoundText = "You've found the habitat! Well done! Let's discover what ladybirds eat. Select Continue to learn more.";
    public const string FoodLearning1Text = "Many ladybirds eat aphids! These tiny insects feed on plant sap, and ladybirds help keep their numbers under control. Now, let's take a closer look at what aphids look like!";
    public const string StartFoodSearchText = "Our ladybird is hungry! Let's help it find ten aphids for a tasty meal! Use your controller to grab the aphids on the leaf. Be careful! Other insects are hiding here too.";
    public const string WrongInsectText = "Oops! That's an ant, not an aphid. Look for the tiny insects with soft, pear-shaped bodies. Try again!";
    public const string MissionCompleteText = "Amazing! You've caught all ten aphids! Ladybirds help protect plants by eating aphids. Great work!";
    void Awake() { ConfigureSource(); }
    // 复用已有 AudioSource，关闭自动播放和循环，使用 2D 旁白避免距离衰减。
    void ConfigureSource()
    {
        if (source == null) source = GetComponent<AudioSource>();
        if (source == null) return;
        source.playOnAwake = false; source.loop = false; source.spatialBlend = 0f;
        source.volume = volume;
    }
    // ===== 2. 游戏阶段入口 =====
    // 由过渡、学习卡或任务脚本直接调用；食物开始录音还检查其口述数量是否匹配任务。
    public void EnterExploration() { Enter(Stage.Exploring, voiceStartExploration, StartExplorationText); }
    public void HabitatFound() { Enter(Stage.Habitat, voiceHabitatFound, HabitatFoundText); }
    public void FoodLearning1() { Enter(Stage.FoodLearning1, voiceFoodLearning1, FoodLearning1Text); }
    public void StartFoodSearch() { Enter(Stage.FoodSearch, recordedStartFoodSearchAphidCount == foodMissionAphidCount ? voiceStartFoodSearch : null, StartFoodSearchText); }
    // 完成请求只接受一次；旧数量元数据不再阻止已分配的完成录音。
    public void MissionComplete()
    {
        if (CurrentStage == Stage.Complete) { CompletionLog("Rejected duplicate request; completion already entered."); return; }
        CompletionLog("Requested clip: " + (voiceMissionComplete == null ? "MISSING" : voiceMissionComplete.name) + ".");
        Enter(Stage.Complete, voiceMissionComplete, MissionCompleteText);
    }
    void CompletionLog(string message)
    {
        if (logCompletionNarration) Debug.Log("[Mission Complete Voice] " + message, this);
    }
    // 重复进入同一阶段不重播；切换阶段先清理旧声音与等待协程，并重置错误语音冷却。
    void Enter(Stage stage, AudioClip clip, string text)
    {
        if (CurrentStage == stage) return;
        Silence(); CurrentStage = stage;
        nextIncorrectTime = float.NegativeInfinity;
        Request(clip, text);
    }
    // 任务验证真实错误抓取后调用；仅在 FoodSearch 阶段接受，并用实时冷却限制重复旁白。
    public void IncorrectGrab()
    {
        if (CurrentStage != Stage.FoodSearch || Time.unscaledTime < nextIncorrectTime) return;
        nextIncorrectTime = Time.unscaledTime + incorrectGrabCooldown;
        Request(voiceWrongInsect, WrongInsectText);
    }
    // ===== 3. 播放生命周期 =====
    // 导航、视频 Replay 或离开任务时调用，取消等待并停止过时旁白。
    public void Silence()
    {
        StopPlayback(); CurrentStage = Stage.Silent; CurrentGuidance = "";
    }
    void StopPlayback()
    {
        if (playback != null) StopCoroutine(playback);
        playback = null;
        if (source != null) { source.Stop(); source.clip = null; }
    }
    // 新请求先替换旧 guidance；缺少录音、组件禁用或视频卡可见时不播放。
    void Request(AudioClip clip, string text)
    {
        StopPlayback(); CurrentGuidance = text;
        if (!isActiveAndEnabled || clip == null || VideoVisible)
        {
            if (CurrentStage == Stage.Complete) CompletionLog("Playback rejected: " +
                (!isActiveAndEnabled ? "voice manager inactive." : clip == null ? "AudioClip missing." : "educational video visible."));
            return;
        }
        ConfigureSource();
        if (source == null || !source.isActiveAndEnabled)
        {
            if (CurrentStage == Stage.Complete) CompletionLog("Playback rejected: voice AudioSource missing or inactive.");
            return;
        }
        playback = StartCoroutine(PlayWhenAvailable(clip));
    }
    bool VideoVisible => videoCard != null && videoCard.activeInHierarchy;
    // 原身体/返回旁白优先；完成语音还等待最后的成功音效结束，避免不必要的音频重叠。
    IEnumerator PlayWhenAvailable(AudioClip clip)
    {
        // The existing body/return narrator has priority. Stage changes cancel this wait.
        if (CurrentStage == Stage.Complete && ((existingNarration != null && existingNarration.IsNarrationPlaying) ||
            (completionFeedbackSource != null && completionFeedbackSource.isPlaying)))
            CompletionLog("Queued until existing narration and final catch SFX finish.");
        while ((existingNarration != null && existingNarration.IsNarrationPlaying) ||
            (CurrentStage == Stage.Complete && completionFeedbackSource != null && completionFeedbackSource.isPlaying))
        { if (VideoVisible) yield break; yield return null; }
        if (VideoVisible) yield break;
        if (source == null || !source.isActiveAndEnabled)
        {
            if (CurrentStage == Stage.Complete) CompletionLog("Playback rejected after waiting: voice AudioSource missing or inactive.");
            playback = null; yield break;
        }
        source.clip = clip; source.volume = volume; source.Play();
        if (CurrentStage == Stage.Complete) CompletionLog(source.isPlaying
            ? "Playback started: " + clip.name + "." : "Playback did not start; check clip load state and AudioSource.");
        while (source.isPlaying)
        {
            if (VideoVisible || (existingNarration != null && existingNarration.IsNarrationPlaying))
            { source.Stop(); break; }
            source.volume = volume;
            yield return null;
        }
        playback = null;
    }
    void OnDisable() { Silence(); }
}
