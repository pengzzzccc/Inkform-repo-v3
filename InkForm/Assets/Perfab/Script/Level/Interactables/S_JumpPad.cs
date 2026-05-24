using UnityEngine;

public class S_JumpPad : MonoBehaviour
{
    private const float MinJumpForce = 25f;
    private const float MaxJumpForce = 70f;

    [SerializeField, Range(MinJumpForce, MaxJumpForce)] private float jumpForce = 35f;

    private SpriteRenderer targetSprite;

    private void Awake()
    {
        UpdatePadColor();
    }

    private void OnValidate()
    {
        jumpForce = Mathf.Clamp(jumpForce, MinJumpForce, MaxJumpForce);
        UpdatePadColor();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!S_PlayerLookup.TryGet(collision, out IPlayerActor player))
            return;

        Debug.Log("jumpPad!!");
        player.Rigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private void UpdatePadColor()
    {
        if (targetSprite == null)
        {
            targetSprite = GetComponent<SpriteRenderer>();
        }

        if (targetSprite == null)
        {
            return;
        }

        float forceRate = Mathf.InverseLerp(MinJumpForce, MaxJumpForce, jumpForce);
        targetSprite.color = Color.Lerp(Color.green, Color.red, forceRate);
    }
}
