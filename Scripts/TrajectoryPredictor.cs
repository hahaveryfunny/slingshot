using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPredictor : MonoBehaviour
{
    #region Members
    LineRenderer trajectoryLine;
    [SerializeField, Tooltip("The marker will show where the projectile will hit")]
    Transform hitMarker;
    [SerializeField, Range(10, 100), Tooltip("The maximum number of points the LineRenderer can have")]
    int maxPoints = 50;
    [SerializeField, Range(0.01f, 0.5f), Tooltip("The time increment used to calculate the trajectory")]
    float increment = 0.025f;
    [SerializeField, Range(1.05f, 2f), Tooltip("The raycast overlap between points in the trajectory, this is a multiplier of the length between points. 2 = twice as long")]
    float rayOverlap = 1.1f;

    [Header("Ball Trail Settings")]
    [SerializeField] GameObject ballPrefab;
    [SerializeField] int numberOfBalls = 15;
    [SerializeField] float startBallSize = 0.3f;
    [SerializeField] float endBallSize = 0.15f;
    [SerializeField] Color startColor = Color.white;
    [SerializeField] Color endColor = new Color(1, 1, 1, 0.5f);

    private List<GameObject> trajectoryBalls = new List<GameObject>();
    private Vector3[] trajectoryPoints = new Vector3[0];
    private int trajectoryPointCount = 0;
    #endregion

    public bool showLine = true;

    private void Start()
    {
        if (trajectoryLine == null)
            trajectoryLine = GetComponent<LineRenderer>();
        if (hitMarker == null)
        {
            hitMarker = GameObject.FindGameObjectWithTag("Marker").GetComponent<Transform>();
        }

        // Hide the actual line renderer - we'll use it for calculations only
        trajectoryLine.enabled = false;

        // Create ball prefab if not assigned
        if (ballPrefab == null)
        {
            CreateDefaultBallPrefab();
        }

        // Initialize empty ball pool
        CreateBallPool();
        SetTrajectoryVisible(false);
    }

    private void CreateDefaultBallPrefab()
    {
        GameObject defaultBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        defaultBall.transform.localScale = Vector3.one * 0.2f;

        Material ballMat = new Material(Shader.Find("Standard"));
        ballMat.color = Color.white;
        defaultBall.GetComponent<Renderer>().material = ballMat;

        Destroy(defaultBall.GetComponent<Collider>());
        defaultBall.SetActive(false);
        ballPrefab = defaultBall;
    }

    private void CreateBallPool()
    {
        for (int i = 0; i < numberOfBalls; i++)
        {
            GameObject ball = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity, transform);
            ball.SetActive(false);
            trajectoryBalls.Add(ball);
        }
    }

    public void PredictTrajectory(ProjectileProperties projectile)
    {
        if (!showLine)
        {
            HideTrajectoryBalls();
            return;
        }

        // FIX: Use the initial speed directly as velocity magnitude
        Vector3 velocity = projectile.direction.normalized * projectile.initialSpeed;
        Vector3 position = projectile.initialPosition;
        Vector3 nextPosition;
        float overlap;

        // Debug the initial values
        Debug.Log($"Initial velocity: {velocity}, magnitude: {velocity.magnitude}");
        Debug.Log($"Initial position: {position}");
        Debug.Log($"Direction: {projectile.direction}");

        // Resize array if needed
        if (trajectoryPoints.Length < maxPoints)
        {
            trajectoryPoints = new Vector3[maxPoints];
        }

        // Set first point
        trajectoryPoints[0] = position;
        trajectoryPointCount = 1;

        for (int i = 1; i < maxPoints; i++)
        {
            // Calculate physics properly
            velocity = CalculateNewVelocity(velocity, projectile.drag, increment);
            nextPosition = position + velocity * increment;

            // Overlap our rays by small margin to ensure we never miss a surface
            overlap = Vector3.Distance(position, nextPosition) * rayOverlap;

            // When hitting a surface we want to show the surface marker and stop updating our line
            if (Physics.Raycast(position, velocity.normalized, out RaycastHit hit, overlap))
            {
                trajectoryPoints[i] = hit.point;
                trajectoryPointCount = i + 1;
                MoveHitMarker(hit);
                break;
            }

            // If nothing is hit, continue rendering the arc without a visual marker
            if (hitMarker != null)
                hitMarker.gameObject.SetActive(false);

            position = nextPosition;
            trajectoryPoints[i] = position;
            trajectoryPointCount = i + 1;

            // Stop if trajectory goes too far down (optional safety)
            if (position.y < -50f)
            {
                break;
            }
        }

        // Update the visual balls based on our trajectory points
        UpdateTrajectoryBalls();
    }

    private void UpdateTrajectoryBalls()
    {
        if (trajectoryPointCount < 2)
        {
            HideTrajectoryBalls();
            return;
        }

        // Calculate positions along the trajectory for ball placement
        for (int i = 0; i < numberOfBalls; i++)
        {
            // Ensure we have enough balls
            if (i >= trajectoryBalls.Count) break;

            // Calculate position based on distribution along the path
            float t = (float)i / (numberOfBalls - 1);
            int pointIndex = Mathf.FloorToInt(t * (trajectoryPointCount - 1));

            // Get adjacent points for interpolation
            Vector3 point = trajectoryPoints[pointIndex];
            Vector3 nextPoint = (pointIndex < trajectoryPointCount - 1) ?
                trajectoryPoints[pointIndex + 1] : point;

            // Fine interpolation between points
            float localT = t * (trajectoryPointCount - 1) - pointIndex;
            Vector3 position = Vector3.Lerp(point, nextPoint, localT);

            // Apply to the ball
            GameObject ball = trajectoryBalls[i];
            ball.transform.position = position;

            // Scale ball size along trajectory
            float scale = Mathf.Lerp(startBallSize, endBallSize, t);
            ball.transform.localScale = new Vector3(scale, scale, scale);

            // Set color with alpha fade
            Renderer renderer = ball.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.Lerp(startColor, endColor, t);
            }

            ball.SetActive(true);
        }
    }

    private void HideTrajectoryBalls()
    {
        foreach (GameObject ball in trajectoryBalls)
        {
            ball.SetActive(false);
        }
    }

    private Vector3 CalculateNewVelocity(Vector3 velocity, float drag, float increment)
    {
        // Apply gravity
        velocity += Physics.gravity * increment;

        // Apply drag - this should reduce velocity over time
        velocity *= Mathf.Clamp01(1f - drag * increment);

        return velocity;
    }

    private void MoveHitMarker(RaycastHit hit)
    {
        if (hitMarker != null)
        {
            hitMarker.gameObject.SetActive(true);

            // Offset marker from surface
            float offset = 0.025f;
            hitMarker.position = hit.point + hit.normal * offset;
            hitMarker.rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
        }
    }

    public void SetTrajectoryVisible(bool visible)
    {
        showLine = visible;

        if (!visible)
        {
            HideTrajectoryBalls();
            if (hitMarker != null)
                hitMarker.gameObject.SetActive(false);
        }
    }
}