using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSmoothness = 10f;

    [Header("Shooting Settings")]
    [SerializeField] private Bullet bulletPrefab;
    [Range(1, 10)] [SerializeField] public int powerLevel = 1;
    [SerializeField] private float spreadAngle = 10f;
    public float respawnDelay = 3f;
    public float respawnInvulnerability = 3f;

    [Header("Other Settings")]
    public bool screenWrapping = true;
    private Bounds screenBounds;
    private AudioSource audioSource;
    [SerializeField] private AudioClip shootClip;
    
    public static bool canShoot = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0.3f;
        rb.angularDamping = 0.1f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Start()
    {
        GameObject[] boundaries = GameObject.FindGameObjectsWithTag("Boundary");
        audioSource = GetComponent<AudioSource>();

        // Disable all boundaries if screen wrapping is enabled
        for (int i = 0; i < boundaries.Length; i++) {
            boundaries[i].SetActive(!screenWrapping);
        }

        // Convert screen space bounds to world space bounds
        screenBounds = new Bounds();
        screenBounds.Encapsulate(Camera.main.ScreenToWorldPoint(Vector3.zero));
        screenBounds.Encapsulate(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0f)));

        OnEnable();
    }

    private void Update()
    {
        // Shooting
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) && canShoot)
            Shoot();
    }

    private void FixedUpdate()
    {
        // --- Movement input ---
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");
        Vector2 moveInput = new Vector2(moveX, moveY);

        // --- Apply movement ---
        if (moveInput.sqrMagnitude > 0.01f)
        {
            // Apply smooth acceleration force
            rb.AddForce(moveInput.normalized * moveSpeed, ForceMode2D.Force);

            // Rotate toward movement direction
            float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg - 90f;
            float newAngle = Mathf.LerpAngle(rb.rotation, targetAngle, rotationSmoothness * Time.fixedDeltaTime);
            rb.MoveRotation(newAngle);
        }

        // --- Screen wrapping ---
        if (screenWrapping)
            ScreenWrap();
    }

    private void OnEnable()
    {
        // Turn off collisions for a few seconds after spawning to ensure the
        // player has enough time to safely move away from asteroids
        TurnOffCollisions();
        Invoke(nameof(TurnOnCollisions), respawnInvulnerability);
    }

    private void TurnOnCollisions()
    {
        gameObject.layer = LayerMask.NameToLayer("Player");
    }

    private void TurnOffCollisions()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Collisions");
    }

    private void ScreenWrap()
    {
        // Move to the opposite side of the screen if the player exceeds the bounds
        if (rb.position.x > screenBounds.max.x + 0.5f) {
            rb.position = new Vector2(screenBounds.min.x - 0.5f, rb.position.y);
        }
        else if (rb.position.x < screenBounds.min.x - 0.5f) {
            rb.position = new Vector2(screenBounds.max.x + 0.5f, rb.position.y);
        }
        else if (rb.position.y > screenBounds.max.y + 0.5f) {
            rb.position = new Vector2(rb.position.x, screenBounds.min.y - 0.5f);
        }
        else if (rb.position.y < screenBounds.min.y - 0.5f) {
            rb.position = new Vector2(rb.position.x, screenBounds.max.y + 0.5f);
        }
    }

    private void Shoot()
    {
        int bulletCount = Mathf.Clamp(powerLevel, 1, 10);
        float totalSpread = (bulletCount - 1) * spreadAngle;

        for (int i = 0; i < bulletCount; i++)
        {
            float angleOffset = -totalSpread / 2f + i * spreadAngle;
            Quaternion rotation = transform.rotation * Quaternion.Euler(0, 0, angleOffset);
            Bullet bullet = Instantiate(bulletPrefab, transform.position, rotation);
            bullet.Shoot(rotation * Vector2.up);
        }
        
        audioSource.PlayOneShot(shootClip);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Asteroid" && !PowerUpManager.Instance.shieldActive)
        {
            GameManager.Instance.OnPlayerDeath(this);
        }
    }

    public void enableShooting()
    {
        canShoot = true;
    }
    
    public void preventShooting()
    {
        canShoot = false;
    }
}