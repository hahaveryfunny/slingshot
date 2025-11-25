using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TrajectoryPredictor))]
public class ProjectileThrow : MonoBehaviour
{
    public float angle = -45;
    public static ProjectileThrow Instance;
    TrajectoryPredictor trajectoryPredictor;
    public MeshRenderer target;
    public Material resetMaterial;

    [SerializeField]
    Rigidbody throwPrefab;

    // public float forceMultiplier = 2;
    public float maxForce = 45;
    // public float minForce = 5;

    private Vector2 startPos;
    private Vector2 endPos;
    private bool isSwiping;

    public float distanceThreshold = 10f;
    float distanceToBall;
    bool canShoot = false;

    public float force = 10;

    [SerializeField]
    Transform StartPosition;

    MeshRenderer meshRenderer;
    public float spawnTime = .5f;

    // Keep track of thrown objects for cleanup
    private GameObject lastThrownObject;

    [SerializeField] Image fillImage;
    float fillAmount;

    // POWER SLIDER SYSTEM
    public enum ShootPhase { AngleAdjustment, PowerAdjustment, Shooting, Cooldown }
    public ShootPhase currentPhase = ShootPhase.AngleAdjustment;

    // Power slider UI elements
    [SerializeField] private Image powerSliderBackground; // Background image with green/red zones
    [SerializeField] private Image powerSlider; // The moving slider indicator
    [SerializeField] private float sliderSpeed = 20f; // Speed of slider movement

    private bool sliderMovingUp = true;
    private float sliderPosition = 0.5f; // 0 to 1, where 0.5 is center (green zone)
    private int sliderCycles = 0;
    private int maxSliderCycles = 2;
    private bool powerPhaseActive = false;

    // Power calculation settings
    public float greenZoneSize = 0.2f; // Size of perfect accuracy zone
    public float minPowerMultiplier = 0.3f;
    public float maxPowerMultiplier = 2.5f;

    [SerializeField] MeshRenderer spawnAreaMesh;// Hidden mesh from Blender
    [SerializeField] float spawnHeight = 2f; // Height above the ground

    [SerializeField] Vector3 resetLocation;




    void OnEnable()
    {
        trajectoryPredictor = GetComponent<TrajectoryPredictor>();

        if (StartPosition == null)
            StartPosition = transform;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    void Start()
    {
        force = 0;
        meshRenderer = GetComponent<MeshRenderer>();

        // Initialize power slider UI as hidden
        if (powerSliderBackground != null)
            powerSliderBackground.gameObject.SetActive(false);
        if (powerSlider != null)
            powerSlider.gameObject.SetActive(false);
    }

    void Update()
    {
        // Check for space key press to reset
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(RespawnBallAfterDelay(0f,true));
            ResetShooter();
        }

        if (meshRenderer.enabled == true)
        {
            if (currentPhase == ShootPhase.AngleAdjustment)
            {
                ProcessInput();
                Predict();
            }
            else if (currentPhase == ShootPhase.PowerAdjustment)
            {
                ProcessPowerSlider();
                Predict();
            }
        }
    }

    Vector3 GetRandomSpawnPosition()
    {
        Bounds bounds = spawnAreaMesh.bounds;
        Vector3 randomPos = new Vector3(
          Random.Range(bounds.min.x, bounds.max.x),
          bounds.center.y + spawnHeight, // Or use bounds for Y too
          Random.Range(bounds.min.z, bounds.max.z)
      );
        return randomPos;
    }

    void Predict()
    {
        trajectoryPredictor.PredictTrajectory(ProjectileData());
    }

    ProjectileProperties ProjectileData()
    {
        ProjectileProperties properties = new ProjectileProperties();
        Rigidbody r = throwPrefab.GetComponent<Rigidbody>();

        // FIX: Use the same direction as the actual throw
        properties.direction = StartPosition.forward;
        properties.initialPosition = StartPosition.position;

        // FIX: Convert force to velocity properly
        // When using ForceMode.Impulse, the velocity = force / mass
        properties.initialSpeed = force / r.mass;
        properties.mass = r.mass;
        properties.drag = r.drag;

        return properties;
    }

    public void ThrowObject()
    {
        Rigidbody thrownObject = Instantiate(throwPrefab, StartPosition.position, StartPosition.rotation);
        lastThrownObject = thrownObject.gameObject;
        meshRenderer.enabled = false;
        thrownObject.useGravity = true;
        thrownObject.AddForce(StartPosition.forward * force, ForceMode.Impulse);
        trajectoryPredictor.showLine = false;
        currentPhase = ShootPhase.Cooldown;

        // Start coroutine to respawn ball after delay
        StartCoroutine(RespawnBallAfterDelay(2.5f,false)); // 3 second delay
    }

    IEnumerator RespawnBallAfterDelay(float delay, bool reset)
    {
        yield return new WaitForSeconds(delay);

        // Move the shooter to new random position
        Vector3 newPos = !reset ? GetRandomSpawnPosition() : resetLocation;
        transform.position = newPos;

        // Reset the shooter
        ResetShooter();
    }

    // Reset function to allow shooting again
    public void ResetShooter()
    {
        // Destroy the last thrown object if it exists
        if (lastThrownObject != null)
        {
            Destroy(lastThrownObject);
            lastThrownObject = null;
        }

        // Reset shooter state
        meshRenderer.enabled = true;
        canShoot = false;
        isSwiping = false;
        force = 0;
        currentPhase = ShootPhase.AngleAdjustment;
        powerPhaseActive = false;
        sliderCycles = 0;

        // Reset rotation
        transform.rotation = Quaternion.Euler(angle, 0, 0);

        // Reset trajectory predictor
        trajectoryPredictor.showLine = false;
        trajectoryPredictor.SetTrajectoryVisible(false);

        // Hide power slider UI
        if (powerSliderBackground != null)
            powerSliderBackground.gameObject.SetActive(false);
        if (powerSlider != null)
            powerSlider.gameObject.SetActive(false);

        // Reset target material
        if (target != null && resetMaterial != null)
        {
            target.material = resetMaterial;
        }

        Debug.Log("Shooter reset - ready to shoot again!");
    }

    void UpdateBallRotation()
    {
        // Get ball position in screen space
        Vector3 ballScreenPos = Camera.main.WorldToScreenPoint(transform.position);

        // Calculate direction from ball to current touch position
        Vector2 ballToTouch = new Vector2(endPos.x - ballScreenPos.x, endPos.y - ballScreenPos.y);

        // We want to shoot in the OPPOSITE direction of the pull
        Vector2 shootDirection = -ballToTouch.normalized;

        // Convert screen direction to world direction
        // Method 1: Use camera's transform to convert screen space direction to world space
        Vector3 screenPoint1 = new Vector3(ballScreenPos.x, ballScreenPos.y, Camera.main.nearClipPlane);
        Vector3 screenPoint2 = new Vector3(ballScreenPos.x + shootDirection.x * 100, ballScreenPos.y + shootDirection.y * 100, Camera.main.nearClipPlane);

        Vector3 worldPoint1 = Camera.main.ScreenToWorldPoint(screenPoint1);
        Vector3 worldPoint2 = Camera.main.ScreenToWorldPoint(screenPoint2);

        Vector3 worldDirection = (worldPoint2 - worldPoint1).normalized;

        // Project the direction onto the horizontal plane (Y = 0)
        worldDirection.y = 0;
        worldDirection.Normalize();

        // Calculate the rotation to look in that direction
        Quaternion targetRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
        float yawAngle = targetRotation.eulerAngles.y;

        // Calculate force based on pull distance from ball
        float pullDistance = ballToTouch.magnitude;
        float normalizedDistance = Mathf.Clamp01(pullDistance / (Screen.height * 0.5f));

        force = maxForce * normalizedDistance;

        // Apply rotation (keep the -45 pitch, use calculated yaw)
        transform.rotation = Quaternion.Euler(angle, yawAngle, 0);

        // Make sure StartPosition follows the same rotation if it's a child
        if (StartPosition != transform)
        {
            StartPosition.rotation = transform.rotation;
        }

        // Debug info
        Debug.Log($"Pull direction: {ballToTouch}, Shoot direction: {shootDirection}, World direction: {worldDirection}, Yaw: {yawAngle}");
    }

    float GetTouchDistanceToBall(Touch touch)
    {
        Vector3 touchWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, Camera.main.transform.position.y - transform.position.y));
        return Vector3.Distance(transform.position, touchWorldPos);
    }

    void ProcessInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                startPos = touch.position;
                isSwiping = true;
            }

            if (touch.phase == TouchPhase.Moved && isSwiping)
            {
                endPos = touch.position;
                UpdateBallRotation();
                trajectoryPredictor.SetTrajectoryVisible(true);
                canShoot = true;
            }

            if (touch.phase == TouchPhase.Ended)
            {
                isSwiping = false;
                endPos = touch.position;

                UpdateBallRotation();
                if (canShoot)
                {
                    // Instead of throwing immediately, start power phase
                    StartPowerPhase();
                }
                // Don't reset rotation here anymore since we're going to power phase
            }
        }
    }

    // POWER SLIDER SYSTEM METHODS
    void StartPowerPhase()
    {
        currentPhase = ShootPhase.PowerAdjustment;
        powerPhaseActive = true;
        sliderCycles = 0;
        sliderPosition = 0.5f; // Start at center
        sliderMovingUp = true;

        // Show power UI
        if (powerSliderBackground != null)
            powerSliderBackground.gameObject.SetActive(true);
        if (powerSlider != null)
            powerSlider.gameObject.SetActive(true);

        Debug.Log("Power phase started!");
    }

    void ProcessPowerSlider()
    {
        if (!powerPhaseActive) return;

        // Move slider
        if (sliderMovingUp)
        {
            sliderPosition += sliderSpeed * Time.deltaTime;
            if (sliderPosition >= 1f)
            {
                sliderPosition = 1f;
                sliderMovingUp = false;
                sliderCycles++;
            }
        }
        else
        {
            sliderPosition -= sliderSpeed * Time.deltaTime;
            if (sliderPosition <= 0f)
            {
                sliderPosition = 0f;
                sliderMovingUp = true;
                sliderCycles++;
            }
        }

        // Update slider UI position
        UpdateSliderUI();

        // Check for touch to shoot or auto-stop after max cycles
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            ShootWithCurrentPower();
        }
        else if (sliderCycles >= maxSliderCycles)
        {
            // Random stop after max cycles
            if (Random.Range(0f, 1f) < 0.3f) // 30% chance to stop each frame after max cycles
            {
                ShootWithCurrentPower();
            }
        }
    }

    void UpdateSliderUI()
    {
        if (powerSlider == null || powerSliderBackground == null) return;

        // Get the background rect
        RectTransform bgRect = powerSliderBackground.rectTransform;
        RectTransform sliderRect = powerSlider.rectTransform;

        // Calculate slider position within background bounds
        float bgHeight = bgRect.rect.height - (21 * 2 * powerSliderBackground.transform.localScale.y);
        float yPosition = (sliderPosition - 0.5f) * bgHeight;

        // Update slider position
        sliderRect.anchoredPosition = new Vector2(sliderRect.anchoredPosition.x, yPosition);
    }

    float CalculatePowerMultiplier()
    {
        // Calculate distance from center (0.5)
        float distanceFromCenter = Mathf.Abs(sliderPosition - 0.5f);

        // Check if in green zone (perfect accuracy)
        if (distanceFromCenter <= greenZoneSize / 2f)
        {
            return 1f; // Perfect accuracy, normal power
        }

        // Calculate power multiplier based on distance from green zone
        float normalizedDistance = (distanceFromCenter - greenZoneSize / 2f) / (0.5f - greenZoneSize / 2f);
        normalizedDistance = Mathf.Clamp01(normalizedDistance);

        // Random factor for sometimes more/less power
        float randomFactor = Random.Range(0.7f, 1.3f);

        // Calculate final multiplier
        float multiplier = Mathf.Lerp(1f, Random.Range(minPowerMultiplier, maxPowerMultiplier), normalizedDistance);
        multiplier *= randomFactor;

        return multiplier;
    }

    void ShootWithCurrentPower()
    {
        float powerMult = CalculatePowerMultiplier();
        force = force * powerMult; // Apply multiplier to the force already calculated from angle adjustment

        // Hide power UI
        if (powerSliderBackground != null)
            powerSliderBackground.gameObject.SetActive(false);
        if (powerSlider != null)
            powerSlider.gameObject.SetActive(false);

        powerPhaseActive = false;
        currentPhase = ShootPhase.Shooting;

        Debug.Log($"Shooting with power multiplier: {powerMult:F2}, Final force: {force:F1}");

        ThrowObject();
    }

    public IEnumerator ShowShooter(float time, GameObject ball)
    {
        yield return new WaitForSeconds(time);
        meshRenderer.enabled = true;
        target.material = resetMaterial;
        print("resetted material");
        Destroy(ball);
    }
}