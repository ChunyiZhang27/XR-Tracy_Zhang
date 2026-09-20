using System.Collections;
using UnityEngine;

public class ElytraInteraction : MonoBehaviour
{
    [Header("Elytra Pivots")]
    [SerializeField]
    private Transform leftElytraPivot;

    [SerializeField]
    private Transform rightElytraPivot;


    [Header("Open Rotation")]
    [SerializeField]
    private Vector3 leftOpenEuler =
        new Vector3(45.443f, 62.761f, 10f);

    [SerializeField]
    private Vector3 rightOpenEuler =
        new Vector3(45.443f, -62.761f, -10f);


    [Header("Animation")]
    [SerializeField]
    private float animationDuration = 0.8f;


    private Quaternion leftClosedRotation;
    private Quaternion rightClosedRotation;

    private bool isOpen = false;
    private bool isAnimating = false;


    public bool IsOpen
    {
        get { return isOpen; }
    }


    private void Start()
    {
        // 记录游戏开始时的关闭姿态
        leftClosedRotation =
            leftElytraPivot.localRotation;

        rightClosedRotation =
            rightElytraPivot.localRotation;
    }


    public void ToggleElytra()
    {
        if (isAnimating)
            return;

        StartCoroutine(
            AnimateElytra(!isOpen)
        );
    }


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
            // 直接使用你手动调好的展开角度
            leftTarget =
                Quaternion.Euler(leftOpenEuler);

            rightTarget =
                Quaternion.Euler(rightOpenEuler);
        }
        else
        {
            // 回到游戏开始时记录的关闭姿态
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

            // 让动画起止更柔和
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