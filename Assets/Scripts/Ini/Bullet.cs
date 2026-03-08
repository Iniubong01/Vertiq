using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    private Rigidbody2D rb;

    public float speed = 500f;
    public float maxLifetime = 10f;

    // Reference to the prefab this bullet was created from (for pool tracking)
    [HideInInspector] public Bullet prefabReference;

    // PERFORMANCE: Cached WaitForSeconds per duration value — shared across all bullets
    // that have the same maxLifetime. Eliminates a heap allocation on every bullet fire.
    // Dictionary allows multiple bullet prefabs with different lifetimes to each cache theirs.
    private static readonly Dictionary<float, WaitForSeconds> _wfsCache =
        new Dictionary<float, WaitForSeconds>();
    private WaitForSeconds _lifetimeWFS;

    // PERFORMANCE: Coroutine-based lifetime — eliminates per-bullet Update() calls.
    // With spread fire there can be 10–20+ bullets alive at once, each running Update().
    // The coroutine just sleeps until the timer fires — zero per-frame overhead.
    private Coroutine lifetimeCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Build (or retrieve) the cached WaitForSeconds for this bullet's lifetime.
        // Done in Awake so it's ready before the first OnEnable.
        if (!_wfsCache.TryGetValue(maxLifetime, out _lifetimeWFS))
        {
            _lifetimeWFS = new WaitForSeconds(maxLifetime);
            _wfsCache[maxLifetime] = _lifetimeWFS;
        }
    }

    // Called by the pool when this bullet is retrieved
    private void OnEnable()
    {
        rb.linearVelocity = Vector2.zero;

        // Start lifetime countdown coroutine
        if (lifetimeCoroutine != null) StopCoroutine(lifetimeCoroutine);
        lifetimeCoroutine = StartCoroutine(LifetimeRoutine());
    }

    private void OnDisable()
    {
        // Stop coroutine so it doesn't fire on a pooled (inactive) object
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return _lifetimeWFS; // reuse cached object — no heap allocation
        ReturnToPool();
    }

    public void Shoot(Vector2 direction, Bullet prefab)
    {
        prefabReference = prefab;
        rb.AddForce(direction * speed);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        BulletPool.Instance.Release(this);
    }
}