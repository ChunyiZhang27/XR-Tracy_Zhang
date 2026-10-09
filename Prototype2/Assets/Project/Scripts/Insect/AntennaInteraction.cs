using System.Collections;
using UnityEngine;

public class AntennaInteraction : MonoBehaviour
{
    [Header("Antenna Pivots")]
    [SerializeField]
    private Transform leftAntennaPivot;

    [SerializeField]
    private Transform rightAntennaPivot;


    [Header("X Movement Range")]
    [SerializeField]
    private float xMin = -1.2f;

    [SerializeField]
    private float xMax = 0.4f;


    [Header("Fixed Y Rotation")]
    [SerializeField]
    private float leftFixedY = 0.4f;

    [SerializeField]
    private float rightFixedY = -0.4f;


    [Header("Animation")]
    [SerializeField]
    private float wiggleDuration = 1.4f;

    [SerializeField]
    private float wiggleSpeed = 7.0f;


    private bool isAnimating = false;


    // =========================
    // PUBLIC STATE
    // =========================

    public bool IsAnimating
    {
        get
        {
            return isAnimating;
        }
    }


    // =========================
    // PUBLIC FUNCTION
    // XR Select Entered 调用
    // =========================

    public void WiggleAntennae()
    {
        if (isAnimating)
        {
            return;
        }

        StartCoroutine(
            WiggleRoutine()
        );
    }


    // =========================
    // ANTENNA ANIMATION
    // 现在只有 X 轴运动
    // =========================

    private IEnumerator WiggleRoutine()
    {
        isAnimating = true;


        float time = 0f;


        while (time < wiggleDuration)
        {
            time += Time.deltaTime;


            // -1 ～ +1
            float wave =
                Mathf.Sin(
                    time * wiggleSpeed
                );


            // 转换成 0 ～ 1
            float xT =
                (wave + 1f) * 0.5f;


            // X 始终限制在
            // -1.2 ～ 0.4
            float currentX =
                Mathf.Lerp(
                    xMin,
                    xMax,
                    xT
                );


            // =========================
            // APPLY ROTATION
            //
            // 只有 X 改变
            // Y 固定
            // Z 固定为 0
            // =========================

            leftAntennaPivot.localRotation =
                Quaternion.Euler(
                    currentX,
                    leftFixedY,
                    0f
                );


            rightAntennaPivot.localRotation =
                Quaternion.Euler(
                    currentX,
                    rightFixedY,
                    0f
                );


            yield return null;
        }


        // =========================
        // REST POSE
        // 回到安全的静止状态
        // =========================

        leftAntennaPivot.localRotation =
            Quaternion.Euler(
                xMax,
                leftFixedY,
                0f
            );


        rightAntennaPivot.localRotation =
            Quaternion.Euler(
                xMax,
                rightFixedY,
                0f
            );


        isAnimating = false;
    }
}