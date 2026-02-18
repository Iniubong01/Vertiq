using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    [Header("Asteroid Settings")]
    public Asteroid[] asteroidPrefab;
    public float spawnBuffer = 2f; // How far outside the screen to spawn
    public int baseAmountPerSpawn = 1;
    public int maxAmountPerSpawn = 5;

    [Range(0f, 45f)]
    public float trajectoryVariance = 15f;

    [Header("Difficulty Settings")]
    public float startSpawnInterval = 1f;       
    public float minimumSpawnInterval = 0.25f;  
    public float difficultyRampTime = 90f;      
    public float amountRamp = 0.02f;            

    [Header("Audio")]
    [SerializeField] private AudioClip spawnSound;

    private AudioSource audioSource;
    private Camera mainCam;
    private float elapsedTime = 0f;
    private float spawnTimer = 0f;
    private float currentSpawnInterval = 1f;
    //? Todo: Fix this using player.cs
    [SerializeField] private Transform asteroidParent;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        mainCam = Camera.main;
        currentSpawnInterval = startSpawnInterval;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        // Difficulty Math
        float t = Mathf.Clamp01(elapsedTime / difficultyRampTime);
        currentSpawnInterval = Mathf.Lerp(startSpawnInterval, minimumSpawnInterval, t);

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= currentSpawnInterval)
        {
            spawnTimer = 0f;
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
        if (spawnSound != null && audioSource != null)
            audioSource.PlayOneShot(spawnSound);

        for (int i = 0; i < amount; i++)
        {
            // 1. Get a spawn point outside the camera view
            Vector3 spawnPoint = GetRandomPointOutsideCamera();

            // 2. Calculate direction towards the CENTER (0,0)
            Vector2 directionToCenter = (Vector3.zero - spawnPoint).normalized;

            // 3. Add randomness to the angle
            float variance = Random.Range(-trajectoryVariance, trajectoryVariance);
            Quaternion rotation = Quaternion.AngleAxis(variance, Vector3.forward);

            // 4. Pick a random prefab variant and size
            int index = Random.Range(0, asteroidPrefab.Length);
            Asteroid prefab = asteroidPrefab[index];
            float randomSize = Random.Range(prefab.minSize, prefab.maxSize);

            // 5. Get from pool (no Instantiate/GC spike)
            Asteroid asteroid = AsteroidPool.Instance.Get(prefab);
            asteroid.prefabReference = prefab;
            asteroid.transform.SetParent(asteroidParent);
            asteroid.transform.position = spawnPoint;
            asteroid.transform.rotation = rotation;

            // 6. Initialize with size
            asteroid.Initialize(randomSize);

            // 7. Set trajectory
            asteroid.SetTrajectory(rotation * directionToCenter);
        }
    }

    // --- CALCULATE CAMERA BOUNDS ---
    private Vector3 GetRandomPointOutsideCamera()
    {
        float height = 2f * mainCam.orthographicSize;
        float width = height * mainCam.aspect;
        
        // Calculate the edges (Center 0,0 assumed)
        float top = mainCam.transform.position.y + (height / 2f);
        float bottom = mainCam.transform.position.y - (height / 2f);
        float right = mainCam.transform.position.x + (width / 2f);
        float left = mainCam.transform.position.x - (width / 2f);

        // Pick a random side: 0=Top, 1=Bottom, 2=Right, 3=Left
        int side = Random.Range(0, 4);
        Vector3 point = Vector3.zero;

        switch (side)
        {
            case 0: // Top
                point = new Vector3(Random.Range(left, right), top + spawnBuffer, 0);
                break;
            case 1: // Bottom
                point = new Vector3(Random.Range(left, right), bottom - spawnBuffer, 0);
                break;
            case 2: // Right
                point = new Vector3(right + spawnBuffer, Random.Range(bottom, top), 0);
                break;
            case 3: // Left
                point = new Vector3(left - spawnBuffer, Random.Range(bottom, top), 0);
                break;
        }

        return point;
    }
    
    // Debug: Draw the spawn zone in Scene View
    private void OnDrawGizmos()
    {
        if (Camera.main == null) return;
        
        Camera cam = Camera.main;
        float height = 2f * cam.orthographicSize;
        float width = height * cam.aspect;
        
        Gizmos.color = Color.green;
        // Draw the spawn rectangle (View size + buffer)
        Gizmos.DrawWireCube(cam.transform.position, new Vector3(width + (spawnBuffer * 2), height + (spawnBuffer * 2), 0));
    }
}