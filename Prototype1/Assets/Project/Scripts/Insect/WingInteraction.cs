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
    // ElytraInteraction 会调用
    // =========================

    public void OpenWings()
    {
        // 正在动画时不要重复触发
        if (isAnimating)
        {
            return;
        }


        // 已经打开时不用再打开
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
    // ElytraInteraction 会调用
    // =========================

    public void CloseWings()
    {
        // 正在动画时不要重复触发
        if (isAnimating)
        {
            return;
        }


        // 已经关闭时不用重复关闭
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
    // GENERAL WING ANIMATION
    // =========================

    private IEnumerator AnimateWings(
        Vector3 leftTargetEuler,
        Vector3 rightTargetEuler,
        bool opening
    )
    {
        isAnimating = true;


        // 记录动画开始时的位置
        Quaternion leftStart =
            leftWingPivot.localRotation;

        Quaternion rightStart =
            rightWingPivot.localRotation;


        // 把我们手动调好的 Euler 数值
        // 转成 Quaternion
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


            // 让动画开始和结束更柔和
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


        // 确保最后准确到目标位置
        leftWingPivot.localRotation =
            leftTarget;

        rightWingPivot.localRotation =
            rightTarget;


        // 更新当前状态
        isOpen = opening;

        isAnimating = false;
    }
}