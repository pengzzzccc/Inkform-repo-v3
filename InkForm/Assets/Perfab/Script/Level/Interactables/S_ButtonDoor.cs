using UnityEngine;

public class S_ButtonDoor : MonoBehaviour
{
    [SerializeField] private S_Door doorSystem;

    private bool isActivated = false;

    void Awake()
    {
        // Auto-find the system in parent if not assigned
        if (doorSystem == null)
        {
            doorSystem = GetComponentInParent<S_Door>();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isActivated) return;

        if (other.CompareTag("block"))
        {
            Activate();
        }
    }

    private void Activate()
    {
        isActivated = true;

        if (doorSystem != null)
        {
            doorSystem.TriggerDoor();
        }
    }
}
