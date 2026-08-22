using System.Collections;
using UnityEngine;

public class WingInteraction : MonoBehaviour
{
    [SerializeField]
    private Transform leftWingPivot;

    [SerializeField]
    private Transform rightWingPivot;

    [SerializeField]
    private float flapAngle = 18f;

    [SerializeField]
    private float flapSpeed = 18f;

    [SerializeField]
    private float flapDuration = 1.5f;

    private Quaternion leftOriginalRotation;
    private Quaternion rightOriginalRotation;

    private Coroutine flapCoroutine;

    private void Start()
    {
        leftOriginalRotation = leftWingPivot.localRotation;
        rightOriginalRotation = rightWingPivot.localRotation;
    }

    public void FlapWings()
    {
        if (flapCoroutine != null)
        {
            StopCoroutine(flapCoroutine);
        }

        flapCoroutine = StartCoroutine(FlapRoutine());
    }

    private IEnumerator FlapRoutine()
    {
        float time = 0f;

        while (time < flapDuration)
        {
            time += Time.deltaTime;

            float angle =
                Mathf.Sin(time * flapSpeed) * flapAngle;

            leftWingPivot.localRotation =
                leftOriginalRotation *
                Quaternion.Euler(0f, 0f, angle);

            rightWingPivot.localRotation =
                rightOriginalRotation *
                Quaternion.Euler(0f, 0f, -angle);

            yield return null;
        }

        leftWingPivot.localRotation = leftOriginalRotation;
        rightWingPivot.localRotation = rightOriginalRotation;

        flapCoroutine = null;
    }
}