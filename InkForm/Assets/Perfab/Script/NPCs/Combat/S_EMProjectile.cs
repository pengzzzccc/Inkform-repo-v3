using UnityEngine;

/// <summary>
/// Electromagnetic projectile fired by S_NPCEnemy during Attack state.
/// Applies paralyze to the player on contact, then self-destructs.
/// </summary>
public class S_EMProjectile : MonoBehaviour
{
    [Header("Paralyze Effect")]
    [SerializeField] private float paralyzeDuration = 3f;
    [SerializeField] private float moveSpeedReduction = 0.5f;

    [Header("Movement")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float maxLifetime = 5f;

    private Vector2 direction;
    private float lifetime;
    private S_NPCEnemy shooter;

    public void Launch(Vector2 dir, S_NPCEnemy shooterRef)
    {
        direction = dir.normalized;
        shooter = shooterRef;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        lifetime = 0f;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        lifetime += Time.deltaTime;
        if (lifetime >= maxLifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            S_Player.Instance.ApplyParalyze(paralyzeDuration, moveSpeedReduction);
            if (shooter != null)
                shooter.OnProjectileHitPlayer();
            Destroy(gameObject);
        }
        else if (!other.isTrigger && !other.CompareTag("NPC"))
        {
            Destroy(gameObject);
        }
    }
}