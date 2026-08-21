using System.Collections;
using UnityEngine;

public class AntennaInteraction : MonoBehaviour
{
    [SerializeField]
    private float swingAngle = 20f;

    [SerializeField]
    private float swingSpeed = 4f;

    private Quaternion originalRotation;
    private Coroutine swingCoroutine;

    private void Start()
    {
        originalRotation = transform.localRotation;
    }

    public void ExploreAntenna()
    {
        // 防止动画重复叠加
        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);
        }

        swingCoroutine = StartCoroutine(SwingAntenna());
    }

    private IEnumerator SwingAntenna()
    {
        float duration = 1.2f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float angle =
                Mathf.Sin(time * swingSpeed * Mathf.PI) * swingAngle;

            transform.localRotation =
                originalRotation *
                Quaternion.Euler(0f, angle, 0f);

            yield return null;
        }

        transform.localRotation = originalRotation;
        swingCoroutine = null;
    }
}