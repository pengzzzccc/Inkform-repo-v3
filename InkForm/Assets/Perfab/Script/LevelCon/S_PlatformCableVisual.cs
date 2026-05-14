using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(100)]
public class S_PlatformCableVisual : MonoBehaviour
{
    [Header("Cable Anchors")]
    [SerializeField] private Transform topAnchor;
    [SerializeField] private Transform platformAttachPoint;
    [SerializeField] private bool cacheTopAnchorOnStart = false;

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
    private float cachedTopAnchorY;
    private bool hasCachedTopAnchor;

    void Reset()
    {
        platformAttachPoint = transform;
        EnsureCableRenderers();
        CacheTopAnchor();
        UpdateCable();
    }

    void Awake()
    {
        EnsureCableRenderers();
    }

    void OnEnable()
    {
        EnsureCableRenderers();
        CacheTopAnchor();
        UpdateCable();
    }

    void Start()
    {
        CacheTopAnchor();
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
        ConfigureLineRenderer(cableLeft);
        ConfigureLineRenderer(cableRight);

        if (!Application.isPlaying)
        {
            CacheTopAnchor();
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

        if (cableLeft == null || cableLeftTransform == null)
        {
            cableLeftTransform = CreateOrGetChild("CableLeft");
            cableLeft = cableLeftTransform.GetComponent<LineRenderer>();
            if (cableLeft == null)
                cableLeft = cableLeftTransform.gameObject.AddComponent<LineRenderer>();
        }

        if (cableRight == null || cableRightTransform == null)
        {
            cableRightTransform = CreateOrGetChild("CableRight");
            cableRight = cableRightTransform.GetComponent<LineRenderer>();
            if (cableRight == null)
                cableRight = cableRightTransform.gameObject.AddComponent<LineRenderer>();
        }

        ConfigureLineRenderer(cableLeft);
        ConfigureLineRenderer(cableRight);
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

    private void CacheTopAnchor()
    {
        if (!cacheTopAnchorOnStart || topAnchor == null)
            return;

        cachedTopAnchorY = topAnchor.position.y;
        hasCachedTopAnchor = true;
    }

    private float GetTopAnchorY()
    {
        if (cacheTopAnchorOnStart && hasCachedTopAnchor)
            return cachedTopAnchorY;

        return topAnchor != null ? topAnchor.position.y : transform.position.y;
    }

    private void UpdateCable()
    {
        Vector3 bottomPos = GetAttachPosition();
        float topY = GetTopAnchorY();
        float centerX = bottomPos.x;

        // Left cable
        if (cableLeft != null)
        {
            cableLeft.SetPosition(0, new Vector3(centerX - cableOffset, topY, 0f));
            cableLeft.SetPosition(1, new Vector3(centerX - cableOffset, bottomPos.y, 0f));
        }

        // Right cable
        if (cableRight != null)
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

        // Left cable gizmo
        Gizmos.DrawLine(topLeft, bottomLeft);
        Gizmos.DrawWireSphere(topLeft, 0.08f);
        Gizmos.DrawWireSphere(bottomLeft, 0.08f);

        // Right cable gizmo
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawWireSphere(topRight, 0.08f);
        Gizmos.DrawWireSphere(bottomRight, 0.08f);
    }
}