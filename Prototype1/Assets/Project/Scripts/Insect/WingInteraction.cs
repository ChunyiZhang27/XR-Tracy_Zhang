using System.Collections;
using UnityEngine;

public class WingInteraction : MonoBehaviour
{
    [Header("Wing Pivots")]
    [SerializeField]
    private Transform leftWingPivot;

    [SerializeField]
    private Transform rightWingPivot;


    [Header("Closed Pose")]
    [SerializeField]
    private Vector3 leftClosedEuler =
        new Vector3(29.413f, -91.719f, 15.575f);

    [SerializeField]
    private Vector3 rightClosedEuler =
        new Vector3(29.413f, 91.719f, -15.575f);


    [Header("Open Pose")]
    [SerializeField]
    private Vector3 leftOpenEuler =
        new Vector3(49.559f, -54.47f, -23.65f);

    [SerializeField]
    private Vector3 rightOpenEuler =
        new Vector3(49.559f, 54.47f, 23.65f);


    [Header("Animation")]
    [SerializeField]
    private float transitionDuration = 0.45f;


    private bool isOpen = false;
    private bool isAnimating = false;


    public bool IsOpen
    {
        get { return isOpen; }
    }


    // ========================================
    // OPEN
    // Elytra 打开时之后会调用这个函数
    // ========================================

    public void OpenWings()
    {
        if (isAnimating || isOpen)
            return;

        StartCoroutine(
            AnimateWings(
                leftOpenEuler,
                rightOpenEuler,
                true
            )
        );
    }


    // ========================================
    // CLOSE
    // Elytra 关闭时之后会调用这个函数
    // ========================================

    public void CloseWings()
    {
        if (isAnimating || !isOpen)
            return;

        StartCoroutine(
            AnimateWings(
                leftClosedEuler,
                rightClosedEuler,
                false
            )
        );
    }


    // ========================================
    // GENERAL ANIMATION
    // ========================================

    private IEnumerator AnimateWings(
        Vector3 leftTargetEuler,
        Vector3 rightTargetEuler,
        bool opening
    )
    {
        isAnimating = true;


        Quaternion leftStart =
            leftWingPivot.localRotation;

        Quaternion rightStart =
            rightWingPivot.localRotation;


        Quaternion leftTarget =
            Quaternion.Euler(leftTargetEuler);

        Quaternion rightTarget =
            Quaternion.Euler(rightTargetEuler);


        float time = 0f;


        while (time < transitionDuration)
        {
            time += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    time / transitionDuration
                );

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            leftWingPivot.localRotation =
                Quaternion.Slerp(
                    leftStart,
                    leftTarget,
                    t
                );


            rightWingPivot.localRotation =
                Quaternion.Slerp(
                    rightStart,
                    rightTarget,
                    t
                );


            yield return null;
        }


        leftWingPivot.localRotation =
            leftTarget;

        rightWingPivot.localRotation =
            rightTarget;


        isOpen = opening;
        isAnimating = false;
    }
}