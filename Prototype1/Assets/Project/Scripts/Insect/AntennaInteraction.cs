using System.Collections;
using UnityEngine;

public class AntennaInteraction : MonoBehaviour
{
    [Header("Antenna Pivots")]
    [SerializeField]
    private Transform leftAntennaPivot;

    [SerializeField]
    private Transform rightAntennaPivot;


    [Header("X Range")]
    [SerializeField]
    private float xMin = -1.2f;

    [SerializeField]
    private float xMax = 0.4f;


    [Header("Left Y Range")]
    [SerializeField]
    private float leftYMin = 0.4f;

    [SerializeField]
    private float leftYMax = 3.0f;


    [Header("Right Y Range")]
    [SerializeField]
    private float rightYMin = -3.0f;

    [SerializeField]
    private float rightYMax = -0.4f;


    [Header("Animation")]
    [SerializeField]
    private float wiggleDuration = 1.4f;

    [SerializeField]
    private float wiggleSpeed = 7.0f;


    private bool isAnimating = false;


    public bool IsAnimating
    {
        get
        {
            return isAnimating;
        }
    }


    // =========================
    // PUBLIC FUNCTION
    // 后面 XR Select Entered 会调用
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
    // =========================

    private IEnumerator WiggleRoutine()
    {
        isAnimating = true;


        float time = 0f;


        while (time < wiggleDuration)
        {
            time += Time.deltaTime;


            // X 和 Y 使用不同相位
            // 避免两个方向完全同步
            float xWave =
                Mathf.Sin(
                    time * wiggleSpeed
                );

            float yWave =
                Mathf.Sin(
                    time * wiggleSpeed + 1.2f
                );


            // 从 -1 ~ 1 转成 0 ~ 1
            float xT =
                (xWave + 1f) * 0.5f;

            float yT =
                (yWave + 1f) * 0.5f;


            // -------------------------
            // X
            // 两边使用相同范围
            // -1.2 ~ 0.4
            // -------------------------

            float currentX =
                Mathf.Lerp(
                    xMin,
                    xMax,
                    xT
                );


            // -------------------------
            // LEFT Y
            // 0.4 ~ 3
            // -------------------------

            float leftY =
                Mathf.Lerp(
                    leftYMin,
                    leftYMax,
                    yT
                );


            // -------------------------
            // RIGHT Y
            // 镜像：-3 ~ -0.4
            // -------------------------

            float rightY =
                Mathf.Lerp(
                    rightYMax,
                    rightYMin,
                    yT
                );


            // =========================
            // APPLY ROTATION
            // Z 始终保持 0
            // =========================

            leftAntennaPivot.localRotation =
                Quaternion.Euler(
                    currentX,
                    leftY,
                    0f
                );


            rightAntennaPivot.localRotation =
                Quaternion.Euler(
                    currentX,
                    rightY,
                    0f
                );


            yield return null;
        }


        // =========================
        // REST POSE
        // 动画结束后回到安全范围内
        // =========================

        leftAntennaPivot.localRotation =
            Quaternion.Euler(
                xMax,
                leftYMin,
                0f
            );


        rightAntennaPivot.localRotation =
            Quaternion.Euler(
                xMax,
                rightYMax,
                0f
            );


        isAnimating = false;
    }
}