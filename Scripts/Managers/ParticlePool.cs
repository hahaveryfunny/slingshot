using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class ParticlePool : MonoBehaviour
{
    public static ParticlePool Instance;

    [SerializeField] private Transform particleParent;
    [SerializeField] private ParticleSystem poofPrefab;
    [SerializeField] private int poolSize = 10;

    private Queue<ParticleSystem> pool = new Queue<ParticleSystem>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }


        for (int i = 0; i < poolSize; i++)
        {
            ParticleSystem ps = Instantiate(poofPrefab, transform);
            ps.transform.SetParent(particleParent);
            ps.gameObject.SetActive(false);
            pool.Enqueue(ps);
        }
    }

    public ParticleSystem GetPoofEffect(Vector3 position, Quaternion rotation)
    {
        ParticleSystem ps = pool.Count > 0 ? pool.Dequeue() : Instantiate(poofPrefab);
        ps.transform.position = position;
        ps.transform.rotation = rotation;
        ps.gameObject.SetActive(true);
        ps.Play();
        StartCoroutine(ReturnAfter(ps, ps.main.duration + ps.main.startLifetime.constantMax));
        return ps;
    }

    private IEnumerator ReturnAfter(ParticleSystem ps, float delay)
    {
        yield return new WaitForSeconds(delay);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.gameObject.SetActive(false);
        pool.Enqueue(ps);
    }
}
