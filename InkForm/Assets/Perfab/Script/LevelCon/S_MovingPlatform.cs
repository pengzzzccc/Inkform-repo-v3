using UnityEngine;

public enum PlatformState
{
    HiddenAtTop,
    Descending,
    VisibleAtBottom,
    Ascending
}

public enum MovementControlMode
{
    EventDriven,
    AutoLoop
}

public class S_MovingPlatform : MonoBehaviour
{
    [Header("Control Mode")]
    [SerializeField] private MovementControlMode controlMode = MovementControlMode.EventDriven;
    [SerializeField, Min(0f)] private float waitAtTop = 0.5f;
    [SerializeField, Min(0f)] private float waitAtBottom = 0.5f;
    [SerializeField] private bool autoStartsTowardBottom = true;

    [Header("Movement Settings")]
    [SerializeField] private Transform topPoint;
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private float moveSpeed = 2f;

    [Header("Claw Settings")]
    [SerializeField] private Animator clawAnimator;
    [SerializeField] private float clawOpenAngle = 30f;
    [SerializeField] private float clawCloseAngle = 5f;
    [SerializeField] private float clawTransitionTime = 0.5f;
    [SerializeField] private float clawDelay = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource motorSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip motorStartClip;
    [SerializeField] private AudioClip motorLoopClip;
    [SerializeField] private AudioClip motorStopClip;
    [SerializeField] private AudioClip landingClip;
    [SerializeField] private AudioClip clawOpenClip;
    [SerializeField] private AudioClip clawCloseClip;
    [SerializeField] private AudioClip ascendStartClip;

    [Header("Debug")]
    [SerializeField] private PlatformState currentState = PlatformState.HiddenAtTop;

    private bool playerOnPlatform = false;
    private Rigidbody2D playerRb;
    private Vector3 topWorldPos;
    private Vector3 bottomWorldPos;
    private Vector3 lastPosition;
    private float autoWaitTimer;

    void Start()
    {
        topWorldPos = topPoint != null ? topPoint.position : transform.position;
        bottomWorldPos = bottomPoint != null ? bottomPoint.position : transform.position;
        lastPosition = transform.position;
        autoWaitTimer = 0f;

        if (controlMode == MovementControlMode.AutoLoop)
            InitializeAutoLoopState();
    }

    void Update()
    {
        // Transfer platform delta movement to player (replaces SetParent approach)
        if (playerOnPlatform && playerRb != null)
        {
            Vector3 delta = transform.position - lastPosition;
            playerRb.position += new Vector2(delta.x, delta.y);
        }
        lastPosition = transform.position;

        switch (currentState)
        {
            case PlatformState.Descending:
                HandleDescending();
                break;
            case PlatformState.Ascending:
                HandleAscending();
                break;
        }

        if (controlMode == MovementControlMode.AutoLoop)
            HandleAutoLoop();
    }

    public void Reveal()
    {
        if (currentState != PlatformState.HiddenAtTop) return;
        currentState = PlatformState.Descending;
        autoWaitTimer = 0f;
        PlayMotorStart();
    }

    public void Hide()
    {
        if (currentState != PlatformState.VisibleAtBottom) return;
        currentState = PlatformState.Ascending;
        autoWaitTimer = 0f;
        PlaySfx(ascendStartClip);
        PlayMotorLoop();
    }

    public bool IsHidden() => currentState == PlatformState.HiddenAtTop;
    public bool IsVisible() => currentState == PlatformState.VisibleAtBottom;
    public bool IsMoving() => currentState == PlatformState.Descending || currentState == PlatformState.Ascending;

    public PlatformState GetCurrentState() => currentState;

    void HandleDescending()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            bottomWorldPos,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, bottomWorldPos) < 0.01f)
        {
            currentState = PlatformState.VisibleAtBottom;
            autoWaitTimer = 0f;

            StopMotor();
            PlaySfx(landingClip);
            Invoke(nameof(TriggerClawOpen), clawDelay);
        }
    }

    void HandleAscending()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            topWorldPos,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, topWorldPos) < 0.01f)
        {
            currentState = PlatformState.HiddenAtTop;
            autoWaitTimer = 0f;
            StopMotor();
        }
    }

    private void InitializeAutoLoopState()
    {
        // Auto mode keeps the scene-authored platform position and chooses only
        // the first direction of travel.
        currentState = autoStartsTowardBottom
            ? PlatformState.HiddenAtTop
            : PlatformState.VisibleAtBottom;
    }

    private void HandleAutoLoop()
    {
        if (currentState == PlatformState.HiddenAtTop)
        {
            autoWaitTimer += Time.deltaTime;
            if (autoWaitTimer >= waitAtTop)
                Reveal();
        }
        else if (currentState == PlatformState.VisibleAtBottom)
        {
            autoWaitTimer += Time.deltaTime;
            if (autoWaitTimer >= waitAtBottom)
                Hide();
        }
    }

    void TriggerClawOpen()
    {
        if (clawAnimator != null)
            clawAnimator.SetTrigger("Open");
        PlaySfx(clawOpenClip);
        Invoke(nameof(TriggerClawClose), clawTransitionTime);
    }

    void TriggerClawClose()
    {
        if (clawAnimator != null)
            clawAnimator.SetTrigger("Close");
        PlaySfx(clawCloseClip);
    }

    void PlayMotorStart()
    {
        if (motorSource == null) return;
        if (motorStartClip != null)
        {
            motorSource.PlayOneShot(motorStartClip);
            Invoke(nameof(PlayMotorLoop), motorStartClip.length);
        }
        else
        {
            PlayMotorLoop();
        }
    }

    void PlayMotorLoop()
    {
        if (motorSource == null) return;
        if (motorLoopClip != null)
        {
            motorSource.clip = motorLoopClip;
            motorSource.loop = true;
            motorSource.Play();
        }
    }

    void StopMotor()
    {
        if (motorSource == null) return;
        motorSource.Stop();
        motorSource.loop = false;
        if (motorStopClip != null)
            motorSource.PlayOneShot(motorStopClip);
    }

    void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            playerOnPlatform = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerRb = null;
            playerOnPlatform = false;
        }
    }
}
