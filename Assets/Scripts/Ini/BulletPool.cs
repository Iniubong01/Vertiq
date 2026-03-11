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

    // PERFORMANCE: Tracks all currently checked-out bullets (same pattern as AsteroidPool).
    // Allows null-safe iteration and future ReleaseAll() without FindObjectsOfType.
    private readonly HashSet<Bullet> _activeBullets = new HashSet<Bullet>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void EnsurePool(Bullet prefab)
    {
        if (pools.ContainsKey(prefab)) return;
        pools[prefab] = new ObjectPool<Bullet>(
            createFunc:      () => Instantiate(prefab),
            // NULL-GUARD: pool may hold stale (externally destroyed) slots — skip them safely.
            actionOnGet:     b => { if (b != null) b.gameObject.SetActive(true); },
            actionOnRelease: b => { if (b != null) b.gameObject.SetActive(false); },
            actionOnDestroy: b => { if (b != null) Destroy(b.gameObject); },
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize:         maxSize
        );
    }

    /// <summary>
    /// Gets a bullet from the pool for the specified prefab type.
    /// Skips stale (externally destroyed) slots rather than crashing.
    /// </summary>
    public Bullet Get(Bullet prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("BulletPool: Cannot get bullet from pool - prefab is null!");
            return null;
        }

        EnsurePool(prefab);

        // SAFETY CAP: loop past destroyed slots without hanging the main thread.
        // Same pattern as AsteroidPool.Get() — prevents MissingReferenceException crash.
        Bullet bullet;
        int attempts = 0;
        const int maxAttempts = 100;
        do
        {
            bullet = pools[prefab].Get();
            attempts++;
            if (attempts >= maxAttempts)
            {
                Debug.LogError($"[BulletPool] Could not get a valid bullet after {maxAttempts} attempts for prefab '{prefab.name}'.");
                return null;
            }
        }
        while (bullet == null); // Unity's == null returns true for destroyed objects

        _activeBullets.Add(bullet);
        return bullet;
    }

    /// <summary>
    /// Returns a bullet to its appropriate pool.
    /// </summary>
    public void Release(Bullet bullet)
    {
        if (bullet == null) return;

        _activeBullets.Remove(bullet);

        Bullet prefabType = bullet.prefabReference;

        if (prefabType != null && pools.ContainsKey(prefabType))
        {
            pools[prefabType].Release(bullet);
        }
        else
        {
            Debug.LogWarning($"BulletPool: Could not find pool for bullet '{bullet.name}', destroying instead.");
            Destroy(bullet.gameObject);
        }
    }

    /// <summary>
    /// Returns ALL active bullets to their pools — call from GameManager.NewGame()
    /// to avoid bullets carrying over between games.
    /// </summary>
    public void ReleaseAll()
    {
        var snapshot = new List<Bullet>(_activeBullets);
        foreach (Bullet b in snapshot)
        {
            if (b != null) Release(b);
        }
        _activeBullets.Clear();
    }
}