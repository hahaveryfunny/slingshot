using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Spawner : MonoBehaviour
{
    static public Spawner instance;

    [Header("Ban Rotation")]
    [SerializeField] float minBanTime = 5f;
    [SerializeField] float maxBanTime = 10f;
    Dictionary<int, float> signBanTimers = new Dictionary<int, float>();
    Dictionary<int, float> signNextBanTime = new Dictionary<int, float>();
    [Space(5)]

    [Header("Parameters")]
    [SerializeField] float spawnInterval = 2f;
    float defaultSpawnInterval;
    [SerializeField] float movementSpeed = 6f;
    float spawnTimer;
    [Space(5)]

    [SerializeField] Transform[] spawnPositions;
    [SerializeField] GameObject[] carPrefabs;
    [SerializeField] List<Transform> signCarPositions = new();

    List<GameObject> toDestroy = new();

    Dictionary<int, GameObject> bannedCars = new();  // Banned cars for each lane
    Dictionary<int, List<GameObject>> activeCarsPerLane = new();

    int[][] signToLanes = new[]
{
    new[]{ 0, 1 }, // sign 0 covers lanes 0 & 1
    new[]{ 2, 3 }, // sign 1 covers lanes 2 & 3
    new[]{ 4, 5 }, // sign 2 covers lanes 4 & 5
};


    [SerializeField] List<Image> heartImages = new();
    public int can = 6;
    public int maxHealth = 10;

    float banTime = 6f;
    float banTimer;

    float healthPerHeart;

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
    }

    void Start()
    {
        StartSetUp();
    }

    bool NextPrefabActiveUnderSign(int signIndex, GameObject prefab)
    {
        foreach (int lane in signToLanes[signIndex])
            if (activeCarsPerLane[lane].Any(c => c.GetComponent<CarInfo>().sourcePrefab == prefab))
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
        for (int signIndex = 0; signIndex < signToLanes.Length; signIndex++)
        {
            signBanTimers[signIndex] += Time.deltaTime;

            if (signBanTimers[signIndex] >= signNextBanTime[signIndex])
            {
                TryChangeBannedCar(signIndex);
                signBanTimers[signIndex] = 0f;
                signNextBanTime[signIndex] = UnityEngine.Random.Range(minBanTime, maxBanTime);
            }
        }
    }

    void TryChangeBannedCar(int signIndex)
    {
        if (BannedCarAlreadyInLane(signIndex))
        {
            return;
        }
        // Get all possible car prefabs except current banned one
        List<GameObject> possibleCars = new List<GameObject>(carPrefabs);

        possibleCars.Remove(bannedCars[signIndex].GetComponent<CarInfo>().sourcePrefab);

        foreach (GameObject candidatePrefab in possibleCars.OrderBy(x => UnityEngine.Random.value))
        {

            if (!NextPrefabActiveUnderSign(signIndex, candidatePrefab))
            {
                BanCar(signIndex, candidatePrefab);
                return;
            }
        }

        // Fallback: If all cars are active, keep current ban
        Debug.LogWarning($"All cars active for sign {signIndex}! Keeping current ban");
    }

    bool BannedCarAlreadyInLane(int signIndex)
    {
        foreach (int lane in signToLanes[signIndex])
            if (activeCarsPerLane[lane].Any(c => c.GetComponent<CarInfo>().sourcePrefab == bannedCars[signIndex].GetComponent<CarInfo>().sourcePrefab))
            {
                print("can't change to a new car while an active banned instance is on the road");
                //PauseEditor();
                return true;
            }
        print(bannedCars[signIndex].GetComponent<CarInfo>().sourcePrefab);
        return false;
    }

    void SpawnCar()
    {
        if (spawnTimer < spawnInterval) return;

        int chosenLaneIndex = UnityEngine.Random.Range(0, spawnPositions.Length);
        Debug.Log($"Spawning car in lane {chosenLaneIndex}");
        Vector3 spawnPos = spawnPositions[chosenLaneIndex].position;
        float yRotation = spawnPos.x < 0 ? 90f : -90f;
        Quaternion rotation = Quaternion.Euler(0, yRotation, 0);

        int chosenCarIndex = UnityEngine.Random.Range(0, carPrefabs.Length);
        GameObject newSpawn = Instantiate(carPrefabs[chosenCarIndex], spawnPos, rotation);
        CarInfo info = newSpawn.AddComponent<CarInfo>();
        info.laneIndex = chosenLaneIndex;
        info.sourcePrefab = carPrefabs[chosenCarIndex];

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

        // Find which sign governs this lane
        int signIndex = -1;
        for (int i = 0; i < signToLanes.Length; i++)
        {
            if (Array.IndexOf(signToLanes[i], laneIndex) != -1)
            {
                signIndex = i;
                break;
            }
        }
        if (signIndex == -1) return false;
        // Compare the CAR'S PREFAB to the BANNED PREFAB
        GameObject bannedPrefab = bannedCars[signIndex].GetComponent<CarInfo>().sourcePrefab;
        return car.sourcePrefab == bannedPrefab;
    }



    public void StartSetUp()
    {
        ClearGame();
        SetUpBanTimes();
        can = maxHealth;

        for (var i = 0; i < signCarPositions.Count; i++)
        {
            BanCar(i);
        }

        for (var i = 0; i < spawnPositions.Count(); i++)
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

                int signIdx = 0; // Assuming you want to change sign 0

                // Get the prefab we want to ban
                GameObject candidatePrefab = carPrefabs[index];
                if (BannedCarAlreadyInLane(signIdx))
                {
                    return;
                }
                if (NextPrefabActiveUnderSign(signIdx, candidatePrefab))
                {
                    Debug.Log($"Can't ban {candidatePrefab.name} - it's still driving in lanes {string.Join(",", signToLanes[signIdx])}");
                    //PauseEditor();
                }
                else
                {
                    Debug.Log($"Changing banned car from {bannedCars[signIdx].GetComponent<CarInfo>().sourcePrefab.name} to {candidatePrefab.name}");
                    BanCar(signIdx, candidatePrefab);
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
        // Initialize timers for all signs
        for (int i = 0; i < signToLanes.Length; i++)
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
        for (var i = 0; i < signCarPositions.Count; i++)
        {
            foreach (Transform t in signCarPositions[i])
            {
                Destroy(t.gameObject);
            }
        }
    }



    void BanCar(int index, GameObject prefab = null)
    {
        foreach (Transform child in signCarPositions[index])
        {
            Destroy(child.gameObject);
        }
        int banIndex = UnityEngine.Random.Range(0, carPrefabs.Count());
        prefab = prefab ?? carPrefabs[banIndex];
        bannedCars[index] = Instantiate(prefab, signCarPositions[index].position + (-Vector3.up * .64f), Quaternion.Euler(0, 90, 15));
        bannedCars[index].AddComponent<CarInfo>();
        bannedCars[index].GetComponent<CarInfo>().laneIndex = -1;
        bannedCars[index].GetComponent<CarInfo>().sourcePrefab = prefab;

        bannedCars[index].transform.localScale = Vector3.one;
        bannedCars[index].transform.parent = signCarPositions[index];

        bannedCars[index].gameObject.layer = 0;
        bannedCars[index].gameObject.isStatic = true;
        print(bannedCars[index]);
    }



}