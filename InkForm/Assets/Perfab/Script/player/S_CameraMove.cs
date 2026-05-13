using UnityEngine;

public class S_CameraMove : MonoBehaviour
{
    [SerializeField] private GameObject target;
    [SerializeField] private float minMoveSpeed = 50f;
    [SerializeField] private float manualMoveSpeed = 8f;
    [SerializeField] private float manualMaxDistanceFromTarget = 8f;
    [SerializeField] private float returnSmoothSpeed = 12f;
    [SerializeField, Min(0f)] private float followDeadZoneRadius = 1.5f;
    [SerializeField] private bool drawDeadZoneGizmo = true;

    private float speed;
    private Vector3 targetPos;
    private bool manualControlActive = false;

    void Start()
    {
        if (target != null)
            transform.position = new Vector3(target.transform.position.x, target.transform.position.y, transform.position.z);

        speed = minMoveSpeed;
    }

    void Update()
    {
        if (target == null)
            return;

        if (manualControlActive)
            return;

        speed = returnSmoothSpeed > 0f ? returnSmoothSpeed : minMoveSpeed;

        targetPos = new Vector3(
            target.transform.position.x,
            target.transform.position.y,
            transform.position.z
        );

        Vector2 cameraToTarget = targetPos - transform.position;
        if (cameraToTarget.magnitude <= followDeadZoneRadius)
            return;

        float t = 1f - Mathf.Exp(-speed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPos, t);


    }

    public void BeginManualControl()
    {
        manualControlActive = true;
    }

    public void ManualControlTick(Vector2 moveInput)
    {
        if (!manualControlActive || target == null)
            return;

        Vector3 delta = new Vector3(moveInput.x, moveInput.y, 0f);
        if (delta.sqrMagnitude > 1f)
            delta.Normalize();

        transform.position += delta * (manualMoveSpeed * Time.unscaledDeltaTime);

        Vector3 targetCenter = new Vector3(target.transform.position.x, target.transform.position.y, transform.position.z);
        Vector3 offset = transform.position - targetCenter;
        offset.z = 0f;

        if (offset.magnitude > manualMaxDistanceFromTarget)
            transform.position = targetCenter + offset.normalized * manualMaxDistanceFromTarget;
    }

    public void EndManualControl()
    {
        manualControlActive = false;
        speed = returnSmoothSpeed > 0f ? returnSmoothSpeed : minMoveSpeed;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDeadZoneGizmo)
            return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, followDeadZoneRadius);
    }
}
