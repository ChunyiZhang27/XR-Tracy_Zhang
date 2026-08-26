using UnityEngine;

public class AntennaProximityInfo : MonoBehaviour
{
    [Header("Antenna Points")]
    [SerializeField]
    private Transform leftAntennaPoint;

    [SerializeField]
    private Transform rightAntennaPoint;


    [Header("Controllers")]
    [SerializeField]
    private Transform leftController;

    [SerializeField]
    private Transform rightController;


    [Header("Info Panel")]
    [SerializeField]
    private InsectInfoPanel infoPanel;


    [Header("Distance Settings")]
    [SerializeField]
    private float showDistance = 0.35f;

    [SerializeField]
    private float hideDistance = 0.45f;


    private bool antennaInfoVisible = false;


    private void Update()
    {
        if (leftAntennaPoint == null ||
            rightAntennaPoint == null ||
            leftController == null ||
            rightController == null ||
            infoPanel == null)
        {
            return;
        }


        // 左手到两个触角的距离
        float leftToLeftAntenna =
            Vector3.Distance(
                leftController.position,
                leftAntennaPoint.position
            );

        float leftToRightAntenna =
            Vector3.Distance(
                leftController.position,
                rightAntennaPoint.position
            );


        // 右手到两个触角的距离
        float rightToLeftAntenna =
            Vector3.Distance(
                rightController.position,
                leftAntennaPoint.position
            );

        float rightToRightAntenna =
            Vector3.Distance(
                rightController.position,
                rightAntennaPoint.position
            );


        // 找出 Controller 和任意一个触角之间的最短距离
        float closestDistance =
            Mathf.Min(
                leftToLeftAntenna,
                leftToRightAntenna,
                rightToLeftAntenna,
                rightToRightAntenna
            );


        // ------------------------
        // SHOW
        // ------------------------

        if (!antennaInfoVisible &&
            closestDistance <= showDistance)
        {
            infoPanel.ShowAntennaInfo();

            antennaInfoVisible = true;
        }


        // ------------------------
        // HIDE
        // ------------------------

        if (antennaInfoVisible &&
            closestDistance >= hideDistance)
        {
            infoPanel.HideInfo();

            antennaInfoVisible = false;
        }
    }
}