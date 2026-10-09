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


    [Header("SFX")]
    [SerializeField]
    private SFXManager sfxManager;


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


    // =========================
    // TOGGLE ELYTRA
    // =========================

    public void ToggleElytra()
    {
        if (isAnimating)
        {
            return;
        }


        if (!isOpen)
        {
            StartCoroutine(
                OpenSequence()
            );
        }
        else
        {
            StartCoroutine(
                CloseSequence()
            );
        }
    }


    // =========================
    // OPEN SEQUENCE
    //
    // 1. Elytra starts opening
    // 2. Open SFX plays
    // 3. Short delay
    // 4. Wings unfold
    // =========================

    private IEnumerator OpenSequence()
    {
        isAnimating = true;


        // Elytra 真正开始打开时播放音效
        if (sfxManager != null)
        {
            sfxManager.PlayElytraOpen();
        }


        // 开始 Elytra 动画
        StartCoroutine(
            AnimateElytra(
                leftOpenEuler,
                rightOpenEuler
            )
        );


        // 让 Elytra 先抬起来一点，
        // 避免 Wings 穿模
        yield return new WaitForSeconds(
            wingOpenDelay
        );


        if (wingInteraction != null)
        {
            wingInteraction.OpenWings();
        }


        // 等待剩余 Elytra 动画完成
        yield return new WaitForSeconds(
            Mathf.Max(
                0f,
                elytraDuration - wingOpenDelay
            )
        );


        isOpen = true;
        isAnimating = false;
    }


    // =========================
    // CLOSE SEQUENCE
    //
    // 1. Wait if Wing is animating
    // 2. Wings retract
    // 3. Short delay
    // 4. Elytra close + Close SFX
    // =========================

    private IEnumerator CloseSequence()
    {
        isAnimating = true;


        if (wingInteraction != null)
        {
            // 如果 Wings 正在拍动 / 动画中，
            // 先等它结束
            while (wingInteraction.IsAnimating)
            {
                yield return null;
            }


            // 先收 Wings
            wingInteraction.CloseWings();


            // 给 Wings 一点时间先收回
            yield return new WaitForSeconds(
                wingCloseDelay
            );
        }


        // Elytra 真正开始关闭时播放音效
        if (sfxManager != null)
        {
            sfxManager.PlayElytraClose();
        }


        // 然后关闭 Elytra
        yield return StartCoroutine(
            AnimateElytraToClosed()
        );


        isOpen = false;
        isAnimating = false;
    }


    // =========================
    // OPEN ELYTRA ANIMATION
    // =========================

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
            Quaternion.Euler(
                leftTargetEuler
            );

        Quaternion rightTarget =
            Quaternion.Euler(
                rightTargetEuler
            );


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


    // =========================
    // CLOSE ELYTRA ANIMATION
    // =========================

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