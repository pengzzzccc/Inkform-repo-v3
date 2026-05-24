using UnityEngine;

public class S_Door : MonoBehaviour
{
    [SerializeField] private Transform door;
    [SerializeField] private Transform targetPosition;
    [SerializeField] private float speed = 3f;

    private bool isMoving = false;

    void Awake()
    {
        // Auto-find children if not assigned
        if (door == null)
            door = transform.Find("Door");

        if (targetPosition == null)
            targetPosition = transform.Find("MovingPosition");
    }

    void Update()
    {
        if (!isMoving || door == null || targetPosition == null)
            return;

        door.position = Vector3.MoveTowards(
            door.position,
            targetPosition.position,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(door.position, targetPosition.position) < 0.01f)
        {
            isMoving = false;
        }
    }

    // Called by the button
    public void TriggerDoor()
    {
        isMoving = true;
    }
}