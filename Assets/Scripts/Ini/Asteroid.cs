using DG.Tweening;
using System.Collections;
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

    private Coroutine lifetimeCoroutine;
    private bool isDying = false;

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
        // Reset visual & physics state so pooled asteroids are clean
        isDying = false;
        col.enabled = true;

        // Kill any leftover tweens from a previous use
        spriteRenderer.DOKill();
        Color c = spriteRenderer.color;
        c.a = 1f;
        spriteRenderer.color = c;

        // Cache PowerUpManager once per activation (avoids per-frame lookup)
        powerUpManager = PowerUpManager.Instance;
    }

    private void OnDisable()
    {
        // Kill tweens so they don't run on a pooled (inactive) object
        spriteRenderer.DOKill();

        // Stop lifetime coroutine
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }
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

        // 3. Start lifetime timer (replaces Destroy(gameObject, maxLifetime))
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
        yield return new WaitForSeconds(maxLifetime);
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

        // Fade out, then return to pool
        spriteRenderer.DOFade(0f, 0.15f).OnComplete(() =>
        {
            AsteroidPool.Instance.Release(this);
        });
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

    private void Update()
    {
        // Use cached reference — no per-frame singleton lookup
        if (powerUpManager != null && powerUpManager.IsFreezeTimeActive)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}