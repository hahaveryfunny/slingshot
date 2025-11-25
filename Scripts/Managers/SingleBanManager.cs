using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class SingleBanManager : MonoBehaviour
{
    static public SingleBanManager instance;

    [Header("Ban Display Options")]
    [SerializeField] bool use3DDisplay = true; // Toggle between 3D object and image
    [SerializeField] Image bannedCarImageUI; // Assign your UI Image component here

    [Header("Ban Rotation")]
    [SerializeField] int spawnCooldownCount = 2;
    [SerializeField] float minBanTime = 5f;
    [SerializeField] float maxBanTime = 10f;
    Queue<int> recentSpawnedLanes = new Queue<int>();
    float banTimer = 0f;
    float nextBanTime;
    [Space(5)]

    [Header("Parameters")]
    [SerializeField] float spawnInterval = 2f;
    float defaultSpawnInterval;
    [SerializeField] float movementSpeed = 6f;
    float spawnTimer;
    [Space(5)]

    [Header("Banned Car Spawn Weight")]
    [SerializeField] [Range(1f, 10f)] float bannedCarSpawnWeight = 2f; // Higher = more likely to spawn banned car
    [SerializeField] [Tooltip("If true, banned car spawn rate increases over time")] bool increaseBannedCarSpawnOverTime = false;
    [SerializeField] [Range(0f, 2f)] float weightIncreasePerSecond = 0.1f;
    private float currentBannedCarWeight;
    [Space(5)]

    [SerializeField] Transform[] spawnPositions;
    [SerializeField] GameObject[] carPrefabs;
    [SerializeField] Transform signCarPosition;

    [Header("Pool Manager Reference")]
    [SerializeField] CarPoolManager carPoolManager;

    GameObject bannedCar; // For 3D display
    GameObject bannedCarPrefab; // Reference to the actual prefab that's banned
    Dictionary<int, List<GameObject>> activeCarsPerLane = new();

    [SerializeField] GameObject uiLightParent;
    [SerializeField] List<Image> heartImages = new();
    [SerializeField] float flashingSpeed = 3f;
    [SerializeField] List<Image> flashingImages = new();
    private int can = 6;
    public int maxHealth = 10;

    float healthPerHeart;
    [SerializeField] SpriteRenderer red3D;
    [SerializeField] SpriteRenderer yellow3D;
    [SerializeField] SpriteRenderer green3D;
    [SerializeField] Color unlitTrafficLightColor;

    Color originalRed;
    Color originalYellow;
    Color originalGreen;



    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this.gameObject);
        }
        defaultSpawnInterval = spawnInterval;
        can = maxHealth;

        if (carPoolManager == null)
        {
            carPoolManager = FindObjectOfType<CarPoolManager>();
            if (carPoolManager == null)
            {
                Debug.LogError("CarPoolManager not found! Please assign it in the inspector or add it to the scene.");
            }
        }

        // Validate that all car prefabs have CarInfo component
        ValidateCarPrefabs();
    }

    void ValidateCarPrefabs()
    {
        foreach (GameObject prefab in carPrefabs)
        {
            CarInfo carInfo = prefab.GetComponent<CarInfo>();
            if (carInfo == null)
            {
                Debug.LogError($"Car prefab {prefab.name} is missing CarInfo component!");
            }
            else if (carInfo.bannedCarSprite == null && !Is3D())
            {
                Debug.LogWarning($"Car prefab {prefab.name} is missing banned car sprite!");
            }
        }
    }

    void Start()
    {
        StartSetUp();
    }

    bool BannedPrefabActiveInAnyLane(GameObject prefab)
    {
        foreach (var laneList in activeCarsPerLane.Values)
        {
            if (laneList.Any(c => c != null && c.GetComponent<CarInfo>().sourcePrefab == prefab))
                return true;
        }
        return false;
    }

    void Update()
    {
        if (GameManager.instance.CurrentState != GameState.Playing) return;
        UpdateDisplayMode();
        spawnTimer += Time.deltaTime;
        SpawnCar();
        MoveObjects();
        HandleAutomaticBanRotation();
        ChangeBannedCar();

        // Increase banned car weight over time if enabled
        if (increaseBannedCarSpawnOverTime)
        {
            currentBannedCarWeight += weightIncreasePerSecond * Time.deltaTime;
        }
    }

    void HandleAutomaticBanRotation()
    {
        banTimer += Time.deltaTime;

        if (banTimer >= nextBanTime)
        {
            TryChangeBannedCar();
            banTimer = 0f;
            nextBanTime = UnityEngine.Random.Range(minBanTime, maxBanTime);
        }
    }

    void TryChangeBannedCar()
    {
        if (BannedCarAlreadyOnRoad())
        {
            return;
        }

        List<GameObject> possibleCars = new List<GameObject>(carPrefabs);
        if (bannedCarPrefab != null)
        {
            possibleCars.Remove(bannedCarPrefab);
        }

        foreach (GameObject candidatePrefab in possibleCars.OrderBy(x => UnityEngine.Random.value))
        {
            if (!BannedPrefabActiveInAnyLane(candidatePrefab))
            {
                BanCar(candidatePrefab);
                return;
            }
        }

        Debug.LogWarning("All cars active! Keeping current ban");
    }

    bool BannedCarAlreadyOnRoad()
    {
        if (bannedCarPrefab == null) return false;

        if (BannedPrefabActiveInAnyLane(bannedCarPrefab))
        {
            print("can't change to a new car while an active banned instance is on the road");
            return true;
        }

        return false;
    }

    void SpawnCar()
    {
        if (spawnTimer < spawnInterval) return;
        if (carPoolManager == null) return;

        List<int> availableLanes = GetAvailableLanes();
        if (availableLanes.Count == 0)
        {
            Debug.LogWarning("No available lanes for spawning!");
            return;
        }

        int chosenLaneIndex = availableLanes[UnityEngine.Random.Range(0, availableLanes.Count)];

        List<GameObject> availableCarPrefabs = new List<GameObject>();
        foreach (GameObject prefab in carPrefabs)
        {
            if (carPoolManager.IsCarAvailable(prefab))
            {
                availableCarPrefabs.Add(prefab);
            }
        }

        if (availableCarPrefabs.Count == 0)
        {
            Debug.LogWarning("No cars available in pool for spawning!");
            return;
        }

        // Use weighted selection to favor banned car
        GameObject chosenCarPrefab = SelectCarWithWeight(availableCarPrefabs);

        Debug.Log($"Spawning car {chosenCarPrefab.name} in lane {chosenLaneIndex}");
        Vector3 spawnPos = spawnPositions[chosenLaneIndex].position;
        float yRotation = spawnPos.x < 0 ? 90f : -90f;
        Quaternion rotation = Quaternion.Euler(0, yRotation, 0);

        GameObject newSpawn = carPoolManager.GetCarFromPool(chosenCarPrefab, spawnPos, rotation, chosenLaneIndex);
        newSpawn.transform.localScale = Vector3.one * 1.35f;

        if (newSpawn != null)
        {
            // Set the lane index on the existing CarInfo component
            CarInfo carInfo = newSpawn.GetComponent<CarInfo>();
            if (carInfo != null)
            {
                carInfo.laneIndex = chosenLaneIndex;
                // Ensure sourcePrefab is set correctly
                if (carInfo.sourcePrefab == null)
                {
                    carInfo.sourcePrefab = chosenCarPrefab;
                }
                Debug.Log($"Spawned car: {newSpawn.name}, sourcePrefab: {carInfo.sourcePrefab?.name}, laneIndex: {chosenLaneIndex}");
            }
            else
            {
                Debug.LogError($"CarInfo component missing on spawned car: {newSpawn.name}");
            }

            activeCarsPerLane[chosenLaneIndex].Add(newSpawn);
            TrackLaneSpawn(chosenLaneIndex);

            // Reset weight after spawning banned car
            if (chosenCarPrefab == bannedCarPrefab && increaseBannedCarSpawnOverTime)
            {
                currentBannedCarWeight = bannedCarSpawnWeight;
            }
        }

        spawnTimer = 0;
    }

    GameObject SelectCarWithWeight(List<GameObject> availableCars)
    {
        if (bannedCarPrefab == null || !availableCars.Contains(bannedCarPrefab))
        {
            // No banned car or it's not available, pick randomly
            return availableCars[UnityEngine.Random.Range(0, availableCars.Count)];
        }

        // Calculate total weight
        float effectiveWeight = increaseBannedCarSpawnOverTime ? currentBannedCarWeight : bannedCarSpawnWeight;
        float totalWeight = (availableCars.Count - 1) + effectiveWeight; // Other cars have weight 1, banned car has custom weight

        // Pick a random value
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);

        // Check if we hit the banned car
        if (randomValue < effectiveWeight)
        {
            return bannedCarPrefab;
        }

        // Otherwise pick from other cars
        List<GameObject> otherCars = availableCars.Where(c => c != bannedCarPrefab).ToList();
        return otherCars[UnityEngine.Random.Range(0, otherCars.Count)];
    }

    List<int> GetAvailableLanes()
    {
        List<int> availableLanes = new List<int>();

        for (int i = 0; i < spawnPositions.Length; i++)
        {
            availableLanes.Add(i);
        }

        foreach (int recentLane in recentSpawnedLanes)
        {
            availableLanes.Remove(recentLane);
        }

        return availableLanes;
    }

    void TrackLaneSpawn(int laneIndex)
    {
        recentSpawnedLanes.Enqueue(laneIndex);

        if (recentSpawnedLanes.Count > spawnCooldownCount)
        {
            int freedLane = recentSpawnedLanes.Dequeue();
            Debug.Log($"Lane {freedLane} is now available for spawning again");
        }

        Debug.Log($"Recent spawned lanes: [{string.Join(", ", recentSpawnedLanes)}]");
    }

    void MoveObjects()
    {
        foreach (List<GameObject> laneList in activeCarsPerLane.Values)
        {
            for (int i = laneList.Count - 1; i >= 0; i--)
            {
                GameObject car = laneList[i];

                if (car == null)
                {
                    laneList.RemoveAt(i);
                    continue;
                }

                // Move forward
                car.transform.position += movementSpeed * Time.deltaTime * car.transform.forward;

                // Add a small shake/sway
                float shakeAmount = 0.05f; // adjust for intensity
                float shakeSpeed = 5f;     // oscillation frequency

                Vector3 sideways = car.transform.right * Mathf.Sin(Time.time * shakeSpeed + i);
                Vector3 upDown = car.transform.up * Mathf.Cos(Time.time * shakeSpeed * 0.5f + i);

                car.transform.position += (sideways + upDown) * shakeAmount * Time.deltaTime;
            }
        }
    }


    public bool IsThisCarBanned(int laneIndex, CarInfo car)
    {
        if (bannedCarPrefab == null) return false;

        // Debug logging to help troubleshoot
        Debug.Log($"Checking banned car: bannedCarPrefab={bannedCarPrefab.name}, car.sourcePrefab={car.sourcePrefab?.name}");

        return car.sourcePrefab == bannedCarPrefab;
    }

    public void StartSetUp()
    {
        ClearGame();
        SetUpBanTime();
        can = maxHealth;

        recentSpawnedLanes.Clear();
        BanCar();
        // Store original colors to restore later
        originalRed = red3D.color;
        originalYellow = yellow3D.color;
        originalGreen = green3D.color;

        for (var i = 0; i < spawnPositions.Length; i++)
        {
            activeCarsPerLane[i] = new List<GameObject>();
        }

        healthPerHeart = (float)heartImages.Count / maxHealth;
        UpdateHealth();
        spawnTimer = 5;
        banTimer = 0f;
        spawnInterval = defaultSpawnInterval;
        currentBannedCarWeight = bannedCarSpawnWeight; // Initialize weight
        //spawnInterval += UnityEngine.Random.Range(0, 1f);
        GameManager.instance.SetTimeScale(1);
    }

    void ChangeBannedCar()
    {
        foreach (char c in Input.inputString)
        {
            if (char.IsDigit(c))
            {
                int index = c - '0';
                if (index >= carPrefabs.Length)
                {
                    Debug.Log("No car at index: " + index);
                    return;
                }

                GameObject candidatePrefab = carPrefabs[index];

                if (BannedCarAlreadyOnRoad())
                {
                    return;
                }

                if (BannedPrefabActiveInAnyLane(candidatePrefab))
                {
                    Debug.Log($"Can't ban {candidatePrefab.name} - it's still driving on the road");
                }
                else
                {
                    string previousCarName = bannedCarPrefab != null ? bannedCarPrefab.name : "none";
                    Debug.Log($"Changing banned car from {previousCarName} to {candidatePrefab.name}");
                    BanCar(candidatePrefab);
                }
            }
        }
    }

    public void OnCarReachedEnd(GameObject car, int laneIndex)
    {
        if (activeCarsPerLane.ContainsKey(laneIndex))
        {
            activeCarsPerLane[laneIndex].Remove(car);
        }

        if (carPoolManager != null)
        {
            carPoolManager.ReturnCarToPool(car);
        }
    }


    void UpdateDisplayMode()
    {
        if (use3DDisplay)
        {
            if (uiLightParent.activeInHierarchy) uiLightParent.SetActive(false);
            bannedCarImageUI.gameObject.SetActive(false);
        }
        else
        {
            if (!uiLightParent.activeInHierarchy) uiLightParent.SetActive(true);
            bannedCarImageUI.gameObject.SetActive(true);

        }
    }

    void PauseEditor()
    {
#if UNITY_EDITOR
        EditorApplication.isPaused = true;
#endif
    }

    public void TakeDamage(int damage)
    {
        can -= damage;
        UpdateHealth();
        if (can == 0)
        {
            GameManager.instance.GameOver();
        }
    }

    bool Is3D()
    {
        return use3DDisplay;
    }

    public void UpdateHealth()
    {
        if (Is3D())
        {
            // Turn everything off first
            red3D.color = unlitTrafficLightColor;
            yellow3D.color = unlitTrafficLightColor;
            green3D.color = unlitTrafficLightColor;

            // Then light up according to health
            if (can >= 3)
            {
                green3D.color = originalGreen;
                yellow3D.color = originalYellow;
                red3D.color = originalRed;
            }
            else if (can == 2)
            {
                green3D.color = unlitTrafficLightColor;
                yellow3D.color = originalYellow;
                red3D.color = originalRed;
            }
            else if (can == 1)
            {
                green3D.color = unlitTrafficLightColor;
                yellow3D.color = unlitTrafficLightColor;
                red3D.color = originalRed;
            }
        }
        else
        {
            float remainingTotalHealth = can * healthPerHeart;
            for (int i = 0; i < heartImages.Count; i++)
            {
                float fillAmount = Mathf.Clamp01(remainingTotalHealth - i);
                heartImages[i].fillAmount = fillAmount;
            }
            // Start the flashing coroutine
            StartCoroutine(UpdateFlashingHearts(remainingTotalHealth));
        }
    }

    IEnumerator UpdateFlashingHearts(float targetHealth)
    {
        bool stillMoving = true;

        while (stillMoving)
        {
            stillMoving = false;

            for (int i = 0; i < flashingImages.Count; i++)
            {
                float targetFill = Mathf.Clamp01(targetHealth - i);
                float currentFill = flashingImages[i].fillAmount;

                // Move at constant speed (adjust the 0.5f value for speed)
                flashingImages[i].fillAmount = Mathf.MoveTowards(currentFill, targetFill, Time.deltaTime * flashingSpeed);

                // Check if still moving
                if (Mathf.Abs(flashingImages[i].fillAmount - targetFill) > 0.001f)
                {
                    stillMoving = true;
                }
            }

            yield return null;
        }
    }


    void SetUpBanTime()
    {
        nextBanTime = UnityEngine.Random.Range(minBanTime, maxBanTime);
    }

    public void ClearGame()
    {
        // Clear banned display
        ClearBannedDisplay();

        // Return all active cars to pool
        if (carPoolManager != null)
        {
            carPoolManager.ReturnAllCarsToPool();
        }

        foreach (var lane in activeCarsPerLane.Values)
        {
            lane.Clear();
        }
        activeCarsPerLane.Clear();
    }

    void ClearBannedDisplay()
    {
        // Clear 3D display
        if (bannedCar != null)
        {
            Destroy(bannedCar.gameObject);
            bannedCar = null;
        }

        if (signCarPosition != null)
        {
            foreach (Transform t in signCarPosition)
            {
                Destroy(t.gameObject);
            }
        }

        // Clear image display
        if (bannedCarImageUI != null)
        {
            bannedCarImageUI.sprite = null;
            bannedCarImageUI.gameObject.SetActive(false);
        }

        bannedCarPrefab = null;
    }

    void BanCar(GameObject prefab = null)
    {
        // Clear existing display
        ClearBannedDisplay();

        // Select random prefab if none provided
        if (prefab == null)
        {
            int banIndex = UnityEngine.Random.Range(0, carPrefabs.Length);
            prefab = carPrefabs[banIndex];
        }

        bannedCarPrefab = prefab;
        CarInfo carInfo = prefab.GetComponent<CarInfo>();

        // Reset weight when changing banned car
        currentBannedCarWeight = bannedCarSpawnWeight;

        if (Is3D())
        {
            // Create 3D display (your current system)
            bannedCar = Instantiate(prefab, signCarPosition.position + (-Vector3.up * .64f), Quaternion.Euler(0, 90, 15));
            bannedCar.transform.localScale = Vector3.one;
            bannedCar.transform.parent = signCarPosition;
            bannedCar.gameObject.layer = 0;
            bannedCar.gameObject.isStatic = true;
        }
        else
        {
            // Use image display
            if (bannedCarImageUI != null && carInfo != null && carInfo.bannedCarSprite != null)
            {
                bannedCarImageUI.sprite = carInfo.bannedCarSprite;
                bannedCarImageUI.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError($"Cannot display banned car image for {prefab.name}. Check if bannedCarImageUI is assigned and CarInfo has bannedCarSprite.");
            }
        }
        print($"Banned car: {prefab.name} (Display mode: {(Is3D() ? "3D" : "Image")})");
    }
}