using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    Rigidbody rb;
    [SerializeField] float steerForce = 10f;
    [SerializeField] float brakeForce = 10f;
    [SerializeField] float gasForce = 15f;  // Increased to help overcome gravity on uphill sections
    [SerializeField] float downForce = 10f; // Reduced downforce to prevent excessive slowdown

    Vector3 direction;

    bool jumped = false;
    Vector3 initialPos;

    [SerializeField] ForceMode forcemode = ForceMode.Force;

    private bool isGrounded = false;
    [SerializeField] float groundCheckDistance = 0.3f;

    void Start()
    {
        initialPos = transform.position;
        rb = GetComponent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 7)
        {
            jumped = true;
            print("jumped");
        }
        if (other.gameObject.layer == 8)
        {
            // UpdateGravity();
        }
    }

    void FixedUpdate()
    {

        // Apply moderate downforce to keep on ground but not slow too much on uphill
        if (jumped != false)
        {
            rb.AddForce(Vector3.down * downForce, ForceMode.Force);
        }


        direction = GetRampDirection();

        if (Input.GetKey(KeyCode.W))
        {
            direction = jumped ? Vector3.forward : direction;
            Forward(direction);
        }
        if (Input.GetKey(KeyCode.S))
        {
            direction = jumped ? -Vector3.forward : direction;
            Brake(direction);
        }
        if (Input.GetKey(KeyCode.A))
        {
            Left();
        }
        if (Input.GetKey(KeyCode.D))
        {
            Right();
        }
        if (Input.GetKey(KeyCode.Space))
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = initialPos;
            jumped = false;
        }
    }


    Vector3 GetRampDirection()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 20f))
        {
            // Improved ramp direction calculation for better uphill movement
            Vector3 rampDirection = Vector3.ProjectOnPlane(Vector3.forward, hit.normal).normalized;
            var slope = Quaternion.FromToRotation(Vector3.up, hit.normal);

            print("ramp direction = " + rampDirection);
            print("slope = " + slope.eulerAngles);

            // Calculate the steepness factor (dot product between ramp direction and forward)
            float steepnessFactor = Vector3.Dot(rampDirection, Vector3.forward);
            //("steepness Factor " + steepnessFactor);
            // Display info for debugging
            Debug.DrawRay(transform.position, rampDirection * 2f, Color.blue);

            // print("rampDirection.y" + rampDirection.y);
            // Bias the direction slightly downward on steep uphill to help acceleration
            // if (steepnessFactor < 0.5f && rampDirection.y > 0)
            // {
            //     print("uphill");
            //     // Blend with a slightly more horizontal direction on steep uphills
            //     rampDirection = Vector3.Lerp(rampDirection, new Vector3(rampDirection.x, 0, rampDirection.z).normalized, 0.3f);
            //     rampDirection.Normalize();
            // }

            return rampDirection;
        }
        return Vector3.forward;
    }

    Vector3 GetRampRightDirection()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 20f))
        {
            Vector3 rampDirection = GetRampDirection();
            Vector3 rampRightDirection = Vector3.Cross(hit.normal, rampDirection).normalized;

            Debug.DrawRay(transform.position, rampRightDirection * 2f, Color.red);

            return rampRightDirection;
        }
        return Vector3.right;
    }

    void Right()
    {
        // if (isGrounded || jumped)
        {
            Vector3 rightDir = jumped ? Vector3.right : GetRampRightDirection();

            // Adjust steering force to work better on ramps
            float adjustedForce = steerForce;
            rb.AddForce(rightDir * adjustedForce, forcemode);
        }
    }

    void Left()
    {
        //if (isGrounded || jumped)
        {
            Vector3 rightDir = jumped ? Vector3.right : GetRampRightDirection();

            // Adjust steering force to work better on ramps
            float adjustedForce = steerForce;
            rb.AddForce(-rightDir * adjustedForce, forcemode);
        }
    }

    void Forward(Vector3 moveDirection)
    {
        // Add extra force for uphill movement
        float extraForce = 0;
        if (!jumped && moveDirection.y > 0)
        {
            // Apply more force when going uphill (based on steepness)
            extraForce = moveDirection.y * 10f;
        }
        rb.AddForce(moveDirection * (gasForce + extraForce), forcemode);

    }

    void Brake(Vector3 moveDirection)
    {
        if (isGrounded || jumped)
        {
            rb.AddForce(moveDirection * brakeForce, forcemode);
        }
    }

    // Vector3 AdjustVelocityToSlope()
    // {
    //     var ray = new Ray(transform.position, Vector3.down);

    //     if (Physics.Raycast(ray, out RaycastHit hitInfo, .2f))
    //     {
    //         var slopeRotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
    //     }
    // }


    void UpdateGravity()
    {
        Physics.gravity = new Vector3(0, -9.81f, 0);
    }
}