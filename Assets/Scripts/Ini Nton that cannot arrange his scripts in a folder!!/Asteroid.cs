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

    [SerializeField]
    private Sprite[] sprites;

    public float size = 1f;
    public float minSize = 0.35f;
    public float maxSize = 1.65f;
    public float movementSpeed = 50f;
    public float maxLifetime = 30f;
    private SpriteRenderer sprite;
    
    private bool isInitialized = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Only initialize if not already done (prevents double initialization)
        if (!isInitialized)
        {
            Initialize();
        }
    }

    // Public method to initialize the asteroid with a specific size BEFORE Start() runs
    public void Initialize(float asteroidSize)
    {
        size = asteroidSize;
        Initialize();
    }

    private void Initialize()
    {
        isInitialized = true;

        // Assign random properties to make each asteroid feel unique
        transform.eulerAngles = new Vector3(0f, 0f, Random.value * 360f);

        // Set the scale and mass of the asteroid based on the assigned size so
        // the physics is more realistic
        transform.localScale = Vector3.one * size;
        rb.mass = size;

        Debug.Log($"Asteroid initialized: Size={size}, Scale={transform.localScale}, Mass={rb.mass}");

        // Destroy the asteroid after it reaches its max lifetime
        Destroy(gameObject, maxLifetime);
    }

    public void SetTrajectory(Vector2 direction)
    {
        // The asteroid only needs a force to be added once since they have no
        // drag to make them stop moving
        rb.AddForce(direction * movementSpeed);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            // Check if the asteroid is large enough to split in half
            // (both parts must be greater than the minimum size)
            if ((size * 0.5f) >= minSize)
            {
                CreateSplit();
                CreateSplit();
            }

            GameManager.Instance.OnAsteroidDestroyed(this);

            // Destroy the current asteroid since it is either replaced by two
            // new asteroids or small enough to be destroyed by the bullet
            
            sprite.DOFade(0, 0.01f);
            Destroy(this.gameObject);
            Debug.LogWarning("Asteroid Destroyed!");
        }
    }

    private Asteroid CreateSplit()
    {
        // Set the new asteroid position to be the same as the current asteroid
        // but with a slight offset so they do not spawn inside each other
        Vector2 position = transform.position;
        position += Random.insideUnitCircle * 0.5f;

        // Create the new asteroid at half the size of the current
        Asteroid half = Instantiate(this, position, transform.rotation);
        
        // Initialize the split asteroid with the correct size BEFORE Start runs
        half.Initialize(size * 0.5f);

        // Set a random trajectory
        half.SetTrajectory(Random.insideUnitCircle.normalized);

        return half;
    }

    private void Update()
    {
        // Debug check for disabled components
        if (!col.enabled || !this.enabled)
        {
            Debug.LogError($"[ASTEROID] Components disabled! GameObject: {gameObject.name}, Collider: {col.enabled}, Script: {this.enabled}, Position: {transform.position}");
        }

        if(PowerUpManager.Instance != null && PowerUpManager.Instance.IsFreezeTimeActive)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void OnDisable()
    {
        Debug.LogWarning($"[ASTEROID] Script disabled on {gameObject.name}! Size: {size}, Position: {transform.position}");
        Debug.LogWarning($"Stack trace: {System.Environment.StackTrace}");
    }
}