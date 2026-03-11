using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Asteroid : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    [Header("Setup")]
    [SerializeField] private Asteroid asteroidPrefab;

    [SerializeField]
    private Sprite[] sprites;

    [Header("Settings")]
    public float size = 1f;
    public float minSize = 0.35f;
    public float maxSize = 1.65f;
    public float movementSpeed = 50f;
    public float maxLifetime = 30f;

    // Pool tracking — set by AsteroidPool or CreateSplit before activation
    [HideInInspector] public Asteroid prefabReference;

    // Cached reference — avoids per-frame singleton lookup in Update
    private PowerUpManager powerUpManager;

    // Global count of currently active asteroids — used by AsteroidSpawner to cap spawns
    public static int ActiveCount { get; private set; }

    private Coroutine lifetimeCoroutine;
    private bool isDying = false;

    // PERFORMANCE: Cached WaitForSeconds per maxLifetime value — shared across all
    // asteroids with the same lifetime. Avoids a heap allocation on every pool activation.
    private static readonly Dictionary<float, WaitForSeconds> _wfsCache =
        new Dictionary<float, WaitForSeconds>();
    private WaitForSeconds _lifetimeWFS;

    // PERFORMANCE: Cached fade duration yield — reused across all ReturnToPool() calls
    // so no lambda closure or WaitForSeconds object is allocated per asteroid death.
    private static WaitForSeconds _fadeWFS;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // -------------------------------------------------------
    // POOL LIFECYCLE
    // -------------------------------------------------------

    private void OnEnable()
    {
        ActiveCount++;

        // Reset visual & physics state so pooled asteroids are clean
        isDying = false;
        col.enabled = true;

        // Kill any leftover tweens from a previous use (safety net for any other DOTween users)
        spriteRenderer.DOKill();

        // Ensure full opacity — coroutine-based fade (FadeAndRelease) may have left alpha < 1
        // if this asteroid was re-pooled mid-fade by ReleaseAll().
        Color c = spriteRenderer.color;
        c.a = 1f;
        spriteRenderer.color = c;

        // Cache PowerUpManager once per activation (avoids per-frame lookup)
        powerUpManager = PowerUpManager.Instance;

        // Subscribe to the freeze event — zero per-frame cost
        PowerUpManager.OnFreezeChanged += OnFreezeChanged;

        // If freeze is already active when we spawn, apply it immediately
        if (powerUpManager != null && powerUpManager.IsFreezeTimeActive)
            ApplyFreeze(true);
    }

    private void OnDisable()
    {
        ActiveCount--;
        if (ActiveCount < 0) ActiveCount = 0; // safety clamp

        // Kill tweens so they don't run on a pooled (inactive) object
        spriteRenderer.DOKill();

        // Stop lifetime coroutine
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }

        // Unsubscribe — no dangling references
        PowerUpManager.OnFreezeChanged -= OnFreezeChanged;

        // Restore physics constraints so the pooled object is clean for next use
        if (rb != null) rb.constraints = RigidbodyConstraints2D.None;
    }

    private void OnFreezeChanged(bool isFrozen) => ApplyFreeze(isFrozen);

    private void ApplyFreeze(bool isFrozen)
    {
        if (rb == null) return;
        rb.constraints = isFrozen
            ? RigidbodyConstraints2D.FreezeAll
            : RigidbodyConstraints2D.None;
    }

    // -------------------------------------------------------
    // INITIALIZATION (called by AsteroidSpawner or CreateSplit)
    // -------------------------------------------------------

    public void Initialize(float asteroidSize)
    {
        size = asteroidSize;
        Initialize();
    }

    private void Initialize()
    {
        // 1. Random Rotation
        transform.eulerAngles = new Vector3(0f, 0f, Random.value * 360f);

        // 2. Set Scale & Mass based on Size
        transform.localScale = Vector3.one * size;
        rb.mass = size;

        // 3. Retrieve (or create once) the cached WaitForSeconds for this asteroid's lifetime.
        if (_lifetimeWFS == null)
        {
            if (!_wfsCache.TryGetValue(maxLifetime, out _lifetimeWFS))
            {
                _lifetimeWFS = new WaitForSeconds(maxLifetime);
                _wfsCache[maxLifetime] = _lifetimeWFS;
            }
        }

        // 4. Cache the fade WaitForSeconds once (shared across all instances)
        if (_fadeWFS == null)
            _fadeWFS = new WaitForSeconds(0.15f);

        // 5. Start lifetime timer (replaces Destroy(gameObject, maxLifetime))
        if (lifetimeCoroutine != null) StopCoroutine(lifetimeCoroutine);
        lifetimeCoroutine = StartCoroutine(LifetimeRoutine());
    }

    public void SetTrajectory(Vector2 direction)
    {
        rb.AddForce(direction * movementSpeed);
    }

    // -------------------------------------------------------
    // LIFETIME
    // -------------------------------------------------------

    private IEnumerator LifetimeRoutine()
    {
        yield return _lifetimeWFS; // reuse cached object — no heap allocation
        ReturnToPool();
    }

    // -------------------------------------------------------
    // COLLISION
    // -------------------------------------------------------

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDying) return; // Already fading — ignore further hits
        if (!collision.gameObject.CompareTag("Bullet")) return;

        // Split into two smaller asteroids
        if ((size * 0.5f) >= minSize)
        {
            CreateSplit();
            CreateSplit();
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnAsteroidDestroyed(this);

        ReturnToPool();
    }

    // -------------------------------------------------------
    // DESTRUCTION (pool-compatible)
    // -------------------------------------------------------

    private void ReturnToPool()
    {
        if (isDying) return;
        isDying = true;

        // Disable collider immediately so no further collisions fire during fade
        col.enabled = false;

        // PERFORMANCE: Replace DOTween lambda (allocates a closure object per death)
        // with a plain coroutine that reuses a cached WaitForSeconds — zero allocations.
        StartCoroutine(FadeAndRelease());
    }

    private IEnumerator FadeAndRelease()
    {
        // Manually lerp alpha over 0.15 s — same visual as DOFade but GC-free
        float elapsed = 0f;
        const float duration = 0.15f;
        Color c = spriteRenderer.color;
        float startAlpha = c.a;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            spriteRenderer.color = c;
            yield return null;
        }

        c.a = 0f;
        spriteRenderer.color = c;
        AsteroidPool.Instance.Release(this);
    }

    // -------------------------------------------------------
    // SPLIT
    // -------------------------------------------------------

    private Asteroid CreateSplit()
    {
        if (asteroidPrefab == null)
        {
            Debug.LogError("Asteroid: asteroidPrefab is missing! Drag it into the Inspector slot.");
            return null;
        }

        Vector2 position = (Vector2)transform.position + Random.insideUnitCircle * 0.5f;

        // Get from pool instead of Instantiate
        Asteroid half = AsteroidPool.Instance.Get(asteroidPrefab);
        half.prefabReference = asteroidPrefab;
        half.transform.SetParent(transform.parent);
        half.transform.position = position;
        half.transform.rotation = transform.rotation;

        half.Initialize(size * 0.5f);
        half.SetTrajectory(Random.insideUnitCircle.normalized);

        return half;
    }

    // -------------------------------------------------------
    // UPDATE — freeze powerup
    // -------------------------------------------------------

}