using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarObject : MonoBehaviour
{
    [SerializeField] Mesh newMesh;
    MeshFilter meshFilter;

    void Start()
    {
        meshFilter = transform.GetComponent<MeshFilter>();
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer == 6)
        {
            meshFilter.mesh = newMesh;
        }
    }

}
