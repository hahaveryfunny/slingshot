using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    private Queue<GameObject> pool = new Queue<GameObject>();
    private GameObject prefab;

    public void Initialize(GameObject prefab, int initialSize)
    {
        this.prefab = prefab;
        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public GameObject GetObject(Vector3 position, Quaternion rotation)
    {
        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, position, rotation);
        }
        return obj;
    }

    public void ReturnObject(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
