using System.Collections;
using UnityEngine;

public class WingInteraction : MonoBehaviour
{
    [Header("Wing Pivots")]
    [SerializeField]
    private Transform leftWingPivot;

    [SerializeField]
    private Transform rightWingPivot;


    [Header("SFX")]
    [SerializeField]
    private SFXManager sfxManager;


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
    private float leftFlapMinZ = -40f;

    [SerializeField]
    private float leftFlapMaxZ = -6f;

    [SerializeField]
    private float rightFlapMinZ = 6f;

    [SerializeField]
    private float rightFlapMaxZ = 40f;

    [SerializeField]
    private float flapSpeed = 28f;

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
        // Wings 必须已经展开
        if (!isOpen)
        {
            return;
        }


        // 正在执行其他动画时不能重复触发
        if (isAnimating)
        {
            return;
        }


        // 只有真正开始拍翅时才播放音效
        if (sfxManager != null)
        {
            sfxManager.PlayWingFlap();
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
    // Wings 围绕 Z 轴拍动
    // =========================

    private IEnumerator FlapRoutine()
    {
        isAnimating = true;


        float leftCenterZ =
            (leftFlapMinZ + leftFlapMaxZ) * 0.5f;

        float leftAmplitude =
            (leftFlapMaxZ - leftFlapMinZ) * 0.5f;


        float rightCenterZ =
            (rightFlapMinZ + rightFlapMaxZ) * 0.5f;

        float rightAmplitude =
            (rightFlapMaxZ - rightFlapMinZ) * 0.5f;


        float time = 0f;


        while (time < flapDuration)
        {
            time += Time.deltaTime;


            float wave =
                Mathf.Sin(
                    time * flapSpeed
                );


            Vector3 leftEuler =
                leftOpenEuler;

            Vector3 rightEuler =
                rightOpenEuler;


            leftEuler.z =
                leftCenterZ +
                wave * leftAmplitude;


            rightEuler.z =
                rightCenterZ -
                wave * rightAmplitude;


            leftWingPivot.localRotation =
                Quaternion.Euler(
                    leftEuler
                );


            rightWingPivot.localRotation =
                Quaternion.Euler(
                    rightEuler
                );


            yield return null;
        }


        // 拍动结束后恢复 Open Pose
        leftWingPivot.localRotation =
            Quaternion.Euler(
                leftOpenEuler
            );

        rightWingPivot.localRotation =
            Quaternion.Euler(
                rightOpenEuler
            );


        isAnimating = false;
    }
}