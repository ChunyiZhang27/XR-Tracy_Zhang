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


    // WingInteraction 会查询这个状态
    public bool IsOpen
    {
        get
        {
            return isOpen;
        }
    }


    private void Start()
    {
        leftClosedRotation =
            leftElytraPivot.localRotation;

        rightClosedRotation =
            rightElytraPivot.localRotation;
    }


    // ==========================================
    // USER SELECT
    // ==========================================

    public void ToggleElytra()
    {
        if (isAnimating)
        {
            return;
        }


        // --------------------------
        // CLOSED → OPEN
        // --------------------------

        if (!isOpen)
        {
            StartCoroutine(
                AnimateElytra(true)
            );

            return;
        }


        // --------------------------
        // OPEN → CLOSED
        // --------------------------

        // 如果 Wing 现在打开，
        // 同时让 Wing 开始收回。
        if (wingInteraction != null)
        {
            wingInteraction.ForceCloseWings();
        }


        // Elytra 自己也同时开始关闭。
        StartCoroutine(
            AnimateElytra(false)
        );
    }


    // ==========================================
    // ELYTRA ANIMATION
    // ==========================================

    private IEnumerator AnimateElytra(bool open)
    {
        isAnimating = true;


        Quaternion leftStart =
            leftElytraPivot.localRotation;

        Quaternion rightStart =
            rightElytraPivot.localRotation;


        Quaternion leftTarget;
        Quaternion rightTarget;


        if (open)
        {
            leftTarget =
                leftClosedRotation *
                Quaternion.Euler(
                    -liftAngle,
                    -outwardAngle,
                    0f
                );


            rightTarget =
                rightClosedRotation *
                Quaternion.Euler(
                    -liftAngle,
                    outwardAngle,
                    0f
                );
        }
        else
        {
            leftTarget =
                leftClosedRotation;

            rightTarget =
                rightClosedRotation;
        }


        float time = 0f;


        while (time < animationDuration)
        {
            time += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    time / animationDuration
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


        isOpen = open;
        isAnimating = false;
    }
}