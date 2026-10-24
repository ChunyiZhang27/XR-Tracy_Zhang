using TMPro;
using UnityEngine;

/// <summary>Prototype2b 的计数视觉层；只读取任务状态，不修改抓取、分数或完成条件。</summary>
[DisallowMultipleComponent]
public sealed class FoodSearchProgressView : MonoBehaviour
{
    public FoodSearchActivity activity;
    public TMP_Text countText, completionText;
    public UnityEngine.UI.Image fill;
    public Color collectingColor = new Color32(255, 209, 106, 255);
    public Color completedColor = new Color32(156, 240, 176, 255);
    [Min(0f)] public float fillDuration = .18f;

    int previousCount = -1, previousRequired = -1;
    bool previousCompleted;
    float startFill, targetFill, elapsed;

    void OnEnable() { previousCount = previousRequired = -1; Refresh(0f, true); }
    void LateUpdate()
    {
        if (activity == null) return;
        bool searchVisible = activity.searchPanel != null && activity.searchPanel.activeInHierarchy;
        bool completionVisible = activity.completionPanel != null && activity.completionPanel.activeInHierarchy;
        if (searchVisible || completionVisible) Refresh(Time.unscaledDeltaTime, false);
    }

    // 在原进度文本更新之后刷新显示；重玩降回零时立即清空条形，不残留上一轮进度。
    void Refresh(float deltaTime, bool immediate)
    {
        if (activity == null) return;
        int count = activity.FoundCount, required = Mathf.Max(1, activity.requiredAphids);
        bool completed = activity.Completed;
        bool changed = count != previousCount || required != previousRequired || completed != previousCompleted;
        if (changed)
        {
            immediate |= previousCount < 0 || count < previousCount || completed;
            previousCount = count; previousRequired = required; previousCompleted = completed;
            startFill = fill != null ? fill.fillAmount : 0f;
            targetFill = Mathf.Clamp01((float)count / required);
            elapsed = 0f;
            Color color = completed ? completedColor : collectingColor;
            string hex = ColorUtility.ToHtmlStringRGB(color);
            if (countText != null)
                countText.text = $"<b><size=52><color=#{hex}>{count}</color></size><size=32><color=#EAF2EC> / {required}</color></size></b>";
            if (completionText != null)
                completionText.text = $"<b>Aphids Caught   <color=#{hex}>{count} / {required}</color></b>";
            if (fill != null) fill.color = color;
        }
        // 游戏仍写入原 progressText；若重新初始化写回普通文本，只重建视觉格式。
        if (!changed && countText != null && !countText.text.StartsWith("<b><size=52>"))
        {
            previousCount = -1;
            Refresh(deltaTime, true);
            return;
        }
        if (fill == null) return;
        elapsed += Mathf.Max(0f, deltaTime);
        float t = immediate || fillDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fillDuration);
        fill.fillAmount = Mathf.Lerp(startFill, targetFill, Mathf.SmoothStep(0f, 1f, t));
    }
}
