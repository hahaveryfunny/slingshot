using UnityEngine;

public class CarInfo : MonoBehaviour
{
    [Header("Car Data")]
    public int laneIndex;
    public GameObject sourcePrefab; // This will be set to 'this.gameObject' for prefabs

    [Header("UI Display")]
    public Sprite bannedCarSprite; // Assign this in the prefab inspector

    void Awake()
    {
        // For prefabs, set sourcePrefab to themselves
        if (sourcePrefab == null)
        {
            sourcePrefab = this.gameObject;
        }
    }
}