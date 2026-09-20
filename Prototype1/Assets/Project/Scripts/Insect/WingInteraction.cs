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


    [Header("Open / Close Animation")]
    [SerializeField]
    private float transitionDuration = 0.45f;


    [Header("Flap Animation")]
    [SerializeField]
    private float flapAngle = 12f;

    [SerializeField]
    private float flapSpeed = 18f;

    [SerializeField]
    private float flapDuration = 1.2f;


    private bool isOpen = false;
    private bool isAnimating = false;


    // =========================
    // PUBLIC STATE
    // =========================

    public bool IsOpen
    {
        get
        {
            return isOpen;
        }
    }


    public bool IsAnimating
    {
        get
        {
            return isAnimating;
        }
    }


    // =========================
    // OPEN WINGS
    // ElytraInteraction 调用
    // =========================

    public void OpenWings()
    {
        if (isAnimating)
        {
            return;
        }

        if (isOpen)
        {
            return;
        }


        StartCoroutine(
            AnimateWings(
                leftOpenEuler,
                rightOpenEuler,
                true
            )
        );
    }


    // =========================
    // CLOSE WINGS
    // ElytraInteraction 调用
    // =========================

    public void CloseWings()
    {
        if (isAnimating)
        {
            return;
        }

        if (!isOpen)
        {
            return;
        }


        StartCoroutine(
            AnimateWings(
                leftClosedEuler,
                rightClosedEuler,
                false
            )
        );
    }


    // =========================
    // FLAP WINGS
    // 用户点击 Wing 时调用
    // =========================

    public void FlapWings()
    {
        // Wing 必须已经展开
        if (!isOpen)
        {
            return;
        }


        // 正在执行其他动画时不要重复触发
        if (isAnimating)
        {
            return;
        }


        StartCoroutine(
            FlapRoutine()
        );
    }


    // =========================
    // OPEN / CLOSE ANIMATION
    // =========================

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
            Quaternion.Euler(
                leftTargetEuler
            );

        Quaternion rightTarget =
            Quaternion.Euler(
                rightTargetEuler
            );


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


    // =========================
    // FLAP ANIMATION
    // =========================

    private IEnumerator FlapRoutine()
    {
        isAnimating = true;


        Quaternion leftOpenRotation =
            Quaternion.Euler(
                leftOpenEuler
            );

        Quaternion rightOpenRotation =
            Quaternion.Euler(
                rightOpenEuler
            );


        float time = 0f;


        while (time < flapDuration)
        {
            time += Time.deltaTime;


            float angle =
                Mathf.Sin(
                    time * flapSpeed
                ) * flapAngle;


            // 暂时使用 Local X 轴进行拍动
            leftWingPivot.localRotation =
                leftOpenRotation *
                Quaternion.Euler(
                    angle,
                    0f,
                    0f
                );


            rightWingPivot.localRotation =
                rightOpenRotation *
                Quaternion.Euler(
                    angle,
                    0f,
                    0f
                );


            yield return null;
        }


        // 拍完以后一定回到展开姿态
        leftWingPivot.localRotation =
            leftOpenRotation;

        rightWingPivot.localRotation =
            rightOpenRotation;


        isAnimating = false;
    }
}