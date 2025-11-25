using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraMover : MonoBehaviour
{
    public Transform gameCameraTarget; // The empty object at your gameplay camera’s position
    public float transitionTime = 2f;

    private bool isTransitioning = false;
    private float t = 0f;
    private Vector3 startPos;
    private Quaternion startRot;
    [SerializeField] Image cloud;
    [SerializeField] float xOffsetSpeed;
    [SerializeField] float yOffsetSpeed;
    [SerializeField] bool imageCloudON = false;





    void Update()
    {
        if (imageCloudON)
        {
            if (cloud.isActiveAndEnabled == false)
            {
                cloud.enabled = true;
            }
            float time = Time.time;

            float x = xOffsetSpeed + (Mathf.PerlinNoise(time * 0.2f, 0f) - 0.5f) * 0.2f;
            float y = yOffsetSpeed + (Mathf.PerlinNoise(0f, time * 0.2f) - 0.5f) * 0.2f;

            cloud.material.mainTextureOffset += new Vector2(x, y) * Time.deltaTime * 0.005f;
        }
        else
        {
            if (cloud.isActiveAndEnabled == true)
            {
                cloud.enabled = false;
            }
        }

        if (isTransitioning)
        {
            t += Time.deltaTime / transitionTime;
            transform.position = Vector3.Lerp(startPos, gameCameraTarget.position, t);
            transform.rotation = Quaternion.Slerp(startRot, gameCameraTarget.rotation, t);

            if (t >= 1f)
                isTransitioning = false; // finished
        }
    }

    public void StartTransition()
    {
        startPos = transform.position;
        startRot = transform.rotation;
        t = 0f;
        isTransitioning = true;
    }
}
