using UnityEngine;

public class S_Checkpoint : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            S_GameEvent.ReNewSpwnPoint(this.gameObject.transform);
        }
    }
}