using UnityEngine;

public class S_Checkpoint : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (S_PlayerLookup.IsPlayer(collision))
        {
            S_GameEvent.SpawnPointChanged(this.gameObject.transform);
        }
    }
}
