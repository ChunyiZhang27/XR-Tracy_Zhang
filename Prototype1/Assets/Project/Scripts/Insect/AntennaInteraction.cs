using System.Collections;
using UnityEngine;

public class AntennaInteraction : MonoBehaviour
{
    [SerializeField]
    private float swingAngle = 20f;

    [SerializeField]
    private float swingSpeed = 4f;

    [SerializeField]
    private float swingDuration = 1.2f;

    private Quaternion originalRotation;
    private Coroutine swingCoroutine;

    private void Start()
    {
        originalRotation = transform.localRotation;
    }

    public void ExploreAntenna()
    {
        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);
        }

        swingCoroutine = StartCoroutine(SwingAntenna());
    }

    private IEnumerator SwingAntenna()
    {
        float time = 0f;

        while (time < swingDuration)
        {
            time += Time.deltaTime;

            float angle =
                Mathf.Sin(time * swingSpeed * Mathf.PI)
                * swingAngle;

            transform.localRotation =
                originalRotation *
                Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        transform.localRotation = originalRotation;
        swingCoroutine = null;
    }
}