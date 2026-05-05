using UnityEngine;

/// <summary>
/// K-01 labourer NPC with fixed patrol routes — no chase behaviour.
/// Used for factory floor workers (Ch1 Branch2 mimicry) and Nurserie workers (Ch2).
/// The player can mimic K-01 movement patterns to avoid suspicion.
/// </summary>
public class S_NPCStory : S_NPCbase
{
    [Header("Patrol Route")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waypointWaitTime = 2f;
    [SerializeField] private bool loopRoute = true;

    [Header("Mimicry")]
    [SerializeField] private bool isMimicTarget = false;
    [SerializeField] private float mimicDetectionRange = 5f;

    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    private void Update()
    {
        if (!isActive) return;
        UpdatePatrol();
    }

    private void FixedUpdate()
    {
        if (!isActive) return;
        ExecuteMovement();
    }

    private void UpdatePatrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                AdvanceWaypoint();
            }
            return;
        }

        // Check if reached current waypoint
        Transform target = waypoints[currentWaypointIndex];
        if (target != null && Vector2.Distance(transform.position, target.position) < 0.1f)
        {
            isWaiting = true;
            waitTimer = waypointWaitTime;
        }
    }

    private void ExecuteMovement()
    {
        if (waypoints == null || waypoints.Length == 0 || isWaiting) return;

        Transform target = waypoints[currentWaypointIndex];
        if (target == null) return;

        Vector2 direction = (target.position - transform.position).normalized;
        npcRig.linearVelocity = new Vector2(direction.x * moveSpeed, npcRig.linearVelocity.y);
        FlipSprite(direction.x);
    }

    private void AdvanceWaypoint()
    {
        currentWaypointIndex++;
        if (currentWaypointIndex >= waypoints.Length)
        {
            if (loopRoute)
                currentWaypointIndex = 0;
            else
                currentWaypointIndex = waypoints.Length - 1;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
                Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
        }
        if (waypoints.Length > 1)
        {
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] != null && waypoints[i + 1] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }
    }
}