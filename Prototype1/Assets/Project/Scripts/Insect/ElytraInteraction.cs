using System.Collections;
using UnityEngine;

public class ElytraInteraction : MonoBehaviour
{
    [Header("Elytra Pivots")]
    [SerializeField]
    private Transform leftElytraPivot;

    [SerializeField]
    private Transform rightElytraPivot;


    [Header("Elytra Colliders")]
    [SerializeField]
    private Collider leftElytraCollider;

    [SerializeField]
    private Collider rightElytraCollider;


    [Header("Animation Settings")]
    [SerializeField]
    private float liftAngle = 65f;

    [SerializeField]
    private float outwardAngle = 12f;

    [SerializeField]
    private float animationDuration = 0.8f;


    private Quaternion leftClosedRotation;
    private Quaternion rightClosedRotation;

    private bool isOpen = false;
    private bool isAnimating = false;


    private void Start()
    {
        leftClosedRotation = leftElytraPivot.localRotation;
        rightClosedRotation = rightElytraPivot.localRotation;
    }


    public void ToggleElytra()
    {
        if (isAnimating || isOpen)
            return;

        StartCoroutine(OpenElytra());
    }


    private IEnumerator OpenElytra()
    {
        isAnimating = true;

        Quaternion leftStart =
            leftElytraPivot.localRotation;

        Quaternion rightStart =
            rightElytraPivot.localRotation;


        Quaternion leftTarget =
            leftClosedRotation *
            Quaternion.Euler(
                -liftAngle,
                -outwardAngle,
                0f
            );


        Quaternion rightTarget =
            rightClosedRotation *
            Quaternion.Euler(
                -liftAngle,
                outwardAngle,
                0f
            );


        float time = 0f;

        while (time < animationDuration)
        {
            time += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    time / animationDuration
                );

            t = Mathf.SmoothStep(0f, 1f, t);


            leftElytraPivot.localRotation =
                Quaternion.Slerp(
                    leftStart,
                    leftTarget,
                    t
                );


            rightElytraPivot.localRotation =
                Quaternion.Slerp(
                    rightStart,
                    rightTarget,
                    t
                );


            yield return null;
        }


        leftElytraPivot.localRotation =
            leftTarget;

        rightElytraPivot.localRotation =
            rightTarget;


        isOpen = true;
        isAnimating = false;


        // Elytra 打开以后，
        // 禁止 Collider 继续挡住 Wing。
        if (leftElytraCollider != null)
            leftElytraCollider.enabled = false;

        if (rightElytraCollider != null)
            rightElytraCollider.enabled = false;
    }
}