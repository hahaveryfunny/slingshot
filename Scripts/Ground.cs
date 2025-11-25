using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
{
    [SerializeField] GameObject hitMark;
    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer == 6)
        {
            Instantiate(hitMark, other.contacts[0].point + Vector3.up * .5f, Quaternion.identity);
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
