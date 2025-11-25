using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class SpawnerRoadSection : MonoBehaviour
{
    static public SpawnerRoadSection instance;

    [Header("Ban Rotation")]
    [SerializeField] float minBanTime = 5f;
    [SerializeField] float maxBanTime = 10f;
    Dictionary<int, float> signBanTimers = new Dictionary<int, float>();
    Dictionary<int, float> signNextBanTime = new Dictionary<int, float>();
    [Space(5)]

    [Header("Parameters")]
    [SerializeField] float spawnInterval = 2f;
    [SerializeField] float movementSpeed = 6f;
    float spawnTimer;
    [Space(5)]

    [Header("Road Sections")]
    [SerializeField] RoadSection[] roadSections;
    [SerializeField] GameObject[] carPrefabs;

    List<GameObject> toDestroy = new();

    Dictionary<int, GameObject> bannedCars = new();  // Banned cars for each road section
    Dictionary<int, List<GameObject>> activeCarsPerLane = new();

    // Each road section manages 2 lanes (spawnPoint and spawnPoint2)
    // So road section 0 manages lanes 0,1 - road section 1 manages lanes 2,3 etc.

    [SerializeField] List<Image> heartImages = new();
    public int can = 6;
    public int maxHealth = 10;

    float banTime = 6f;
    float banTimer;

    float healthPerHeart;
    float defaultSpawnInterval;

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
        can = maxHealth;
        gameObject.SetActive(false);
        defaultSpawnInterval = spawnInterval;
    }

    void Start()
    {
        StartSetUp();
    }

    bool NextPrefabActiveUnderSign(int roadSectionIndex, GameObject prefab)
    {
        // Each road section manages 2 lanes: lane index = roadSectionIndex * 2 and roadSectionIndex * 2 + 1
        int lane1 = roadSectionIndex * 2;
        int lane2 = roadSectionIndex * 2 + 1;

        if (activeCarsPerLane.ContainsKey(lane1) && activeCarsPerLane[lane1].Any(c => c.GetComponent<CarInfo>().sourcePrefab == prefab))
            return true;
        if (activeCarsPerLane.ContainsKey(lane2) && activeCarsPerLane[lane2].Any(c => c.GetComponent<CarInfo>().sourcePrefab == prefab))
            return true;

        return false;
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.instance.CurrentState != GameState.Playing) return;

        spawnTimer += Time.deltaTime;
        banTimer += Time.deltaTime;
        SpawnCar();
        MoveObjects();
        HandleAutomaticBanRotation();

        ChangeBannedCar();

        foreach (var car in toDestroy)
        {
            Destroy(car);
        }
        toDestroy.Clear();
    }

    void HandleAutomaticBanRotation()
    {
        for (int roadSectionIndex = 0; roadSectionIndex < roadSections.Length; roadSectionIndex++)
        {
            signBanTimers[roadSectionIndex] += Time.deltaTime;

            if (signBanTimers[roadSectionIndex] >= signNextBanTime[roadSectionIndex])
            {
                TryChangeBannedCar(roadSectionIndex);
                signBanTimers[roadSectionIndex] = 0f;
                signNextBanTime[roadSectionIndex] = UnityEngine.Random.Range(minBanTime, maxBanTime);
            }
        }
    }

    void TryChangeBannedCar(int roadSectionIndex)
    {
        if (BannedCarAlreadyInLane(roadSectionIndex))
        {
            return;
        }
        // Get all possible car prefabs except current banned one
        List<GameObject> possibleCars = new List<GameObject>(carPrefabs);

        possibleCars.Remove(bannedCars[roadSectionIndex].GetComponent<CarInfo>().sourcePrefab);

        foreach (GameObject candidatePrefab in possibleCars.OrderBy(x => UnityEngine.Random.value))
        {
            if (!NextPrefabActiveUnderSign(roadSectionIndex, candidatePrefab))
            {
                BanCar(roadSectionIndex, candidatePrefab);
                return;
            }
        }

        // Fallback: If all cars are active, keep current ban
        Debug.LogWarning($"All cars active for road section {roadSectionIndex}! Keeping current ban");
    }

    bool BannedCarAlreadyInLane(int roadSectionIndex)
    {
        // Check both lanes managed by this road section
        int lane1 = roadSectionIndex * 2;
        int lane2 = roadSectionIndex * 2 + 1;

        if (activeCarsPerLane.ContainsKey(lane1) &&
            activeCarsPerLane[lane1].Any(c => c.GetComponent<CarInfo>().sourcePrefab == bannedCars[roadSectionIndex].GetComponent<CarInfo>().sourcePrefab))
        {
            print("can't change to a new car while an active banned instance is on the road");
            return true;
        }

        if (activeCarsPerLane.ContainsKey(lane2) &&
            activeCarsPerLane[lane2].Any(c => c.GetComponent<CarInfo>().sourcePrefab == bannedCars[roadSectionIndex].GetComponent<CarInfo>().sourcePrefab))
        {
            print("can't change to a new car while an active banned instance is on the road");
            return true;
        }

        print(bannedCars[roadSectionIndex].GetComponent<CarInfo>().sourcePrefab);
        return false;
    }

    void SpawnCar()
    {
        if (spawnTimer < spawnInterval) return;

        // Get total number of spawn points (each road section has 2)
        int totalSpawnPoints = roadSections.Length * 2;
        int chosenLaneIndex = UnityEngine.Random.Range(0, totalSpawnPoints);

        // Determine which road section and which spawn point within that section
        int roadSectionIndex = chosenLaneIndex / 2;
        int spawnPointIndex = chosenLaneIndex % 2; // 0 for spawnPoint, 1 for spawnPoint2

        Transform spawnTransform = spawnPointIndex == 0 ? roadSections[roadSectionIndex].spawnPoint : roadSections[roadSectionIndex].spawnPoint2;

        Debug.Log($"Spawning car in lane {chosenLaneIndex} (Road Section {roadSectionIndex}, Spawn Point {spawnPointIndex})");

        Vector3 spawnPos = spawnTransform.position;
        float yRotation = spawnPos.x < 0 ? 90f : -90f;
        Quaternion rotation = Quaternion.Euler(0, yRotation, 0);

        int chosenCarIndex = UnityEngine.Random.Range(0, carPrefabs.Length);
        GameObject newSpawn = Instantiate(carPrefabs[chosenCarIndex], spawnPos, rotation);
        CarInfo info = newSpawn.AddComponent<CarInfo>();
        info.laneIndex = chosenLaneIndex;
        info.sourcePrefab = carPrefabs[chosenCarIndex];
        newSpawn.transform.localScale *= 3;

        if (!activeCarsPerLane.ContainsKey(chosenLaneIndex))
            activeCarsPerLane[chosenLaneIndex] = new List<GameObject>();

        activeCarsPerLane[chosenLaneIndex].Add(newSpawn);

        spawnTimer = 0;
    }

    void MoveObjects()
    {
        foreach (List<GameObject> laneList in activeCarsPerLane.Values)
        {
            // Iterate in reverse so we can remove null/destroyed cars safely
            for (int i = laneList.Count - 1; i >= 0; i--)
            {
                GameObject car = laneList[i];

                if (car == null)
                {
                    laneList.RemoveAt(i);
                    continue;
                }

                car.transform.position += movementSpeed * Time.deltaTime * car.transform.forward;
            }
        }
    }

    public bool IsThisCarBanned(int laneIndex, CarInfo car)
    {
        // Find which road section governs this lane
        int roadSectionIndex = laneIndex / 2;

        if (roadSectionIndex >= roadSections.Length) return false;

        // Compare the CAR'S PREFAB to the BANNED PREFAB
        GameObject bannedPrefab = bannedCars[roadSectionIndex].GetComponent<CarInfo>().sourcePrefab;
        return car.sourcePrefab == bannedPrefab;
    }

    public void StartSetUp()
    {
        ClearGame();
        SetUpBanTimes();
        can = maxHealth;

        for (var i = 0; i < roadSections.Length; i++)
        {
            BanCar(i);
        }

        // Initialize active cars for all lanes (each road section has 2 lanes)
        for (var i = 0; i < roadSections.Length * 2; i++)
        {
            activeCarsPerLane[i] = new List<GameObject>();
        }

        healthPerHeart = (float)heartImages.Count / maxHealth;
        UpdateHealth();
        spawnTimer = 5;
        banTimer = 0f;
        spawnInterval = defaultSpawnInterval;
        spawnInterval += UnityEngine.Random.Range(0, 1f);
        GameManager.instance.SetTimeScale(1);
    }

    void ChangeBannedCar()
    {
        foreach (char c in Input.inputString)
        {
            if (char.IsDigit(c))
            {
                int index = c - '0'; // Converts '0'–'9' to 0–9
                if (index >= carPrefabs.Length)
                {
                    Debug.Log("No car at index: " + index);
                    return;
                }

                int roadSectionIdx = 0; // Assuming you want to change road section 0

                // Get the prefab we want to ban
                GameObject candidatePrefab = carPrefabs[index];
                if (BannedCarAlreadyInLane(roadSectionIdx))
                {
                    return;
                }
                if (NextPrefabActiveUnderSign(roadSectionIdx, candidatePrefab))
                {
                    Debug.Log($"Can't ban {candidatePrefab.name} - it's still driving in road section {roadSectionIdx}");
                }
                else
                {
                    Debug.Log($"Changing banned car from {bannedCars[roadSectionIdx].GetComponent<CarInfo>().sourcePrefab.name} to {candidatePrefab.name}");
                    BanCar(roadSectionIdx, candidatePrefab);
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
        toDestroy.Add(car);
    }

    void PauseEditor()
    {
#if UNITY_EDITOR
        EditorApplication.isPaused = true;
#endif
    }

    public void UpdateHealth()
    {
        float remainingTotalHealth = can * healthPerHeart;
        for (int i = 0; i < heartImages.Count; i++)
        {
            float fillAmount = Mathf.Clamp01(remainingTotalHealth - i);
            heartImages[i].fillAmount = fillAmount;
        }
    }

    void SetUpBanTimes()
    {
        // Initialize timers for all road sections
        for (int i = 0; i < roadSections.Length; i++)
        {
            signBanTimers[i] = 0f;
            signNextBanTime[i] = UnityEngine.Random.Range(minBanTime, maxBanTime);
        }
    }

    public void ClearGame()
    {
        // Clear banned cars
        foreach (var car in bannedCars.Values)
        {
            if (car != null) Destroy(car.gameObject);
        }
        bannedCars.Clear();

        // Clear active cars
        foreach (var lane in activeCarsPerLane.Values)
        {
            foreach (var car in lane)
            {
                if (car != null) Destroy(car);
            }
            lane.Clear();
        }
        activeCarsPerLane.Clear();

        // Clear timers
        signBanTimers.Clear();
        signNextBanTime.Clear();

        // Clear signs
        ClearSigns();
        //GameManager.instance.ClearSlingshotBalls();
    }

    void ClearSigns()
    {
        for (var i = 0; i < roadSections.Length; i++)
        {
            foreach (Transform t in roadSections[i].signPos)
            {
                Destroy(t.gameObject);
            }
        }
    }

    void BanCar(int roadSectionIndex, GameObject prefab = null)
    {
        foreach (Transform child in roadSections[roadSectionIndex].signPos)
        {
            Destroy(child.gameObject);
        }
        int banIndex = UnityEngine.Random.Range(0, carPrefabs.Count());
        prefab = prefab ?? carPrefabs[banIndex];
        bannedCars[roadSectionIndex] = Instantiate(prefab, roadSections[roadSectionIndex].signPos.position, Quaternion.Euler(0, 90, 0));
        bannedCars[roadSectionIndex].AddComponent<CarInfo>();
        bannedCars[roadSectionIndex].GetComponent<CarInfo>().laneIndex = -1;
        bannedCars[roadSectionIndex].GetComponent<CarInfo>().sourcePrefab = prefab;
        bannedCars[roadSectionIndex].transform.parent = roadSections[roadSectionIndex].signPos;
        bannedCars[roadSectionIndex].transform.localScale = new Vector3(4f, 4f, 4f);
        bannedCars[roadSectionIndex].gameObject.layer = 0;

        print(bannedCars[roadSectionIndex]);
    }
}