using UnityEngine;

/// <summary>
/// Procedurally renders the grappling tentacle as a tapered, slightly curved mesh ribbon
/// from the player's body to the hook anchor. Self-contained: builds its own child
/// MeshFilter/MeshRenderer and writes world-space vertices each tick. Kept separate from
/// S_PlayerProceduralRenderer (the slime body) so that renderer is untouched.
/// </summary>
[DisallowMultipleComponent]
public class S_HookTentacleRenderer : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField, Range(2, 48)] private int segments = 16;
    [SerializeField, Min(0.01f)] private float baseWidth = 0.16f;
    [SerializeField, Min(0.01f)] private float tipWidth = 0.04f;
    [Tooltip("How much the rope curves/sags. Scaled down as the rope tightens.")]
    [SerializeField, Min(0f)] private float slack = 0.5f;
    [Tooltip("Extra wobble amplitude scaled by swing speed.")]
    [SerializeField, Min(0f)] private float wobbleAmount = 0.06f;
    [SerializeField, Min(0f)] private float wobbleSpeed = 14f;

    [Header("Color")]
    [SerializeField] private Color tentacleColor = Color.black;

    private const string MeshObjectName = "HookTentacle";

    private Transform meshObject;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private Material material;

    private Vector3[] vertices;
    private Color[] colors;
    private Vector2[] uvs;
    private int[] triangles;
    private int cachedSegments = -1;
    private float wobblePhase;

    public void Initialize(Transform parent, SpriteRenderer sortingReference)
    {
        if (meshObject == null)
        {
            GameObject child = new GameObject(MeshObjectName);
            child.transform.SetParent(parent != null ? parent : transform, false);
            meshObject = child.transform;
            meshObject.localPosition = Vector3.zero;
            meshObject.localRotation = Quaternion.identity;
            meshObject.localScale = Vector3.one;

            meshFilter = child.AddComponent<MeshFilter>();
            meshRenderer = child.AddComponent<MeshRenderer>();

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            material = new Material(shader) { name = "HookTentacleMat", hideFlags = HideFlags.DontSave };
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);
            meshRenderer.sharedMaterial = material;

            if (sortingReference != null)
            {
                // Render behind the slime body so the tentacle looks attached underneath.
                meshRenderer.sortingLayerID = sortingReference.sortingLayerID;
                meshRenderer.sortingOrder = sortingReference.sortingOrder - 1;
            }

            mesh = new Mesh { name = "HookTentacleMesh" };
            mesh.MarkDynamic();
            meshFilter.sharedMesh = mesh;
        }

        SetActive(false);
    }

    public void SetActive(bool active)
    {
        if (meshObject != null)
            meshObject.gameObject.SetActive(active);
    }

    /// <summary>Rebuild the ribbon from the player position to the hook position (world space).</summary>
    public void RenderTick(Vector2 fromPlayer, Vector2 toHook, float swingSpeed)
    {
        if (mesh == null || meshObject == null)
            return;

        EnsureBuffers();

        // The mesh object is parented under the body; write vertices in its local space.
        Vector3 from = meshObject.InverseTransformPoint(fromPlayer);
        Vector3 to = meshObject.InverseTransformPoint(toHook);

        Vector3 axis = to - from;
        float length = axis.magnitude;
        if (length < 0.0001f)
            return;

        Vector3 dir = axis / length;
        Vector3 normal = new Vector3(-dir.y, dir.x, 0f);

        // Sag bends toward "down" relative to the rope; tightens (less slack) at high tension.
        float tension = Mathf.Clamp01(length / Mathf.Max(0.01f, slack * 8f));
        float sagAmount = slack * (1f - tension);
        float wobble = wobbleAmount * Mathf.Clamp01(Mathf.Abs(swingSpeed) / 12f);
        wobblePhase += wobbleSpeed * Time.deltaTime;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 center = Vector3.Lerp(from, to, t);

            // Parabolic sag (0 at both ends, max in the middle) plus a little travelling wobble.
            float curve = Mathf.Sin(t * Mathf.PI);
            float offset = -sagAmount * curve + wobble * curve * Mathf.Sin(wobblePhase + t * 6f);
            center += normal * offset;

            float width = Mathf.Lerp(baseWidth, tipWidth, t) * 0.5f;
            vertices[i * 2] = center - normal * width;
            vertices[i * 2 + 1] = center + normal * width;
        }

        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private void EnsureBuffers()
    {
        if (cachedSegments == segments && vertices != null)
            return;

        cachedSegments = segments;
        int vertCount = (segments + 1) * 2;
        vertices = new Vector3[vertCount];
        colors = new Color[vertCount];
        uvs = new Vector2[vertCount];
        triangles = new int[segments * 6];

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            colors[i * 2] = tentacleColor;
            colors[i * 2 + 1] = tentacleColor;
            uvs[i * 2] = new Vector2(0f, t);
            uvs[i * 2 + 1] = new Vector2(1f, t);
        }

        for (int i = 0; i < segments; i++)
        {
            int v = i * 2;
            int ti = i * 6;
            triangles[ti] = v;
            triangles[ti + 1] = v + 2;
            triangles[ti + 2] = v + 1;
            triangles[ti + 3] = v + 1;
            triangles[ti + 4] = v + 2;
            triangles[ti + 5] = v + 3;
        }

        mesh.Clear();
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
        if (mesh != null)
            Destroy(mesh);
    }
}
