using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    [Header("Asteroid Settings")]
    public Asteroid[] asteroidPrefab;
    public float spawnDistance = 12f;
    public int baseAmountPerSpawn = 1;
    public int maxAmountPerSpawn = 5;

    [Range(0f, 45f)]
    public float trajectoryVariance = 15f;

    [Header("Difficulty Settings")]
    public float startSpawnInterval = 1f;       // Slowest (beginning)
    public float minimumSpawnInterval = 0.25f;  // Fastest allowed
    public float difficultyRampTime = 90f;      // How long to reach max speed

    [Tooltip("Extra multiplier that increases the amount of asteroids per wave over time.")]
    public float amountRamp = 0.02f;            // 0 = disabled, 0.02 = grows slowly

    [Header("Audio")]
    [SerializeField] private AudioClip spawnSound;

    private AudioSource audioSource;

    private float elapsedTime = 0f;
    private float spawnTimer = 0f;
    private float currentSpawnInterval = 1f;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Start interval
        currentSpawnInterval = startSpawnInterval;
    }

    private void Update()
    {
        // Time alive = difficulty
        elapsedTime += Time.deltaTime;

        // Difficulty lerp (0 → 1)
        float t = Mathf.Clamp01(elapsedTime / difficultyRampTime);

        // Interval shrinks as time increases
        currentSpawnInterval = Mathf.Lerp(startSpawnInterval, minimumSpawnInterval, t);

        // Spawn timing
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= currentSpawnInterval)
        {
            spawnTimer = 0f;

            // Amount increases over time
            int amountThisWave = Mathf.Clamp(
                baseAmountPerSpawn + Mathf.FloorToInt(elapsedTime * amountRamp),
                baseAmountPerSpawn,
                maxAmountPerSpawn
            );

            Spawn(amountThisWave);
        }
    }

    private void Spawn(int amount)
    {
        if (spawnSound != null)
            audioSource.PlayOneShot(spawnSound);

        for (int i = 0; i < amount; i++)
        {
            Vector3 spawnDirection = Random.insideUnitCircle.normalized;
            Vector3 spawnPoint = transform.position + (spawnDirection * spawnDistance);

            float variance = Random.Range(-trajectoryVariance, trajectoryVariance);
            Quaternion rotation = Quaternion.AngleAxis(variance, Vector3.forward);

            int index = Random.Range(0, asteroidPrefab.Length);
            Asteroid asteroid = Instantiate(asteroidPrefab[index], spawnPoint, rotation);
            asteroid.size = Random.Range(asteroid.minSize, asteroid.maxSize);

            Vector2 trajectory = rotation * -spawnDirection;
            asteroid.SetTrajectory(trajectory);
        }
    }
}
