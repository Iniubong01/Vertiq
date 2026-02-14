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
        // repeatWidth = GetComponent<BoxCollider2D>().bounds.size.y / 2;
    }

    void FixedUpdate()
    {
        transform.Translate(Vector3.down * scrollSpeed * Time.deltaTime);

        if (transform.position.y < startPos.y - repeatWidth)
        {
            transform.position = startPos;
        }
    }
}