using System.Collections.Generic;
using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    [SerializeField] private List<GameObject> brokenPrefabs;
    [SerializeField] private float collisionMultiplier = 5f;
    private Dictionary<GameObject, ObjectPool> pools = new Dictionary<GameObject, ObjectPool>();
    [SerializeField] int poolSize = 1;

    private void Start()
    {
        // Create object pools for each broken prefab
        foreach (var prefab in brokenPrefabs)
        {
            GameObject poolObj = new GameObject(prefab.name + "_Pool");
            poolObj.transform.parent = transform;
            ObjectPool pool = poolObj.AddComponent<ObjectPool>();
            pool.Initialize(prefab, poolSize); // Preload 5 objects
            pools[prefab] = pool;
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer == 6)
        {
            Vector3 worldPos = transform.position;
            Quaternion worldRot = transform.rotation;

            GameObject selectedPrefab = brokenPrefabs[Random.Range(0, brokenPrefabs.Count)];
            GameObject brokenPiece = pools[selectedPrefab].GetObject(worldPos, worldRot);

            var rbs = brokenPiece.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rbs)
            {
                rb.AddExplosionForce(other.relativeVelocity.magnitude * collisionMultiplier, other.contacts[0].point, 10);
            }

            gameObject.SetActive(false); // Disable instead of destroying
        }
    }

    public void ReturnBrokenPiece(GameObject brokenPiece, GameObject prefab)
    {
        pools[prefab].ReturnObject(brokenPiece);
    }
}
