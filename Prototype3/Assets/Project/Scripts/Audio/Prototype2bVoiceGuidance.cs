using System.Collections;
using UnityEngine;

/// <summary>Optional Prototype2b narration. Gameplay owns stage entry; this component only plays audio.</summary>
[RequireComponent(typeof(AudioSource))]
public sealed class Prototype2bVoiceGuidance : MonoBehaviour
{
    public AudioClip voiceStartExploration, voiceHabitatFound, voiceStartFoodSearch,
        voiceWrongInsect, voiceMissionComplete;
    public NarrationManager existingNarration;
    public GameObject videoCard;
    [Range(0f, 1f)] public float volume = 1f;
    [Min(0f)] public float incorrectGrabCooldown = 4f;
    public enum Stage { Silent, Exploring, Habitat, FoodSearch, Complete }
    public Stage CurrentStage { get; private set; }
    public string CurrentGuidance { get; private set; } = "";
    AudioSource source;
    Coroutine playback;
    float nextIncorrectTime = float.NegativeInfinity;
    public const string StartExplorationText = "Great job! Now it's time to explore! Use your left thumbstick to guide your ladybird towards its habitat.";
    public const string HabitatFoundText = "You've found the habitat! Well done! Let's discover what ladybirds eat. Select Continue to learn more.";
    public const string StartFoodSearchText = "Now it's your turn! Use your controller to grab five aphids. Look carefully! Other insects are hiding here too.";
    public const string WrongInsectText = "Oops! That's an ant, not an aphid. Look for the tiny insects with soft, pear-shaped bodies. Try again!";
    public const string MissionCompleteText = "Amazing! You've caught all five aphids! Ladybirds help protect plants by eating aphids. Great work!";
    void Awake() { ConfigureSource(); }
    void ConfigureSource()
    {
        if (source == null) source = GetComponent<AudioSource>();
        source.playOnAwake = false; source.loop = false; source.spatialBlend = 0f;
        source.volume = volume;
    }
    public void EnterExploration() { Enter(Stage.Exploring, voiceStartExploration, StartExplorationText); }
    public void HabitatFound() { Enter(Stage.Habitat, voiceHabitatFound, HabitatFoundText); }
    public void StartFoodSearch() { Enter(Stage.FoodSearch, voiceStartFoodSearch, StartFoodSearchText); }
    public void MissionComplete() { Enter(Stage.Complete, voiceMissionComplete, MissionCompleteText); }
    void Enter(Stage stage, AudioClip clip, string text)
    {
        if (CurrentStage == stage) return;
        Silence(); CurrentStage = stage;
        nextIncorrectTime = float.NegativeInfinity;
        Request(clip, text);
    }
    public void IncorrectGrab()
    {
        if (CurrentStage != Stage.FoodSearch || Time.unscaledTime < nextIncorrectTime) return;
        nextIncorrectTime = Time.unscaledTime + incorrectGrabCooldown;
        Request(voiceWrongInsect, WrongInsectText);
    }
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
    void Request(AudioClip clip, string text)
    {
        StopPlayback(); CurrentGuidance = text;
        if (!isActiveAndEnabled || clip == null || VideoVisible) return;
        ConfigureSource();
        playback = StartCoroutine(PlayWhenAvailable(clip));
    }
    bool VideoVisible => videoCard != null && videoCard.activeInHierarchy;
    IEnumerator PlayWhenAvailable(AudioClip clip)
    {
        // The existing body/return narrator has priority. Stage changes cancel this wait.
        while (existingNarration != null && existingNarration.IsNarrationPlaying)
        { if (VideoVisible) yield break; yield return null; }
        if (VideoVisible) yield break;
        source.clip = clip; source.volume = volume; source.Play();
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
