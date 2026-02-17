using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxSize = 100;

    // Dictionary to hold a separate pool for each bullet prefab type
    private Dictionary<Bullet, ObjectPool<Bullet>> pools = new Dictionary<Bullet, ObjectPool<Bullet>>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Gets a bullet from the pool for the specified prefab type.
    /// If no pool exists for this prefab, creates one.
    /// </summary>
    public Bullet Get(Bullet prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("BulletPool: Cannot get bullet from pool - prefab is null!");
            return null;
        }

        // If we don't have a pool for this prefab yet, create one
        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new ObjectPool<Bullet>(
                createFunc:      () => Instantiate(prefab),
                actionOnGet:     b => b.gameObject.SetActive(true),
                actionOnRelease: b => b.gameObject.SetActive(false),
                actionOnDestroy: b => Destroy(b.gameObject),
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        return pools[prefab].Get();
    }

    /// <summary>
    /// Returns a bullet to its appropriate pool.
    /// The pool is determined by the bullet's prefab reference stored on the bullet itself.
    /// </summary>
    public void Release(Bullet bullet)
    {
        if (bullet == null) return;

        // Find which pool this bullet belongs to
        Bullet prefabType = bullet.prefabReference;
        
        if (prefabType != null && pools.ContainsKey(prefabType))
        {
            pools[prefabType].Release(bullet);
        }
        else
        {
            // Fallback: if we can't find the pool, just destroy it
            Debug.LogWarning($"BulletPool: Could not find pool for bullet {bullet.name}, destroying instead.");
            Destroy(bullet.gameObject);
        }
    }
}