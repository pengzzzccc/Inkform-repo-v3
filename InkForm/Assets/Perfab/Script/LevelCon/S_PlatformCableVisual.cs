using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(100)]
public class S_PlatformCableVisual : MonoBehaviour
{
    public enum CableSideMode
    {
        Both = 0,
        None = 1,
        Left = 2,
        Right = 3
    }

    [Header("Cable Anchors")]
    [SerializeField] private Transform topAnchor;
    [SerializeField] private Transform platformAttachPoint;

    [Header("Cable Generation")]
    [SerializeField] private CableSideMode cableSideMode = CableSideMode.Both;

    [Header("Cable Layout")]
    [SerializeField, Min(0f)] private float cableOffset = 0.3f;

    [Header("Cable Appearance")]
    [SerializeField, Min(0.001f)] private float cableWidth = 0.05f;
    [SerializeField] private Material cableMaterial;
    [SerializeField] private string sortingLayerName = "";
    [SerializeField] private int sortingOrder = 4;
    [SerializeField] private Color cableColor = new Color(0.55f, 0.58f, 0.6f, 1f);
    [SerializeField] private bool drawGizmos = true;

    private static Material fallbackCableMaterial;
    private LineRenderer cableLeft;
    private LineRenderer cableRight;
    private Transform cableLeftTransform;
    private Transform cableRightTransform;

    void Reset()
    {
        platformAttachPoint = transform;
        EnsureCableRenderers();
        UpdateCable();
    }

    void Awake()
    {
        EnsureCableRenderers();
    }

    void OnEnable()
    {
        EnsureCableRenderers();
        UpdateCable();
    }

    void Start()
    {
        UpdateCable();
    }

    void LateUpdate()
    {
        UpdateCable();
    }

    void OnValidate()
    {
        cableWidth = Mathf.Max(0.001f, cableWidth);
        cableOffset = Mathf.Max(0f, cableOffset);
        EnsureCableRenderers();

        if (!Application.isPlaying)
        {
            UpdateCable();
        }
    }

    private void EnsureCableRenderers()
    {
        // Remove legacy single LineRenderer on main object
        LineRenderer legacy = GetComponent<LineRenderer>();
        if (legacy != null)
        {
            if (Application.isPlaying)
                Destroy(legacy);
            else
                DestroyImmediate(legacy);
        }

        EnsureCableRenderer(ref cableLeftTransform, ref cableLeft, "CableLeft", ShouldRenderLeft());
        EnsureCableRenderer(ref cableRightTransform, ref cableRight, "CableRight", ShouldRenderRight());
    }

    private void EnsureCableRenderer(ref Transform cableTransform, ref LineRenderer cable, string childName, bool shouldRender)
    {
        if (cableTransform == null)
            cableTransform = transform.Find(childName);

        if (shouldRender)
        {
            if (cableTransform == null)
                cableTransform = CreateOrGetChild(childName);

            if (cable == null)
                cable = cableTransform.GetComponent<LineRenderer>();

            if (cable == null)
                cable = cableTransform.gameObject.AddComponent<LineRenderer>();

            ConfigureLineRenderer(cable);
            cable.enabled = true;
            return;
        }

        if (cable == null && cableTransform != null)
            cable = cableTransform.GetComponent<LineRenderer>();

        if (cable != null)
            cable.enabled = false;
    }

    private Transform CreateOrGetChild(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            child = go.transform;
        }
        return child;
    }

    private void ConfigureLineRenderer(LineRenderer lr)
    {
        if (lr == null)
            return;

        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = cableWidth;
        lr.endWidth = cableWidth;
        lr.startColor = cableColor;
        lr.endColor = cableColor;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 2;
        lr.textureMode = LineTextureMode.Stretch;
        lr.alignment = LineAlignment.View;
        lr.sortingOrder = sortingOrder;

        if (!string.IsNullOrEmpty(sortingLayerName))
            lr.sortingLayerName = sortingLayerName;

        lr.sharedMaterial = cableMaterial != null ? cableMaterial : GetFallbackMaterial();
    }

    private bool ShouldRenderLeft()
    {
        return cableSideMode == CableSideMode.Both || cableSideMode == CableSideMode.Left;
    }

    private bool ShouldRenderRight()
    {
        return cableSideMode == CableSideMode.Both || cableSideMode == CableSideMode.Right;
    }

    private float GetTopAnchorY()
    {
        return topAnchor != null ? topAnchor.position.y : transform.position.y;
    }

    private void UpdateCable()
    {
        Vector3 bottomPos = GetAttachPosition();
        float topY = GetTopAnchorY();
        float centerX = bottomPos.x;

        if (ShouldRenderLeft() && cableLeft != null && cableLeft.enabled)
        {
            cableLeft.SetPosition(0, new Vector3(centerX - cableOffset, topY, 0f));
            cableLeft.SetPosition(1, new Vector3(centerX - cableOffset, bottomPos.y, 0f));
        }

        if (ShouldRenderRight() && cableRight != null && cableRight.enabled)
        {
            cableRight.SetPosition(0, new Vector3(centerX + cableOffset, topY, 0f));
            cableRight.SetPosition(1, new Vector3(centerX + cableOffset, bottomPos.y, 0f));
        }
    }

    private Vector3 GetAttachPosition()
    {
        return platformAttachPoint != null ? platformAttachPoint.position : transform.position;
    }

    private static Material GetFallbackMaterial()
    {
        if (fallbackCableMaterial != null)
            return fallbackCableMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        fallbackCableMaterial = new Material(shader);
        fallbackCableMaterial.name = "Generated Platform Cable Material";
        return fallbackCableMaterial;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Vector3 bottomPos = GetAttachPosition();
        float topY = GetTopAnchorY();
        float centerX = bottomPos.x;

        Vector3 topLeft = new Vector3(centerX - cableOffset, topY, 0f);
        Vector3 bottomLeft = new Vector3(centerX - cableOffset, bottomPos.y, 0f);
        Vector3 topRight = new Vector3(centerX + cableOffset, topY, 0f);
        Vector3 bottomRight = new Vector3(centerX + cableOffset, bottomPos.y, 0f);

        Gizmos.color = cableColor;

        if (ShouldRenderLeft())
        {
            Gizmos.DrawLine(topLeft, bottomLeft);
            Gizmos.DrawWireSphere(topLeft, 0.08f);
            Gizmos.DrawWireSphere(bottomLeft, 0.08f);
        }

        if (ShouldRenderRight())
        {
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawWireSphere(topRight, 0.08f);
            Gizmos.DrawWireSphere(bottomRight, 0.08f);
        }
    }
}
