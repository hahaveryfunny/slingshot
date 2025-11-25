// Updated ProjectilePool.cs
using UnityEngine;
using System.Collections.Generic;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;

    [SerializeField] Transform projectileParent;
    [SerializeField] GameObject projectilePrefab;
    public int maxPoolSize = 20;

    private Queue<GameObject> projectilePool = new Queue<GameObject>();
    public List<GameObject> activeProjectiles = new List<GameObject>(); // Track active projectiles

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializePool();
    }

    void InitializePool()
    {
        for (int i = 0; i < maxPoolSize; i++)
        {
            CreateNewProjectile();
        }
    }

    void CreateNewProjectile()
    {
        GameObject projectile = Instantiate(projectilePrefab, transform);
        projectile.transform.SetParent(projectileParent);
        projectile.SetActive(false);
        projectilePool.Enqueue(projectile);
    }

    public GameObject GetProjectile()
    {
        // Get from pool if available
        if (projectilePool.Count > 0)
        {
            GameObject projectile = projectilePool.Dequeue();
            projectile.SetActive(true);
            activeProjectiles.Add(projectile); // Track as active
            return projectile;
        }

        // Create new if under limit
        if (activeProjectiles.Count + projectilePool.Count < maxPoolSize)
        {
            GameObject projectile = Instantiate(projectilePrefab, transform);
            projectile.SetActive(true);
            activeProjectiles.Add(projectile);
            return projectile;
        }

        Debug.LogWarning("Projectile pool exhausted!");
        return null;
    }

    public void ReturnProjectile(GameObject projectile)
    {
        // Early exit if already returning
        if (!activeProjectiles.Contains(projectile)) return;

        // Remove from active list first
        activeProjectiles.Remove(projectile);

        // Reset projectile state
        projectile.SetActive(false);

        // Only set parent if not already set
        if (projectile.transform.parent != projectileParent)
        {
            projectile.transform.SetParent(projectileParent);
        }

        // Add back to pool
        projectilePool.Enqueue(projectile);
    }
    public void ReturnAllProjectiles()
    {
        // Create copy to avoid modification during iteration
        List<GameObject> toReturn = new List<GameObject>(activeProjectiles);

        foreach (GameObject projectile in toReturn)
        {
            ReturnProjectile(projectile);
        }
    }
}