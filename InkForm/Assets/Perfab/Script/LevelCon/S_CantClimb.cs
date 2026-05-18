using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class S_CantClimb : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("Affect Area")]
    [SerializeField] private float affectArea = 0.6f;

    [Header("Climb Disable")]
    [SerializeField] private float climbUnavailableTime = 0.35f;

    [Header("Fall Off Settings")]
    [SerializeField] private float gravityScaleOnDetach = 4f;
    [SerializeField] private float detachPushForce = 2f;
    [SerializeField] private float downwardForce = 6f;
    [SerializeField] private bool applyWhileStaying = true;

    private ContactFilter2D contactFilter;
    private Collider2D[] hits = new Collider2D[8];
    private Coroutine disableClimbRoutine;

    private void Awake()
    {
        contactFilter = new ContactFilter2D();
        contactFilter.useTriggers = true;
        contactFilter.useLayerMask = false;
    }

    private void Reset()
    {
        int cantClimbLayer = LayerMask.NameToLayer("CantClimb");

        if (cantClimbLayer >= 0)
            gameObject.layer = cantClimbLayer;

        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void FixedUpdate()
    {
        CheckAffectArea();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!applyWhileStaying)
            TryDetachPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!applyWhileStaying)
            return;

        TryDetachPlayer(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!applyWhileStaying)
            TryDetachPlayer(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!applyWhileStaying)
            return;

        TryDetachPlayer(collision.collider);
    }

    private void CheckAffectArea()
    {
        int hitCount = Physics2D.OverlapCircle(
            transform.position,
            affectArea,
            contactFilter,
            hits
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
                continue;

            TryDetachPlayer(hit);
        }
    }

    private void TryDetachPlayer(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        S_Player player = other.GetComponentInParent<S_Player>();
        if (player == null)
            return;

        Rigidbody2D rig = player.GetRigidbody();
        if (rig == null)
            return;

        S_fluid_climb fluidClimb = FindFluidClimbSkill(player);

        if (fluidClimb != null)
        {
            fluidClimb.SetSurface(S_fluid_climb.SurfaceType.None);
            fluidClimb.ResetClimbTimer();

            if (disableClimbRoutine != null)
                StopCoroutine(disableClimbRoutine);

            disableClimbRoutine = StartCoroutine(DisableClimbTemporarily(player, fluidClimb));
        }

        rig.gravityScale = gravityScaleOnDetach;

        Vector2 pushDirection = GetDetachDirection(player);

        rig.linearVelocity = new Vector2(
            pushDirection.x * detachPushForce,
            -Mathf.Abs(downwardForce)
        );
    }

    private IEnumerator DisableClimbTemporarily(S_Player player, S_fluid_climb fluidClimb)
    {
        float timer = 0f;

        while (timer < climbUnavailableTime)
        {
            if (player == null || fluidClimb == null)
                yield break;

            Rigidbody2D rig = player.GetRigidbody();

            fluidClimb.SetSurface(S_fluid_climb.SurfaceType.None);
            fluidClimb.ResetClimbTimer();

            if (rig != null)
                rig.gravityScale = gravityScaleOnDetach;

            timer += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        disableClimbRoutine = null;
    }

    private Vector2 GetDetachDirection(S_Player player)
    {
        Transform bodyTransform = player.GetBodyTransform();

        if (bodyTransform == null)
            return Vector2.down;

        Vector2 direction = bodyTransform.position - transform.position;

        if (direction.sqrMagnitude < 0.001f)
            return Vector2.down;

        return new Vector2(direction.normalized.x, -1f).normalized;
    }

    private S_fluid_climb FindFluidClimbSkill(S_Player player)
    {
        MonoBehaviour[] behaviours = player.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            System.Reflection.FieldInfo[] fields = behaviour.GetType().GetFields(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic
            );

            foreach (System.Reflection.FieldInfo field in fields)
            {
                if (field.FieldType == typeof(S_fluid_climb))
                {
                    return field.GetValue(behaviour) as S_fluid_climb;
                }
            }
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, affectArea);
    }
}