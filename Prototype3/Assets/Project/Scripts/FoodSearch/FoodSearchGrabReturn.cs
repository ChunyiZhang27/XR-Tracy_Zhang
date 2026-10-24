using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Collect correct insects only after XR release; return distractors and replay to their saved pose.</summary>
[RequireComponent(typeof(XRGrabInteractable))]
public sealed class FoodSearchGrabReturn : MonoBehaviour
{
    XRGrabInteractable grab;
    Rigidbody body;
    FoodSearchTarget target;
    Transform homeParent;
    Vector3 homePosition;
    Quaternion homeRotation;
    bool initialized;
    Coroutine pending;
    void Awake() { Initialize(); }
    // ===== 1. 保存初始姿态 =====
    // 首次初始化缓存父物体和世界位置/旋转，供错误目标归位和任务重玩使用。
    void Initialize()
    {
        if (initialized) return;
        grab = GetComponent<XRGrabInteractable>(); body = GetComponent<Rigidbody>(); target = GetComponent<FoodSearchTarget>();
        homeParent = transform.parent; homePosition = transform.position; homeRotation = transform.rotation;
        initialized = true;
    }
    // 监听 XR 松手；禁用时解除监听，避免同一释放事件重复处理。
    void OnEnable() { Initialize(); grab.selectExited.AddListener(Released); }
    void OnDisable()
    {
        grab.selectExited.RemoveListener(Released);
        if (pending != null) StopCoroutine(pending);
        pending = null;
        // XRI may still be cancelling selection during OnDisable. Reset happens on next BeginSearch.
        if (!grab.isSelected) RestorePose();
    }
    void Released(SelectExitEventArgs args)
    {
        if (!isActiveAndEnabled) return;
        if (pending != null) StopCoroutine(pending);
        pending = StartCoroutine(ReturnAfterRelease());
    }
    // ===== 2. 安全释放与收藏 =====
    // 离开 XR 回调并等物理更新，确认无人持有后归位；已计分 aphid 隐藏，ant 保留。
    IEnumerator ReturnAfterRelease()
    {
        yield return null;
        yield return new WaitForFixedUpdate();
        while (grab.isSelected) yield return null;
        RestorePose();
        pending = null;
        if (target != null && target.Found && target.activity != null && target.activity.Running)
            gameObject.SetActive(false); // Collected, never destroyed, and no interactor is holding it.
    }
    // 任务重玩前恢复姿态；调用方必须先通过 XRI 清理仍在持有的选择。
    public void ResetForSearch()
    {
        Initialize();
        if (pending != null) StopCoroutine(pending);
        pending = null;
        // Completion waits for release; a lesson exit cancels through XRI's normal disable path.
        if (grab.isSelected) throw new System.InvalidOperationException("Release selected insects before restarting Food Search.");
        RestorePose();
    }
    // 恢复父级、位置和旋转，同步刚体；动态刚体额外清零速度，不恢复缩放。
    void RestorePose()
    {
        if (transform.parent != homeParent) transform.SetParent(homeParent, true);
        transform.SetPositionAndRotation(homePosition, homeRotation);
        if (body != null)
        {
            body.position = transform.position; body.rotation = transform.rotation;
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
        }
    }
}
