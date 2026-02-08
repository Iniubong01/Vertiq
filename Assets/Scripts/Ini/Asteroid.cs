using DG.Tweening;
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
    // DRAG YOUR ASTEROID PREFAB HERE! This is crucial.
    [SerializeField] private Asteroid asteroidPrefab; 

    [SerializeField]
    private Sprite[] sprites;

    [Header("Settings")]
    public float size = 1f;
    public float minSize = 0.35f;
    public float maxSize = 1.65f;
    public float movementSpeed = 50f;
    public float maxLifetime = 30f;

    private bool isInitialized = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }

    public void Initialize(float asteroidSize)
    {
        size = asteroidSize;
        Initialize();
    }

    private void Initialize()
    {
        isInitialized = true;

        // 1. Random Rotation
        transform.eulerAngles = new Vector3(0f, 0f, Random.value * 360f);

        // 2. Set Scale & Mass based on Size
        transform.localScale = Vector3.one * size;
        rb.mass = size;

        // 3. Destroy after lifetime
        Destroy(gameObject, maxLifetime);
    }

    public void SetTrajectory(Vector2 direction)
    {
        rb.AddForce(direction * movementSpeed);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            // --- SPLIT LOGIC ---
            if ((size * 0.5f) >= minSize)
            {
                CreateSplit();
                CreateSplit();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnAsteroidDestroyed(this);
            }

            // --- DESTRUCTION LOGIC ---
            
            // 1. Disable OUR collider so we don't get hit again while fading
            col.enabled = false;

            // 2. Visual Fade
            spriteRenderer.DOFade(0, 0.15f);

            // 3. Destroy object
            Destroy(gameObject, 0.15f); 
        }
    }

    private Asteroid CreateSplit()
    {
        // Safety check to prevent crashing if you forgot the prefab
        if (asteroidPrefab == null)
        {
            Debug.LogError("Asteroid Prefab is missing! Drag it into the Inspector slot.");
            return null;
        }

        Vector2 position = transform.position;
        position += Random.insideUnitCircle * 0.5f;

        // FIX: Instantiate from PREFAB, not 'this'. 
        // This guarantees the new asteroid has a fresh, ENABLED collider.
        Asteroid half = Instantiate(asteroidPrefab, position, transform.rotation);
        
        // Initialize with half size
        half.Initialize(size * 0.5f);

        // Set Trajectory
        half.SetTrajectory(Random.insideUnitCircle.normalized);

        return half;
    }

    private void Update()
    {
        // Freeze time logic
        if(PowerUpManager.Instance != null && PowerUpManager.Instance.IsFreezeTimeActive)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}