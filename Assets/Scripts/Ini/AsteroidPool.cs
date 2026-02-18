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

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Gets an asteroid from the pool for the given prefab type.
    /// Creates a new pool for this prefab if one doesn't exist yet.
    /// </summary>
    public Asteroid Get(Asteroid prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("AsteroidPool: Cannot get asteroid — prefab is null!");
            return null;
        }

        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new ObjectPool<Asteroid>(
                createFunc:      () => Instantiate(prefab),
                actionOnGet:     a  => a.gameObject.SetActive(true),
                actionOnRelease: a  => a.gameObject.SetActive(false),
                actionOnDestroy: a  => Destroy(a.gameObject),
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize:         maxSize
            );
        }

        return pools[prefab].Get();
    }

    /// <summary>
    /// Returns an asteroid to its pool. The asteroid must have prefabReference set.
    /// </summary>
    public void Release(Asteroid asteroid)
    {
        if (asteroid == null) return;

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
}
