using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    private Rigidbody2D rb;

    public float speed = 500f;
    public float maxLifetime = 10f;

    private float lifetimeTimer;
    
    // Reference to the prefab this bullet was created from (for pool tracking)
    [HideInInspector] public Bullet prefabReference;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Called by the pool when this bullet is retrieved
    private void OnEnable()
    {
        lifetimeTimer = maxLifetime;
        rb.linearVelocity = Vector2.zero;
    }

    public void Shoot(Vector2 direction, Bullet prefab)
    {
        prefabReference = prefab;
        rb.AddForce(direction * speed);
    }

    private void Update()
    {
        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
            ReturnToPool();
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