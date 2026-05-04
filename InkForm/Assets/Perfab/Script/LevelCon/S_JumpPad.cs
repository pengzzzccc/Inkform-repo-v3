using UnityEngine;

public class S_JumpPad : MonoBehaviour
{
    [SerializeField] private float jumpForce = 200f;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("jumpPad!!");
            S_Player.Instance.GetRigidbody().AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }
}
