using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class S_UIManager : MonoBehaviour
{
    public static S_UIManager Instance { get; private set; }

    [SerializeField] private GameObject background;
    [SerializeField] private Button StartButton;
    [SerializeField] private Button ReStartButton;
    [SerializeField] private Button ExitButton;

    private InputSystem_Actions m_ui;
    private InputAction openMemu;
    private int menuCount = 1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        m_ui = new InputSystem_Actions();
        openMemu = m_ui.UI.OpenMenu;
    }

    void Start()
    {
        background.SetActive(false);
        StartButton.onClick.AddListener(() => S_GameEvent.GameStart());
        ReStartButton.onClick.AddListener(() => S_GameEvent.GameReStart());
        ExitButton.onClick.AddListener(() => S_GameEvent.ExitGame());
    }

    void Update()
    {
        if (openMemu.WasPressedThisFrame() && menuCount % 2 == 0)
        {
            ShowUI();
            Time.timeScale = 0f;
            menuCount++;
        }
        else if (openMemu.WasPressedThisFrame() && menuCount % 2 != 0)
        {
            HideUI();
            Time.timeScale = 1f;
            menuCount++;
        }
    }

    void OnEnable()
    {
        S_GameEvent.OnGameStart += HideUI;
        S_GameEvent.OnGameRestart += HideUI;
        S_GameEvent.OnPlayerDied += ShowUI;
        m_ui.Enable();
    }

    void OnDisable()
    {
        S_GameEvent.OnGameStart -= HideUI;
        S_GameEvent.OnGameRestart -= HideUI;
        S_GameEvent.OnPlayerDied -= ShowUI;
        m_ui?.Disable();
    }

    void ShowUI() => background.SetActive(true);
    void HideUI() => background.SetActive(false);
}