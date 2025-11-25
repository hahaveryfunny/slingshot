using UnityEngine;
using System.Collections.Generic;

public class MapSelectionController : MonoBehaviour
{
    [Header("Map Setup")]
    [SerializeField] private List<GameObject> mapPrefabs;
    [SerializeField] private float gapBetweenMaps = 5f;

    [Header("Snapping")]
    [SerializeField] private float snapDistance = 2f;
    [SerializeField] private float snapSpeed = 10f;

    [Header("Drag Settings")]
    [SerializeField] private float dragSensitivity = 0.01f;
    [SerializeField] private Camera mainCamera;

    private List<GameObject> spawnedMaps = new List<GameObject>();
    private Vector3 dragStartPos;
    private Vector3 containerStartPos;
    private bool isDragging = false;
    private bool isSnapping = false;
    private int currentMapIndex = 0;
    private Vector3 targetSnapPosition;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        SpawnMaps();
    }

    void SpawnMaps()
    {
        for (int i = 0; i < mapPrefabs.Count; i++)
        {
            Vector3 spawnPos = transform.position + Vector3.right * (i * gapBetweenMaps);
            GameObject map = Instantiate(mapPrefabs[i], spawnPos, Quaternion.identity, transform);
            spawnedMaps.Add(map);
        }

        // Center the first map
        currentMapIndex = 0;
        SnapToMap(currentMapIndex, true);
    }

    void Update()
    {
        HandleInput();

        if (isSnapping)
        {
            SnapToPosition();
        }
    }

    void HandleInput()
    {
        // Start drag
        if (Input.GetMouseButtonDown(0) && !isSnapping)
        {
            isDragging = true;
            isSnapping = false;
            dragStartPos = GetMouseWorldPosition();
            containerStartPos = transform.position;
        }

        // During drag
        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 currentMousePos = GetMouseWorldPosition();
            Vector3 dragDelta = currentMousePos - dragStartPos;
            transform.position = containerStartPos + new Vector3(dragDelta.x * dragSensitivity, 0, 0);
        }

        // End drag
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            FindAndSnapToClosestMap();
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCamera.transform.position.z);
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    void FindAndSnapToClosestMap()
    {
        float closestDistance = float.MaxValue;
        int closestIndex = 0;
        Vector3 centerPoint = mainCamera.transform.position;

        for (int i = 0; i < spawnedMaps.Count; i++)
        {
            float distance = Mathf.Abs(spawnedMaps[i].transform.position.x - centerPoint.x);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        // Check if within snap distance
        if (closestDistance <= snapDistance)
        {
            SnapToMap(closestIndex);
        }
        else
        {
            // Snap to closest regardless
            SnapToMap(closestIndex);
        }
    }

    void SnapToMap(int index, bool instant = false)
    {
        currentMapIndex = Mathf.Clamp(index, 0, spawnedMaps.Count - 1);

        Vector3 mapWorldPos = spawnedMaps[currentMapIndex].transform.position;
        Vector3 centerPoint = mainCamera.transform.position;

        float offsetX = centerPoint.x - mapWorldPos.x;
        targetSnapPosition = transform.position + new Vector3(offsetX, 0, 0);

        if (instant)
        {
            transform.position = targetSnapPosition;
            isSnapping = false;
        }
        else
        {
            isSnapping = true;
        }
    }

    void SnapToPosition()
    {
        transform.position = Vector3.Lerp(transform.position, targetSnapPosition, Time.deltaTime * snapSpeed);

        if (Vector3.Distance(transform.position, targetSnapPosition) < 0.01f)
        {
            transform.position = targetSnapPosition;
            isSnapping = false;
        }
    }

    // Public method to get the currently selected map
    public GameObject GetCurrentMap()
    {
        return spawnedMaps[currentMapIndex];
    }

    public int GetCurrentMapIndex()
    {
        return currentMapIndex;
    }
}