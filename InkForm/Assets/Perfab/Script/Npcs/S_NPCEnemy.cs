using UnityEngine;

/// <summary>
/// Guard NPC with patrol + chase state machine.
/// Listens to OnSuspicionChanged and OnArrestTriggered for behaviour changes.
/// </summary>
public class S_NPCEnemy : S_NPCbase
{
    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float patrolSpeed = 3f;
    [SerializeField] private float waypointWaitTime = 1f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 6f;
    [SerializeField] private float chaseRange = 8f;
    [SerializeField] private float loseRange = 12f;

    [Header("Arrest")]
    [SerializeField] private float arrestSpeed = 9f;

    private enum State { Idle, Patrol, Chase, Arrest, Disabled }
    private State currentState = State.Patrol;

    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private float suspicionLevel = 0f;

    protected override void OnEnable()
    {
        base.OnEnable();
        S_GameEvent.OnSuspicionChanged += HandleSuspicionChanged;
        S_GameEvent.OnArrestTriggered += HandleArrestTriggered;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        S_GameEvent.OnSuspicionChanged -= HandleSuspicionChanged;
        S_GameEvent.OnArrestTriggered -= HandleArrestTriggered;
    }

    private void Update()
    {
        if (!isActive || currentState == State.Disabled) return;
        UpdateStateMachine();
    }

    private void FixedUpdate()
    {
        if (!isActive || currentState == State.Disabled) return;
        ExecuteMovement();
    }

    private void UpdateStateMachine()
    {
        switch (currentState)
        {
            case State.Idle:
                // TODO: Idle logic
                break;
            case State.Patrol:
                // TODO: Waypoint patrol logic
                break;
            case State.Chase:
                // TODO: Chase player logic
                break;
            case State.Arrest:
                // TODO: Arrest behaviour (EMP + capture)
                break;
        }
    }

    private void ExecuteMovement()
    {
        switch (currentState)
        {
            case State.Patrol:
                // TODO: Physics-based patrol movement
                break;
            case State.Chase:
                // TODO: Physics-based chase movement
                break;
            case State.Arrest:
                // TODO: Physics-based arrest rush
                break;
        }
    }

    private void HandleSuspicionChanged(float value)
    {
        suspicionLevel = value;
        // TODO: Adjust patrol/chase parameters based on suspicion thresholds
    }

    private void HandleArrestTriggered()
    {
        currentState = State.Arrest;
        // TODO: Begin arrest sequence
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        Gizmos.color = Color.yellow;
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