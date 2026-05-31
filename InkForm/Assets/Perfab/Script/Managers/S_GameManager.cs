using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class S_GameManager : MonoBehaviour
{
    private const string SceneTransitionInputLockId = "SceneTransition";

    public static S_GameManager Instance { get; private set; }

    [Header("Legacy Start Scene")]
    [SerializeField] private S_SceneReference legacyStartScene = new S_SceneReference();
    [SerializeField, HideInInspector] private string scene;

    [Header("Level Flow")]
    [SerializeField] private S_SceneReference startMenuScene = new S_SceneReference("Assets/Scenes/For_game/Start.unity");
    [SerializeField] private S_SceneReference[] levelScenes =
    {
        new S_SceneReference("Assets/Scenes/For_test/Playtest1.unity"),
        new S_SceneReference("Assets/Scenes/For_test/NPCPlayTestScene.unity"),
        new S_SceneReference("Assets/Scenes/For_game/END.unity")
    };
    [SerializeField, HideInInspector] private string startMenuSceneName = "Start";
    [SerializeField, HideInInspector] private string[] levelSceneNames = { "Playtest1", "NPCPlayTestScene", "END" };
    [SerializeField] private int currentLevelIndex = -1;

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
    [SerializeField, HideInInspector] private float transitionHoldTime = 0.05f;

    private bool isFrameRateUnlocked;
    private bool levelExitDeciding;
    private Canvas transitionCanvas;
    private Image transitionImage;
    private Coroutine sceneLoadRoutine;
    private float defaultFixedDeltaTime;

    public bool IsFrameRateUnlocked => isFrameRateUnlocked;
    public int LockedFrameRate => Mathf.Max(1, lockedFrameRate);

    void Awake()
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

    void Update()
    {
        if (!allowRuntimeFrameRateToggle)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f4Key.wasPressedThisFrame)
            SetFrameRateUnlocked(!isFrameRateUnlocked);
    }

    void OnEnable()
    {
        S_GameEvent.OnGameStart += HandleGameStart;
        S_GameEvent.OnGameRestart += HandleGameRestart;
        S_GameEvent.OnExit += HandleExit;
        S_GameEvent.OnArrestTriggered += HandleArrest;
        S_GameEvent.OnLevelExitRequested += HandleLevelExitRequested;
        S_GameEvent.OnStartFreshGameRequested += StartFreshGameFromMenu;
        S_GameEvent.OnReturnToStartMenuRequested += ReturnToStartMenu;
        S_GameEvent.OnRestartCurrentLevelRequested += ReloadCurrentLevel;
        S_GameEvent.OnSceneLoadRequested += HandleSceneLoadRequested;
    }

    void OnDisable()
    {
        S_GameEvent.OnGameStart -= HandleGameStart;
        S_GameEvent.OnGameRestart -= HandleGameRestart;
        S_GameEvent.OnExit -= HandleExit;
        S_GameEvent.OnArrestTriggered -= HandleArrest;
        S_GameEvent.OnLevelExitRequested -= HandleLevelExitRequested;
        S_GameEvent.OnStartFreshGameRequested -= StartFreshGameFromMenu;
        S_GameEvent.OnReturnToStartMenuRequested -= ReturnToStartMenu;
        S_GameEvent.OnRestartCurrentLevelRequested -= ReloadCurrentLevel;
        S_GameEvent.OnSceneLoadRequested -= HandleSceneLoadRequested;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (transitionHoldTime > 0f && Mathf.Approximately(transitionPostLoadHoldTime, 0.05f))
            transitionPostLoadHoldTime = transitionHoldTime;

        SyncSceneReferenceForEditor(ref legacyStartScene, scene, "legacy start scene");
        SyncSceneReferenceForEditor(ref startMenuScene, startMenuSceneName, "start menu scene");
        SyncLevelReferencesForEditor();
    }
#endif

    void HandleGameStart() => GameStart();
    void HandleGameRestart() => GameReStart();
    void HandleExit() => ExitGame();
    void HandleLevelExitRequested()
    {
        // Defer one frame so other LevelExitRequested subscribers (e.g. the tutorial controller
        // setting up a fixed->training handoff) run first, regardless of subscription order.
        if (levelExitDeciding)
            return;

        levelExitDeciding = true;
        StartCoroutine(DecideLevelExitNextFrame());
    }

    private IEnumerator DecideLevelExitNextFrame()
    {
        yield return null;
        levelExitDeciding = false;

        // Let the progression controller own the run once it has taken over; otherwise linear advance.
        if (S_ProgressionController.Instance != null && S_ProgressionController.Instance.HandleLevelExit())
            yield break;

        LoadNextLevel();
    }
    void HandleSceneLoadRequested(string sceneKey) => LoadSceneByKey(sceneKey);

    public void SetFrameRateUnlocked(bool unlocked)
    {
        ApplyFrameRateMode(unlocked, true);
    }

    private void ApplyFrameRateMode(bool unlocked, bool logChange)
    {
        isFrameRateUnlocked = unlocked;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = unlocked ? -1 : LockedFrameRate;

        if (!logChange)
            return;

        if (unlocked)
            Debug.Log("[GameManager] Frame rate unlocked");
        else
            Debug.Log($"[GameManager] Frame rate locked to {LockedFrameRate} FPS");
    }

    void GameStart()
    {
        StartGameFromMenu();
    }

    void GameReStart()
    {
        Time.timeScale = 1f;
        S_GameEvent.SuspicionResetRequested();
    }

    public void StartGameFromMenu()
    {
        if (GetConfiguredLevelCount() > 0)
        {
            LoadLevel(0);
            return;
        }

        string legacySceneKey = GetSceneKey(legacyStartScene, scene);
        if (!string.IsNullOrEmpty(legacySceneKey))
        {
            currentLevelIndex = -1;
            LoadSceneByKey(legacySceneKey);
            return;
        }

        Debug.LogWarning("[GameManager] No level scenes configured.");
    }

    public void StartFreshGameFromMenu()
    {
        string firstLevelScene = GetFreshStartSceneKey();
        if (string.IsNullOrWhiteSpace(firstLevelScene))
        {
            Debug.LogWarning("[GameManager] No first level scene configured.");
            return;
        }

        if (!CanLoadSceneKey(firstLevelScene))
            return;

        Time.timeScale = 1f;
        S_GameEvent.SuspicionResetRequested();
        currentLevelIndex = IsConfiguredLevel(firstLevelScene, 0) ? 0 : -1;
        LoadSceneByKey(firstLevelScene, true);
    }

    public void LoadLevel(int index)
    {
        if (index < 0 || index >= GetConfiguredLevelCount())
        {
            Debug.LogWarning($"[GameManager] Level index out of range: {index}");
            return;
        }

        string levelScene = GetLevelSceneKey(index);
        if (string.IsNullOrWhiteSpace(levelScene))
        {
            Debug.LogWarning($"[GameManager] Level scene at index {index} is empty.");
            return;
        }

        if (!CanLoadSceneKey(levelScene))
            return;

        currentLevelIndex = index;
        LoadSceneByKey(levelScene, true);
    }

    public void LoadNextLevel()
    {
        int nextIndex = currentLevelIndex + 1;
        if (nextIndex >= 0 && nextIndex < GetConfiguredLevelCount())
        {
            LoadLevel(nextIndex);
            return;
        }

        ReturnToStartMenu();
    }

    public void ReloadCurrentLevel()
    {
        if (currentLevelIndex >= 0 && currentLevelIndex < GetConfiguredLevelCount())
        {
            LoadLevel(currentLevelIndex);
            return;
        }

        LoadSceneByKey(SceneManager.GetActiveScene().name);
    }

    public void ReturnToStartMenu()
    {
        currentLevelIndex = -1;
        string startMenuSceneKey = GetSceneKey(startMenuScene, startMenuSceneName);
        if (string.IsNullOrWhiteSpace(startMenuSceneKey))
        {
            Debug.LogWarning("[GameManager] Start menu scene is empty.");
            return;
        }

        LoadSceneByKey(startMenuSceneKey);
    }

    private string GetFreshStartSceneKey()
    {
        if (GetConfiguredLevelCount() > 0)
        {
            string firstLevelScene = GetLevelSceneKey(0);
            if (!string.IsNullOrWhiteSpace(firstLevelScene))
                return firstLevelScene;
        }

        return GetSceneKey(legacyStartScene, scene);
    }

    private bool IsConfiguredLevel(string sceneKey, int index)
    {
        return index >= 0
            && index < GetConfiguredLevelCount()
            && S_SceneReference.SceneKeysMatch(GetLevelSceneKey(index), sceneKey);
    }

    private int GetConfiguredLevelCount()
    {
        int referenceCount = levelScenes != null ? levelScenes.Length : 0;
        int legacyCount = levelSceneNames != null ? levelSceneNames.Length : 0;
        return Mathf.Max(referenceCount, legacyCount);
    }

    private string GetLevelSceneKey(int index)
    {
        if (levelScenes != null && index >= 0 && index < levelScenes.Length)
        {
            string sceneKey = GetSceneKey(levelScenes[index], null);
            if (!string.IsNullOrWhiteSpace(sceneKey))
                return sceneKey;
        }

        if (levelSceneNames != null && index >= 0 && index < levelSceneNames.Length)
            return levelSceneNames[index];

        return string.Empty;
    }

    private string GetSceneKey(S_SceneReference sceneReference, string fallback)
    {
        if (sceneReference != null && sceneReference.IsValid)
            return sceneReference.RuntimeKey;

        return fallback;
    }

    private void LoadSceneByKey(string sceneKey, bool alreadyValidated = false)
    {
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            Debug.LogWarning("[GameManager] Scene is empty.");
            return;
        }

        if (!alreadyValidated && !CanLoadSceneKey(sceneKey))
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

    private bool CanLoadSceneKey(string sceneKey)
    {
        if (S_SceneReference.CanLoadScene(sceneKey))
            return true;

        Debug.LogError($"[GameManager] Scene '{sceneKey}' cannot be loaded. Drag a valid scene asset in the GameManager Inspector and make sure it is enabled in File > Build Profiles / Build Settings.");
        return false;
    }

    private void LoadSceneImmediate(string sceneKey)
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
        if (S_PlayerLookup.TryGetActive(out IPlayerActor player))
            player.CancelActiveSkills();

        S_GameEvent.SuspicionResetRequested();
        SceneManager.LoadScene(sceneKey);
    }

    private IEnumerator LoadSceneWithTransition(string sceneKey)
    {
        S_GameEvent.PushGameplayInputLock(SceneTransitionInputLockId);
        try
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultFixedDeltaTime;
            if (S_PlayerLookup.TryGetActive(out IPlayerActor player))
                player.CancelActiveSkills();

            S_GameEvent.SuspicionResetRequested();
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

    void HandleArrest()
    {
        Debug.Log("[GameManager] Player arrested");
        S_GameEvent.SuspicionResetRequested();

        // Death UI is shown via OnPlayerDied → ShowUI()
        // Player manually clicks Restart to reload the scene
    }

    void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        Application.OpenURL(Application.absoluteURL);
#else
        Application.Quit();
#endif
    }

#if UNITY_EDITOR
    private void SyncLevelReferencesForEditor()
    {
        int legacyCount = levelSceneNames != null ? levelSceneNames.Length : 0;
        if (legacyCount > 0 && (levelScenes == null || levelScenes.Length < legacyCount))
        {
            int oldLength = levelScenes != null ? levelScenes.Length : 0;
            Array.Resize(ref levelScenes, legacyCount);
            for (int i = oldLength; i < levelScenes.Length; i++)
                levelScenes[i] = new S_SceneReference();
        }

        if (levelScenes == null)
            return;

        for (int i = 0; i < levelScenes.Length; i++)
        {
            string fallback = levelSceneNames != null && i < levelSceneNames.Length ? levelSceneNames[i] : null;
            SyncSceneReferenceForEditor(ref levelScenes[i], fallback, $"level scene {i}");
        }
    }

    private void SyncSceneReferenceForEditor(ref S_SceneReference sceneReference, string fallback, string label)
    {
        if (sceneReference == null)
            sceneReference = new S_SceneReference();

        sceneReference.EditorSyncAsset();

        string key = sceneReference.IsValid ? sceneReference.RuntimeKey : fallback;
        if (!string.IsNullOrWhiteSpace(key))
            sceneReference.EditorTryAssignByKey(key);

        if (sceneReference.IsValid && !sceneReference.EditorIsInEnabledBuildScenes())
            Debug.LogWarning($"[GameManager] {label} '{sceneReference.RuntimeKey}' is not enabled in File > Build Profiles / Build Settings.", this);
    }
#endif
}
