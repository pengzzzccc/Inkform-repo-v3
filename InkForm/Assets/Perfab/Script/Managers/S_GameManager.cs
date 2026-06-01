using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class S_GameManager : MonoBehaviour
{
    private const string SceneTransitionInputLockId = "SceneTransition";

    public static S_GameManager Instance { get; private set; }

    [Header("Frame Rate")]
    [SerializeField] private bool unlockFrameRateOnStart = true;
    [SerializeField, Min(1)] private int lockedFrameRate = 120;
    [SerializeField] private bool allowRuntimeFrameRateToggle = true;

    [Header("Scene Transition")]
    [SerializeField] private bool useSceneTransition = true;
    [SerializeField, Min(0f)] private float transitionFadeOutTime = 0.35f;
    [SerializeField, Min(0f)] private float transitionPreLoadHoldTime = 0f;
    [SerializeField, Min(0f)] private float transitionPostLoadHoldTime = 0.05f;
    [SerializeField, Min(0f)] private float transitionFadeInTime = 0.35f;
    [SerializeField] private Color transitionColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private AudioClip transitionClip;

    private bool isFrameRateUnlocked;
    private Canvas transitionCanvas;
    private Image transitionImage;
    private Coroutine sceneLoadRoutine;
    private float defaultFixedDeltaTime;

    public bool IsFrameRateUnlocked => isFrameRateUnlocked;
    public int LockedFrameRate => Mathf.Max(1, lockedFrameRate);
    public bool IsSceneTransitionRunning => sceneLoadRoutine != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        ApplyFrameRateMode(unlockFrameRateOnStart, false);
    }

    private void OnEnable()
    {
        S_GameEvent.OnExit += HandleExit;
        S_GameEvent.OnArrestTriggered += HandleArrest;
        S_GameEvent.OnSceneLoadRequested += LoadScene;
    }

    private void OnDisable()
    {
        S_GameEvent.OnExit -= HandleExit;
        S_GameEvent.OnArrestTriggered -= HandleArrest;
        S_GameEvent.OnSceneLoadRequested -= LoadScene;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!allowRuntimeFrameRateToggle)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f4Key.wasPressedThisFrame)
            SetFrameRateUnlocked(!isFrameRateUnlocked);
    }

    public void SetFrameRateUnlocked(bool unlocked)
    {
        ApplyFrameRateMode(unlocked, true);
    }

    public void LoadScene(string sceneKey)
    {
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            Debug.LogWarning("[GameManager] Scene is empty.");
            return;
        }

        if (!CanLoadSceneKey(sceneKey))
            return;

        if (!useSceneTransition || !Application.isPlaying || !isActiveAndEnabled)
        {
            LoadSceneImmediate(sceneKey);
            return;
        }

        if (sceneLoadRoutine != null)
            return;

        sceneLoadRoutine = StartCoroutine(LoadSceneWithTransition(sceneKey));
    }

    public void ReloadCurrentScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        string sceneKey = !string.IsNullOrWhiteSpace(activeScene.path) ? activeScene.path : activeScene.name;
        LoadScene(sceneKey);
    }

    private void ApplyFrameRateMode(bool unlocked, bool logChange)
    {
        isFrameRateUnlocked = unlocked;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = unlocked ? -1 : LockedFrameRate;

        if (!logChange)
            return;

        Debug.Log(unlocked
            ? "[GameManager] Frame rate unlocked"
            : $"[GameManager] Frame rate locked to {LockedFrameRate} FPS");
    }

    private bool CanLoadSceneKey(string sceneKey)
    {
        if (S_SceneReference.CanLoadScene(sceneKey))
            return true;

        Debug.LogError($"[GameManager] Scene '{sceneKey}' cannot be loaded. Drag a valid scene asset and make sure it is enabled in File > Build Profiles / Build Settings.");
        return false;
    }

    private void LoadSceneImmediate(string sceneKey)
    {
        PrepareForSceneChange();
        SceneManager.LoadScene(sceneKey);
    }

    private IEnumerator LoadSceneWithTransition(string sceneKey)
    {
        S_GameEvent.PushGameplayInputLock(SceneTransitionInputLockId);
        try
        {
            PrepareForSceneChange();
            EnsureTransitionOverlay();

            if (transitionClip != null)
                S_GameEvent.PlaySFX(transitionClip);

            yield return FadeTransitionOverlay(0f, 1f, transitionFadeOutTime);

            if (transitionPreLoadHoldTime > 0f)
                yield return new WaitForSecondsRealtime(transitionPreLoadHoldTime);

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneKey);
            if (loadOperation == null)
            {
                Debug.LogError($"[GameManager] Scene '{sceneKey}' failed to start loading.");
                yield return FadeTransitionOverlay(1f, 0f, transitionFadeInTime);
                if (transitionCanvas != null)
                    transitionCanvas.gameObject.SetActive(false);
                yield break;
            }

            while (!loadOperation.isDone)
                yield return null;

            if (transitionPostLoadHoldTime > 0f)
                yield return new WaitForSecondsRealtime(transitionPostLoadHoldTime);

            yield return FadeTransitionOverlay(1f, 0f, transitionFadeInTime);

            if (transitionCanvas != null)
                transitionCanvas.gameObject.SetActive(false);
        }
        finally
        {
            sceneLoadRoutine = null;
            S_GameEvent.PopGameplayInputLock(SceneTransitionInputLockId);
        }
    }

    private void PrepareForSceneChange()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;

        if (S_PlayerLookup.TryGetActive(out IPlayerActor player))
            player.CancelActiveSkills();

        S_GameEvent.SuspicionResetRequested();
    }

    private void EnsureTransitionOverlay()
    {
        if (transitionCanvas != null && transitionImage != null)
        {
            transitionCanvas.gameObject.SetActive(true);
            transitionCanvas.transform.SetAsLastSibling();
            return;
        }

        GameObject canvasObject = new GameObject("SceneTransitionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        transitionCanvas = canvasObject.GetComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366f, 768f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObject = new GameObject("InkFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        transitionImage = imageObject.GetComponent<Image>();
        transitionImage.raycastTarget = true;
        SetTransitionAlpha(0f);
    }

    private IEnumerator FadeTransitionOverlay(float from, float to, float duration)
    {
        EnsureTransitionOverlay();

        if (duration <= 0f)
        {
            SetTransitionAlpha(to);
            yield break;
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            SetTransitionAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetTransitionAlpha(to);
    }

    private void SetTransitionAlpha(float alpha)
    {
        if (transitionImage == null)
            return;

        Color color = transitionColor;
        color.a = Mathf.Clamp01(alpha) * transitionColor.a;
        transitionImage.color = color;
    }

    private void HandleArrest()
    {
        Debug.Log("[GameManager] Player arrested");
        S_GameEvent.SuspicionResetRequested();
    }

    private void HandleExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        Application.OpenURL(Application.absoluteURL);
#else
        Application.Quit();
#endif
    }
}
