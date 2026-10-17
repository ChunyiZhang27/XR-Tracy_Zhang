using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Video;

/// <summary>Adds video only to the saved second learning card, without moving its display anchor.</summary>
public static class Prototype2bAphidVideoSetup
{
    const string ScenePath = "Assets/Project/Scenes/Prototype2b.unity";
    const string TexturePath = "Assets/Project/FoodSearch/AphidLearningVideo.renderTexture";
    public static void ConfigureBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only.");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var objective = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<LadybirdHabitatObjective>(true)).Single();
        var card = objective.foodLearningCard2;
        if (card == null || card.GetComponent<AphidLearningVideo>() != null) throw new InvalidOperationException("Missing card or video already configured.");
        var title = card.transform.Find("Title").GetComponent<TMP_Text>();
        var description = card.transform.Find("Description").GetComponent<TMP_Text>();
        Debug.Log($"VIDEO existing title font={title.fontSize}, description font={description.fontSize}, card={((RectTransform)card.transform).rect}");
        Rect(title.rectTransform, 0, 130, 660, 60);
        Rect(description.rectTransform, 175, 10, 290, 180);
        description.fontSize = 22; description.enableAutoSizing = false;
        var video = card.AddComponent<AphidLearningVideo>();
        var player = card.AddComponent<VideoPlayer>();
        player.playOnAwake = false; player.isLooping = false; player.source = VideoSource.VideoClip;
        player.renderMode = VideoRenderMode.RenderTexture; player.audioOutputMode = VideoAudioOutputMode.None;
        player.aspectRatio = VideoAspectRatio.FitInside; player.waitForFirstFrame = true; player.sendFrameReadyEvents = true;
        var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(TexturePath);
        if (texture == null)
        {
            texture = new RenderTexture(512, 288, 0, RenderTextureFormat.ARGB32) { name = "AphidLearningVideo", useMipMap = false, autoGenerateMips = false, antiAliasing = 1 };
            AssetDatabase.CreateAsset(texture, TexturePath);
        }
        player.targetTexture = texture;
        var area = NewRect("VideoArea", card.transform, -155, 10, 320, 180);
        var background = area.gameObject.AddComponent<UnityEngine.UI.Image>(); background.color = new Color(.05f,.08f,.04f,.95f); background.raycastTarget = false;
        var imageRect = NewRect("VideoImage", area, 0, 0, 320, 180);
        var image = imageRect.gameObject.AddComponent<UnityEngine.UI.RawImage>(); image.texture = texture; image.raycastTarget = false; image.enabled = false;
        var aspect = imageRect.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
        aspect.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent; aspect.aspectRatio = 16f/9f;
        var placeholder = UnityEngine.Object.Instantiate(description, area, false); placeholder.name = "VideoFallback";
        Rect(placeholder.rectTransform, 0, 0, 300, 170); placeholder.fontSize = 19; placeholder.alignment = TextAlignmentOptions.Center;
        placeholder.color = Color.white; placeholder.raycastTarget = false; placeholder.text = AphidLearningVideo.MissingMediaMessage;
        var subtitleBackground = NewRect("SubtitleBackground", area, 0, -61, 320, 58);
        var shade = subtitleBackground.gameObject.AddComponent<UnityEngine.UI.Image>(); shade.color = new Color(0,0,0,.75f); shade.raycastTarget = false;
        var subtitle = UnityEngine.Object.Instantiate(placeholder, subtitleBackground, false); subtitle.name = "Subtitles";
        Rect(subtitle.rectTransform, 0, 0, 304, 54); subtitle.fontSize = 19; subtitle.text = AphidLearningVideo.SubtitleAt(0);
        subtitleBackground.gameObject.SetActive(false);
        var back = card.transform.Find("BackButton").GetComponent<UnityEngine.UI.Button>();
        var start = card.transform.Find("StartFoodSearchButton").GetComponent<UnityEngine.UI.Button>();
        var replay = UnityEngine.Object.Instantiate(back, card.transform, false); replay.name = "ReplayVideoButton";
        for(int i=replay.onClick.GetPersistentEventCount()-1;i>=0;i--) UnityEventTools.RemovePersistentListener(replay.onClick,i);
        UnityEventTools.AddPersistentListener(replay.onClick,video.Replay);
        replay.GetComponentInChildren<TMP_Text>(true).text = "Replay";
        Button(back,-245,140); Button(replay,-85,140); Button(start,165,320);
        foreach(var text in card.GetComponentsInChildren<TMP_Text>(true)) text.raycastTarget = false;
        video.player = player; video.renderTexture = texture; video.videoImage = image;
        video.fallbackText = placeholder; video.subtitles = subtitle; video.subtitleBackground = subtitleBackground.gameObject;
        var clips = AssetDatabase.FindAssets("AphidIntroduction t:VideoClip").Select(AssetDatabase.GUIDToAssetPath)
            .Where(x=>System.IO.Path.GetFileName(x)=="AphidIntroduction.mp4").ToArray();
        if(clips.Length==1) video.clip = AssetDatabase.LoadAssetAtPath<VideoClip>(clips[0]);
        player.clip = video.clip;
        EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets();
        if(!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Save failed.");
        Debug.Log("APHID VIDEO configured: existing Card 2, 16:9, Replay callback, assignable clip="+(video.clip==null?"missing (educational fallback)":video.clip.name));
    }
    public static void IntegrateFinishedBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only.");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var objective = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<LadybirdHabitatObjective>(true)).Single();
        var card = objective.foodLearningCard2;
        var video = card.GetComponent<AphidLearningVideo>();
        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Project/Videos/AphidIntroduction.mp4");
        if (clip == null || clip.audioTrackCount == 0 || video == null || video.player == null || video.renderTexture == null)
            throw new InvalidOperationException("Expected the imported narrated clip and existing video UI.");
        if (card.GetComponents<VideoPlayer>().Length != 1 || card.GetComponents<AudioSource>().Length > 1)
            throw new InvalidOperationException("Inspect duplicate media components before proceeding.");
        var source = card.GetComponent<AudioSource>();
        if (source == null) source = card.AddComponent<AudioSource>();
        source.playOnAwake = false; source.loop = false; source.spatialBlend = 0;
        source.volume = 1; source.mute = false; source.clip = null;
        video.clip = clip; video.narrationAudioSource = source;
        video.showSubtitles = false; // The finished video already contains English captions.
        var player = video.player;
        player.clip = clip; player.playOnAwake = false; player.isLooping = false;
        player.audioOutputMode = VideoAudioOutputMode.AudioSource; player.controlledAudioTrackCount = 1;
        player.EnableAudioTrack(0, true); player.SetTargetAudioSource(0, source);
        player.aspectRatio = VideoAspectRatio.FitInside;
        // Native 720p preserves the finished video's fine details with a single small render target.
        video.renderTexture.Release(); video.renderTexture.width = 1280; video.renderTexture.height = 720;
        video.renderTexture.useMipMap = false; video.renderTexture.autoGenerateMips = false;
        video.videoImage.raycastTarget = false;
        EditorUtility.SetDirty(video.renderTexture); EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Save failed.");
        Debug.Log($"FINISHED APHID VIDEO: {AssetDatabase.GetAssetPath(clip)}, {clip.width}x{clip.height}, {clip.frameRate}fps, {clip.length:F3}s, audioTracks={clip.audioTrackCount}, channels={clip.GetAudioChannelCount(0)}, sampleRate={clip.GetAudioSampleRate(0)}; one 2D AudioSource, non-looping, native 16:9 texture.");
    }
    static RectTransform NewRect(string name,Transform parent,float x,float y,float width,float height)
    {
        var go = new GameObject(name,typeof(RectTransform)); go.layer = parent.gameObject.layer; go.transform.SetParent(parent,false);
        var rect = (RectTransform)go.transform; Rect(rect,x,y,width,height); return rect;
    }
    static void Rect(RectTransform rect,float x,float y,float width,float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
        rect.anchoredPosition = new Vector2(x,y); rect.sizeDelta = new Vector2(width,height);
    }
    static void Button(UnityEngine.UI.Button button,float x,float width)
    {
        Rect((RectTransform)button.transform,x,-125,width,65);
        var text = button.GetComponentInChildren<TMP_Text>(true); Rect(text.rectTransform,0,0,width-16,55);
    }
}
