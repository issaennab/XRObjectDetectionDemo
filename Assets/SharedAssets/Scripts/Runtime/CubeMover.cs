using UnityEngine;

public class CubeMover : MonoBehaviour
{
    public float speed = 2f;
    public float distance = 2f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * speed) * distance;
        transform.position = startPosition + new Vector3(offset, 0f, 0f);
    }
}