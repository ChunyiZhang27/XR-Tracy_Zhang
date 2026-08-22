using System.Collections;
using UnityEngine;

public class WingInteraction : MonoBehaviour
{
    [Header("Wing Pivots")]
    [SerializeField] private Transform leftWingPivot;
    [SerializeField] private Transform rightWingPivot;

    [Header("Wing Open Pose")]
    [SerializeField] private float openLiftAngle = 55f;      // 像外壳一样抬起
    [SerializeField] private float openOutwardAngle = 10f;   // 稍微向外展开

    [Header("Flap Settings")]
    [SerializeField] private float flapAmplitude = 18f;      // 拍打幅度
    [SerializeField] private float flapSpeed = 18f;          // 拍打速度
    [SerializeField] private float flapDuration = 1.5f;      // 持续时间

    private Quaternion leftOriginalRotation;
    private Quaternion rightOriginalRotation;

    private Quaternion leftOpenRotation;
    private Quaternion rightOpenRotation;

    private Coroutine flapCoroutine;

    private void Start()
    {
        leftOriginalRotation = leftWingPivot.localRotation;
        rightOriginalRotation = rightWingPivot.localRotation;

        // 先定义“展开后的基础姿态”
        leftOpenRotation = leftOriginalRotation *
                           Quaternion.Euler(-openLiftAngle, -openOutwardAngle, 0f);

        rightOpenRotation = rightOriginalRotation *
                            Quaternion.Euler(-openLiftAngle, openOutwardAngle, 0f);
    }

    public void FlapWings()
    {
        if (leftWingPivot == null || rightWingPivot == null)
        {
            Debug.LogWarning("Wing pivots are not assigned.");
            return;
        }

        if (flapCoroutine != null)
        {
            StopCoroutine(flapCoroutine);
        }

        flapCoroutine = StartCoroutine(FlapRoutine());
    }

    private IEnumerator FlapRoutine()
    {
        float openTime = 0.25f;
        float t = 0f;

        // Step 1: 先展开到基础姿态
        Quaternion leftStart = leftWingPivot.localRotation;
        Quaternion rightStart = rightWingPivot.localRotation;

        while (t < openTime)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / openTime);
            lerp = Mathf.SmoothStep(0f, 1f, lerp);

            leftWingPivot.localRotation = Quaternion.Slerp(leftStart, leftOpenRotation, lerp);
            rightWingPivot.localRotation = Quaternion.Slerp(rightStart, rightOpenRotation, lerp);

            yield return null;
        }

        // Step 2: 在展开姿态上快速拍打
        float flapTime = 0f;

        while (flapTime < flapDuration)
        {
            flapTime += Time.deltaTime;

            float flap = Mathf.Sin(flapTime * flapSpeed) * flapAmplitude;

            leftWingPivot.localRotation =
                leftOpenRotation * Quaternion.Euler(flap, 0f, 0f);

            rightWingPivot.localRotation =
                rightOpenRotation * Quaternion.Euler(flap, 0f, 0f);

            yield return null;
        }

        // Step 3: 回到展开姿态（如果你想最后停在展开状态）
        leftWingPivot.localRotation = leftOpenRotation;
        rightWingPivot.localRotation = rightOpenRotation;

        flapCoroutine = null;
    }
}