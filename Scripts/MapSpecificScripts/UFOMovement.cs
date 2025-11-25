using UnityEngine;

public class UFOMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotationSpeed = 3f;
    [SerializeField] Vector2 moveAreaX = new Vector2(-50, 50);
    [SerializeField] Vector2 moveAreaZ = new Vector2(-50, 50);
    [SerializeField] bool useWorldSpace = false; // Toggle between local/world space
    [Header("Rotation")]
    [SerializeField] float idleRotationSpeed = 30f; // 🔹 Continuous Y rotation speed (degrees/sec)

    [Header("Hovering")]
    [SerializeField] float hoverHeight = 10f;
    [SerializeField] float heightAdjustSpeed = 5f;
    [SerializeField] float groundCheckDistance = 50f;

    [Header("Avoidance")]
    [SerializeField] float obstacleCheckDistance = 5f;
    [SerializeField] LayerMask obstacleMask;



    Vector3 targetPos;

    void Start()
    {
        PickNewTarget();
    }

    void Update()
    {
        MoveToTarget();
        AdjustHeight();
        CheckForObstacles();
        SpinY();
    }

    void MoveToTarget()
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Smoothly rotate toward movement direction (only affects horizontal facing)
        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, targetPos) < 2f)
        {
            PickNewTarget();
        }
    }

    void AdjustHeight()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance))
        {
            float desiredY = hit.point.y + hoverHeight;
            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(pos.y, desiredY, heightAdjustSpeed * Time.deltaTime);
            transform.position = pos;
        }
    }

    void CheckForObstacles()
    {
        if (Physics.Raycast(transform.position, transform.forward, obstacleCheckDistance, obstacleMask))
        {
            PickNewTarget();
        }
    }

    void SpinY()
    {
        // 🔹 Continuous rotation along Y-axis
        transform.Rotate(Vector3.up, idleRotationSpeed * Time.deltaTime, Space.Self);
    }

    void PickNewTarget()
    {
        float randomX = Random.Range(moveAreaX.x, moveAreaX.y);
        float randomZ = Random.Range(moveAreaZ.x, moveAreaZ.y);

        if (useWorldSpace)
        {
            targetPos = new Vector3(randomX, transform.position.y, randomZ);
        }
        else
        {
            Vector3 localTarget = new Vector3(randomX, transform.localPosition.y, randomZ);
            targetPos = transform.parent.TransformPoint(localTarget);
        }
    }
}
