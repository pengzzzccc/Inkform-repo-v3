using UnityEngine;

public class S_MoveBlock : MonoBehaviour
{
    [SerializeField] private GameObject side_1;
    [SerializeField] private GameObject side_2;
    [SerializeField] private GameObject block;
    [SerializeField] private GameObject trigger;
    [Header("move controll")]
    [SerializeField] private float MoveSpeed = 10f;

    private float moveSpeed;
    private CircleCollider2D b_collider;
    private bool isTriggered = false;

    void Awake()
    {
        b_collider = trigger.GetComponent<CircleCollider2D>();
        moveSpeed = MoveSpeed;
    }

    void Update()
    {
        if (isTriggered) MoveBlock();
        else MoveBack();
    }

    void MoveBlock()
    {
        float t = 1f - Mathf.Exp(-moveSpeed * Time.deltaTime);
        block.transform.position = Vector2.Lerp(block.transform.position, side_2.transform.position, t);
    }

    void MoveBack()
    {
        float t = 1f - Mathf.Exp(-moveSpeed * Time.deltaTime);
        block.transform.position = Vector2.Lerp(block.transform.position, side_1.transform.position, t);
    }

    public void SetTriggered(bool value)
    {
        isTriggered = value;
    }
}