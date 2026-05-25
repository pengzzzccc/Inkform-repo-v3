using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Profiling;

[DisallowMultipleComponent]
public class S_PerformanceMonitor : MonoBehaviour
{
    private const Key ToggleKey = Key.F3;
    private const float MinimumSampleInterval = 0.05f;
    private const float SpikeLogCooldown = 2f;
    private const int NvmlSuccess = 0;

    public static S_PerformanceMonitor Instance { get; private set; }

    [Header("Display")]
    [SerializeField] private bool showOnStart = false;
    [SerializeField, Min(MinimumSampleInterval)] private float sampleInterval = 0.25f;
    [SerializeField, Min(0.5f)] private float sceneStatsInterval = 2f;

    [Header("Spike Detection")]
    [SerializeField, Min(1f)] private float spikeThresholdMs = 33.3f;
    [SerializeField] private bool logSpikesToConsole = true;

    [Header("Windows CSV Logging")]
    [SerializeField] private bool windowsCsvLogging = false;

    private readonly FrameTiming[] frameTimings = new FrameTiming[1];
    private readonly StringBuilder overlayBuilder = new StringBuilder(1400);

    private ProfilerRecorder gcAllocatedInFrameRecorder;
    private ProfilerRecorder gcUsedMemoryRecorder;
    private ProfilerRecorder totalUsedMemoryRecorder;
    private ProfilerRecorder batchesRecorder;
    private ProfilerRecorder setPassRecorder;
    private ProfilerRecorder trianglesRecorder;
    private ProfilerRecorder verticesRecorder;

    private bool isVisible;
    private bool recordersStarted;
    private float sampleTimer;
    private float sceneStatsTimer;
    private float oneSecondTimer;
    private float lastSpikeLogTime = -SpikeLogCooldown;
    private int oneSecondFrameCount;
    private int activeGameObjectCount;
    private string overlayText = string.Empty;
    private string sceneName = string.Empty;

    private float currentFps;
    private float averageFps;
    private float currentFrameMs;
    private float worstFrameMs;
    private float cpuFrameMs;
    private float gpuFrameMs;
    private float cpuPowerWatts;
    private float gpuPowerWatts;
    private bool cpuFrameAvailable;
    private bool gpuFrameAvailable;
    private bool cpuPowerAvailable;
    private bool gpuPowerAvailable;

    private long gcAllocatedInFrameBytes;
    private long gcUsedMemoryBytes;
    private long totalUsedMemoryBytes;
    private long batchesCount;
    private long setPassCallsCount;
    private long trianglesCount;
    private long verticesCount;
    private bool gcAllocatedInFrameAvailable;
    private bool gcUsedMemoryAvailable;
    private bool totalUsedMemoryAvailable;
    private bool batchesCountAvailable;
    private bool setPassCallsCountAvailable;
    private bool trianglesCountAvailable;
    private bool verticesCountAvailable;

    private GUIStyle panelStyle;
    private GUIStyle labelStyle;
    private Texture2D panelTexture;
    private StreamWriter csvWriter;
    private string csvPath;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private IntPtr nvmlDevice;
    private bool nvmlInitialized;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            S_ManagerRoot.DestroyDuplicate(this);
            return;
        }

        Instance = this;
        isVisible = showOnStart;
        sceneName = SceneManager.GetActiveScene().name;
        BuildOverlayText();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        StartRecorders();
        StartPowerSensors();
        OpenCsvWriterIfNeeded();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        StopRecorders();
        StopPowerSensors();
        CloseCsvWriter();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        StopRecorders();
        StopPowerSensors();
        CloseCsvWriter();
        DestroyRuntimeObject(panelTexture);
    }

    private void Update()
    {
        HandleToggleInput();

        float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.000001f);
        currentFrameMs = deltaTime * 1000f;
        currentFps = 1f / deltaTime;
        worstFrameMs = Mathf.Max(worstFrameMs, currentFrameMs);

        oneSecondTimer += deltaTime;
        oneSecondFrameCount++;
        if (oneSecondTimer >= 1f)
        {
            averageFps = oneSecondFrameCount / oneSecondTimer;
            oneSecondTimer = 0f;
            oneSecondFrameCount = 0;
        }

        if (logSpikesToConsole && currentFrameMs >= spikeThresholdMs && Time.unscaledTime - lastSpikeLogTime >= SpikeLogCooldown)
        {
            lastSpikeLogTime = Time.unscaledTime;
            Debug.Log($"[PerformanceMonitor] Spike {currentFrameMs:0.0}ms in {sceneName}");
        }

        sampleTimer -= deltaTime;
        if (sampleTimer <= 0f)
        {
            sampleTimer = Mathf.Max(MinimumSampleInterval, sampleInterval);
            SampleMetrics();
            BuildOverlayText();
            WriteCsvSampleIfNeeded();
        }

        if (isVisible)
        {
            sceneStatsTimer -= deltaTime;
            if (sceneStatsTimer <= 0f)
            {
                sceneStatsTimer = Mathf.Max(0.5f, sceneStatsInterval);
                SampleSceneStats();
                BuildOverlayText();
            }
        }

        FrameTimingManager.CaptureFrameTimings();
    }

    private void OnGUI()
    {
        if (!isVisible)
            return;

        EnsureGuiStyles();

        float width = Mathf.Min(520f, Screen.width - 24f);
        float height = labelStyle.CalcHeight(new GUIContent(overlayText), width - 20f) + 20f;
        Rect panelRect = new Rect(12f, 12f, width, height);
        Rect textRect = new Rect(panelRect.x + 10f, panelRect.y + 10f, panelRect.width - 20f, panelRect.height - 20f);

        GUI.Box(panelRect, GUIContent.none, panelStyle);
        GUI.Label(textRect, overlayText, labelStyle);
    }

    public void ToggleVisible()
    {
        SetVisible(!isVisible);
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if (isVisible)
        {
            SampleSceneStats();
            BuildOverlayText();
        }
    }

    private void HandleToggleInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard[ToggleKey] != null && keyboard[ToggleKey].wasPressedThisFrame)
            ToggleVisible();
    }

    private void HandleSceneLoaded(Scene loadedScene, LoadSceneMode mode)
    {
        sceneName = loadedScene.name;
        sceneStatsTimer = 0f;
        worstFrameMs = 0f;
        BuildOverlayText();
    }

    private void StartRecorders()
    {
        if (recordersStarted)
            return;

        gcAllocatedInFrameRecorder = StartRecorder(ProfilerCategory.Memory, "GC Allocated In Frame");
        gcUsedMemoryRecorder = StartRecorder(ProfilerCategory.Memory, "GC Used Memory");
        totalUsedMemoryRecorder = StartRecorder(ProfilerCategory.Memory, "Total Used Memory");
        batchesRecorder = StartRecorder(ProfilerCategory.Render, "Batches Count");
        setPassRecorder = StartRecorder(ProfilerCategory.Render, "SetPass Calls Count");
        trianglesRecorder = StartRecorder(ProfilerCategory.Render, "Triangles Count");
        verticesRecorder = StartRecorder(ProfilerCategory.Render, "Vertices Count");
        recordersStarted = true;
    }

    private static ProfilerRecorder StartRecorder(ProfilerCategory category, string counterName)
    {
        try
        {
            ProfilerRecorder recorder = ProfilerRecorder.StartNew(category, counterName, 1);
            if (recorder.Valid)
                return recorder;

            recorder.Dispose();
        }
        catch
        {
            // Some profiler counters are not available on every platform or build type.
        }

        return default;
    }

    private void StopRecorders()
    {
        if (!recordersStarted)
            return;

        DisposeRecorder(ref gcAllocatedInFrameRecorder);
        DisposeRecorder(ref gcUsedMemoryRecorder);
        DisposeRecorder(ref totalUsedMemoryRecorder);
        DisposeRecorder(ref batchesRecorder);
        DisposeRecorder(ref setPassRecorder);
        DisposeRecorder(ref trianglesRecorder);
        DisposeRecorder(ref verticesRecorder);
        recordersStarted = false;
    }

    private static void DisposeRecorder(ref ProfilerRecorder recorder)
    {
        if (recorder.Valid)
            recorder.Dispose();

        recorder = default;
    }

    private void SampleMetrics()
    {
        SampleFrameTimings();
        SamplePowerSensors();

        gcAllocatedInFrameAvailable = TryGetRecorderValue(gcAllocatedInFrameRecorder, out gcAllocatedInFrameBytes);
        gcUsedMemoryAvailable = TryGetRecorderValue(gcUsedMemoryRecorder, out gcUsedMemoryBytes);
        totalUsedMemoryAvailable = TryGetRecorderValue(totalUsedMemoryRecorder, out totalUsedMemoryBytes);

        if (!gcUsedMemoryAvailable)
        {
            gcUsedMemoryBytes = GC.GetTotalMemory(false);
            gcUsedMemoryAvailable = true;
        }

        if (!totalUsedMemoryAvailable)
        {
            totalUsedMemoryBytes = Profiler.GetTotalAllocatedMemoryLong();
            totalUsedMemoryAvailable = totalUsedMemoryBytes > 0;
        }

        batchesCountAvailable = TryGetRecorderValue(batchesRecorder, out batchesCount);
        setPassCallsCountAvailable = TryGetRecorderValue(setPassRecorder, out setPassCallsCount);
        trianglesCountAvailable = TryGetRecorderValue(trianglesRecorder, out trianglesCount);
        verticesCountAvailable = TryGetRecorderValue(verticesRecorder, out verticesCount);
    }

    private void SampleFrameTimings()
    {
        uint count = FrameTimingManager.GetLatestTimings(1, frameTimings);
        if (count == 0)
        {
            cpuFrameAvailable = false;
            gpuFrameAvailable = false;
            cpuFrameMs = 0f;
            gpuFrameMs = 0f;
            return;
        }

        FrameTiming timing = frameTimings[0];
        cpuFrameMs = (float)timing.cpuFrameTime;
        gpuFrameMs = (float)timing.gpuFrameTime;
        cpuFrameAvailable = cpuFrameMs > 0.001f;
        gpuFrameAvailable = gpuFrameMs > 0.001f;
    }

    private void StartPowerSensors()
    {
        cpuPowerAvailable = false;
        gpuPowerAvailable = false;
        cpuPowerWatts = 0f;
        gpuPowerWatts = 0f;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try
        {
            if (nvmlInit_v2() != NvmlSuccess)
                return;

            if (nvmlDeviceGetHandleByIndex_v2(0, out nvmlDevice) != NvmlSuccess)
            {
                nvmlShutdown();
                nvmlDevice = IntPtr.Zero;
                return;
            }

            nvmlInitialized = true;
        }
        catch
        {
            nvmlInitialized = false;
            nvmlDevice = IntPtr.Zero;
        }
#endif
    }

    private void StopPowerSensors()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (!nvmlInitialized)
            return;

        try
        {
            nvmlShutdown();
        }
        catch
        {
            // Ignore shutdown failures. Power telemetry is best-effort only.
        }

        nvmlInitialized = false;
        nvmlDevice = IntPtr.Zero;
#endif
    }

    private void SamplePowerSensors()
    {
        cpuPowerAvailable = false;
        gpuPowerAvailable = false;
        cpuPowerWatts = 0f;
        gpuPowerWatts = 0f;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (!nvmlInitialized || nvmlDevice == IntPtr.Zero)
            return;

        try
        {
            if (nvmlDeviceGetPowerUsage(nvmlDevice, out uint milliwatts) == NvmlSuccess)
            {
                gpuPowerWatts = milliwatts / 1000f;
                gpuPowerAvailable = true;
            }
        }
        catch
        {
            gpuPowerAvailable = false;
        }
#endif
    }

    private static bool TryGetRecorderValue(ProfilerRecorder recorder, out long value)
    {
        value = 0;
        if (!recorder.Valid || !recorder.IsRunning || recorder.Count <= 0)
            return false;

        value = recorder.LastValue;
        return true;
    }

    private void SampleSceneStats()
    {
        sceneName = SceneManager.GetActiveScene().name;
        GameObject[] activeObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        activeGameObjectCount = activeObjects != null ? activeObjects.Length : 0;
    }

    private void BuildOverlayText()
    {
        overlayBuilder.Length = 0;
        overlayBuilder.AppendLine("InkForm Performance Monitor  (F3)");
        overlayBuilder.AppendLine("--------------------------------");
        overlayBuilder.Append("FPS: ").Append(currentFps.ToString("0.0"));
        overlayBuilder.Append("  Avg: ").Append(averageFps.ToString("0.0"));
        overlayBuilder.Append("  Frame: ").Append(currentFrameMs.ToString("0.00")).Append("ms");
        overlayBuilder.Append("  Worst: ").Append(worstFrameMs.ToString("0.00")).AppendLine("ms");
        overlayBuilder.Append("CPU Frame: ").Append(cpuFrameAvailable ? cpuFrameMs.ToString("0.00") + "ms" : "N/A");
        overlayBuilder.Append("  GPU Frame: ").AppendLine(gpuFrameAvailable ? gpuFrameMs.ToString("0.00") + "ms" : "N/A");
        overlayBuilder.Append("CPU Power: ").Append(cpuPowerAvailable ? cpuPowerWatts.ToString("0.0") + " W" : "N/A");
        overlayBuilder.Append("  GPU Power: ").AppendLine(gpuPowerAvailable ? gpuPowerWatts.ToString("0.0") + " W" : "N/A");
        overlayBuilder.Append("GC Alloc/frame: ").AppendLine(gcAllocatedInFrameAvailable ? FormatBytes(gcAllocatedInFrameBytes) : "N/A");
        overlayBuilder.Append("GC Used: ").Append(gcUsedMemoryAvailable ? FormatBytes(gcUsedMemoryBytes) : "N/A");
        overlayBuilder.Append("  Total Used: ").AppendLine(totalUsedMemoryAvailable ? FormatBytes(totalUsedMemoryBytes) : "N/A");
        overlayBuilder.AppendLine();
        overlayBuilder.Append("Scene: ").Append(sceneName);
        overlayBuilder.Append("  Active Objects: ").AppendLine(activeGameObjectCount.ToString());
        overlayBuilder.Append("Batches: ").Append(batchesCountAvailable ? FormatCount(batchesCount) : "N/A");
        overlayBuilder.Append("  SetPass: ").AppendLine(setPassCallsCountAvailable ? FormatCount(setPassCallsCount) : "N/A");
        overlayBuilder.Append("Triangles: ").Append(trianglesCountAvailable ? FormatCount(trianglesCount) : "N/A");
        overlayBuilder.Append("  Vertices: ").AppendLine(verticesCountAvailable ? FormatCount(verticesCount) : "N/A");
        overlayBuilder.AppendLine();
        overlayBuilder.Append("Platform: ").Append(Application.platform);
        overlayBuilder.Append("  API: ").AppendLine(SystemInfo.graphicsDeviceType.ToString());
        overlayBuilder.Append("GPU: ").AppendLine(SystemInfo.graphicsDeviceName);
        overlayBuilder.Append("Resolution: ").Append(Screen.width).Append("x").Append(Screen.height);
        overlayBuilder.Append("  Quality: ").AppendLine(GetQualityName());
        overlayBuilder.Append("Target FPS: ").Append(Application.targetFrameRate);
        overlayBuilder.Append("  VSync: ").AppendLine(QualitySettings.vSyncCount.ToString());

#if UNITY_WEBGL
        overlayBuilder.AppendLine("CSV: unavailable on WebGL");
#else
        overlayBuilder.Append("CSV: ").AppendLine(csvWriter != null ? csvPath : "off");
#endif

        overlayText = overlayBuilder.ToString();
    }

    private static string GetQualityName()
    {
        string[] names = QualitySettings.names;
        int index = QualitySettings.GetQualityLevel();
        if (names == null || index < 0 || index >= names.Length)
            return index.ToString();

        return names[index];
    }

    private static string FormatBytes(long bytes)
    {
        const double kb = 1024.0;
        const double mb = kb * 1024.0;
        const double gb = mb * 1024.0;

        if (bytes >= gb)
            return (bytes / gb).ToString("0.00") + " GB";
        if (bytes >= mb)
            return (bytes / mb).ToString("0.0") + " MB";
        if (bytes >= kb)
            return (bytes / kb).ToString("0.0") + " KB";

        return bytes + " B";
    }

    private static string FormatCount(long value)
    {
        if (value >= 1000000)
            return (value / 1000000f).ToString("0.00") + "M";
        if (value >= 1000)
            return (value / 1000f).ToString("0.0") + "K";

        return value.ToString();
    }

    private void EnsureGuiStyles()
    {
        if (panelStyle != null && labelStyle != null)
            return;

        panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        panelTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.78f));
        panelTexture.Apply(false, true);

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTexture;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.015f), 12, 18);
        labelStyle.richText = false;
        labelStyle.wordWrap = true;
    }

    private void OpenCsvWriterIfNeeded()
    {
#if UNITY_WEBGL
        return;
#else
        if (!windowsCsvLogging || !IsWindowsRuntime())
            return;

        try
        {
            csvPath = Path.Combine(Application.persistentDataPath, "performance_log.csv");
            csvWriter = new StreamWriter(csvPath, false, Encoding.UTF8);
            csvWriter.WriteLine("time,scene,fps,avg_fps,frame_ms,worst_frame_ms,cpu_ms,gpu_ms,cpu_power_w,gpu_power_w,gc_alloc_frame,gc_used,total_used,batches,setpass,triangles,vertices,active_objects,width,height,quality,graphics_api");
            csvWriter.Flush();
        }
        catch (Exception ex)
        {
            csvWriter = null;
            Debug.LogWarning($"[PerformanceMonitor] CSV logging disabled: {ex.Message}");
        }
#endif
    }

    private static bool IsWindowsRuntime()
    {
        return Application.platform == RuntimePlatform.WindowsPlayer
            || Application.platform == RuntimePlatform.WindowsEditor;
    }

    private void WriteCsvSampleIfNeeded()
    {
#if UNITY_WEBGL
        return;
#else
        if (csvWriter == null)
            return;

        csvWriter.Write(Time.unscaledTime.ToString("0.000"));
        csvWriter.Write(',');
        csvWriter.Write(EscapeCsv(sceneName));
        csvWriter.Write(',');
        csvWriter.Write(currentFps.ToString("0.00"));
        csvWriter.Write(',');
        csvWriter.Write(averageFps.ToString("0.00"));
        csvWriter.Write(',');
        csvWriter.Write(currentFrameMs.ToString("0.000"));
        csvWriter.Write(',');
        csvWriter.Write(worstFrameMs.ToString("0.000"));
        csvWriter.Write(',');
        csvWriter.Write(cpuFrameAvailable ? cpuFrameMs.ToString("0.000") : string.Empty);
        csvWriter.Write(',');
        csvWriter.Write(gpuFrameAvailable ? gpuFrameMs.ToString("0.000") : string.Empty);
        csvWriter.Write(',');
        csvWriter.Write(cpuPowerAvailable ? cpuPowerWatts.ToString("0.000") : string.Empty);
        csvWriter.Write(',');
        csvWriter.Write(gpuPowerAvailable ? gpuPowerWatts.ToString("0.000") : string.Empty);
        csvWriter.Write(',');
        csvWriter.Write(gcAllocatedInFrameAvailable ? gcAllocatedInFrameBytes.ToString() : string.Empty);
        csvWriter.Write(',');
        csvWriter.Write(gcUsedMemoryAvailable ? gcUsedMemoryBytes.ToString() : string.Empty);
        csvWriter.Write(',');
        csvWriter.Write(totalUsedMemoryAvailable ? totalUsedMemoryBytes.ToString() : string.Empty);
        csvWriter.Write(',');
        csvWriter.Write(batchesCountAvailable ? batchesCount.ToString() : string.Empty);
        csvWriter.Write(',');
        csvWriter.Write(setPassCallsCountAvailable ? setPassCallsCount.ToString() : string.Empty);
        csvWriter.Write(',');
        csvWriter.Write(trianglesCountAvailable ? trianglesCount.ToString() : string.Empty);
        csvWriter.Write(',');
        csvWriter.Write(verticesCountAvailable ? verticesCount.ToString() : string.Empty);
        csvWriter.Write(',');
        csvWriter.Write(activeGameObjectCount.ToString());
        csvWriter.Write(',');
        csvWriter.Write(Screen.width.ToString());
        csvWriter.Write(',');
        csvWriter.Write(Screen.height.ToString());
        csvWriter.Write(',');
        csvWriter.Write(EscapeCsv(GetQualityName()));
        csvWriter.Write(',');
        csvWriter.WriteLine(SystemInfo.graphicsDeviceType.ToString());
        csvWriter.Flush();
#endif
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
            return value;

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private void CloseCsvWriter()
    {
        if (csvWriter == null)
            return;

        csvWriter.Flush();
        csvWriter.Dispose();
        csvWriter = null;
    }

    private static void DestroyRuntimeObject(UnityEngine.Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int nvmlInit_v2();

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int nvmlShutdown();

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int nvmlDeviceGetPowerUsage(IntPtr device, out uint power);
#endif
}
