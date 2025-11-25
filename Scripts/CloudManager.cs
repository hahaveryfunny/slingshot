using System.Linq;
using UnityEngine;

public class CloudManager : MonoBehaviour
{
    [SerializeField] float speed = 1f;
    [SerializeField] float rotationSpeed = 10f;

    Camera cam;
    Transform[] clouds;
    float[] cloudRotationSpeeds; // Each cloud gets its own random rotation speed

    Vector3 topLeftCorner;
    Vector3 bottomLeftCorner;
    Vector3 topRightCorner;
    Vector3 bottomRightCorner;
    [SerializeField] Vector3 cloudDirection = new Vector3();
    [SerializeField] Transform arrowDirectionVisualization;

    void VisualizeDirection()
    {
        Vector3 dirXZ = new Vector3(cloudDirection.x, 0, cloudDirection.z);

        if (dirXZ.sqrMagnitude > 0.001f)
        {
            arrowDirectionVisualization.rotation = Quaternion.LookRotation(-dirXZ);
        }
    }

    void Start()
    {
        cam = Camera.main;

        int count = transform.childCount;
        clouds = new Transform[count];
        cloudRotationSpeeds = new float[count];

        for (var i = 0; i < count; i++)
        {
            clouds[i] = transform.GetChild(i);
            // Give each cloud a random rotation speed variation
            cloudRotationSpeeds[i] = Random.Range(0.5f, 1.5f);
        }

        GetBorders();
        transform.position = GetRandomPosWithinBorders();
        GetTarget(transform);
        VisualizeDirection();
        ScatterCloudsAtStart();
    }

    void ScatterCloudsAtStart()
    {
        Vector3 dirXZ = new Vector3(cloudDirection.x, 0, cloudDirection.z);

        for (var i = 0; i < clouds.Count(); i++)
        {
            clouds[i].transform.position = GetRandomPosWithinBorders();
            //  SetCloudRotation(clouds[i], dirXZ);
        }
    }

    // void SetCloudRotation(Transform cloud, Vector3 directionXZ)
    // {
    //     // Face the movement direction
    //     cloud.rotation = Quaternion.LookRotation(-directionXZ);

    //     // Add random initial rotation around forward axis (blue gizmo/local Z)
    //     float randomZRotation = Random.Range(0f, 360f);
    //     cloud.Rotate(0, 0, randomZRotation, Space.Self);
    // }

    Vector3 GetRandomPosWithinBorders()
    {
        return new Vector3(
            Random.Range(topLeftCorner.x, topRightCorner.x),
            transform.position.y,
            Random.Range(bottomLeftCorner.z, topLeftCorner.z)
        );
    }

    void GetBorders()
    {
        float height = cam.transform.position.y - transform.position.y;
        bottomLeftCorner = cam.ViewportToWorldPoint(new Vector3(0, 0, height));
        bottomRightCorner = cam.ViewportToWorldPoint(new Vector3(1, 0, height));
        topLeftCorner = cam.ViewportToWorldPoint(new Vector3(0, 1, height));
        topRightCorner = cam.ViewportToWorldPoint(new Vector3(1, 1, height));
    }

    void GetTarget(Transform cloud)
    {
        Vector2 _target;
        int randomEdgeCount = Random.Range(0, 4);

        if (randomEdgeCount == 0)
            _target = new Vector2(Random.Range(topLeftCorner.x, topRightCorner.x), topRightCorner.z);
        else if (randomEdgeCount == 1)
            _target = new Vector2(topLeftCorner.x, Random.Range(bottomLeftCorner.z, topLeftCorner.z));
        else if (randomEdgeCount == 2)
            _target = new Vector2(Random.Range(bottomLeftCorner.x, bottomRightCorner.x), bottomRightCorner.z);
        else
            _target = new Vector2(topRightCorner.x, Random.Range(bottomRightCorner.z, topRightCorner.z));

        Vector3 target = new Vector3(_target.x, cloud.position.y, _target.y);
        cloudDirection = (target - cloud.position).normalized;
    }

    void Update()
    {
        Vector3 dirXZ = new Vector3(cloudDirection.x, 0, cloudDirection.z);

        for (int i = 0; i < clouds.Length; i++)
        {
            Transform cloud = clouds[i];

            // Move the cloud
            cloud.position += cloudDirection * speed * Time.deltaTime;

            // Rotate the cloud around its forward axis (local Z)
            cloud.Rotate(0, 0, rotationSpeed * cloudRotationSpeeds[i] * Time.deltaTime, Space.Self);

            if (IsOutsideBoundary(cloud, 5f))
            {
                RepositionCloudOutsideBorders(cloud, dirXZ);
            }
        }
    }

    bool IsOutsideBoundary(Transform cloud, float margin = 5f)
    {
        Vector3 pos = cloud.position;

        if (pos.x < bottomLeftCorner.x - margin || pos.x > topRightCorner.x + margin ||
            pos.z < bottomLeftCorner.z - margin || pos.z > topRightCorner.z + margin)
        {
            return true;
        }

        return false;
    }

    void RepositionCloudOutsideBorders(Transform cloud, Vector3 directionXZ)
    {
        // Get a random position within the borders
        Vector3 randomPos = GetRandomPosWithinBorders();

        // Project backwards (opposite to cloud direction)
        Vector3 oppositeDirection = -cloudDirection;

        // Find where the ray from randomPos in opposite direction hits the border
        Vector3 newPosition = FindBorderIntersection(randomPos, oppositeDirection, cloud.position.y);

        // Place the cloud at the border intersection
        cloud.position = newPosition;

        // Set new random rotation
        //   SetCloudRotation(cloud, directionXZ);
    }

    Vector3 FindBorderIntersection(Vector3 origin, Vector3 direction, float yPosition)
    {
        // Calculate where a line from origin in the given direction intersects the camera borders
        // The borders are defined by the camera viewport corners
        float minT = float.MaxValue;
        Vector3 intersectionPoint = origin;

        // Check intersection with all four borders (mathematical line-line intersection)

        // Left border (x = bottomLeftCorner.x)
        if (Mathf.Abs(direction.x) > 0.001f)
        {
            float t = (bottomLeftCorner.x - origin.x) / direction.x;
            if (t > 0)
            {
                float z = origin.z + direction.z * t;
                if (z >= bottomLeftCorner.z && z <= topLeftCorner.z && t < minT)
                {
                    minT = t;
                    intersectionPoint = new Vector3(bottomLeftCorner.x, yPosition, z);
                }
            }
        }

        // Right border (x = topRightCorner.x)
        if (Mathf.Abs(direction.x) > 0.001f)
        {
            float t = (topRightCorner.x - origin.x) / direction.x;
            if (t > 0)
            {
                float z = origin.z + direction.z * t;
                if (z >= bottomRightCorner.z && z <= topRightCorner.z && t < minT)
                {
                    minT = t;
                    intersectionPoint = new Vector3(topRightCorner.x, yPosition, z);
                }
            }
        }

        // Bottom border (z = bottomLeftCorner.z)
        if (Mathf.Abs(direction.z) > 0.001f)
        {
            float t = (bottomLeftCorner.z - origin.z) / direction.z;
            if (t > 0)
            {
                float x = origin.x + direction.x * t;
                if (x >= bottomLeftCorner.x && x <= bottomRightCorner.x && t < minT)
                {
                    minT = t;
                    intersectionPoint = new Vector3(x, yPosition, bottomLeftCorner.z);
                }
            }
        }

        // Top border (z = topLeftCorner.z)
        if (Mathf.Abs(direction.z) > 0.001f)
        {
            float t = (topLeftCorner.z - origin.z) / direction.z;
            if (t > 0)
            {
                float x = origin.x + direction.x * t;
                if (x >= topLeftCorner.x && x <= topRightCorner.x && t < minT)
                {
                    minT = t;
                    intersectionPoint = new Vector3(x, yPosition, topLeftCorner.z);
                }
            }
        }

        return intersectionPoint;
    }
}