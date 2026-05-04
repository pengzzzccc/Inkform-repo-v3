using UnityEngine;

public class S_LevelSection : MonoBehaviour
{
    [SerializeField] private int sectionIndex = 0;

    [Header("Section Movement")]
    [SerializeField] private Transform sectionTopPoint;
    [SerializeField] private Transform sectionBottomPoint;
    [SerializeField] private float sectionMoveSpeed = 3f;

    private Vector3 topWorldPos;
    private Vector3 bottomWorldPos;
    private bool isRevealed = false;
    private bool isCompleted = false;
    private bool isMoving = false;
    private bool initialized = false;
    private Vector3 moveTarget;

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
        moveTarget = bottomWorldPos;
        isMoving = true;
    }

    public void HideSection()
    {
        if (!initialized) return;
        isRevealed = false;
        moveTarget = topWorldPos;
        isMoving = true;
    }

    public void MarkCompleted()
    {
        isCompleted = true;
    }
}