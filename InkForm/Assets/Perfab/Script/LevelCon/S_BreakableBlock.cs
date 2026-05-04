using UnityEngine;

public class S_BreakableBlock : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !S_Player.Instance.getForm())
        {
            Destroy(gameObject);
        }
    }
}