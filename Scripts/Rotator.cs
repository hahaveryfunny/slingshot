using UnityEngine;

public class MapRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 30, 0);

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}