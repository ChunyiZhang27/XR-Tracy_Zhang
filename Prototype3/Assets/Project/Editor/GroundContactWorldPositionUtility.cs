using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Editor menu commands only. Capture reads world space; applying changes only HabitatTarget on explicit request.</summary>
public static class GroundContactWorldPositionUtility
{
    const string ScenePath = "Assets/Project/Scenes/Prototype2b.unity";
    const string CaptureKey = "TinyWorlds.Prototype2b.GroundContactWorldPosition";
    const string MenuRoot = "Tools/Tiny Worlds/Habitat Position/";
    static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    [MenuItem(MenuRoot + "Capture Selected GroundContact (Play Mode)")]
    static void CaptureSelected()
    {
        if (!CanCapture())
        {
            EditorUtility.DisplayDialog("Select GroundContact",
                "Enter Play Mode in Prototype2b, select the Ladybird's GroundContact child in the Hierarchy, then run this command. Paused Play Mode is supported.", "OK");
            return;
        }
        // Transform.position is world space, independent of parent translation, rotation or scale.
        Vector3 world = Selection.activeTransform.position;
        SessionState.SetString(CaptureKey, world.x.ToString("R", Invariant) + ";" +
            world.y.ToString("R", Invariant) + ";" + world.z.ToString("R", Invariant));
        Copy(world);
        EditorUtility.DisplayDialog("GroundContact — captured world position",
            Format(world) + "\n\nCopied to the clipboard. The full-precision capture is retained for this Editor session, including after leaving Play Mode.\n\nTo save it on HabitatTarget: exit Play Mode, choose Apply Captured Position to HabitatTarget, then save Prototype2b.", "OK");
    }

    [MenuItem(MenuRoot + "Capture Selected GroundContact (Play Mode)", true)]
    static bool CanCapture()
    {
        var selected = Selection.activeTransform;
        return EditorApplication.isPlaying && selected != null && selected.name == "GroundContact" &&
            selected.gameObject.scene.path == ScenePath;
    }

    [MenuItem(MenuRoot + "Copy Last Captured World Position")]
    static void CopyLast()
    {
        if (TryRead(out var world)) Copy(world);
    }
    [MenuItem(MenuRoot + "Copy Last Captured World Position", true)]
    static bool CanCopyLast() { return TryRead(out _); }

    [MenuItem(MenuRoot + "Apply Captured Position to HabitatTarget")]
    static void ApplyToTarget()
    {
        if (!TryRead(out var world)) return;
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        Transform target = null;
        if (scene.IsValid() && scene.isLoaded)
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "InsectExploreZone")
                    target = root.transform.Find("ExploreEnvironment/HabitatTarget");
        if (target == null)
        {
            EditorUtility.DisplayDialog("HabitatTarget not found",
                "Open Prototype2b and create InsectExploreZone/ExploreEnvironment/HabitatTarget. No object was moved.", "OK");
            return;
        }
        Undo.RecordObject(target, "Apply captured GroundContact world position");
        target.position = world;
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        if (!EditorApplication.isPlaying) EditorSceneManager.MarkSceneDirty(scene);
        string persistence = EditorApplication.isPlaying
            ? "This is a temporary Play Mode change. After leaving Play Mode, apply the capture again and save Prototype2b to keep it."
            : "HabitatTarget has been moved in world space. Save Prototype2b (Cmd+S) to keep the change. Undo is available.";
        Debug.Log("HabitatTarget set to captured world position:\n" + Format(world), target);
        EditorUtility.DisplayDialog("HabitatTarget position applied", Format(world) + "\n\n" + persistence, "OK");
    }
    [MenuItem(MenuRoot + "Apply Captured Position to HabitatTarget", true)]
    static bool CanApply() { return TryRead(out _) && !EditorApplication.isCompiling; }

    static void Copy(Vector3 world)
    {
        EditorGUIUtility.systemCopyBuffer = Format(world);
        Debug.Log("Captured GroundContact world position:\n" + Format(world));
    }
    static string Format(Vector3 world)
    {
        return "X = " + world.x.ToString("F6", Invariant) + "\nY = " + world.y.ToString("F6", Invariant) +
            "\nZ = " + world.z.ToString("F6", Invariant);
    }
    static bool TryRead(out Vector3 world)
    {
        world = default;
        var parts = SessionState.GetString(CaptureKey, "").Split(';');
        if (parts.Length != 3 || !float.TryParse(parts[0], NumberStyles.Float, Invariant, out float x) ||
            !float.TryParse(parts[1], NumberStyles.Float, Invariant, out float y) ||
            !float.TryParse(parts[2], NumberStyles.Float, Invariant, out float z)) return false;
        world = new Vector3(x, y, z); return true;
    }
}
