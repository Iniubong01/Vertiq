using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public PowerUpType type; 
    public float duration = 5f, movementSpeed, existencePeriod = 6;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        SetTrajectory(Random.insideUnitCircle.normalized);  // Random direction
        Destroy(this.gameObject, existencePeriod);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PowerUpManager.Instance.ActivatePowerUp(type, duration);
            Destroy(gameObject);
            Debug.Log($"Player picked {type}");
        }
    }

    public void SetTrajectory(Vector2 direction)
    {
        // The asteroid only needs a force to be added once since they have no
        // drag to make them stop moving
        rb.AddForce(direction * movementSpeed);
    }
}