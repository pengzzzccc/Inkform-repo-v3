using UnityEngine;

public class S_CameraMove : MonoBehaviour
{
    [SerializeField] private GameObject target;
    [SerializeField] private float minMoveSpeed = 50f;

    private float speed;

    void Start()
    {
        transform.position = target.transform.position;
        speed = minMoveSpeed;
    }

    void Update()
    {
        if (target == null) return;

        Vector3 targetPos = new Vector3(
            target.transform.position.x,
            target.transform.position.y,
            transform.position.z
        );

        float t = 1f - Mathf.Exp(-speed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPos, t);
    }
}