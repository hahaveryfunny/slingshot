using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    [SerializeField] Vector3 Pos1;
    [SerializeField] Vector3 Pos2;

    float yPos;
    bool touchable = true;

    void Start()
    {
        yPos = transform.position.y;
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.K))
        {
            MoveBall();
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer == 6)
        {
            MoveBall();
        }
    }

    void MoveBall()
    {
        touchable = false;
        transform.position = new Vector3(Random.Range(Pos1.x, Pos2.x), yPos, Random.Range(Pos1.z, Pos2.z));
        touchable = true;
    }
}
