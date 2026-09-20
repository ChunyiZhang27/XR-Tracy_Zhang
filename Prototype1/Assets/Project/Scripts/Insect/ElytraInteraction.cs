using System.Collections;
using UnityEngine;

public class ElytraInteraction : MonoBehaviour
{
    [Header("Elytra Pivots")]
    [SerializeField]
    private Transform leftElytraPivot;

    [SerializeField]
    private Transform rightElytraPivot;


    [Header("Wing Interaction")]
    [SerializeField]
    private WingInteraction wingInteraction;


    [Header("Open Rotation")]
    [SerializeField]
    private Vector3 leftOpenEuler =
        new Vector3(45.443f, 62.761f, 10f);

    [SerializeField]
    private Vector3 rightOpenEuler =
        new Vector3(45.443f, -62.761f, -10f);


    [Header("Timing")]
    [SerializeField]
    private float elytraDuration = 0.8f;

    [SerializeField]
    private float wingOpenDelay = 0.15f;

    [SerializeField]
    private float wingCloseDelay = 0.2f;


    private Quaternion leftClosedRotation;
    private Quaternion rightClosedRotation;

    private bool isOpen = false;
    private bool isAnimating = false;


    public bool IsOpen
    {
        get { return isOpen; }
    }


    private void Start()
    {
        leftClosedRotation =
            leftElytraPivot.localRotation;

        rightClosedRotation =
            rightElytraPivot.localRotation;
    }


    public void ToggleElytra()
    {
        if (isAnimating)
            return;


        if (!isOpen)
        {
            StartCoroutine(OpenSequence());
        }
        else
        {
            StartCoroutine(CloseSequence());
        }
    }


    // ========================================
    // OPEN
    // Elytra starts first,
    // then wings unfold shortly afterwards.
    // ========================================

    private IEnumerator OpenSequence()
    {
        isAnimating = true;


        // Elytra starts opening.
        StartCoroutine(
            AnimateElytra(
                leftOpenEuler,
                rightOpenEuler
            )
        );


        // Give the shell a small head start.
        yield return new WaitForSeconds(
            wingOpenDelay
        );


        if (wingInteraction != null)
        {
            wingInteraction.OpenWings();
        }


        // Wait for Elytra animation to finish.
        yield return new WaitForSeconds(
            Mathf.Max(
                0f,
                elytraDuration - wingOpenDelay
            )
        );


        isOpen = true;
        isAnimating = false;
    }


    // ========================================
    // CLOSE
    // Wings retract first,
    // then Elytra closes over them.
    // ========================================

    private IEnumerator CloseSequence()
    {
        isAnimating = true;


        if (wingInteraction != null)
        {
            // If the wings are currently flapping/opening,
            // wait for that animation to finish first.
            while (wingInteraction.IsAnimating)
            {
                yield return null;
            }


            wingInteraction.CloseWings();


            // Give wings time to start retracting.
            yield return new WaitForSeconds(
                wingCloseDelay
            );
        }


        yield return StartCoroutine(
            AnimateElytraToClosed()
        );


        isOpen = false;
        isAnimating = false;
    }


    // ========================================
    // OPEN ELYTRA ANIMATION
    // ========================================

    private IEnumerator AnimateElytra(
        Vector3 leftTargetEuler,
        Vector3 rightTargetEuler
    )
    {
        Quaternion leftStart =
            leftElytraPivot.localRotation;

        Quaternion rightStart =
            rightElytraPivot.localRotation;


        Quaternion leftTarget =
            Quaternion.Euler(leftTargetEuler);

        Quaternion rightTarget =
            Quaternion.Euler(rightTargetEuler);


        float time = 0f;


        while (time < elytraDuration)
        {
            time += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    time / elytraDuration
                );

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


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
    }


    // ========================================
    // CLOSE ELYTRA ANIMATION
    // ========================================

    private IEnumerator AnimateElytraToClosed()
    {
        Quaternion leftStart =
            leftElytraPivot.localRotation;

        Quaternion rightStart =
            rightElytraPivot.localRotation;


        float time = 0f;


        while (time < elytraDuration)
        {
            time += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    time / elytraDuration
                );

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            leftElytraPivot.localRotation =
                Quaternion.Slerp(
                    leftStart,
                    leftClosedRotation,
                    t
                );


            rightElytraPivot.localRotation =
                Quaternion.Slerp(
                    rightStart,
                    rightClosedRotation,
                    t
                );


            yield return null;
        }


        leftElytraPivot.localRotation =
            leftClosedRotation;

        rightElytraPivot.localRotation =
            rightClosedRotation;
    }
}