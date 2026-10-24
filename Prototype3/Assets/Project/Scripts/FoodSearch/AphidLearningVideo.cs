using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

/// <summary>Card-local video lifecycle; never gates navigation or changes Food Search state.</summary>
[DisallowMultipleComponent]
public sealed class AphidLearningVideo : MonoBehaviour
{
    [Tooltip("Assign the imported H.264 AphidIntroduction.mp4 VideoClip here. Optional.")]
    // ===== 1. 视频显示与音轨引用 =====
    // VideoPlayer 解码到 RenderTexture，RawImage 在原世界空间卡片内显示同一张纹理。
    public Prototype2bVoiceGuidance voiceGuidance;
    public VideoClip clip;
    public VideoPlayer player;
    [Tooltip("Dedicated 2D source for the embedded narration; never play it independently.")]
    public AudioSource narrationAudioSource;
    public RenderTexture renderTexture;
    public UnityEngine.UI.RawImage videoImage;
    public TMP_Text fallbackText, subtitles;
    public GameObject subtitleBackground;
    public bool showSubtitles = true;
    [Min(1f)] public float preparationTimeout = 12f;
    public enum PlaybackState { Hidden, MissingMedia, Preparing, Playing, Finished, Unavailable }
    public PlaybackState State { get; private set; } = PlaybackState.Hidden;
    public const string MissingMediaMessage = "Video coming soon\n\nLook for a pear-shaped body, six legs and antennae. Aphids often gather on leaves and stems.\n\nYou can start Food Search now.";
    Coroutine timeout;

    // ===== 2. 卡片生命周期 =====
    // 第二张卡出现时注册准备、首帧、错误和结束回调，并自动从头准备视频。
    void OnEnable()
    {
        if (player != null)
        {
            player.prepareCompleted += Prepared;
            player.frameReady += FrameReady;
            player.errorReceived += Failed;
            player.loopPointReached += Finished;
        }
        Replay();
    }
    // 离开卡片时取消超时、移除对应回调，并停止视频和内嵌旁白，避免下次重复订阅。
    void OnDisable()
    {
        CancelTimeout();
        if (player != null)
        {
            player.prepareCompleted -= Prepared;
            player.frameReady -= FrameReady;
            player.errorReceived -= Failed;
            player.loopPointReached -= Finished;
            player.Stop();
        }
        if (narrationAudioSource != null) narrationAudioSource.Stop();
        State = PlaybackState.Hidden;
        ShowVideo(false);
    }
    // ===== 3. 准备与播放 =====
    // Replay 先停旧声音和视频，再准备新一轮播放；媒体缺失仍允许继续食物任务。
    public void Replay()
    {
        if (!isActiveAndEnabled) return;
        if (voiceGuidance != null) voiceGuidance.Silence();
        CancelTimeout();
        if (player != null) player.Stop();
        if (narrationAudioSource != null) narrationAudioSource.Stop();
        ShowVideo(false);
        if (clip == null) { Fallback(PlaybackState.MissingMedia, MissingMediaMessage); return; }
        if (player == null || renderTexture == null || videoImage == null)
        { Fallback(PlaybackState.Unavailable, "Video unavailable. Look for a pear-shaped body, six legs and antennae. You can start Food Search now."); return; }
        player.playOnAwake = false; player.isLooping = false;
        player.source = VideoSource.VideoClip; player.clip = clip;
        player.renderMode = VideoRenderMode.RenderTexture; player.targetTexture = renderTexture;
        player.aspectRatio = VideoAspectRatio.FitInside;
        ConfigureNarration();
        player.sendFrameReadyEvents = true; player.waitForFirstFrame = true;
        videoImage.texture = renderTexture;
        Fallback(PlaybackState.Preparing, "Preparing aphid video…\n\nYou can start Food Search at any time.");
        player.Prepare();
        // Preparation and first-frame decode must both finish; a decoder failure never blocks navigation.
        if (State == PlaybackState.Preparing) timeout = StartCoroutine(WaitForPreparation());
    }
    // 只输出第一条内嵌音轨到独立 2D AudioSource；播放时钟由 VideoPlayer 管理，不另播一份旁白。
    void ConfigureNarration()
    {
        // Only the first embedded track is audible; VideoPlayer owns its playback clock.
        player.controlledAudioTrackCount = (ushort)(clip.audioTrackCount > 0 ? 1 : 0);
        if (narrationAudioSource != null)
        {
            narrationAudioSource.playOnAwake = false;
            narrationAudioSource.loop = false;
            narrationAudioSource.spatialBlend = 0f;
            narrationAudioSource.clip = null;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            if (clip.audioTrackCount > 0)
            {
                player.EnableAudioTrack(0, true);
                player.SetTargetAudioSource(0, narrationAudioSource);
            }
        }
        else
        {
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            if (clip.audioTrackCount > 0)
            {
                player.EnableAudioTrack(0, true);
                player.SetDirectAudioMute(0, false);
                player.SetDirectAudioVolume(0, 1f);
            }
        }
    }
    // 准备或首帧解码超时后显示文字退路；不会锁住 Back 或 Start 按钮。
    IEnumerator WaitForPreparation()
    {
        yield return new WaitForSecondsRealtime(preparationTimeout);
        timeout = null;
        if (State == PlaybackState.Preparing)
        {
            player.Stop();
            if (narrationAudioSource != null) narrationAudioSource.Stop();
            Fallback(PlaybackState.Unavailable, "Video could not load. Select Replay to retry, or start Food Search.\n\nLook for a pear-shaped body, six legs and antennae.");
        }
    }
    // 准备完成才 Play；收到首帧后才显示 RawImage，避免先展示旧帧。
    void Prepared(VideoPlayer source)
    {
        if (Accept(source) && State == PlaybackState.Preparing) source.Play();
    }
    void FrameReady(VideoPlayer source, long frame)
    {
        if (!Accept(source) || State != PlaybackState.Preparing) return;
        CancelTimeout(); State = PlaybackState.Playing; ShowVideo(true); UpdateSubtitles();
    }
    // 解码失败时停止音视频并显示可重试提示，保留导航。
    void Failed(VideoPlayer source, string message)
    {
        if (!Accept(source)) return;
        CancelTimeout(); source.Stop();
        if (narrationAudioSource != null) narrationAudioSource.Stop();
        Fallback(PlaybackState.Unavailable, "Video unavailable. Select Replay to retry, or start Food Search.\n\nLook for a pear-shaped body, six legs and antennae.");
        Debug.LogWarning("[Aphid Learning Video] " + message, this);
    }
    // 正常播完保留最后一帧，不自动循环；Replay 或离开卡片才改变显示。
    void Finished(VideoPlayer source)
    {
        if (!Accept(source) || State != PlaybackState.Playing) return;
        State = PlaybackState.Finished; // Keep the last frame visible until Replay or navigation.
    }
    bool Accept(VideoPlayer source) => isActiveAndEnabled && source == player && source.clip == clip;
    void CancelTimeout() { if (timeout != null) StopCoroutine(timeout); timeout = null; }
    void Fallback(PlaybackState state, string message)
    {
        State = state; ShowVideo(false);
        if (fallbackText != null) fallbackText.text = message;
    }
    void ShowVideo(bool visible)
    {
        if (videoImage != null) videoImage.enabled = visible;
        if (fallbackText != null) fallbackText.enabled = !visible;
        if (subtitles != null) subtitles.enabled = visible && showSubtitles;
        if (subtitleBackground != null) subtitleBackground.SetActive(visible && showSubtitles);
    }
    void Update() { if (State == PlaybackState.Playing) UpdateSubtitles(); }
    // 按播放器时间选择短字幕；是否显示仍受 Inspector 的 showSubtitles 控制。
    void UpdateSubtitles()
    {
        if (subtitles != null && player != null) subtitles.text = SubtitleAt(player.time);
    }
    public static string SubtitleAt(double seconds)
    {
        if (seconds < 4) return "Aphids are tiny insects that feed on plant sap.";
        if (seconds < 9) return "Look for a pear-shaped body, six legs and antennae.";
        return "Can you recognise and find ten aphids?";
    }
}
