using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    [SerializeField] float scrollSpeed = 2f;

    float repeatWidth;
    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        repeatWidth = GetComponent<BoxCollider2D>().bounds.size.y;
    }

    // PERFORMANCE: Visual-only movement belongs in Update, not FixedUpdate.
    // FixedUpdate couples this to the physics timestep — when we widen the
    // timestep to reduce physics cost, purely visual transforms should stay
    // in Update for smooth, frame-rate-independent movement.
    void Update()
    {
        transform.Translate(Vector3.down * scrollSpeed * Time.deltaTime);

        if (transform.position.y < startPos.y - repeatWidth)
        {
            transform.position = startPos;
        }
    }
}