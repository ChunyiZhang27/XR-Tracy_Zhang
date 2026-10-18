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
    void Initialize()
    {
        if (initialized) return;
        grab = GetComponent<XRGrabInteractable>(); body = GetComponent<Rigidbody>(); target = GetComponent<FoodSearchTarget>();
        homeParent = transform.parent; homePosition = transform.position; homeRotation = transform.rotation;
        initialized = true;
    }
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
    public void ResetForSearch()
    {
        Initialize();
        if (pending != null) StopCoroutine(pending);
        pending = null;
        // Completion waits for release; a lesson exit cancels through XRI's normal disable path.
        if (grab.isSelected) throw new System.InvalidOperationException("Release selected insects before restarting Food Search.");
        RestorePose();
    }
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
