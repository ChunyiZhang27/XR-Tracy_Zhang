using System.Collections;
using UnityEngine;

public class ElytraInteraction : MonoBehaviour
{
    [SerializeField]
    private Transform leftElytraPivot;

    [SerializeField]
    private Transform rightElytraPivot;

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

    private void Start()
    {
        leftClosedRotation = leftElytraPivot.localRotation;
        rightClosedRotation = rightElytraPivot.localRotation;
    }

    public void ToggleElytra()
    {
        if (isAnimating)
            return;

        StartCoroutine(AnimateElytra(!isOpen));
    }

    private IEnumerator AnimateElytra(bool open)
    {
        isAnimating = true;

        Quaternion leftStart = leftElytraPivot.localRotation;
        Quaternion rightStart = rightElytraPivot.localRotation;

        Quaternion leftTarget;
        Quaternion rightTarget;

        if (open)
        {
            // 主要绕 X 轴向上抬起
            // 同时绕 Y 轴稍微向左右打开
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
            leftTarget = leftClosedRotation;
            rightTarget = rightClosedRotation;
        }

        float time = 0f;

        while (time < animationDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(
                time / animationDuration
            );

            // 让动画稍微柔和一些
            t = Mathf.SmoothStep(0f, 1f, t);

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

        leftElytraPivot.localRotation = leftTarget;
        rightElytraPivot.localRotation = rightTarget;

        isOpen = open;
        isAnimating = false;
    }
}