using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerInput playerInput; 

    private Vector2 moveInput; 
    private Vector2 lookInput; 
    private bool isBraking = false;

    [Header("Movement Settings")]
    // 1. INCREASED SPEED: Needs to be higher to fight the higher drag
    [SerializeField] private float moveSpeed = 15f; 
    [SerializeField] private float rotationSmoothness = 15f;
    
    [Header("Braking Settings")]
    // 2. INCREASED DRAG: This stops the "sliding on ice" feeling. 
    // 2.0f is much tighter than 0.3f.
    [SerializeField] private float normalDrag = 2.0f; 
    [SerializeField] private float brakeDrag = 6.0f;  

    [Header("Shooting Settings")]
    [SerializeField] private Bullet bulletPrefab;
    [Range(1, 10)] [SerializeField] public int powerLevel = 1;
    [SerializeField] private float spreadAngle = 10f;
    [SerializeField] private float fireRate = 0.15f; 
    
    private Coroutine firingCoroutine; 

    [Header("Other Settings")]
    public float respawnDelay = 3f;
    public float respawnInvulnerability = 3f;
    public bool screenWrapping = true;
    private Bounds screenBounds;
    private AudioSource audioSource;
    [SerializeField] private AudioClip shootClip;
    
    public static bool canShoot = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();

        rb.gravityScale = 0f;
        rb.angularDamping = 0.1f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        // Initialize drag immediately
        rb.linearDamping = normalDrag;
    }

    private void Start()
    {
        GameObject[] boundaries = GameObject.FindGameObjectsWithTag("Boundary");
        audioSource = GetComponent<AudioSource>();

        for (int i = 0; i < boundaries.Length; i++) {
            if(boundaries[i] != null) boundaries[i].SetActive(!screenWrapping);
        }

        screenBounds = new Bounds();
        screenBounds.Encapsulate(Camera.main.ScreenToWorldPoint(Vector3.zero));
        screenBounds.Encapsulate(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0f)));

        OnEnable();
    }

    private void Update()
    {
        HandleShooting();
    }

    private void FixedUpdate()
    {
        // Apply drag based on braking state
        if (isBraking)
            rb.linearDamping = brakeDrag;
        else
            rb.linearDamping = normalDrag;

        // Move Force
        if (moveInput.sqrMagnitude > 0.01f)
            rb.AddForce(moveInput.normalized * moveSpeed, ForceMode2D.Force);

        // Rotation
        if (lookInput.sqrMagnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(lookInput.y, lookInput.x) * Mathf.Rad2Deg - 90f;
            float newAngle = Mathf.LerpAngle(rb.rotation, targetAngle, rotationSmoothness * Time.fixedDeltaTime);
            rb.MoveRotation(newAngle);
        }

        if (screenWrapping) ScreenWrap();
    }

    // ... (Rest of your Input Callbacks and Collision code remains the same) ...

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();
    public void OnBrake(InputValue value) => isBraking = value.isPressed;

    public void OnActivatePowerup(InputValue value)
    {
        if (value.isPressed) 
        {
            PowerUpManager.Instance.TriggerSelectedPowerUp();
        }
    }

    public void OnNavigateLeft(InputValue value)
    {
        if (value.isPressed) PowerUpManager.Instance.Navigate(-1);
    }

    public void OnNavigateRight(InputValue value)
    {
        if (value.isPressed) PowerUpManager.Instance.Navigate(1);
    }

    private void HandleShooting()
    {


        float triggerValue = playerInput.actions["Fire"].ReadValue<float>();

        if (triggerValue > 0.5f && canShoot)
        {
            if (firingCoroutine == null) firingCoroutine = StartCoroutine(FireContinuously());
        }
        else
        {
            if (firingCoroutine != null)
            {
                StopCoroutine(firingCoroutine);
                firingCoroutine = null;
            }
        }
    }

    private IEnumerator FireContinuously()
    {
        while (true)
        {
            Shoot(); 
            yield return new WaitForSeconds(fireRate); 
        }
    }

    public void Shoot()
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
        
        if(audioSource && shootClip) audioSource.PlayOneShot(shootClip);
    }

    private void OnEnable()
    {
        TurnOffCollisions();
        Invoke(nameof(TurnOnCollisions), respawnInvulnerability);
    }

    private void OnDisable()
    {
        if (firingCoroutine != null)
        {
            StopCoroutine(firingCoroutine);
            firingCoroutine = null;
        }
    }

    private void TurnOnCollisions() => gameObject.layer = LayerMask.NameToLayer("Player");
    private void TurnOffCollisions() => gameObject.layer = LayerMask.NameToLayer("Ignore Collisions");

    private void ScreenWrap()
    {
        if (rb.position.x > screenBounds.max.x + 0.5f) 
            rb.position = new Vector2(screenBounds.min.x - 0.5f, rb.position.y);
        else if (rb.position.x < screenBounds.min.x - 0.5f) 
            rb.position = new Vector2(screenBounds.max.x + 0.5f, rb.position.y);
        else if (rb.position.y > screenBounds.max.y + 0.5f) 
            rb.position = new Vector2(rb.position.x, screenBounds.min.y - 0.5f);
        else if (rb.position.y < screenBounds.min.y - 0.5f) 
            rb.position = new Vector2(rb.position.x, screenBounds.max.y + 0.5f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Asteroid" && !PowerUpManager.Instance.shieldActive)
        {
            GameManager.Instance.OnPlayerDeath(this);
        }
    }

    public void enableShooting() => canShoot = true;
    public void preventShooting() => canShoot = false;
}