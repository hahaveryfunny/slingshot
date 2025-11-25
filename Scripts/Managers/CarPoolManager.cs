using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CarPoolManager : MonoBehaviour
{
    [SerializeField] Transform carParent;
    [Header("Pool Settings")]
    [SerializeField] int poolSize = 5;
    [SerializeField] float autoReturnTime = 10f;

    [Header("Car Prefabs")]
    [SerializeField] GameObject[] carPrefabs;

    // Pool storage: prefab -> list of pooled instances
    Dictionary<GameObject, Queue<GameObject>> carPools = new Dictionary<GameObject, Queue<GameObject>>();

    // Track active cars for auto-return timing
    Dictionary<GameObject, float> activeCarTimers = new Dictionary<GameObject, float>();

    // Track which prefab each active car came from
    Dictionary<GameObject, GameObject> activeCarToPrefab = new Dictionary<GameObject, GameObject>();

    public static CarPoolManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        InitializePools();
    }

    void Update()
    {
        HandleAutoReturn();
    }

    void InitializePools()
    {
        foreach (GameObject prefab in carPrefabs)
        {
            // Validate that the prefab has CarInfo
            CarInfo prefabCarInfo = prefab.GetComponent<CarInfo>();
            if (prefabCarInfo == null)
            {
                Debug.LogError($"Car prefab {prefab.name} is missing CarInfo component! Please add it to the prefab.");
                continue;
            }

            Queue<GameObject> pool = new Queue<GameObject>();

            for (int i = 0; i < poolSize; i++)
            {
                GameObject pooledCar = Instantiate(prefab, transform);
                pooledCar.name = prefab.name + "_Pooled_" + i;
                pooledCar.transform.SetParent(carParent);

                // Get the existing CarInfo component (should already exist from prefab)
                CarInfo carInfo = pooledCar.GetComponent<CarInfo>();
                if (carInfo != null)
                {
                    // Reset to default pool state
                    carInfo.laneIndex = -1;
                    // sourcePrefab should already be set correctly from the prefab
                    if (carInfo.sourcePrefab == null)
                    {
                        carInfo.sourcePrefab = prefab;
                        Debug.LogWarning($"sourcePrefab was null for {prefab.name}, setting it manually");
                    }
                }
                else
                {
                    // Fallback: Add CarInfo if somehow missing (shouldn't happen if prefabs are set up correctly)
                    Debug.LogWarning($"CarInfo missing on instantiated car from prefab {prefab.name}, adding it manually");
                    carInfo = pooledCar.AddComponent<CarInfo>();
                    carInfo.laneIndex = -1;
                    carInfo.sourcePrefab = prefab;
                }

                pooledCar.SetActive(false);
                pool.Enqueue(pooledCar);
            }

            carPools[prefab] = pool;
        }

        Debug.Log($"Car pools initialized with {poolSize} instances each for {carPrefabs.Length} prefab types");
    }

    public GameObject GetCarFromPool(GameObject prefab, Vector3 position, Quaternion rotation, int laneIndex)
    {
        if (!carPools.ContainsKey(prefab))
        {
            Debug.LogError($"No pool found for prefab: {prefab.name}");
            return null;
        }

        Queue<GameObject> pool = carPools[prefab];

        if (pool.Count == 0)
        {
            Debug.LogWarning($"No available cars in pool for: {prefab.name}");
            return null;
        }

        GameObject car = pool.Dequeue();
        car.transform.position = position;
        car.transform.rotation = rotation;
        car.SetActive(true);

        // Update CarInfo with correct lane
        CarInfo carInfo = car.GetComponent<CarInfo>();
        if (carInfo != null)
        {
            carInfo.laneIndex = laneIndex;
            // sourcePrefab should already be correct, but verify
            if (carInfo.sourcePrefab != prefab)
            {
                Debug.LogWarning($"sourcePrefab mismatch for {car.name}: expected {prefab.name}, got {carInfo.sourcePrefab?.name}");
                carInfo.sourcePrefab = prefab;
            }
        }
        else
        {
            Debug.LogError($"CarInfo component missing on pooled car: {car.name}");
        }

        // Track this car for auto-return
        activeCarTimers[car] = 0f;
        activeCarToPrefab[car] = prefab;

        return car;
    }

    public void ReturnCarToPool(GameObject car)
    {
        if (car == null) return;

        // Remove from tracking
        if (activeCarTimers.ContainsKey(car))
        {
            activeCarTimers.Remove(car);
        }

        if (!activeCarToPrefab.ContainsKey(car))
        {
            Debug.LogError($"Car {car.name} not found in active tracking!");
            return;
        }

        GameObject sourcePrefab = activeCarToPrefab[car];
        activeCarToPrefab.Remove(car);

        // Reset car state
        car.SetActive(false);
        car.transform.parent = carParent;

        // Reset CarInfo to pool state (but keep sourcePrefab intact)
        CarInfo carInfo = car.GetComponent<CarInfo>();
        if (carInfo != null)
        {
            carInfo.laneIndex = -1;
            // Don't reset sourcePrefab - it should stay as the original prefab reference
        }

        // Return to appropriate pool
        if (carPools.ContainsKey(sourcePrefab))
        {
            carPools[sourcePrefab].Enqueue(car);
        }
        else
        {
            Debug.LogError($"No pool found for returning car with prefab: {sourcePrefab.name}");
        }
    }

    void HandleAutoReturn()
    {
        List<GameObject> carsToReturn = new List<GameObject>();

        // Update timers and collect cars that need to be returned
        foreach (var kvp in activeCarTimers.ToList())
        {
            GameObject car = kvp.Key;
            float timer = kvp.Value;

            if (car == null || !car.activeInHierarchy)
            {
                // Car was destroyed or deactivated externally
                carsToReturn.Add(car);
                continue;
            }

            timer += Time.deltaTime;
            activeCarTimers[car] = timer;

            if (timer >= autoReturnTime)
            {
                carsToReturn.Add(car);
            }
        }

        // Return cars that exceeded time limit
        foreach (GameObject car in carsToReturn)
        {
            if (car != null)
            {
                Debug.Log($"Auto-returning car {car.name} after {autoReturnTime} seconds");
            }
            ReturnCarToPool(car);
        }
    }

    public bool IsCarAvailable(GameObject prefab)
    {
        if (!carPools.ContainsKey(prefab))
            return false;

        return carPools[prefab].Count > 0;
    }

    public int GetAvailableCarCount(GameObject prefab)
    {
        if (!carPools.ContainsKey(prefab))
            return 0;

        return carPools[prefab].Count;
    }

    public void ReturnAllCarsToPool()
    {
        List<GameObject> allActiveCars = new List<GameObject>(activeCarTimers.Keys);

        foreach (GameObject car in allActiveCars)
        {
            ReturnCarToPool(car);
        }

        Debug.Log("All cars returned to pool");
    }
}