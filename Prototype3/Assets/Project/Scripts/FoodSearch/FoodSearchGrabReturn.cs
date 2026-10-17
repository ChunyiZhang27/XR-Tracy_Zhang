using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Step A: keep the test insect available after release, outside XR event callbacks.</summary>
[RequireComponent(typeof(XRGrabInteractable))]
public sealed class FoodSearchGrabReturn : MonoBehaviour
{
    XRGrabInteractable grab;
    Rigidbody body;
    Vector3 homePosition;
    Quaternion homeRotation;
    Coroutine pending;
    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>(); body = GetComponent<Rigidbody>();
        homePosition = transform.position; homeRotation = transform.rotation;
    }
    void OnEnable() { grab.selectExited.AddListener(Released); }
    void OnDisable()
    {
        grab.selectExited.RemoveListener(Released);
        if (pending != null) StopCoroutine(pending);
        pending = null;
        RestorePose();
    }
    void Released(SelectExitEventArgs args)
    {
        if (isActiveAndEnabled) pending = StartCoroutine(ReturnAfterRelease());
    }
    IEnumerator ReturnAfterRelease()
    {
        yield return null;
        yield return new WaitForFixedUpdate();
        if (!grab.isSelected) RestorePose();
        pending = null;
    }
    void RestorePose()
    {
        transform.SetPositionAndRotation(homePosition, homeRotation);
        if (body != null) { body.position = transform.position; body.rotation = transform.rotation; }
    }
}
