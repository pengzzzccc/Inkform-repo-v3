using UnityEngine;
using UnityEngine.SceneManagement;

public class S_GameManager : MonoBehaviour
{
    public static S_GameManager Instance { get; private set; }

    [SerializeField] private string scene;

    private S_Player player;
    private Transform spwnPoint;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        player = FindAnyObjectByType<S_Player>();
        if (player != null)
            spwnPoint = player.transform;
        else
            Debug.LogError("[GameManager] S_Player not found in scene!");
    }

    void OnEnable()
    {
        S_GameEvent.OnPlayerDied += HandlePlayerDied;
        S_GameEvent.OnGameStart += HandleGameStart;
        S_GameEvent.OnGameRestart += HandleGameRestart;
        S_GameEvent.OnExit += HandleExit;
        S_GameEvent.reNewSpwnPoint += newSpwn;
    }

    void OnDisable()
    {
        S_GameEvent.OnPlayerDied -= HandlePlayerDied;
        S_GameEvent.OnGameStart -= HandleGameStart;
        S_GameEvent.OnGameRestart -= HandleGameRestart;
        S_GameEvent.OnExit -= HandleExit;
        S_GameEvent.reNewSpwnPoint -= newSpwn;
    }

    void HandleGameStart() => GameStart();
    void HandleGameRestart() => GameReStart();
    void HandleExit() => ExitGame();
    void HandlePlayerDied() => PlayerDied();

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
        if (!string.IsNullOrEmpty(scene))
            SceneManager.LoadScene(scene);
        else
            Debug.LogWarning("[GameManager] Scene name is empty!");
        Time.timeScale = 1f;
    }

    void GameReStart()
    {
        Time.timeScale = 1f;
        if (S_Player.Instance != null && spwnPoint != null)
            S_Player.Instance.GetRigidbody().transform.position = spwnPoint.position;
    }

    void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}