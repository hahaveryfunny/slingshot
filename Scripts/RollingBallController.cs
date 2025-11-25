using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RollingBallController : MonoBehaviour
{
    public float torqueForce = 10f; // Strength of rolling torque
    public float lateralTorqueForce = 5f; // Left/right rotation torque
    public float maxSpeed = 50f; // Max angular velocity (prevents excessive rolling speed)
    Vector3 initialPos;
    private Rigidbody rb;


    [SerializeField] TextMeshProUGUI speedText;

    float timer;
    bool shouldPrint = true;
    float speed;

    float horizontalInput;
    bool spin = false;


    bool shouldLaunch = false;

    bool shoot = false;
    [SerializeField] float shootForce = 50f;
    [SerializeField] ForceMode forcemode;
    void Start()
    {
        initialPos = transform.position;
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = maxSpeed; // Ensure torque can take effect properly
        rb.useGravity = false;
    }

    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");

        if (Input.GetKey(KeyCode.W))
        {
            spin = true;
        }
        if (Input.GetKeyUp(KeyCode.W))
        {
            shouldLaunch = true;
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            shoot = true;
        }
    }

    void FixedUpdate()
    {
        if (shoot)
        {
            rb.AddForce(transform.forward * shootForce, forcemode);
            rb.useGravity = true;
            shoot = false;

        }

        if (spin)
        {
            rb.AddTorque(Vector3.right * torqueForce, ForceMode.Acceleration);
            speed = torqueForce;
            timer += Time.fixedDeltaTime;

            if (rb.angularVelocity.magnitude >= maxSpeed)
            {

                LaunchBall();
                if (shouldPrint)
                {
                    shouldPrint = false;
                    print(timer + " seconds");
                }
            }
        }
        if (shouldLaunch)
        {
            LaunchBall();
        }

        // rb.AddTorque(speed * Vector3.right, ForceMode.Acceleration);

        // rb.AddTorque(-Vector3.forward * horizontalInput * lateralTorque, ForceMode.Acceleration);


        rb.AddForce(Vector3.right * horizontalInput * lateralTorqueForce, ForceMode.Acceleration);
        speedText.text = ((int)rb.velocity.magnitude).ToString() + "km/h";


        // // Adjust speed multiplier
        // speedMultiplier += torqueForce * Time.deltaTime;
        // speedMultiplier = Mathf.Clamp(speedMultiplier, 0.5f, 5f); // Prevents excessive speed

        // // Clamp max angular velocity to keep physics stable
        // rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, maxSpeed);
        if (Input.GetKey(KeyCode.R))
        {
            ResetBall();
        }
    }
    void ResetBall()
    {
        timer = 0;
        spin = false;
        speed = 0;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = initialPos;
        shouldPrint = true;
        shouldLaunch = false;
        transform.rotation = Quaternion.Euler(-45, 0, 0);
    }

    void LaunchBall()
    {
        rb.useGravity = true;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 8)
        {
            speed = 0;
        }
    }
}
