using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeRotation : MonoBehaviour
{
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 10)
        {
            other.gameObject.transform.Rotate(Vector3.right * 15);
        }
    }
}
