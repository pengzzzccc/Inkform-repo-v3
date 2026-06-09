using UnityEngine;

/// <summary>
/// Lets the player grab and physically drag a nearby "block"-tagged object. While Grip (G) is held,
/// if a block with a Rigidbody2D is in front within range, a FixedJoint2D attaches it to the player
/// so moving pulls/pushes it; the block is blocked by walls via its own collider. Release Grip to let
/// go. Works under any gravity direction (front uses the gravity-relative facing).
/// </summary>
public class S_PlayerGrab : MonoBehaviour
{
    [SerializeField] private float grabRange = 1.2f;
    [SerializeField] private bool freezeBlockRotationWhileHeld = true;
    [SerializeField] private float breakForce = Mathf.Infinity;

    private S_Player player;
    private FixedJoint2D joint;
    private Rigidbody2D grabbedBlock;
    private bool grabbedBlockHadFreezeRotation;

    public bool IsGrabbing => joint != null && grabbedBlock != null;

    private void Awake()
    {
        player = GetComponent<S_Player>();
    }

    private void OnEnable()
    {
        S_GameEvent.OnRespawnRequested += Release;
    }

    private void OnDisable()
    {
        S_GameEvent.OnRespawnRequested -= Release;
        Release();
    }

    private void Update()
    {
        // The joint may have broken on its own (finite breakForce) — finalise the release.
        if (grabbedBlock != null && joint == null)
            Release();

        bool held = S_Input.Actions.Player.grep.IsPressed();

        if (held && !IsGrabbing)
            TryGrab();
        else if (!held && IsGrabbing)
            Release();
    }

    private void TryGrab()
    {
        if (player == null || player.BodyTransform == null)
            return;

        Vector2 origin = player.BodyTransform.position;
        Vector2 facing = player.GetFaceRight() ? player.GravityRight : -player.GravityRight;
        Rigidbody2D playerBody = player.GetRigidbody();

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, grabRange);
        Rigidbody2D best = null;
        float bestSqr = float.MaxValue;

        foreach (Collider2D col in hits)
        {
            if (col == null || !col.CompareTag("block"))
                continue;

            Rigidbody2D rb = col.attachedRigidbody;
            if (rb == null || rb == playerBody)
                continue;

            Vector2 toBlock = (Vector2)col.bounds.center - origin;
            if (Vector2.Dot(toBlock, facing) <= 0f) // only blocks in front
                continue;

            float sqr = toBlock.sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = rb;
            }
        }

        if (best != null)
            Grab(best);
    }

    private void Grab(Rigidbody2D block)
    {
        Rigidbody2D playerBody = player.GetRigidbody();
        if (playerBody == null)
            return;

        grabbedBlock = block;

        if (freezeBlockRotationWhileHeld)
        {
            grabbedBlockHadFreezeRotation = block.freezeRotation;
            block.freezeRotation = true;
        }

        joint = playerBody.gameObject.AddComponent<FixedJoint2D>();
        joint.connectedBody = block;
        joint.autoConfigureConnectedAnchor = true;
        joint.enableCollision = false;
        joint.breakForce = breakForce;
    }

    private void Release()
    {
        if (joint != null)
            Destroy(joint);
        joint = null;

        if (grabbedBlock != null && freezeBlockRotationWhileHeld)
            grabbedBlock.freezeRotation = grabbedBlockHadFreezeRotation;

        grabbedBlock = null;
    }

    private void OnDrawGizmosSelected()
    {
        S_Player p = player != null ? player : GetComponent<S_Player>();
        Vector3 origin = p != null && p.BodyTransform != null ? p.BodyTransform.position : transform.position;
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(origin, grabRange);
    }
}
