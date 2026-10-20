using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Assigns verified project clips to the existing Prototype2b manager only.</summary>
public static class Prototype2bVoiceClipAssignment
{
    const string ScenePath = "Assets/Project/Scenes/Prototype2b.unity";
    const string Folder = "Assets/Project/Audio/VoicePrompts/";
    static readonly string[] Names = {
        "Voice_StartExploration", "Voice_HabitatFound", "Voice_StartFoodSearch",
        "Voice_WrongInsect", "Voice_MissionComplete"
    };

    [MenuItem("Tiny Worlds/Prototype2b/Assign Voice Guidance Clips")]
    public static void Assign()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before assigning voice clips.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException("Open Prototype2b.unity as the active scene first.");
        var managers = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Prototype2bVoiceGuidance>(true)).ToArray();
        if (managers.Length != 1)
            throw new InvalidOperationException("Expected exactly one existing Prototype2bVoiceGuidance; nothing was changed.");
        var voice = managers[0];
        var sources = voice.GetComponents<AudioSource>();
        if (sources.Length != 1 || sources[0].spatialBlend != 0f || voice.incorrectGrabCooldown != 4f)
            throw new InvalidOperationException("Expected the existing single 2D AudioSource and four-second cooldown; nothing was changed.");
        var clips = Names.Select(name => AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + name + ".wav")).ToArray();
        for (int i = 0; i < clips.Length; i++)
            if (clips[i] == null)
                throw new InvalidOperationException("Missing imported AudioClip: " + Folder + Names[i] + ".wav. Nothing was changed.");
        // Validate every asset before changing anything. Undo and normal scene saving remain available.
        Undo.RecordObject(voice, "Assign Prototype2b voice clips");
        voice.voiceStartExploration = clips[0];
        voice.voiceHabitatFound = clips[1];
        voice.voiceStartFoodSearch = clips[2];
        voice.voiceWrongInsect = clips[3];
        voice.voiceMissionComplete = clips[4];
        EditorUtility.SetDirty(voice);
        EditorSceneManager.MarkSceneDirty(scene);
        for (int i = 0; i < clips.Length; i++)
            Debug.Log("[Prototype2b Voice] Assigned " + AssetDatabase.GetAssetPath(clips[i]) + " (" + clips[i].length.ToString("F2") + "s)", voice);
    }

    // For an isolated batch validation copy; never opens or saves another scene.
    public static void AssignBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use the menu action in an interactive Editor.");
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Assign();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }
}
