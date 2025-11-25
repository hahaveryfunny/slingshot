using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallScript : MonoBehaviour
{

    [SerializeField] Material readyMat;
    [SerializeField] Material doneMat;
    MeshRenderer targetMesh;

    int hitCount = 0;

    void Start()
    {
        targetMesh = GameObject.FindGameObjectWithTag("Target").GetComponent<MeshRenderer>();

    }
    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Ground" || other.gameObject.tag == "Target")
        {
            hitCount++;
        }
        if (hitCount == 1 && other.gameObject.tag == "Ground")
        {
            targetMesh.material = readyMat;
        }
        if (hitCount == 2 && other.gameObject.tag == "Target")
        {
            targetMesh.material = doneMat;
            StartCoroutine(ProjectileThrow.Instance.ShowShooter(ProjectileThrow.Instance.spawnTime, gameObject));
        }
        else if (hitCount >= 2) // If the object has been hit more than twice
        {
            StartCoroutine(ProjectileThrow.Instance.ShowShooter(0f, gameObject)); // Destroy the ball
        }
    }
}
