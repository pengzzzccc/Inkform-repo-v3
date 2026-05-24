using UnityEngine;
using UnityEngine.Serialization;

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

    private Vector3 originalLayerPosition;
    private Vector3 originalCameraPosition;
    private bool hasCameraStartPosition;
    private bool hasWarnedMissingCamera;

    private void Awake()
    {
        ApplyDepthPreset();
        ApplyRendererSorting();

        // The layer always returns to this base position plus the camera offset.
        originalLayerPosition = transform.position;
    }

    private void Reset()
    {
        parallaxDepth = ParallaxDepth.Mid;
        useDepthMovementPreset = false;
        parallaxFactor = 0f;
        ApplyDepthPreset();
        ApplyRendererSorting();
    }

    private void OnValidate()
    {
        ApplyDepthPreset();
        ApplyRendererSorting();
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

            // If the layer name does not exist yet, keep Unity's current layer.
            if (hasValidSortingLayer)
                targetRenderer.sortingLayerName = presetSortingLayerName;
        }
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
