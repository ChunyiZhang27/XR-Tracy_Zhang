using System.Collections;
using UnityEngine;

public class WingInteraction : MonoBehaviour
{
    [Header("Wing Pivots")]
    [SerializeField]
    private Transform leftWingPivot;

    [SerializeField]
    private Transform rightWingPivot;


    [Header("Elytra")]
    [SerializeField]
    private ElytraInteraction elytraInteraction;


    [Header("Open Pose")]
    [SerializeField]
    private float openLiftAngle = 25f;

    [SerializeField]
    private float openOutwardAngle = 25f;

    [SerializeField]
    private float transitionDuration = 0.35f;


    [Header("Flap")]
    [SerializeField]
    private float flapAmplitude = 10f;

    [SerializeField]
    private float flapSpeed = 22f;

    [SerializeField]
    private float flapDuration = 1.2f;


    private Quaternion leftClosedRotation;
    private Quaternion rightClosedRotation;

    private Quaternion leftOpenRotation;
    private Quaternion rightOpenRotation;


    private Coroutine currentCoroutine;

    private bool isAnimating = false;


    private enum WingState
    {
        Closed,
        OpenReady,
        OpenFlapped
    }


    private WingState currentState =
        WingState.Closed;


    // 给 ElytraInteraction 查询
    public bool IsClosed
    {
        get
        {
            return currentState == WingState.Closed
                   && !isAnimating;
        }
    }


    private void Start()
    {
        leftClosedRotation =
            leftWingPivot.localRotation;

        rightClosedRotation =
            rightWingPivot.localRotation;


        leftOpenRotation =
            leftClosedRotation *
            Quaternion.Euler(
                -openLiftAngle,
                -openOutwardAngle,
                0f
            );


        rightOpenRotation =
            rightClosedRotation *
            Quaternion.Euler(
                -openLiftAngle,
                openOutwardAngle,
                0f
            );
    }


    // ==========================================
    // 用户点击 Wing 时调用
    // Closed → Open → Flap → Closed → Open...
    // ==========================================

    public void ToggleWings()
    {
        if (isAnimating)
        {
            return;
        }


        // Elytra 没打开时，
        // Wing 不应该单独穿过 Elytra。
        if (elytraInteraction != null &&
            !elytraInteraction.IsOpen)
        {
            Debug.Log(
                "Open the elytra before using the flight wings."
            );

            return;
        }


        // 第一次点击：
        // CLOSED → OPEN
        if (currentState == WingState.Closed)
        {
            currentCoroutine =
                StartCoroutine(OpenWings());

            return;
        }


        // 第二次点击：
        // OPEN → FLAP
        if (currentState == WingState.OpenReady)
        {
            currentCoroutine =
                StartCoroutine(FlapWings());

            return;
        }


        // 第三次点击：
        // FLAPPED → CLOSED
        if (currentState == WingState.OpenFlapped)
        {
            currentCoroutine =
                StartCoroutine(CloseWings());

            return;
        }
    }


    // ==========================================
    // Elytra 关闭时调用
    // 无论 Wing 当前什么状态，直接开始收回
    // ==========================================

    public void ForceCloseWings()
    {
        if (currentState == WingState.Closed &&
            !isAnimating)
        {
            return;
        }


        // 如果正在拍动或者正在展开，
        // 先停止当前动画。
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);

            currentCoroutine = null;
        }


        isAnimating = false;


        currentCoroutine =
            StartCoroutine(CloseWings());
    }


    // ==========================================
    // OPEN
    // ==========================================

    private IEnumerator OpenWings()
    {
        isAnimating = true;


        Quaternion leftStart =
            leftWingPivot.localRotation;

        Quaternion rightStart =
            rightWingPivot.localRotation;


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
                    leftOpenRotation,
                    t
                );


            rightWingPivot.localRotation =
                Quaternion.Slerp(
                    rightStart,
                    rightOpenRotation,
                    t
                );


            yield return null;
        }


        leftWingPivot.localRotation =
            leftOpenRotation;

        rightWingPivot.localRotation =
            rightOpenRotation;


        currentState =
            WingState.OpenReady;

        isAnimating = false;
        currentCoroutine = null;
    }


    // ==========================================
    // FLAP
    // ==========================================

    private IEnumerator FlapWings()
    {
        isAnimating = true;


        float time = 0f;


        while (time < flapDuration)
        {
            time += Time.deltaTime;


            float flap =
                Mathf.Sin(
                    time * flapSpeed
                )
                * flapAmplitude;


            leftWingPivot.localRotation =
                leftOpenRotation *
                Quaternion.Euler(
                    flap,
                    0f,
                    0f
                );


            rightWingPivot.localRotation =
                rightOpenRotation *
                Quaternion.Euler(
                    flap,
                    0f,
                    0f
                );


            yield return null;
        }


        // 拍动结束后回到展开状态
        leftWingPivot.localRotation =
            leftOpenRotation;

        rightWingPivot.localRotation =
            rightOpenRotation;


        currentState =
            WingState.OpenFlapped;

        isAnimating = false;
        currentCoroutine = null;
    }


    // ==========================================
    // CLOSE
    // ==========================================

    private IEnumerator CloseWings()
    {
        isAnimating = true;


        Quaternion leftStart =
            leftWingPivot.localRotation;

        Quaternion rightStart =
            rightWingPivot.localRotation;


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
                    leftClosedRotation,
                    t
                );


            rightWingPivot.localRotation =
                Quaternion.Slerp(
                    rightStart,
                    rightClosedRotation,
                    t
                );


            yield return null;
        }


        leftWingPivot.localRotation =
            leftClosedRotation;

        rightWingPivot.localRotation =
            rightClosedRotation;


        currentState =
            WingState.Closed;

        isAnimating = false;
        currentCoroutine = null;
    }
}