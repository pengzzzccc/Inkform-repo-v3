using UnityEngine;

[DisallowMultipleComponent]
public class S_PlayerProceduralRenderer : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private bool useProceduralRendering = true;
    [SerializeField] private bool hideSpriteRenderer = true;
    [SerializeField] private bool matchCircleColliderRadius = true;
    [SerializeField][Range(12, 96)] private int meshResolution = 64;

    [Header("Shape")]
    [SerializeField][Range(0.1f, 2f)] private float bodyRadius = 0.5f;
    [SerializeField][Range(0f, 0.2f)] private float outlineWidth = 0.055f;
    [SerializeField][Range(0f, 0.2f)] private float wobbleAmount = 0.025f;
    [SerializeField][Range(0f, 20f)] private float wobbleSpeed = 7f;
    [SerializeField][Range(0f, 0.12f)] private float velocityStretch = 0.035f;
    [SerializeField][Range(0f, 0.5f)] private float maxStretch = 0.28f;
    [SerializeField][Range(0f, 0.5f)] private float impactSquash = 0.18f;
    [SerializeField][Range(0f, 0.35f)] private float surfaceStickDeform = 0.12f;
    [SerializeField][Range(0f, 0.35f)] private float idleSquash = 0.08f;
    [SerializeField][Range(0f, 0.45f)] private float bottomBulge = 0.22f;
    [SerializeField][Range(0f, 0.15f)] private float motionLag = 0.012f;
    [SerializeField][Range(0f, 0.2f)] private float maxTailStretch = 0.08f;
    [SerializeField][Range(0f, 1f)] private float bodyTailInfluence = 0.22f;
    [SerializeField] private bool useCircleTail = true;
    [SerializeField][Range(8, 32)] private int tailResolution = 18;
    [SerializeField][Range(0.05f, 0.6f)] private float tailRadiusScale = 0.3f;
    [SerializeField][Range(0.02f, 0.5f)] private float tailMinRadiusScale = 0.16f;
    [SerializeField][Range(0f, 0.6f)] private float tailBaseDistance = 0.16f;
    [SerializeField][Range(0f, 0.8f)] private float tailMaxDistance = 0.42f;
    [SerializeField][Range(0f, 0.12f)] private float tailSpeedDistanceScale = 0.035f;
    [SerializeField][Range(0f, 0.15f)] private float tailBridgeOverlap = 0.04f;
    [SerializeField][Range(1f, 30f)] private float tailFollowSpeed = 14f;
    [SerializeField][Range(0f, 1f)] private float tailGroundStick = 0.72f;
    [SerializeField][Range(0f, 0.08f)] private float tailGroundSkin = 0.014f;
    [SerializeField][Range(0f, 0.2f)] private float tailGroundMemoryTime = 0.08f;
    [SerializeField][Range(1f, 30f)] private float tailGroundBlendSpeed = 16f;
    [SerializeField][Range(0f, 0.35f)] private float contactFlatten = 0.16f;
    [SerializeField][Range(0f, 1f)] private float edgeSmoothStrength = 0.35f;
    [SerializeField][Range(0f, 1f)] private float colliderShapeFollow = 0.65f;
    [SerializeField][Range(1f, 30f)] private float colliderShapeFollowSpeed = 14f;

    [Header("Environment Fit")]
    [SerializeField] private bool fitToContactPlanes = true;
    [SerializeField][Range(0f, 0.08f)] private float contactPlaneSkin = 0.012f;
    [SerializeField] private bool drawContactFill = false;
    [SerializeField][Range(0f, 0.12f)] private float contactFillDepth = 0.035f;
    [SerializeField][Range(0.2f, 2f)] private float contactFillWidthScale = 1.15f;
    [SerializeField][Range(0f, 0.08f)] private float contactFillBodyOverlap = 0.025f;
    [SerializeField][Range(0f, 0.45f)] private float contactSpreadStrength = 0.18f;
    [SerializeField][Range(0f, 0.35f)] private float contactCompressionStrength = 0.14f;
    [SerializeField][Range(0f, 0.45f)] private float contactTriangleStrength = 0.18f;
    [SerializeField][Range(0f, 0.45f)] private float contactShoulderBulge = 0.16f;
    [SerializeField][Range(0f, 0.3f)] private float contactApexTaper = 0.08f;
    [SerializeField][Range(0.1f, 2f)] private float contactInfluenceRadius = 1f;
    [SerializeField] private bool drawContactFitGizmos = true;
    [SerializeField] private Color contactPlaneGizmoColor = new Color(1f, 0.35f, 0.1f, 0.85f);

    [Header("Colors")]
    [SerializeField] private Color fluidCenterColor = Color.black;
    [SerializeField] private Color fluidEdgeColor = Color.black;
    [SerializeField] private Color solidCenterColor = Color.black;
    [SerializeField] private Color solidEdgeColor = Color.black;
    [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private Color highlightColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color paralyzedTint = new Color(0.62f, 0.42f, 1f, 1f);

    [Header("Eyes")]
    [SerializeField] private bool drawEyes = true;
    [SerializeField][Range(8, 32)] private int eyeResolution = 18;
    [SerializeField][Range(0f, 1f)] private float eyeSpacing = 0.35f;
    [SerializeField][Range(-1f, 1f)] private float eyeHorizontalOffset = 0.14f;
    [SerializeField][Range(-1f, 1f)] private float eyeVerticalOffset = 0.18f;
    [SerializeField][Range(0.02f, 0.35f)] private float eyeRadius = 0.095f;
    [SerializeField][Range(0.5f, 2.5f)] private float eyeTallness = 1.35f;
    [SerializeField][Range(0f, 0.08f)] private float eyeFollowVelocity = 0.02f;
    [SerializeField] private Color eyeColor = Color.white;
    [SerializeField] private Color eyeGlowColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField][Range(1f, 5f)] private float eyeGlowScale = 2.4f;
    [SerializeField][Range(0f, 0.5f)] private float eyeGlowPulse = 0.12f;

    [Header("Gizmos")]
    [SerializeField] private bool drawRendererGizmos = true;
    [SerializeField] private Color rendererGizmoColor = new Color(0.2f, 1f, 0.8f, 0.55f);

    private const string OutlineName = "ProceduralSlime_Outline";
    private const string BodyName = "ProceduralSlime_Body";
    private const string TailName = "ProceduralSlime_Tail";
    private const string ContactFillName = "ProceduralSlime_ContactFill";
    private const string HighlightName = "ProceduralSlime_Highlight";
    private const string EyeGlowName = "ProceduralSlime_EyeGlow";
    private const string EyeName = "ProceduralSlime_Eyes";

    private SpriteRenderer fallbackSprite;
    private Rigidbody2D targetRigidbody;
    private Collider2D targetCollider;
    private CircleCollider2D targetCircleCollider;
    private CapsuleCollider2D targetCapsuleCollider;

    private Mesh outlineMesh;
    private Mesh bodyMesh;
    private Mesh tailMesh;
    private Mesh contactFillMesh;
    private Mesh highlightMesh;
    private Mesh eyeGlowMesh;
    private Mesh eyeMesh;
    private MeshRenderer outlineRenderer;
    private MeshRenderer bodyRenderer;
    private MeshRenderer tailRenderer;
    private MeshRenderer contactFillRenderer;
    private MeshRenderer highlightRenderer;
    private MeshRenderer eyeGlowRenderer;
    private MeshRenderer eyeRenderer;
    private MeshFilter outlineFilter;
    private MeshFilter bodyFilter;
    private MeshFilter tailFilter;
    private MeshFilter contactFillFilter;
    private MeshFilter highlightFilter;
    private MeshFilter eyeGlowFilter;
    private MeshFilter eyeFilter;
    private Material outlineMaterial;
    private Material bodyMaterial;
    private Material tailMaterial;
    private Material contactFillMaterial;
    private Material highlightMaterial;
    private Material eyeGlowMaterial;
    private Material eyeMaterial;

    private Vector3[] outlineVertices;
    private Vector3[] bodyVertices;
    private Vector3[] tailVertices;
    private Vector3[] contactFillVertices;
    private Vector3[] smoothedBodyVertices;
    private Vector3[] highlightVertices;
    private Vector3[] eyeGlowVertices;
    private Vector3[] eyeVertices;
    private Color[] outlineColors;
    private Color[] bodyColors;
    private Color[] tailColors;
    private Color[] contactFillColors;
    private Color[] highlightColors;
    private Color[] eyeGlowColors;
    private Color[] eyeColors;
    private int[] bodyTriangles;
    private int[] tailTriangles;
    private int[] contactFillTriangles;
    private int[] highlightTriangles;
    private int[] eyeTriangles;
    private float[] wobbleOffsets;
    private ContactPoint2D[] environmentContacts;
    private ContactPlane[] contactPlanes;
    private ContactPlane cachedTailGroundPlane;
    private int cachedResolution;
    private int cachedTailResolution;
    private int cachedHighlightResolution;
    private int cachedEyeResolution;
    private int contactPlaneCount;
    private float tailGroundMemoryTimer;
    private float tailGroundBlend;
    private Vector2 currentTailCenter;
    private float currentTailRadius;
    private float currentTailBlend;
    private Vector2 currentColliderShapeScale = Vector2.one;
    private bool initialized;
    private Vector2 previousVelocity;
    private float impactPulse;

    private struct ContactPlane
    {
        public Vector2 point;
        public Vector2 normal;
    }

    public bool IsRenderingEnabled => useProceduralRendering && initialized;

    public void Initialize(SpriteRenderer spriteRenderer, Rigidbody2D rig, Collider2D col)
    {
        fallbackSprite = spriteRenderer;
        targetRigidbody = rig;
        SetTargetCollider(col);

        EnsureRenderObjects();
        SyncSorting();
        RebuildMeshBuffers();
        SetRenderersActive(useProceduralRendering);
        SetFallbackSpriteVisible(!useProceduralRendering || !hideSpriteRenderer);
        initialized = true;
    }

    public void RenderTick(S_Player player, S_fluid_climb climbSkill)
    {
        if (!useProceduralRendering)
        {
            SetRenderersActive(false);
            SetFallbackSpriteVisible(true);
            return;
        }

        if (!initialized)
            Initialize(GetComponent<SpriteRenderer>(), GetComponent<Rigidbody2D>(), GetComponent<Collider2D>());

        if (meshResolution != cachedResolution || tailResolution != cachedTailResolution || eyeResolution != cachedEyeResolution)
            RebuildMeshBuffers();

        SetRenderersActive(true);
        SetFallbackSpriteVisible(!hideSpriteRenderer);
        SyncSorting();

        Rigidbody2D rig = player != null ? player.GetRigidbody() : targetRigidbody;
        SetTargetCollider(player != null ? player.GetCollider() : targetCollider);

        Vector2 velocity = rig != null ? rig.linearVelocity : Vector2.zero;
        bool isFluid = player == null || player.getForm();
        bool isParalyzed = player != null && player.IsParalyzed;
        S_fluid_climb.SurfaceType surface = climbSkill != null ? climbSkill.GetSurface() : S_fluid_climb.SurfaceType.None;
        Vector2 surfaceNormal = climbSkill != null ? climbSkill.GetSurfNormal() : Vector2.zero;
        bool facingRight = player == null || player.GetFaceRight();

        UpdateImpactPulse(velocity, surface);
        UpdateMeshes(velocity, surface, surfaceNormal, isFluid, isParalyzed, facingRight);
        previousVelocity = velocity;
    }

    public void DrawRendererGizmos()
    {
        if (!drawRendererGizmos)
            return;

        float radius = GetBodyRadius();
        Gizmos.color = rendererGizmoColor;
        Gizmos.DrawWireSphere(transform.position, radius);

        int sampleCount = Mathf.Clamp(meshResolution, 12, 96);
        for (int i = 0; i < sampleCount; i += Mathf.Max(1, sampleCount / 12))
        {
            float angle = Mathf.PI * 2f * i / sampleCount;
            Vector3 point = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            Gizmos.DrawWireSphere(point, radius * 0.035f);
        }

        if (drawContactFitGizmos && contactPlanes != null)
        {
            Gizmos.color = contactPlaneGizmoColor;
            for (int i = 0; i < contactPlaneCount; i++)
            {
                Vector3 point = contactPlanes[i].point;
                Vector3 normal = contactPlanes[i].normal;
                Vector3 tangent = new Vector3(-normal.y, normal.x, 0f);
                Gizmos.DrawLine(point - tangent * radius * 0.55f, point + tangent * radius * 0.55f);
                Gizmos.DrawLine(point, point + normal * radius * 0.35f);
                Gizmos.DrawWireSphere(point, radius * 0.04f);
            }
        }
    }

    private void OnDisable()
    {
        SetRenderersActive(false);
        SetFallbackSpriteVisible(true);
    }

    private void OnDestroy()
    {
        DestroyRuntimeObject(outlineMesh);
        DestroyRuntimeObject(bodyMesh);
        DestroyRuntimeObject(tailMesh);
        DestroyRuntimeObject(contactFillMesh);
        DestroyRuntimeObject(highlightMesh);
        DestroyRuntimeObject(eyeGlowMesh);
        DestroyRuntimeObject(eyeMesh);
        DestroyRuntimeObject(outlineMaterial);
        DestroyRuntimeObject(bodyMaterial);
        DestroyRuntimeObject(tailMaterial);
        DestroyRuntimeObject(contactFillMaterial);
        DestroyRuntimeObject(highlightMaterial);
        DestroyRuntimeObject(eyeGlowMaterial);
        DestroyRuntimeObject(eyeMaterial);
    }

    private void OnDrawGizmosSelected()
    {
        DrawRendererGizmos();
    }

    private void OnValidate()
    {
        meshResolution = Mathf.Clamp(meshResolution, 12, 96);
        tailResolution = Mathf.Clamp(tailResolution, 8, 32);
        eyeResolution = Mathf.Clamp(eyeResolution, 8, 32);
        bodyRadius = Mathf.Max(0.1f, bodyRadius);
        outlineWidth = Mathf.Max(0f, outlineWidth);
    }

    private void EnsureRenderObjects()
    {
        Shader shader = FindSpriteShader();

        outlineFilter = EnsureRenderChild(OutlineName, out outlineRenderer);
        bodyFilter = EnsureRenderChild(BodyName, out bodyRenderer);
        tailFilter = EnsureRenderChild(TailName, out tailRenderer);
        if (drawContactFill)
            contactFillFilter = EnsureRenderChild(ContactFillName, out contactFillRenderer);
        else
            ClearContactFillChild();
        highlightFilter = EnsureRenderChild(HighlightName, out highlightRenderer);
        eyeGlowFilter = EnsureRenderChild(EyeGlowName, out eyeGlowRenderer);
        eyeFilter = EnsureRenderChild(EyeName, out eyeRenderer);

        outlineMesh = EnsureMesh(outlineMesh, "Procedural Slime Outline");
        bodyMesh = EnsureMesh(bodyMesh, "Procedural Slime Body");
        tailMesh = EnsureMesh(tailMesh, "Procedural Slime Tail");
        if (drawContactFill)
            contactFillMesh = EnsureMesh(contactFillMesh, "Procedural Slime Contact Fill");
        highlightMesh = EnsureMesh(highlightMesh, "Procedural Slime Highlight");
        eyeGlowMesh = EnsureMesh(eyeGlowMesh, "Procedural Slime Eye Glow");
        eyeMesh = EnsureMesh(eyeMesh, "Procedural Slime Eyes");

        outlineFilter.sharedMesh = outlineMesh;
        bodyFilter.sharedMesh = bodyMesh;
        tailFilter.sharedMesh = tailMesh;
        if (drawContactFill && contactFillFilter != null)
            contactFillFilter.sharedMesh = contactFillMesh;
        highlightFilter.sharedMesh = highlightMesh;
        eyeGlowFilter.sharedMesh = eyeGlowMesh;
        eyeFilter.sharedMesh = eyeMesh;

        outlineMaterial = EnsureMaterial(outlineMaterial, shader, "Procedural Slime Outline Material");
        bodyMaterial = EnsureMaterial(bodyMaterial, shader, "Procedural Slime Body Material");
        tailMaterial = EnsureMaterial(tailMaterial, shader, "Procedural Slime Tail Material");
        if (drawContactFill)
            contactFillMaterial = EnsureMaterial(contactFillMaterial, shader, "Procedural Slime Contact Fill Material");
        highlightMaterial = EnsureMaterial(highlightMaterial, shader, "Procedural Slime Highlight Material");
        eyeGlowMaterial = EnsureMaterial(eyeGlowMaterial, shader, "Procedural Slime Eye Glow Material");
        eyeMaterial = EnsureMaterial(eyeMaterial, shader, "Procedural Slime Eye Material");

        outlineRenderer.sharedMaterial = outlineMaterial;
        bodyRenderer.sharedMaterial = bodyMaterial;
        tailRenderer.sharedMaterial = tailMaterial;
        if (drawContactFill && contactFillRenderer != null)
            contactFillRenderer.sharedMaterial = contactFillMaterial;
        highlightRenderer.sharedMaterial = highlightMaterial;
        eyeGlowRenderer.sharedMaterial = eyeGlowMaterial;
        eyeRenderer.sharedMaterial = eyeMaterial;
    }

    private void ClearContactFillChild()
    {
        if (contactFillRenderer != null)
            contactFillRenderer.enabled = false;

        DestroyRuntimeObject(contactFillMesh);
        DestroyRuntimeObject(contactFillMaterial);
        contactFillMesh = null;
        contactFillMaterial = null;
        contactFillFilter = null;
        contactFillRenderer = null;

        Transform child = transform.Find(ContactFillName);
        if (child != null)
            DestroyRuntimeObject(child.gameObject);
    }

    private MeshFilter EnsureRenderChild(string childName, out MeshRenderer meshRenderer)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject childObject = new GameObject(childName);
            childObject.layer = gameObject.layer;
            childObject.transform.SetParent(transform, false);
            child = childObject.transform;
        }
        else
        {
            child.gameObject.layer = gameObject.layer;
        }

        MeshFilter meshFilter = child.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = child.gameObject.AddComponent<MeshFilter>();

        meshRenderer = child.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = child.gameObject.AddComponent<MeshRenderer>();

        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        return meshFilter;
    }

    private Mesh EnsureMesh(Mesh mesh, string meshName)
    {
        if (mesh != null)
            return mesh;

        Mesh newMesh = new Mesh();
        newMesh.name = meshName;
        newMesh.MarkDynamic();
        return newMesh;
    }

    private Material EnsureMaterial(Material material, Shader shader, string materialName)
    {
        if (material != null)
            return material;

        Material newMaterial = new Material(shader);
        newMaterial.name = materialName;
        newMaterial.hideFlags = HideFlags.DontSave;
        if (newMaterial.HasProperty("_Color"))
            newMaterial.SetColor("_Color", Color.white);
        return newMaterial;
    }

    private static Shader FindSpriteShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        return shader;
    }

    private void SyncSorting()
    {
        int sortingLayerId = fallbackSprite != null ? fallbackSprite.sortingLayerID : 0;
        int sortingOrder = fallbackSprite != null ? fallbackSprite.sortingOrder : 0;

        if (outlineRenderer != null)
        {
            outlineRenderer.sortingLayerID = sortingLayerId;
            outlineRenderer.sortingOrder = sortingOrder;
        }

        if (bodyRenderer != null)
        {
            bodyRenderer.sortingLayerID = sortingLayerId;
            bodyRenderer.sortingOrder = sortingOrder + 1;
        }

        if (tailRenderer != null)
        {
            tailRenderer.sortingLayerID = sortingLayerId;
            tailRenderer.sortingOrder = sortingOrder + 1;
        }

        if (contactFillRenderer != null)
        {
            contactFillRenderer.sortingLayerID = sortingLayerId;
            contactFillRenderer.sortingOrder = sortingOrder + 1;
        }

        if (highlightRenderer != null)
        {
            highlightRenderer.sortingLayerID = sortingLayerId;
            highlightRenderer.sortingOrder = sortingOrder + 2;
        }

        if (eyeGlowRenderer != null)
        {
            eyeGlowRenderer.sortingLayerID = sortingLayerId;
            eyeGlowRenderer.sortingOrder = sortingOrder + 3;
        }

        if (eyeRenderer != null)
        {
            eyeRenderer.sortingLayerID = sortingLayerId;
            eyeRenderer.sortingOrder = sortingOrder + 4;
        }
    }

    private void RebuildMeshBuffers()
    {
        cachedResolution = Mathf.Clamp(meshResolution, 12, 96);
        cachedTailResolution = Mathf.Clamp(tailResolution, 8, 32);
        cachedHighlightResolution = Mathf.Clamp(cachedResolution / 2, 12, 32);
        cachedEyeResolution = Mathf.Clamp(eyeResolution, 8, 32);

        int vertexCount = cachedResolution + 1;
        outlineVertices = new Vector3[vertexCount];
        bodyVertices = new Vector3[vertexCount];
        smoothedBodyVertices = new Vector3[vertexCount];
        outlineColors = new Color[vertexCount];
        bodyColors = new Color[vertexCount];
        bodyTriangles = CreateFanTriangles(cachedResolution);
        int tailVertexCount = cachedTailResolution + 5;
        tailVertices = new Vector3[tailVertexCount];
        tailColors = new Color[tailVertexCount];
        tailTriangles = CreateTailTriangles(cachedTailResolution);
        if (drawContactFill && contactFillMesh != null)
        {
            contactFillVertices = new Vector3[4];
            contactFillColors = new Color[4];
            contactFillTriangles = new[] { 0, 1, 2, 2, 1, 3 };
        }
        else
        {
            contactFillVertices = null;
            contactFillColors = null;
            contactFillTriangles = null;
        }
        wobbleOffsets = new float[cachedResolution];
        environmentContacts = new ContactPoint2D[8];
        contactPlanes = new ContactPlane[8];

        for (int i = 0; i < cachedResolution; i++)
            wobbleOffsets[i] = Random.value * Mathf.PI * 2f;

        int highlightVertexCount = cachedHighlightResolution + 1;
        highlightVertices = new Vector3[highlightVertexCount];
        highlightColors = new Color[highlightVertexCount];
        highlightTriangles = CreateFanTriangles(cachedHighlightResolution);

        int eyeVertexCount = (cachedEyeResolution + 1) * 2;
        eyeGlowVertices = new Vector3[eyeVertexCount];
        eyeVertices = new Vector3[eyeVertexCount];
        eyeGlowColors = new Color[eyeVertexCount];
        eyeColors = new Color[eyeVertexCount];
        eyeTriangles = CreateDoubleFanTriangles(cachedEyeResolution);

        outlineMesh.Clear();
        outlineMesh.vertices = outlineVertices;
        outlineMesh.triangles = bodyTriangles;
        outlineMesh.colors = outlineColors;

        bodyMesh.Clear();
        bodyMesh.vertices = bodyVertices;
        bodyMesh.triangles = bodyTriangles;
        bodyMesh.colors = bodyColors;

        tailMesh.Clear();
        tailMesh.vertices = tailVertices;
        tailMesh.triangles = tailTriangles;
        tailMesh.colors = tailColors;

        if (drawContactFill && contactFillMesh != null)
        {
            contactFillMesh.Clear();
            contactFillMesh.vertices = contactFillVertices;
            contactFillMesh.triangles = contactFillTriangles;
            contactFillMesh.colors = contactFillColors;
        }

        highlightMesh.Clear();
        highlightMesh.vertices = highlightVertices;
        highlightMesh.triangles = highlightTriangles;
        highlightMesh.colors = highlightColors;

        eyeGlowMesh.Clear();
        eyeGlowMesh.vertices = eyeGlowVertices;
        eyeGlowMesh.triangles = eyeTriangles;
        eyeGlowMesh.colors = eyeGlowColors;

        eyeMesh.Clear();
        eyeMesh.vertices = eyeVertices;
        eyeMesh.triangles = eyeTriangles;
        eyeMesh.colors = eyeColors;
    }

    private int[] CreateFanTriangles(int ringCount)
    {
        int[] triangles = new int[ringCount * 3];
        for (int i = 0; i < ringCount; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = ((i + 1) % ringCount) + 1;
        }

        return triangles;
    }

    private int[] CreateTailTriangles(int ringCount)
    {
        int[] triangles = new int[6 + ringCount * 3];
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 2;
        triangles[4] = 1;
        triangles[5] = 3;

        int centerIndex = 4;
        int ringStart = centerIndex + 1;
        for (int i = 0; i < ringCount; i++)
        {
            int triangleIndex = 6 + i * 3;
            triangles[triangleIndex] = centerIndex;
            triangles[triangleIndex + 1] = ringStart + i;
            triangles[triangleIndex + 2] = ringStart + ((i + 1) % ringCount);
        }

        return triangles;
    }

    private int[] CreateDoubleFanTriangles(int ringCount)
    {
        int[] triangles = new int[ringCount * 3 * 2];
        int vertexStride = ringCount + 1;

        for (int eye = 0; eye < 2; eye++)
        {
            int vertexOffset = eye * vertexStride;
            int triangleOffset = eye * ringCount * 3;
            for (int i = 0; i < ringCount; i++)
            {
                int triangleIndex = triangleOffset + i * 3;
                triangles[triangleIndex] = vertexOffset;
                triangles[triangleIndex + 1] = vertexOffset + i + 1;
                triangles[triangleIndex + 2] = vertexOffset + ((i + 1) % ringCount) + 1;
            }
        }

        return triangles;
    }

    private void UpdateImpactPulse(Vector2 velocity, S_fluid_climb.SurfaceType surface)
    {
        bool landingLikeImpact = previousVelocity.y < -4f && Mathf.Abs(velocity.y) < 1f;
        bool wallLikeImpact = Mathf.Abs(previousVelocity.x) > 5f && Mathf.Abs(velocity.x) < 1f;

        if (surface == S_fluid_climb.SurfaceType.Floor && landingLikeImpact)
            impactPulse = 1f;
        else if ((surface == S_fluid_climb.SurfaceType.WallLeft || surface == S_fluid_climb.SurfaceType.WallRight) && wallLikeImpact)
            impactPulse = 0.75f;

        impactPulse = Mathf.MoveTowards(impactPulse, 0f, Time.deltaTime * 4.5f);
    }

    private void SampleContactPlanes()
    {
        contactPlaneCount = 0;
        if (!fitToContactPlanes || targetCollider == null)
            return;

        if (environmentContacts == null || environmentContacts.Length == 0)
            environmentContacts = new ContactPoint2D[8];

        if (contactPlanes == null || contactPlanes.Length == 0)
            contactPlanes = new ContactPlane[8];

        int count = targetCollider.GetContacts(environmentContacts);
        for (int i = 0; i < count && contactPlaneCount < contactPlanes.Length; i++)
        {
            ContactPoint2D contact = environmentContacts[i];
            if (contact.collider == null || contact.normal.sqrMagnitude < 0.001f)
                continue;

            Vector2 normal = contact.normal.normalized;
            if (HasSimilarContactPlane(contact.point, normal))
                continue;

            contactPlanes[contactPlaneCount] = new ContactPlane
            {
                point = contact.point,
                normal = normal,
            };
            contactPlaneCount++;
        }
    }

    private bool HasSimilarContactPlane(Vector2 point, Vector2 normal)
    {
        for (int i = 0; i < contactPlaneCount; i++)
        {
            float normalDot = Vector2.Dot(contactPlanes[i].normal, normal);
            float pointDistance = Vector2.Distance(contactPlanes[i].point, point);
            if (normalDot > 0.94f && pointDistance < 0.08f)
                return true;
        }

        return false;
    }

    private void ApplyContactPlaneDeformation(Vector2 dir, float radius, ref float radiusScale)
    {
        if (!fitToContactPlanes || contactPlaneCount == 0)
            return;

        Vector2 worldCenter = transform.position;
        for (int i = 0; i < contactPlaneCount; i++)
        {
            Vector2 normal = contactPlanes[i].normal;
            Vector2 towardSurface = -normal;
            float surfaceDot = Mathf.Max(0f, Vector2.Dot(dir, towardSurface));

            float centerDistance = Mathf.Abs(Vector2.Dot(worldCenter - contactPlanes[i].point, normal));
            float influence = 1f - Mathf.Clamp01(centerDistance / Mathf.Max(radius * contactInfluenceRadius, 0.01f));
            if (influence <= 0f)
                continue;

            float tangentDot = 1f - Mathf.Abs(Vector2.Dot(dir, normal));
            float apexDot = Mathf.Max(0f, Vector2.Dot(dir, normal));
            float shoulder = Mathf.Pow(Mathf.Clamp01(tangentDot), 2.5f);
            radiusScale -= surfaceDot * surfaceDot * contactCompressionStrength * influence;
            radiusScale += shoulder * contactSpreadStrength * influence;
            radiusScale += shoulder * contactShoulderBulge * contactTriangleStrength * influence;
            radiusScale -= apexDot * apexDot * contactApexTaper * contactTriangleStrength * influence;
        }
    }

    private Vector3 FitLocalPointToContactPlanes(Vector3 localPoint, float skin)
    {
        if (!fitToContactPlanes || contactPlaneCount == 0)
            return localPoint;

        Vector3 worldPoint = transform.TransformPoint(localPoint);
        for (int i = 0; i < contactPlaneCount; i++)
        {
            Vector2 normal = contactPlanes[i].normal;
            float signedDistance = Vector2.Dot((Vector2)worldPoint - contactPlanes[i].point, normal);
            if (signedDistance < skin)
                worldPoint += (Vector3)(normal * (skin - signedDistance));
        }

        return transform.InverseTransformPoint(worldPoint);
    }

    private void UpdateMeshes(
        Vector2 velocity,
        S_fluid_climb.SurfaceType surface,
        Vector2 surfaceNormal,
        bool isFluid,
        bool isParalyzed,
        bool facingRight)
    {
        float radius = GetBodyRadius();
        Vector2 colliderShapeScale = GetColliderShapeScale(radius);
        Color centerColor = isFluid ? fluidCenterColor : solidCenterColor;
        Color edgeColor = isFluid ? fluidEdgeColor : solidEdgeColor;
        SampleContactPlanes();
        UpdateTailGroundCache(radius);

        float time = Time.time;
        Vector2 velocityDirection = velocity.sqrMagnitude > 0.001f ? velocity.normalized : Vector2.right * (facingRight ? 1f : -1f);
        float stretch = Mathf.Min(velocity.magnitude * velocityStretch, maxStretch);
        float trailingStretch = Mathf.Min(velocity.magnitude * motionLag, maxTailStretch);
        Vector2 stickDirection = surfaceNormal.sqrMagnitude > 0.001f ? -surfaceNormal.normalized : Vector2.zero;
        Vector2 contactDirection = stickDirection;
        bool isSticking = surface == S_fluid_climb.SurfaceType.WallLeft
            || surface == S_fluid_climb.SurfaceType.WallRight
            || surface == S_fluid_climb.SurfaceType.Ceiling;
        bool hasSurface = surface != S_fluid_climb.SurfaceType.None && surfaceNormal.sqrMagnitude > 0.001f;

        bodyVertices[0] = Vector3.zero;
        outlineVertices[0] = Vector3.zero;
        bodyColors[0] = centerColor;
        outlineColors[0] = outlineColor;

        for (int i = 0; i < cachedResolution; i++)
        {
            float angle = Mathf.PI * 2f * i / cachedResolution;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float axial = Vector2.Dot(dir, velocityDirection);
            float perpendicular = 1f - Mathf.Abs(axial);
            float bottom = Mathf.Max(0f, -dir.y);
            float top = Mathf.Max(0f, dir.y);
            float rear = Mathf.Max(0f, Vector2.Dot(dir, -velocityDirection));
            float colliderShapeRadius = Mathf.Sqrt(
                dir.x * dir.x * colliderShapeScale.x * colliderShapeScale.x
                + dir.y * dir.y * colliderShapeScale.y * colliderShapeScale.y);
            float radiusScale = 1f
                + idleSquash * Mathf.Abs(dir.x)
                - idleSquash * 0.65f * Mathf.Abs(dir.y)
                + bottomBulge * bottom * bottom
                - bottomBulge * 0.25f * top * top * top
                + stretch * axial * axial
                - stretch * 0.55f * perpendicular
                + trailingStretch * rear * rear * bodyTailInfluence;

            radiusScale *= Mathf.Lerp(1f, colliderShapeRadius, colliderShapeFollow);

            float squash = impactPulse * impactSquash;
            radiusScale += squash * Mathf.Abs(dir.x);
            radiusScale -= squash * Mathf.Abs(dir.y) * 0.75f;

            if (hasSurface)
            {
                float contactDot = Mathf.Max(0f, Vector2.Dot(dir, contactDirection));
                float tangentDot = 1f - Mathf.Abs(Vector2.Dot(dir, surfaceNormal.normalized));
                radiusScale -= contactDot * contactDot * contactFlatten;
                radiusScale += tangentDot * tangentDot * contactFlatten * 0.45f;
            }

            ApplyContactPlaneDeformation(dir, radius, ref radiusScale);

            if (isSticking)
            {
                float stickDot = Mathf.Max(0f, Vector2.Dot(dir, stickDirection));
                radiusScale += stickDot * stickDot * surfaceStickDeform;
            }

            if (isFluid)
            {
                float wobble = Mathf.Sin(time * wobbleSpeed + angle * 3f + wobbleOffsets[i]) * wobbleAmount;
                radiusScale += wobble;
            }

            float finalRadius = Mathf.Max(radius * 0.35f, radius * radiusScale);
            Vector3 point = new Vector3(dir.x * finalRadius, dir.y * finalRadius, 0f);

            bodyVertices[i + 1] = point;
            bodyColors[i + 1] = edgeColor;
        }

        SmoothBodyBoundary();
        for (int i = 0; i < cachedResolution; i++)
        {
            Vector3 point = StickTailPointToGround(bodyVertices[i + 1], velocityDirection, radius, trailingStretch);
            point = FitLocalPointToContactPlanes(point, contactPlaneSkin);
            bodyVertices[i + 1] = point;
            outlineVertices[i + 1] = FitLocalPointToContactPlanes(point.normalized * (point.magnitude + outlineWidth), contactPlaneSkin);
            outlineColors[i + 1] = outlineColor;
        }

        UpdateHighlightMesh(radius, facingRight, isFluid, isParalyzed);
        UpdateCircleTailMesh(radius, velocity, velocityDirection, trailingStretch, edgeColor);
        UpdateContactFillMesh(radius, edgeColor);
        UpdateEyeMeshes(radius, velocity, facingRight, isParalyzed);
        ApplyMeshData();
    }

    private void UpdateContactFillMesh(float radius, Color fillColor)
    {
        if (!drawContactFill)
        {
            if (contactFillRenderer != null)
                contactFillRenderer.enabled = false;
            return;
        }

        if (contactFillRenderer == null || contactFillVertices == null || contactFillColors == null)
            return;

        ContactPlane groundPlane = new ContactPlane { point = Vector2.zero, normal = Vector2.up };
        bool hasGround = drawContactFill && TryGetTailGroundPlane(radius, out groundPlane);
        float fillWeight = hasGround ? tailGroundBlend : 0f;
        contactFillRenderer.enabled = useProceduralRendering && fillWeight > 0.001f;
        if (!contactFillRenderer.enabled)
            return;

        Vector2 normal = groundPlane.normal.normalized;
        Vector2 tangent = new Vector2(-normal.y, normal.x);
        Vector2 worldCenter = transform.position;
        float centerDistance = Vector2.Dot(worldCenter - groundPlane.point, normal);
        Vector2 projectedCenter = worldCenter - normal * centerDistance;
        float halfWidth = Mathf.Max(radius * 0.25f, radius * contactFillWidthScale);
        float lowerDistance = Mathf.Max(0.001f, contactPlaneSkin * 0.35f);
        float upperDistance = Mathf.Max(lowerDistance + 0.001f, contactFillDepth + contactFillBodyOverlap);

        Vector2 lowerCenter = projectedCenter + normal * lowerDistance;
        Vector2 upperCenter = projectedCenter + normal * upperDistance;
        Vector2 halfTangent = tangent * halfWidth;

        contactFillVertices[0] = transform.InverseTransformPoint(upperCenter - halfTangent);
        contactFillVertices[1] = transform.InverseTransformPoint(lowerCenter - halfTangent);
        contactFillVertices[2] = transform.InverseTransformPoint(upperCenter + halfTangent);
        contactFillVertices[3] = transform.InverseTransformPoint(lowerCenter + halfTangent);

        Color edge = fillColor;
        edge.a *= fillWeight;
        Color inner = fillColor;
        inner.a *= Mathf.Clamp01(fillWeight * 0.92f);

        contactFillColors[0] = inner;
        contactFillColors[1] = edge;
        contactFillColors[2] = inner;
        contactFillColors[3] = edge;
    }

    private void UpdateCircleTailMesh(float radius, Vector2 velocity, Vector2 velocityDirection, float trailingStretch, Color fillColor)
    {
        if (tailRenderer == null || tailVertices == null || tailColors == null)
            return;

        float speedWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.001f, Mathf.Max(maxTailStretch, 0.002f), trailingStretch));
        float targetBlend = useCircleTail && speedWeight > 0.001f ? 1f : 0f;
        float blendStep = 1f - Mathf.Exp(-tailFollowSpeed * Time.deltaTime);
        currentTailBlend = Mathf.Lerp(currentTailBlend, targetBlend, blendStep);

        tailRenderer.enabled = useProceduralRendering && useCircleTail && currentTailBlend > 0.001f;
        if (!tailRenderer.enabled)
            return;

        float targetRadius = radius * Mathf.Lerp(tailMinRadiusScale, tailRadiusScale, speedWeight);
        float targetDistance = Mathf.Min(tailMaxDistance, tailBaseDistance + velocity.magnitude * tailSpeedDistanceScale);
        Vector2 targetCenter = -velocityDirection * Mathf.Max(targetDistance, targetRadius * 0.55f);
        targetCenter = StickTailCenterToGround(targetCenter, targetRadius);

        currentTailCenter = Vector2.Lerp(currentTailCenter, targetCenter, blendStep);
        currentTailRadius = Mathf.Lerp(currentTailRadius <= 0.001f ? targetRadius : currentTailRadius, targetRadius, blendStep);

        float bodyConnectRadius = Mathf.Max(radius * 0.35f, radius + tailBridgeOverlap);
        float tailConnectRadius = Mathf.Max(currentTailRadius * 0.5f, currentTailRadius + tailBridgeOverlap);
        WriteCircleTailMesh(Vector2.zero, bodyConnectRadius, currentTailCenter, tailConnectRadius, currentTailRadius, fillColor);
    }

    private Vector2 StickTailCenterToGround(Vector2 localCenter, float tailRadius)
    {
        if (tailGroundStick <= 0f || !TryGetTailGroundPlane(tailRadius, out ContactPlane groundPlane))
            return localCenter;

        Vector3 worldCenter = transform.TransformPoint(localCenter);
        Vector2 normal = groundPlane.normal.normalized;
        float distance = Vector2.Dot((Vector2)worldCenter - groundPlane.point, normal);
        float desiredDistance = Mathf.Max(tailGroundSkin, contactPlaneSkin) + tailRadius;
        float stickWeight = tailGroundStick * tailGroundBlend;
        worldCenter += (Vector3)(normal * (desiredDistance - distance) * stickWeight);
        return transform.InverseTransformPoint(worldCenter);
    }

    private void WriteCircleTailMesh(
        Vector2 bodyCenter,
        float bodyConnectRadius,
        Vector2 tailCenter,
        float tailConnectRadius,
        float visibleTailRadius,
        Color fillColor)
    {
        GetExternalTangentPoints(
            bodyCenter,
            bodyConnectRadius,
            tailCenter,
            tailConnectRadius,
            out Vector2 bodyPointA,
            out Vector2 tailPointA,
            out Vector2 bodyPointB,
            out Vector2 tailPointB);

        tailVertices[0] = bodyPointA;
        tailVertices[1] = tailPointA;
        tailVertices[2] = bodyPointB;
        tailVertices[3] = tailPointB;
        tailVertices[4] = tailCenter;

        Color tailColor = fillColor;
        tailColor.a *= currentTailBlend;
        for (int i = 0; i < tailColors.Length; i++)
            tailColors[i] = tailColor;

        for (int i = 0; i < cachedTailResolution; i++)
        {
            float angle = Mathf.PI * 2f * i / cachedTailResolution;
            Vector2 point = tailCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * visibleTailRadius;
            tailVertices[5 + i] = point;
        }
    }

    private void GetExternalTangentPoints(
        Vector2 centerA,
        float radiusA,
        Vector2 centerB,
        float radiusB,
        out Vector2 pointA0,
        out Vector2 pointB0,
        out Vector2 pointA1,
        out Vector2 pointB1)
    {
        Vector2 delta = centerB - centerA;
        float sqrDistance = Mathf.Max(delta.sqrMagnitude, 0.0001f);
        float radiusDelta = radiusA - radiusB;
        float tangentLength = Mathf.Sqrt(Mathf.Max(0.0001f, sqrDistance - radiusDelta * radiusDelta));
        Vector2 perpendicular = new Vector2(-delta.y, delta.x);

        Vector2 normal0 = (delta * radiusDelta + perpendicular * tangentLength) / sqrDistance;
        Vector2 normal1 = (delta * radiusDelta - perpendicular * tangentLength) / sqrDistance;

        if (normal0.sqrMagnitude < 0.0001f || normal1.sqrMagnitude < 0.0001f)
        {
            Vector2 fallback = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
            normal0 = new Vector2(-fallback.y, fallback.x);
            normal1 = -normal0;
        }
        else
        {
            normal0.Normalize();
            normal1.Normalize();
        }

        pointA0 = centerA + normal0 * radiusA;
        pointB0 = centerB + normal0 * radiusB;
        pointA1 = centerA + normal1 * radiusA;
        pointB1 = centerB + normal1 * radiusB;
    }

    private Vector3 StickTailPointToGround(Vector3 localPoint, Vector2 velocityDirection, float radius, float trailingStretch)
    {
        if (tailGroundStick <= 0f || trailingStretch <= 0.001f)
            return localPoint;

        if (!TryGetTailGroundPlane(radius, out ContactPlane groundPlane))
            return localPoint;

        Vector2 dir = localPoint.sqrMagnitude > 0.0001f
            ? ((Vector2)localPoint).normalized
            : Vector2.zero;
        float rear = Mathf.Max(0f, Vector2.Dot(dir, -velocityDirection));
        float bottom = Mathf.Max(0f, -dir.y);
        if (rear <= 0.001f)
            return localPoint;

        float rearWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 1f, rear));
        float bottomWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.85f, bottom));
        float speedWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.001f, Mathf.Max(maxTailStretch, 0.002f), trailingStretch));
        float stickWeight = tailGroundStick * tailGroundBlend * rearWeight * Mathf.Lerp(0.35f, 1f, bottomWeight) * speedWeight;
        if (stickWeight <= 0.001f)
            return localPoint;

        Vector3 worldPoint = transform.TransformPoint(localPoint);
        float distance = Vector2.Dot((Vector2)worldPoint - groundPlane.point, groundPlane.normal);
        float desiredDistance = Mathf.Max(tailGroundSkin, contactPlaneSkin);
        if (distance <= desiredDistance)
            return localPoint;

        worldPoint += (Vector3)(groundPlane.normal * (desiredDistance - distance) * stickWeight);
        return transform.InverseTransformPoint(worldPoint);
    }

    private void UpdateTailGroundCache(float radius)
    {
        bool hasGroundThisFrame = FindBestTailGroundPlane(radius, out ContactPlane groundPlane);
        if (hasGroundThisFrame)
        {
            cachedTailGroundPlane = groundPlane;
            tailGroundMemoryTimer = tailGroundMemoryTime;
        }
        else if (tailGroundMemoryTimer > 0f)
        {
            tailGroundMemoryTimer -= Time.deltaTime;
        }

        float targetBlend = hasGroundThisFrame || tailGroundMemoryTimer > 0f ? 1f : 0f;
        tailGroundBlend = Mathf.MoveTowards(tailGroundBlend, targetBlend, tailGroundBlendSpeed * Time.deltaTime);
    }

    private bool TryGetTailGroundPlane(float radius, out ContactPlane groundPlane)
    {
        groundPlane = cachedTailGroundPlane;
        return tailGroundBlend > 0.001f;
    }

    private bool FindBestTailGroundPlane(float radius, out ContactPlane groundPlane)
    {
        groundPlane = default;
        if (!fitToContactPlanes || contactPlaneCount == 0)
            return false;

        Vector2 worldCenter = transform.position;
        float bestScore = 0f;
        for (int i = 0; i < contactPlaneCount; i++)
        {
            Vector2 normal = contactPlanes[i].normal;
            float upDot = Vector2.Dot(normal, Vector2.up);
            if (upDot < 0.45f)
                continue;

            float centerDistance = Mathf.Abs(Vector2.Dot(worldCenter - contactPlanes[i].point, normal));
            float influence = 1f - Mathf.Clamp01(centerDistance / Mathf.Max(radius * contactInfluenceRadius, 0.01f));
            float score = upDot * influence;
            if (score <= bestScore)
                continue;

            bestScore = score;
            groundPlane = contactPlanes[i];
        }

        return bestScore > 0f;
    }

    private void SmoothBodyBoundary()
    {
        if (edgeSmoothStrength <= 0f || smoothedBodyVertices == null || cachedResolution <= 2)
            return;

        smoothedBodyVertices[0] = bodyVertices[0];
        for (int i = 0; i < cachedResolution; i++)
        {
            int prev = ((i - 1 + cachedResolution) % cachedResolution) + 1;
            int current = i + 1;
            int next = ((i + 1) % cachedResolution) + 1;
            Vector3 neighborAverage = (bodyVertices[prev] + bodyVertices[current] * 2f + bodyVertices[next]) * 0.25f;
            smoothedBodyVertices[current] = Vector3.Lerp(bodyVertices[current], neighborAverage, edgeSmoothStrength);
        }

        for (int i = 1; i <= cachedResolution; i++)
            bodyVertices[i] = smoothedBodyVertices[i];
    }

    private void UpdateHighlightMesh(float radius, bool facingRight, bool isFluid, bool isParalyzed)
    {
        Color highlight = highlightColor;
        if (!isFluid)
            highlight.a *= 0.45f;
        if (isParalyzed)
            highlight = Color.Lerp(highlight, paralyzedTint, 0.25f);

        float side = facingRight ? -1f : 1f;
        Vector2 center = new Vector2(side * radius * 0.2f, radius * 0.24f);
        float width = radius * 0.22f;
        float height = radius * 0.1f;
        float tilt = side * -20f * Mathf.Deg2Rad;
        float cos = Mathf.Cos(tilt);
        float sin = Mathf.Sin(tilt);

        highlightVertices[0] = center;
        highlightColors[0] = highlight;

        Color edge = highlight;
        edge.a = 0f;
        for (int i = 0; i < cachedHighlightResolution; i++)
        {
            float angle = Mathf.PI * 2f * i / cachedHighlightResolution;
            Vector2 ellipse = new Vector2(Mathf.Cos(angle) * width, Mathf.Sin(angle) * height);
            Vector2 rotated = new Vector2(
                ellipse.x * cos - ellipse.y * sin,
                ellipse.x * sin + ellipse.y * cos);

            highlightVertices[i + 1] = center + rotated;
            highlightColors[i + 1] = edge;
        }
    }

    private void UpdateEyeMeshes(float radius, Vector2 velocity, bool facingRight, bool isParalyzed)
    {
        Color eyeFill = isParalyzed ? Color.Lerp(eyeColor, paralyzedTint, 0.25f) : eyeColor;
        Color glowFill = isParalyzed ? Color.Lerp(eyeGlowColor, paralyzedTint, 0.45f) : eyeGlowColor;

        float facingSign = facingRight ? 1f : -1f;
        float lookOffset = Mathf.Clamp(velocity.x * eyeFollowVelocity, -radius * 0.09f, radius * 0.09f);
        float pulse = 1f + Mathf.Sin(Time.time * 7.5f) * eyeGlowPulse;
        float baseEyeRadius = radius * eyeRadius;
        float faceOffset = radius * eyeHorizontalOffset * facingSign + lookOffset;
        float spacing = radius * eyeSpacing;
        float vertical = radius * eyeVerticalOffset;

        for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++)
        {
            float side = eyeIndex == 0 ? -0.5f : 0.5f;
            Vector2 center = new Vector2(faceOffset + side * spacing, vertical);
            WriteEyeFan(eyeVertices, eyeColors, eyeIndex, center, baseEyeRadius, baseEyeRadius * eyeTallness, eyeFill, eyeFill);

            Color glowEdge = glowFill;
            glowEdge.a = 0f;
            WriteEyeFan(
                eyeGlowVertices,
                eyeGlowColors,
                eyeIndex,
                center,
                baseEyeRadius * eyeGlowScale * pulse,
                baseEyeRadius * eyeTallness * eyeGlowScale * pulse,
                glowFill,
                glowEdge);
        }
    }

    private void WriteEyeFan(
        Vector3[] vertices,
        Color[] colors,
        int eyeIndex,
        Vector2 center,
        float width,
        float height,
        Color centerColor,
        Color edgeColor)
    {
        int stride = cachedEyeResolution + 1;
        int offset = eyeIndex * stride;
        vertices[offset] = center;
        colors[offset] = centerColor;

        for (int i = 0; i < cachedEyeResolution; i++)
        {
            float angle = Mathf.PI * 2f * i / cachedEyeResolution;
            Vector2 point = center + new Vector2(Mathf.Cos(angle) * width, Mathf.Sin(angle) * height);
            vertices[offset + i + 1] = point;
            colors[offset + i + 1] = edgeColor;
        }
    }

    private void ApplyMeshData()
    {
        outlineMesh.vertices = outlineVertices;
        outlineMesh.colors = outlineColors;
        outlineMesh.RecalculateBounds();

        bodyMesh.vertices = bodyVertices;
        bodyMesh.colors = bodyColors;
        bodyMesh.RecalculateBounds();

        tailMesh.vertices = tailVertices;
        tailMesh.colors = tailColors;
        tailMesh.RecalculateBounds();

        if (drawContactFill && contactFillMesh != null)
        {
            contactFillMesh.vertices = contactFillVertices;
            contactFillMesh.colors = contactFillColors;
            contactFillMesh.RecalculateBounds();
        }

        highlightMesh.vertices = highlightVertices;
        highlightMesh.colors = highlightColors;
        highlightMesh.RecalculateBounds();

        eyeGlowMesh.vertices = eyeGlowVertices;
        eyeGlowMesh.colors = eyeGlowColors;
        eyeGlowMesh.RecalculateBounds();

        eyeMesh.vertices = eyeVertices;
        eyeMesh.colors = eyeColors;
        eyeMesh.RecalculateBounds();
    }

    private float GetBodyRadius()
    {
        if (matchCircleColliderRadius && targetCircleCollider != null)
            return Mathf.Max(0.1f, targetCircleCollider.radius);

        return Mathf.Max(0.1f, bodyRadius);
    }

    private Vector2 GetColliderShapeScale(float radius)
    {
        Vector2 targetScale = CalculateTargetColliderShapeScale(radius);
        float blend = 1f - Mathf.Exp(-colliderShapeFollowSpeed * Time.deltaTime);
        currentColliderShapeScale = Vector2.Lerp(currentColliderShapeScale, targetScale, blend);
        return currentColliderShapeScale;
    }

    private Vector2 CalculateTargetColliderShapeScale(float radius)
    {
        if (targetCapsuleCollider == null || !targetCapsuleCollider.enabled)
            return Vector2.one;

        float diameter = Mathf.Max(radius * 2f, 0.01f);
        return new Vector2(
            Mathf.Max(0.2f, targetCapsuleCollider.size.x / diameter),
            Mathf.Max(0.2f, targetCapsuleCollider.size.y / diameter));
    }

    private void SetTargetCollider(Collider2D collider)
    {
        if (collider == null || collider == targetCollider)
            return;

        targetCollider = collider;
        targetCircleCollider = collider as CircleCollider2D;
        targetCapsuleCollider = collider as CapsuleCollider2D;
    }

    private void SetRenderersActive(bool active)
    {
        if (outlineRenderer != null) outlineRenderer.enabled = active;
        if (bodyRenderer != null) bodyRenderer.enabled = active;
        if (tailRenderer != null) tailRenderer.enabled = active && useCircleTail;
        if (contactFillRenderer != null) contactFillRenderer.enabled = active && drawContactFill;
        if (highlightRenderer != null) highlightRenderer.enabled = active;
        if (eyeGlowRenderer != null) eyeGlowRenderer.enabled = active && drawEyes;
        if (eyeRenderer != null) eyeRenderer.enabled = active && drawEyes;
    }

    private void SetFallbackSpriteVisible(bool visible)
    {
        if (fallbackSprite != null)
            fallbackSprite.enabled = visible;
    }

    private void DestroyRuntimeObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
