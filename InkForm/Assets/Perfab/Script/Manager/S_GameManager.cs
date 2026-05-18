using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class S_GameManager : MonoBehaviour
{
    public static S_GameManager Instance { get; private set; }

    [Header("Legacy Start Scene")]
    [SerializeField] private string scene;

    [Header("Level Flow")]
    [SerializeField] private string startMenuSceneName = "Start";
    [SerializeField] private string[] levelSceneNames = { "Playtest1" };
    [SerializeField] private int currentLevelIndex = -1;

    [Header("Frame Rate")]
    [SerializeField] private bool unlockFrameRateOnStart = true;
    [SerializeField, Min(1)] private int lockedFrameRate = 120;
    [SerializeField] private bool allowRuntimeFrameRateToggle = true;

    private S_Player player;
    private Transform spwnPoint;
    private bool isFrameRateUnlocked;

    public bool IsFrameRateUnlocked => isFrameRateUnlocked;
    public int LockedFrameRate => Mathf.Max(1, lockedFrameRate);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);
        ApplyFrameRateMode(unlockFrameRateOnStart, false);
    }

    void Start()
    {
        RefreshSceneReferences();
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
        SceneManager.sceneLoaded += HandleSceneLoaded;
        S_GameEvent.OnPlayerDied += HandlePlayerDied;
        S_GameEvent.OnGameStart += HandleGameStart;
        S_GameEvent.OnGameRestart += HandleGameRestart;
        S_GameEvent.OnExit += HandleExit;
        S_GameEvent.OnArrestTriggered += HandleArrest;
        S_GameEvent.reNewSpwnPoint += newSpwn;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        S_GameEvent.OnPlayerDied -= HandlePlayerDied;
        S_GameEvent.OnGameStart -= HandleGameStart;
        S_GameEvent.OnGameRestart -= HandleGameRestart;
        S_GameEvent.OnExit -= HandleExit;
        S_GameEvent.OnArrestTriggered -= HandleArrest;
        S_GameEvent.reNewSpwnPoint -= newSpwn;
    }

    void HandleGameStart() => GameStart();
    void HandleGameRestart() => GameReStart();
    void HandleExit() => ExitGame();
    void HandlePlayerDied() => PlayerDied();

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

    private void HandleSceneLoaded(Scene loadedScene, LoadSceneMode mode)
    {
        RefreshSceneReferences();
    }

    private void RefreshSceneReferences()
    {
        player = FindAnyObjectByType<S_Player>();
        if (player != null)
        {
            spwnPoint = player.transform;
            return;
        }

        spwnPoint = null;
        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.Equals(activeScene.name, startMenuSceneName, System.StringComparison.OrdinalIgnoreCase))
            Debug.LogWarning($"[GameManager] S_Player not found in scene: {activeScene.name}");
    }

    void newSpwn(Transform trans)
    {
        Debug.Log("update spwnPoint");
        spwnPoint = trans;
    }

    void PlayerDied()
    {
        if (S_Player.Instance != null && spwnPoint != null)
            S_Player.Instance.GetRigidbody().transform.position = spwnPoint.position;
    }

    void GameStart()
    {
        StartGameFromMenu();
    }

    void GameReStart()
    {
        Time.timeScale = 1f;

        // Reset PlayerHidden so NPCs can detect the player after restart
        S_SuspicionSystem.PlayerHidden = false;

        if (S_Player.Instance != null && spwnPoint != null)
            S_Player.Instance.GetRigidbody().transform.position = spwnPoint.position;
        else
            ReloadCurrentLevel();
    }

    public void StartGameFromMenu()
    {
        if (levelSceneNames != null && levelSceneNames.Length > 0)
        {
            LoadLevel(0);
            return;
        }

        if (!string.IsNullOrEmpty(scene))
        {
            currentLevelIndex = -1;
            LoadSceneByName(scene);
            return;
        }

        Debug.LogWarning("[GameManager] No level scenes configured.");
    }

    public void StartFreshGameFromMenu()
    {
        string firstLevelScene = GetFreshStartSceneName();
        if (string.IsNullOrWhiteSpace(firstLevelScene))
        {
            Debug.LogWarning("[GameManager] No first level scene configured.");
            return;
        }

        Time.timeScale = 1f;
        S_SuspicionSystem.PlayerHidden = false;
        spwnPoint = null;
        player = null;
        currentLevelIndex = IsConfiguredLevel(firstLevelScene, 0) ? 0 : -1;
        DestroyRuntimeUIManager();
        LoadSceneByName(firstLevelScene);
    }

    public void LoadLevel(int index)
    {
        if (levelSceneNames == null || index < 0 || index >= levelSceneNames.Length)
        {
            Debug.LogWarning($"[GameManager] Level index out of range: {index}");
            return;
        }

        string levelScene = levelSceneNames[index];
        if (string.IsNullOrWhiteSpace(levelScene))
        {
            Debug.LogWarning($"[GameManager] Level scene at index {index} is empty.");
            return;
        }

        currentLevelIndex = index;
        LoadSceneByName(levelScene);
    }

    public void LoadNextLevel()
    {
        int nextIndex = currentLevelIndex + 1;
        if (levelSceneNames != null && nextIndex >= 0 && nextIndex < levelSceneNames.Length)
        {
            LoadLevel(nextIndex);
            return;
        }

        ReturnToStartMenu();
    }

    public void ReloadCurrentLevel()
    {
        if (currentLevelIndex >= 0 && levelSceneNames != null && currentLevelIndex < levelSceneNames.Length)
        {
            LoadLevel(currentLevelIndex);
            return;
        }

        LoadSceneByName(SceneManager.GetActiveScene().name);
    }

    public void ReturnToStartMenu()
    {
        currentLevelIndex = -1;
        if (string.IsNullOrWhiteSpace(startMenuSceneName))
        {
            Debug.LogWarning("[GameManager] Start menu scene name is empty.");
            return;
        }

        LoadSceneByName(startMenuSceneName);
    }

    private string GetFreshStartSceneName()
    {
        if (levelSceneNames != null && levelSceneNames.Length > 0 && !string.IsNullOrWhiteSpace(levelSceneNames[0]))
            return levelSceneNames[0];

        return scene;
    }

    private bool IsConfiguredLevel(string sceneName, int index)
    {
        return levelSceneNames != null
            && index >= 0
            && index < levelSceneNames.Length
            && string.Equals(levelSceneNames[index], sceneName, System.StringComparison.OrdinalIgnoreCase);
    }

    private void DestroyRuntimeUIManager()
    {
        if (S_UIManager.Instance == null)
            return;

        Destroy(S_UIManager.Instance.gameObject);
    }

    private void LoadSceneByName(string sceneName)
    {
        Time.timeScale = 1f;
        S_SuspicionSystem.PlayerHidden = false;
        SceneManager.LoadScene(sceneName);
    }

    void HandleArrest()
    {
        Debug.Log("[GameManager] Player arrested");
        // Reset suspicion
        if (S_SuspicionSystem.Instance != null)
            S_SuspicionSystem.Instance.SetSuspicion(0f);

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
}
