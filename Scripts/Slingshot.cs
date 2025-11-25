using System.Collections;
using TMPro;
using UnityEngine;

public class Slingshot : MonoBehaviour
{
    [Header("Slingshot")]
    [SerializeField] int segmentCount = 20;
    public Transform point1;
    public Transform point2;
    private LineRenderer lineRenderer;
    Vector3 midPoint;

    [Header("Projectile")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] float projectileForce = 100f;
    [SerializeField] ForceMode forceMode;
    [SerializeField] float fastForce = 5000;
    GameObject projectile;
    float projectileOffset;
    Rigidbody projectileRB;

    [Header("Input")]
    private Vector3 startPos;
    private Vector3 endPos;
    float verticalDelta;
    float horizontalDelta;
    private bool isSwiping;
    [SerializeField] float minimumDragDistance = 128;
    [SerializeField] float maxDragDistance = 900;
    float pullDistance = 0;


    [Header("SFX")]
    [Range(0, 1)]
    [SerializeField] float length;
    [SerializeField] AudioSource stretchSource;
    bool canPlaySFX;
    [SerializeField] float minSFXdistance;
    [SerializeField] AudioSource launchSFX;


    static public Slingshot instance;
    int score = 0;
    [SerializeField] TextMeshProUGUI scoreText;

    // void Awake()
    // {
    //     if (instance == null)
    //     {
    //         instance = this;
    //     }
    //     else
    //     {
    //         Destroy(gameObject);
    //     }
    // }

    public int GetScore()
    {
        return score;
    }

    public void ResetScore()
    {
        score = 0;
        scoreText.text = score.ToString();
    }

    public void IncreaseScore(int addition)
    {
        score += addition;
        scoreText.text = score.ToString();
        GameManager.instance.UpdateHighscore(score);

    }
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        ResetRubber();
        midPoint = (point1.position + point2.position) / 2f;
        projectileOffset = projectilePrefab.GetComponent<MeshRenderer>().bounds.size.z / 2;
        CreateProjectile();
        UpdateBallLocation(midPoint);

    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameObject quickShot = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            quickShot.GetComponent<Rigidbody>().AddForce(transform.forward * fastForce);
            Destroy(quickShot, 2f);
        }
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                InitializePull();
                startPos = touch.position;
                canPlaySFX = true;
            }
            if (touch.phase == TouchPhase.Moved && isSwiping)
            {
                endPos = touch.position;
                pullDistance = Vector2.Distance(endPos, startPos);
                if (pullDistance > minimumDragDistance)
                {
                    if (lineRenderer.positionCount != segmentCount)
                        lineRenderer.positionCount = segmentCount;
                    UpdatePullValues(touch);
                    if (!stretchSource.isPlaying && canPlaySFX && pullDistance > minimumDragDistance * 2)
                    {
                        stretchSource.time = stretchSource.clip.length * verticalDelta;
                        stretchSource.Play();
                        canPlaySFX = false;
                    }
                    UpdateRubberVisual();
                }
                else
                {
                    ResetRubber();
                }
            }
            if (touch.phase == TouchPhase.Ended)
            {
                endPos = touch.position;

                if (Vector2.Distance(endPos, startPos) > minimumDragDistance)
                {
                    LaunchBall();
                }
                ReleaseRubber();
                canPlaySFX = true;
                if (stretchSource.isPlaying) stretchSource.Stop();
            }
        }
        else if (isSwiping)
        {
            ReleaseRubber();
        }


        // Ensure new projectile is created if current one is gone
        if (projectile == null && !IsInvoking(nameof(CreateProjectile)))
        {
            Invoke(nameof(CreateProjectile), 0.5f);
        }
    }




    void FixedUpdate()
    {
        // If needed, you can do physics-related logic here.
        // Currently, AddForce is safely called in Update, just once per launch.
    }

    void InitializePull()
    {
        lineRenderer.positionCount = segmentCount;
        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            Vector3 basePos = Vector3.Lerp(point1.position, point2.position, t);
            lineRenderer.SetPosition(i, basePos);
        }
        isSwiping = true;
    }

    void UpdatePullValues(Touch touch)
    {
        verticalDelta = (endPos.y - startPos.y) * 0.01f;
        verticalDelta = Mathf.Clamp(verticalDelta, -maxDragDistance * 0.01f, maxDragDistance * 0.01f);
        horizontalDelta = (endPos.x - startPos.x) * 0.01f;
    }

    void UpdateRubberVisual()
    {
        Vector3 middleSegmentPos = Vector3.zero;
        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            Vector3 basePos = Vector3.Lerp(point1.position, point2.position, t);
            float segmentFactor = Mathf.Sin(t * Mathf.PI);

            Vector3 modifiedPos = basePos +
                (((Vector3.up * verticalDelta * 0) +
                (horizontalDelta * Vector3.right) +
                (-Vector3.back * verticalDelta)) * segmentFactor);

            lineRenderer.SetPosition(i, modifiedPos);

            if (i == (segmentCount - 1) / 2)
            {
                middleSegmentPos = modifiedPos;
            }
        }

        UpdateBallLocation(middleSegmentPos);
    }

    void UpdateBallLocation(Vector3 middleSegment)
    {
        if (projectile != null)
        {
            projectile.transform.position = middleSegment + Vector3.forward * projectileOffset;
        }
    }

    void ReleaseRubber()
    {
        isSwiping = false;
        ResetRubber();
    }

    void CreateProjectile()
    {
        if (ProjectilePool.Instance.activeProjectiles.Count >= ProjectilePool.Instance.maxPoolSize)
        {
            print("Can't create balls right now");
            return;
        }
        projectile = ProjectilePool.Instance.GetProjectile();
        projectile.transform.position = midPoint + Vector3.forward * projectileOffset;

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void LaunchBall()
    {
        if (projectile == null) return;


        projectileRB = projectile.GetComponent<Rigidbody>();
        projectileRB.useGravity = true;
        Vector3 launchDirection = new Vector3(-horizontalDelta, 0, -verticalDelta).normalized;
        projectileRB.AddForce(projectileForce * launchDirection * Mathf.Abs(verticalDelta), forceMode);
        launchSFX.PlayOneShot(launchSFX.clip);

        projectileRB.useGravity = true;
        //projectile.transform.SetParent(null);

        Projectile projComponent = projectile.GetComponent<Projectile>();
        if (projComponent != null)
        {
            projComponent.StartReturnTimer(projComponent.returnTime);
        }

        projectile = null;
        CreateProjectile(); // Immediate replacement
    }

    void ResetRubber()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, point1.position);
        lineRenderer.SetPosition(1, point2.position);
        if (projectile != null)
        {
            UpdateBallLocation(midPoint);
        }
    }
}
