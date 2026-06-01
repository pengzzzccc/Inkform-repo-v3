using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class S_ParallaxLayer : MonoBehaviour
{
    public enum ParallaxDepth
    {
        Far,
        Back,
        Mid,
        Near,
        Front
    }

    [Header("Depth Preset")]
    [SerializeField] private ParallaxDepth parallaxDepth = ParallaxDepth.Mid;
    [FormerlySerializedAs("useDepthPreset")]
    [SerializeField] private bool useDepthMovementPreset = false;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool findMainCameraIfMissing = true;

    [Header("Parallax")]
    [SerializeField] private float parallaxFactor = 0f;
    [SerializeField] private bool enableXParallax = true;
    [SerializeField] private bool enableYParallax = true;
    [SerializeField] private bool lockZ = true;

    [Header("Sorting")]
    [SerializeField] private bool applySortingToChildren = true;
    [SerializeField] private bool includeInactiveChildren = true;

    [Header("Layer Visual")]
    [SerializeField] private bool applyLayerVisualMaterial = true;
    [SerializeField] private Material visualMaterialOverride;

    private const string LayerVisualResourceFolder = "LayerVisuals/";
    private const string LayerVisualMaterialPrefix = "M_";
    private const string LayerVisualMaterialSuffix = "LayerVisual";

    private static readonly Dictionary<ParallaxDepth, Material> layerVisualMaterialCache = new Dictionary<ParallaxDepth, Material>();

    private Vector3 originalLayerPosition;
    private Vector3 originalCameraPosition;
    private bool hasCameraStartPosition;
    private bool hasWarnedMissingCamera;
    private bool hasWarnedMissingVisualMaterial;

    private void Awake()
    {
        ApplyDepthPreset();
        ApplyRendererSorting();
        ApplyLayerVisualMaterial();

        // The layer always returns to this base position plus the camera offset.
        originalLayerPosition = transform.position;
    }

    private void OnEnable()
    {
        ApplyRendererSorting();
        ApplyLayerVisualMaterial();
    }

    private void Reset()
    {
        parallaxDepth = ParallaxDepth.Mid;
        useDepthMovementPreset = false;
        parallaxFactor = 0f;
        applyLayerVisualMaterial = true;
        visualMaterialOverride = null;
        ApplyDepthPreset();
        ApplyRendererSorting();
        ApplyLayerVisualMaterial();
    }

    private void OnValidate()
    {
        ApplyDepthPreset();
        ApplyRendererSorting();
        ApplyLayerVisualMaterial();
    }

    private void Start()
    {
        ResolveCameraTransform();
        CaptureCameraStartPosition();
    }

    private void LateUpdate()
    {
        if (!EnsureCameraReady())
            return;

        // Parallax is based only on how far the camera has moved from its start.
        Vector3 cameraDelta = cameraTransform.position - originalCameraPosition;
        Vector3 targetPosition = originalLayerPosition;

        // Axis toggles let each layer decide whether it reacts horizontally,
        // vertically, or both.
        if (enableXParallax)
            targetPosition.x = originalLayerPosition.x + cameraDelta.x * parallaxFactor;

        if (enableYParallax)
            targetPosition.y = originalLayerPosition.y + cameraDelta.y * parallaxFactor;

        // In 2D scenes, sorting is often tied to Z, so keep it stable by default.
        if (lockZ)
            targetPosition.z = originalLayerPosition.z;

        transform.position = targetPosition;
    }

    private bool EnsureCameraReady()
    {
        if (cameraTransform == null)
            ResolveCameraTransform();

        if (cameraTransform == null)
        {
            if (!hasWarnedMissingCamera)
            {
                Debug.LogWarning($"{nameof(S_ParallaxLayer)} on {name} needs a camera transform or a Main Camera in the scene.", this);
                hasWarnedMissingCamera = true;
            }

            return false;
        }

        if (!hasCameraStartPosition)
            CaptureCameraStartPosition();

        return hasCameraStartPosition;
    }

    private void ResolveCameraTransform()
    {
        if (cameraTransform != null || !findMainCameraIfMissing)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            cameraTransform = mainCamera.transform;
    }

    private void CaptureCameraStartPosition()
    {
        if (cameraTransform == null)
            return;

        originalCameraPosition = cameraTransform.position;
        hasCameraStartPosition = true;
        hasWarnedMissingCamera = false;
    }

    private void ApplyDepthPreset()
    {
        if (!useDepthMovementPreset)
            return;

        parallaxFactor = GetPresetParallaxFactor(parallaxDepth);
    }

    private void ApplyRendererSorting()
    {
        if (!applySortingToChildren)
            return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactiveChildren);
        string presetSortingLayerName = parallaxDepth.ToString();
        int sortingLayerId = SortingLayer.NameToID(presetSortingLayerName);
        bool hasValidSortingLayer = SortingLayer.IsValid(sortingLayerId);

        // The enum name should match the Sorting Layer name exactly.
        // Example: ParallaxDepth.Far uses the "Far" Sorting Layer.
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null)
                continue;

            if (!IsManagedRenderer(targetRenderer))
                continue;

            // If the layer name does not exist yet, keep Unity's current layer.
            if (hasValidSortingLayer)
                targetRenderer.sortingLayerName = presetSortingLayerName;
        }
    }

    private void ApplyLayerVisualMaterial()
    {
        if (!applyLayerVisualMaterial)
            return;

        Material layerVisualMaterial = ResolveLayerVisualMaterial();
        if (layerVisualMaterial == null)
            return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactiveChildren);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (!IsManagedRenderer(targetRenderer) || !SupportsLayerVisualMaterial(targetRenderer))
                continue;

            if (targetRenderer.sharedMaterial != layerVisualMaterial)
                targetRenderer.sharedMaterial = layerVisualMaterial;
        }
    }

    private Material ResolveLayerVisualMaterial()
    {
        if (visualMaterialOverride != null)
        {
            hasWarnedMissingVisualMaterial = false;
            return visualMaterialOverride;
        }

        if (!layerVisualMaterialCache.TryGetValue(parallaxDepth, out Material material) || material == null)
        {
            string resourcePath = $"{LayerVisualResourceFolder}{LayerVisualMaterialPrefix}{parallaxDepth}{LayerVisualMaterialSuffix}";
            material = Resources.Load<Material>(resourcePath);
            layerVisualMaterialCache[parallaxDepth] = material;
        }

        if (material == null && !hasWarnedMissingVisualMaterial)
        {
            Debug.LogWarning($"{nameof(S_ParallaxLayer)} on {name} could not find a layer visual material for {parallaxDepth}.", this);
            hasWarnedMissingVisualMaterial = true;
        }
        else if (material != null)
        {
            hasWarnedMissingVisualMaterial = false;
        }

        return material;
    }

    private bool IsManagedRenderer(Renderer targetRenderer)
    {
        return targetRenderer != null && targetRenderer.GetComponentInParent<S_ParallaxLayer>() == this;
    }

    private static bool SupportsLayerVisualMaterial(Renderer targetRenderer)
    {
        return targetRenderer is SpriteRenderer || targetRenderer is TilemapRenderer;
    }

    private float GetPresetParallaxFactor(ParallaxDepth depth)
    {
        switch (depth)
        {
            case ParallaxDepth.Far:
                return 0.10f;
            case ParallaxDepth.Back:
                return 0.25f;
            case ParallaxDepth.Near:
                return 0.80f;
            case ParallaxDepth.Front:
                return 1.15f;
            case ParallaxDepth.Mid:
            default:
                return 0f;
        }
    }
}
