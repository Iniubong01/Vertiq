using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class AsteroidPool : MonoBehaviour
{
    private static AsteroidPool _instance;
    public static AsteroidPool Instance
    {
        get
        {
            if (_instance == null)
            {
                // Auto-create if not in the scene — no manual setup required
                GameObject go = new GameObject("AsteroidPool (Auto-Created)");
                _instance = go.AddComponent<AsteroidPool>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [SerializeField] private int defaultCapacity = 30;
    [SerializeField] private int maxSize = 200;

    // Separate pool per prefab variant (small, medium, large, etc.)
    private Dictionary<Asteroid, ObjectPool<Asteroid>> pools = new Dictionary<Asteroid, ObjectPool<Asteroid>>();

    // PERFORMANCE: Tracks all currently checked-out asteroids.
    // Allows ReleaseAll() to return them to the pool cleanly without
    // using the slow FindObjectsOfType<Asteroid>() + Destroy() pattern.
    private readonly HashSet<Asteroid> _activeAsteroids = new HashSet<Asteroid>();

    // PERFORMANCE: Pre-allocated scratch buffer for ReleaseAll() snapshots.
    // Reused each call — eliminates the "new Asteroid[n]" allocation on every death / new game.
    private readonly List<Asteroid> _snapshotBuffer = new List<Asteroid>();

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Pre-instantiates 'count' asteroids for the given prefab and immediately
    /// returns them to the pool. Spread across frames to avoid a startup hitch.
    /// Call this from AsteroidSpawner.Start() for each prefab variant.
    /// </summary>
    public void Prewarm(Asteroid prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        StartCoroutine(PrewarmRoutine(prefab, count));
    }

    private IEnumerator PrewarmRoutine(Asteroid prefab, int count)
    {
        // Ensure the pool entry exists
        EnsurePool(prefab);

        for (int i = 0; i < count; i++)
        {
            // Instantiate directly — never call Get() here, which would SetActive(true)
            // and make the asteroid visible for a frame before Release() hides it.
            Asteroid a = Instantiate(prefab);
            a.prefabReference = prefab;
            a.gameObject.SetActive(false); // immediately invisible
            pools[prefab].Release(a);      // hand it to the pool as a ready object
            yield return null;             // one per frame — no startup spike
        }
    }

    private void EnsurePool(Asteroid prefab)
    {
        if (pools.ContainsKey(prefab)) return;
        pools[prefab] = new ObjectPool<Asteroid>(
            createFunc:      () => Instantiate(prefab),
            actionOnGet:     a  => { if (a != null) a.gameObject.SetActive(true); },
            actionOnRelease: a  => { if (a != null) a.gameObject.SetActive(false); },
            actionOnDestroy: a  => { if (a != null) Destroy(a.gameObject); },
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize:         maxSize
        );
    }

    /// <summary>
    /// Gets a live asteroid from the pool for the given prefab type.
    /// If the pool returns a stale (externally-destroyed) slot, it discards it
    /// and fetches again until a valid instance is obtained.
    /// </summary>
    public Asteroid Get(Asteroid prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("AsteroidPool: Cannot get asteroid — prefab is null!");
            return null;
        }

        EnsurePool(prefab);

        // Loop until we get a live object. Stale slots (externally Destroy()ed)
        // are discarded; the next Get() call will trigger createFunc for a fresh one.
        // SAFETY CAP: never spin more than 100 times — a destroyed-but-not-GC'd object
        // can keep returning Unity-null indefinitely and freeze the main thread.
        Asteroid asteroid;
        int attempts = 0;
        const int maxAttempts = 100;
        do
        {
            asteroid = pools[prefab].Get();
            attempts++;
            if (attempts >= maxAttempts)
            {
                Debug.LogError($"[AsteroidPool] Could not get a valid asteroid after {maxAttempts} attempts for prefab '{prefab.name}'. Pool may be exhausted or corrupted.");
                return null;
            }
        }
        while (asteroid == null); // Unity's == null returns true for destroyed objects

        _activeAsteroids.Add(asteroid);
        return asteroid;
    }

    /// <summary>
    /// Returns an asteroid to its pool. The asteroid must have prefabReference set.
    /// </summary>
    public void Release(Asteroid asteroid)
    {
        if (asteroid == null) return;

        _activeAsteroids.Remove(asteroid);

        Asteroid prefabType = asteroid.prefabReference;

        if (prefabType != null && pools.ContainsKey(prefabType))
        {
            pools[prefabType].Release(asteroid);
        }
        else
        {
            Debug.LogWarning($"AsteroidPool: Could not find pool for {asteroid.name}, destroying instead.");
            Destroy(asteroid.gameObject);
        }
    }

    /// <summary>
    /// Returns ALL currently active asteroids to their pools.
    /// Call this from GameManager.NewGame() instead of FindObjectsOfType + Destroy.
    /// Maintains full pool integrity with zero GC allocations.
    /// </summary>
    public void ReleaseAll()
    {
        // PERFORMANCE: Copy active set into a reusable List (no new array each call).
        // We can't modify _activeAsteroids while iterating it, so we snapshot first.
        _snapshotBuffer.Clear();
        _snapshotBuffer.AddRange(_activeAsteroids);

        foreach (Asteroid asteroid in _snapshotBuffer)
        {
            if (asteroid == null) continue;
            Release(asteroid);
        }

        _activeAsteroids.Clear();
    }
}
