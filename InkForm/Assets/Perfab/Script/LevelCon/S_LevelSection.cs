using UnityEngine;

public class S_LevelSection : MonoBehaviour
{
    [SerializeField] private int sectionIndex = 0;

    [Header("Section Movement")]
    [SerializeField] private Transform sectionTopPoint;
    [SerializeField] private Transform sectionBottomPoint;
    [SerializeField] private float sectionMoveSpeed = 3f;
    [SerializeField, Min(1f)] private float descentEasePower = 3f;

    private Vector3 topWorldPos;
    private Vector3 bottomWorldPos;
    private bool isRevealed = false;
    private bool isCompleted = false;
    private bool isMoving = false;
    private bool initialized = false;
    private Vector3 moveTarget;
    private Vector3 moveStartPos;
    private float moveElapsed;
    private float moveDuration;
    private bool isDescending;

    void Start()
    {
        float topY = sectionTopPoint != null ? sectionTopPoint.position.y : transform.position.y;
        float bottomY = sectionBottomPoint != null ? sectionBottomPoint.position.y : transform.position.y;
        topWorldPos = new Vector3(transform.position.x, topY, transform.position.z);
        bottomWorldPos = new Vector3(transform.position.x, bottomY, transform.position.z);
        transform.position = topWorldPos;
        initialized = true;
    }

    void Update()
    {
        if (!isMoving) return;

        if (isDescending)
        {
            HandleDescending();
            return;
        }

        HandleAscending();
    }

    void HandleDescending()
    {
        moveElapsed += Time.deltaTime;
        float t = moveDuration > 0f ? Mathf.Clamp01(moveElapsed / moveDuration) : 1f;
        float easedT = 1f - Mathf.Pow(1f - t, descentEasePower);

        transform.position = Vector3.LerpUnclamped(moveStartPos, moveTarget, easedT);

        if (t >= 1f || Mathf.Abs(transform.position.y - moveTarget.y) < 0.01f)
        {
            transform.position = moveTarget;
            isMoving = false;
            isDescending = false;
            S_GameEvent.SectionDescentCompleted(sectionIndex);
        }
    }

    void HandleAscending()
    {
        Vector3 current = transform.position;
        float newY = Mathf.MoveTowards(current.y, moveTarget.y, sectionMoveSpeed * Time.deltaTime);
        transform.position = new Vector3(current.x, newY, current.z);

        if (Mathf.Abs(transform.position.y - moveTarget.y) < 0.01f)
        {
            transform.position = moveTarget;
            isMoving = false;
        }
    }

    public int GetSectionIndex() => sectionIndex;
    public bool IsRevealed() => isRevealed;
    public bool IsCompleted() => isCompleted;
    public bool IsMoving() => isMoving;

    public void RevealSection()
    {
        if (!initialized) return;
        if (isRevealed) return;
        isRevealed = true;
        moveStartPos = transform.position;
        moveTarget = bottomWorldPos;
        moveElapsed = 0f;
        moveDuration = CalculateMoveDuration(moveStartPos, moveTarget);
        isDescending = true;
        isMoving = true;
        S_GameEvent.SectionDescentStarted(sectionIndex);
    }

    public void HideSection()
    {
        if (!initialized) return;
        isRevealed = false;
        moveTarget = topWorldPos;
        isDescending = false;
        isMoving = true;
    }

    public void MarkCompleted()
    {
        isCompleted = true;
    }

    private float CalculateMoveDuration(Vector3 start, Vector3 target)
    {
        float distance = Vector3.Distance(start, target);
        if (distance <= 0.01f)
            return 0f;

        return distance / Mathf.Max(0.01f, sectionMoveSpeed);
    }
}
