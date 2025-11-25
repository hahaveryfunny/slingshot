using System.Collections;
using UnityEngine;

public class FOVManager : MonoBehaviour
{
    [SerializeField] Transform[] borders;
    [SerializeField] float minFOV = 30f;
    [SerializeField] float maxFOV = 90f;
    [SerializeField] float adjustSpeed = 10f;

    Camera cam;
    bool isAdjusting = true;

    void Start()
    {
        cam = Camera.main;
        StartCoroutine(AdjustFOV());
    }

    IEnumerator AdjustFOV()
    {
        // Wait a frame to ensure everything is initialized
        yield return null;

        while (isAdjusting && cam.fieldOfView > minFOV)
        {
            bool anyBorderVisible = false;

            foreach (Transform border in borders)
            {
                if (IsPositionVisibleInCamera(border.position, cam))
                {
                    anyBorderVisible = true;
                    break;
                }
            }

            if (anyBorderVisible)
            {
                // Reduce FOV
                cam.fieldOfView = Mathf.Max(minFOV, cam.fieldOfView - adjustSpeed * Time.deltaTime);
            }
            else
            {
                // All borders are out of view, we're done
                isAdjusting = false;
                Debug.Log($"FOV adjusted to: {cam.fieldOfView}");
            }

            yield return null;
        }
    }

    bool IsPositionVisibleInCamera(Vector3 position, Camera camera)
    {
        Vector3 viewportPoint = camera.WorldToViewportPoint(position);

        // Check if point is in front of camera and within viewport bounds
        return viewportPoint.z > 0 &&
               viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1;
    }
}