using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Projectile : MonoBehaviour
{

    int hitCount = 0;
    SingleBanManager banManager;
    public float returnTime = 4f;
    private Coroutine returnRoutine;

    void Start()
    {
        banManager = SingleBanManager.instance;
    }
    void OnEnable()
    {
        hitCount = 0;
        //meshRenderer.enabled = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
    }


    public void StartReturnTimer(float delay)
    {
        if (returnRoutine != null) StopCoroutine(returnRoutine);
        returnRoutine = StartCoroutine(ReturnAfterDelay(delay));
    }

    public IEnumerator ReturnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Check if object still exists
        if (this != null && gameObject != null)
        {
            ReturnToPool();
        }
    }

    public void ReturnToPool()
    {
        // This is the only place where we return to pool
        if (ProjectilePool.Instance != null)
        {
            ProjectilePool.Instance.ReturnProjectile(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    void OnCollisionEnter(Collision other)
    {
        hitCount++;
        if (hitCount > 3)
        {
            ReturnToPool();
            return; // Exit early after returning to pool
        }
        //AMBULANCE
        if (other.gameObject.layer == 11 && hitCount < 2)
        {
            ReturnToPool();
            //meshRenderer.enabled = false;
            banManager.TakeDamage(1);
            int index = Random.Range(0, AudioManager.instance.wrongHitSFX.Count());
            AudioManager.instance.PlaySFX(AudioManager.instance.wrongHitSFX[index]);
            GameManager.instance.GameOver();

        }
        else if (other.gameObject.layer == 10 && hitCount < 2)
        {
            // meshRenderer.enabled = false;
            CarInfo carInfo = other.gameObject.GetComponent<CarInfo>();
            if (carInfo != null)
            {
                int laneIndex = carInfo.laneIndex;
                if (banManager.IsThisCarBanned(laneIndex, carInfo))
                {
                    Destroy(other.gameObject);
                    ParticlePool.Instance.GetPoofEffect(other.transform.position, Quaternion.Euler(-90, 0, 0));
                    int index = Random.Range(0, AudioManager.instance.poofSFX.Count());
                    Slingshot.instance.IncreaseScore(1);
                    AudioManager.instance.PlaySFX(AudioManager.instance.poofSFX[index]);
                    ReturnToPool();
                }
                else
                {
                    ReturnToPool();
                    int index = Random.Range(0, AudioManager.instance.wrongHitSFX.Count());
                    AudioManager.instance.PlaySFX(AudioManager.instance.wrongHitSFX[index]);
                    banManager.TakeDamage(1);
                }
            }
            else
            {
                print("car info not attached to game obj");
            }
        }

        else
        {
            int index = Random.Range(0, AudioManager.instance.bounceSFX.Count());
            AudioManager.instance.PlaySFX(AudioManager.instance.bounceSFX[index]);
        }
    }


}
