using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class S_UIManager : MonoBehaviour
{
    private const string PauseMenuInputLockId = "UIManager.PauseMenu";
    private const string DeathUIInputLockId = "UIManager.DeathUI";

    public static S_UIManager Instance { get; private set; }

    [SerializeField] private GameObject background;
    [SerializeField] private Button StartButton;
    [SerializeField] private Button ReStartButton;
    [SerializeField] private Button ExitButton;
    [Header("Suspicion UI")]
    [SerializeField] private Slider suspicionSlider;
    [SerializeField] private GameObject suspicionUI;
    [Header("Key UI")]
    [SerializeField] private TMP_Text keyCountText;
    [Header("Energy UI")]
    [SerializeField] private Slider energySlider;
    [SerializeField] private GameObject energyUI;
    [Header("Death UI")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Button backToCheckpointButton;
    [SerializeField] private TMP_Text deathCountText;
    [SerializeField, Min(0)] private int deathCountStartValue = 1;

    private InputAction openMemu;
    private InputAction cancelAction;
    private bool menuOpen = false;
    private bool deathPanelOpen = false;
    private int deathCount;
    private Coroutine resumeGameplayInputCoroutine;
    private Coroutine energyUIBuildCoroutine;
    private bool isBuildingEnergyUI;
    private bool hasPendingEnergyValue;
    private float pendingEnergyCurrent = 1f;
    private float pendingEnergyMax = 1f;

    void Awake()
    {
        if (ShouldDiscardLooseSceneInstance())
        {
            Destroy(gameObject);
            return;
        }

        if (Instance != null && Instance != this)
        {
            S_ManagerRoot.DestroyDuplicate(this);
            return;
        }

        Instance = this;

        InputSystem_Actions actions = S_Input.Actions;
        openMemu = actions.UI.OpenMenu;
        cancelAction = actions.UI.Cancel;
    }

    private bool ShouldDiscardLooseSceneInstance()
    {
        S_ManagerRoot root = S_ManagerRoot.Instance != null
            ? S_ManagerRoot.Instance
            : FindAnyObjectByType<S_ManagerRoot>();

        if (root == null || transform.IsChildOf(root.transform))
            return false;

        S_UIManager rootUIManager = root.GetComponentInChildren<S_UIManager>(true);
        return rootUIManager != null && rootUIManager != this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (StartButton != null)
        {
            StartButton.onClick.AddListener(RequestRunStart);
        }

        if (ReStartButton != null)
            ReStartButton.onClick.AddListener(() => S_GameEvent.RespawnRequested());

        if (ExitButton != null)
            ExitButton.onClick.AddListener(() => S_GameEvent.ExitGame());

        ApplyAutomaticNavigation(StartButton);
        ApplyAutomaticNavigation(ReStartButton);
        ApplyAutomaticNavigation(ExitButton);

        EnsureDeathUIBuilt();
        HideDeathUI(false);
        RefreshEnergyBarFromPlayer();
        HideUI();
    }

    void Update()
    {
        if (deathPanelOpen)
        {
            SelectDeathDefaultIfNeeded();
            return;
        }

        bool toggledMenu = false;

        if (openMemu != null && openMemu.WasPressedThisFrame())
        {
            if (menuOpen)
                HideUI();
            else
            {
                ShowUI();
                Time.timeScale = 0f;
            }

            toggledMenu = true;
        }

        if (!toggledMenu && menuOpen && cancelAction != null && cancelAction.WasPressedThisFrame())
            HideUI();

        if (menuOpen)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
                SelectMainMenuDefault();
        }
    }

    void OnEnable()
    {
        S_GameEvent.OnRunStartRequested += HideAllGameplayBlockingUI;
        S_GameEvent.OnRespawnRequested += HideAllGameplayBlockingUI;
        S_GameEvent.OnPlayerDied += ShowDeathUI;
        S_GameEvent.OnSuspicionValueChanged += UpdateSuspicionBar;
        S_GameEvent.OnSuspicionChanged += UpdateSuspicionBar;
        S_GameEvent.OnKeyCountChanged += UpdateKeyCount;
        S_GameEvent.OnPlayerEnergyChanged += UpdateEnergyBar;
    }

    void OnDisable()
    {
        S_GameEvent.OnRunStartRequested -= HideAllGameplayBlockingUI;
        S_GameEvent.OnRespawnRequested -= HideAllGameplayBlockingUI;
        S_GameEvent.OnPlayerDied -= ShowDeathUI;
        S_GameEvent.OnSuspicionValueChanged -= UpdateSuspicionBar;
        S_GameEvent.OnSuspicionChanged -= UpdateSuspicionBar;
        S_GameEvent.OnKeyCountChanged -= UpdateKeyCount;
        S_GameEvent.OnPlayerEnergyChanged -= UpdateEnergyBar;
    }

    void ShowUI()
    {
        if (deathPanelOpen)
            return;

        if (background == null) return;

        PauseGameplayInput(PauseMenuInputLockId);
        background.SetActive(true);
        menuOpen = true;
        Canvas.ForceUpdateCanvases();
        SelectMainMenuDefault();
    }

    void HideUI()
    {
        if (background == null) return;

        background.SetActive(false);
        menuOpen = false;
        ClearSelection();
        Time.timeScale = 1f;

        if (!deathPanelOpen)
            ResumeGameplayInputNextFrame(PauseMenuInputLockId);
    }

    private void HideAllGameplayBlockingUI()
    {
        HideDeathUI(false);
        HideUI();
    }

    private void ShowDeathUI()
    {
        if (deathPanelOpen)
            return;

        EnsureDeathUIBuilt();
        PauseGameplayInput(DeathUIInputLockId);

        if (background != null)
            background.SetActive(false);

        menuOpen = false;

        deathPanelOpen = true;
        if (deathPanel != null)
            deathPanel.SetActive(true);

        int visibleDeathCount = deathCountStartValue + deathCount;
        deathCount++;

        if (deathCountText != null)
        {
            deathCountText.text = "DEATHS: " + visibleDeathCount;
            deathCountText.color = new Color(1f, 0.08f, 0.08f, 1f);
        }

        Time.timeScale = 0f;
        Canvas.ForceUpdateCanvases();
        SelectButton(backToCheckpointButton);
    }

    private void HideDeathUI(bool clearSelection)
    {
        deathPanelOpen = false;

        if (deathPanel != null)
            deathPanel.SetActive(false);

        S_GameEvent.PopGameplayInputLock(DeathUIInputLockId);

        if (clearSelection)
            ClearSelection();
    }

    private void BackToCheckpoint()
    {
        if (!deathPanelOpen)
            return;

        HideDeathUI(false);
        Time.timeScale = 1f;
        S_GameEvent.RespawnRequested();
    }

    private void SelectDeathDefaultIfNeeded()
    {
        if (EventSystem.current == null)
            return;

        if (EventSystem.current.currentSelectedGameObject == null)
            SelectButton(backToCheckpointButton);
    }

    private void EnsureEnergyUIBuilt()
    {
        if (energySlider != null)
            return;

        if (isBuildingEnergyUI)
            return;

        if (energyUI != null)
        {
            energySlider = energyUI.GetComponentInChildren<Slider>(true);
            if (energySlider != null)
            {
                ApplyPendingEnergyBarValue();
                return;
            }
        }

        isBuildingEnergyUI = true;
        try
        {
            Canvas canvas = ResolveCanvas();
            if (canvas == null)
                return;

            GameObject sliderObject = new GameObject("EnergyUI", typeof(RectTransform), typeof(Slider));
            Transform sliderTransform = sliderObject.transform;
            if (sliderTransform.parent != canvas.transform)
                sliderTransform.SetParent(canvas.transform, false);
            energyUI = sliderObject;

            RectTransform rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -24f);
            rect.sizeDelta = new Vector2(240f, 18f);

            RectTransform backgroundRect = CreateFullRectImage(sliderObject.transform, "Background", new Color(0.03f, 0.035f, 0.045f, 0.78f));
            RectTransform fillArea = CreateRect("Fill Area", sliderObject.transform);
            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(3f, 3f);
            fillArea.offsetMax = new Vector2(-3f, -3f);

            RectTransform fillRect = CreateFullRectImage(fillArea, "Fill", new Color(0.2f, 0.95f, 0.9f, 0.95f));

            energySlider = sliderObject.GetComponent<Slider>();
            energySlider.minValue = 0f;
            energySlider.maxValue = 1f;
            energySlider.value = 1f;
            energySlider.interactable = false;
            energySlider.transition = Selectable.Transition.None;
            energySlider.targetGraphic = fillRect.GetComponent<Image>();
            energySlider.fillRect = fillRect;

            Image backgroundImage = backgroundRect.GetComponent<Image>();
            if (backgroundImage != null)
                backgroundImage.raycastTarget = false;

            ApplyPendingEnergyBarValue();
        }
        finally
        {
            isBuildingEnergyUI = false;
        }
    }

    private void EnsureDeathUIBuilt()
    {
        if (deathPanel != null && backToCheckpointButton != null && deathCountText != null)
        {
            ConfigureDeathButton();
            return;
        }

        Canvas canvas = ResolveCanvas();
        if (canvas == null)
            return;

        if (deathPanel == null)
        {
            deathPanel = new GameObject("DeathPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            deathPanel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = deathPanel.GetComponent<RectTransform>();
            Stretch(panelRect);

            Image panelImage = deathPanel.GetComponent<Image>();
            panelImage.color = new Color(0.045f, 0.005f, 0.008f, 0.92f);
        }

        if (deathCountText == null)
        {
            deathCountText = CreateText("DeathCount", deathPanel.transform, string.Empty, 34f, TextAlignmentOptions.Center);
            RectTransform textRect = deathCountText.GetComponent<RectTransform>();
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0f, 58f);
            textRect.sizeDelta = new Vector2(520f, 54f);
            deathCountText.fontStyle = FontStyles.Bold;
            deathCountText.color = new Color(1f, 0.08f, 0.08f, 1f);
        }

        if (backToCheckpointButton == null)
        {
            backToCheckpointButton = CreateButton("BackToCheckpointButton", deathPanel.transform, "restart current level");
            RectTransform buttonRect = backToCheckpointButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0f, -22f);
            buttonRect.sizeDelta = new Vector2(260f, 42f);
        }

        ConfigureDeathButton();
    }

    private void ConfigureDeathButton()
    {
        if (backToCheckpointButton == null)
            return;

        TMP_Text buttonText = backToCheckpointButton.GetComponentInChildren<TMP_Text>(true);
        if (buttonText != null)
            buttonText.text = "restart current level";

        backToCheckpointButton.onClick.RemoveListener(BackToCheckpoint);
        backToCheckpointButton.onClick.AddListener(BackToCheckpoint);
        ApplyAutomaticNavigation(backToCheckpointButton);
    }

    private Canvas ResolveCanvas()
    {
        if (background != null)
        {
            Canvas parentCanvas = background.GetComponentInParent<Canvas>(true);
            if (parentCanvas != null)
                return parentCanvas;
        }

        Canvas existingCanvas = GetComponentInChildren<Canvas>(true);
        if (existingCanvas != null)
            return existingCanvas;

        GameObject canvasObject = new GameObject("RuntimeUICanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366f, 768f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private void UpdateEnergyBar(float current, float max)
    {
        pendingEnergyCurrent = current;
        pendingEnergyMax = max;
        hasPendingEnergyValue = true;

        if (energySlider == null)
        {
            QueueEnergyUIBuild();
            return;
        }

        ApplyPendingEnergyBarValue();
    }

    private void RefreshEnergyBarFromPlayer()
    {
        if (S_Player.Instance != null && S_Player.Instance.Energy != null)
            UpdateEnergyBar(S_Player.Instance.Energy.CurrentEnergy, S_Player.Instance.Energy.MaxEnergy);
        else
            UpdateEnergyBar(1f, 1f);
    }

    private void QueueEnergyUIBuild()
    {
        if (energySlider != null)
        {
            ApplyPendingEnergyBarValue();
            return;
        }

        if (!isActiveAndEnabled || energyUIBuildCoroutine != null)
            return;

        energyUIBuildCoroutine = StartCoroutine(BuildEnergyUIAfterFrame());
    }

    private IEnumerator BuildEnergyUIAfterFrame()
    {
        yield return null;
        energyUIBuildCoroutine = null;
        EnsureEnergyUIBuilt();
    }

    private void ApplyPendingEnergyBarValue()
    {
        if (!hasPendingEnergyValue)
        {
            pendingEnergyCurrent = 1f;
            pendingEnergyMax = 1f;
            hasPendingEnergyValue = true;
        }

        if (energyUI != null)
            energyUI.SetActive(pendingEnergyMax > 0f);

        if (energySlider != null)
            energySlider.value = pendingEnergyMax > 0f ? Mathf.Clamp01(pendingEnergyCurrent / pendingEnergyMax) : 0f;
    }

    private void UpdateKeyCount(int collected, int total)
    {
        if (keyCountText != null)
            keyCountText.text = collected + " / " + total;
    }

    private void RequestRunStart()
    {
        S_GameEvent.RunStartRequested();
    }

    private void UpdateSuspicionBar(float value)
    {
        UpdateSuspicionBar(value, 100f);
    }

    private void UpdateSuspicionBar(float value, float max)
    {
        if (suspicionUI != null)
            suspicionUI.SetActive(value > 0f);

        if (suspicionSlider != null)
        {
            suspicionSlider.value = max > 0f ? value / max : 0f;
        }
    }

    private TMP_Text CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontSizeMax = fontSize;
        tmp.fontSizeMin = 10f;
        tmp.enableAutoSizing = true;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return tmp;
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private RectTransform CreateFullRectImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        Stretch(rect);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private void Stretch(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Button CreateButton(string name, Transform parent, string text)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.86f, 0.92f, 0.94f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        ApplyAutomaticNavigation(button);

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.86f, 0.92f, 0.94f, 1f);
        colors.highlightedColor = new Color(0.62f, 1f, 0.96f, 1f);
        colors.pressedColor = new Color(0.35f, 0.58f, 0.62f, 1f);
        colors.selectedColor = new Color(0.62f, 1f, 0.96f, 1f);
        colors.disabledColor = new Color(0.32f, 0.36f, 0.38f, 0.65f);
        button.colors = colors;
        button.targetGraphic = image;

        TMP_Text buttonText = CreateText("Text (TMP)", buttonObject.transform, text, 15f, TextAlignmentOptions.Center);
        RectTransform textRect = buttonText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 2f);
        textRect.offsetMax = new Vector2(-8f, -2f);

        return button;
    }

    private void PauseGameplayInput(string lockId)
    {
        if (resumeGameplayInputCoroutine != null)
        {
            StopCoroutine(resumeGameplayInputCoroutine);
            resumeGameplayInputCoroutine = null;
        }

        if (S_PlayerLookup.TryGetActive(out IPlayerActor player))
            player.CancelActiveSkills();

        S_GameEvent.PushGameplayInputLock(lockId);
    }

    private void ResumeGameplayInputNextFrame(string lockId)
    {
        if (resumeGameplayInputCoroutine != null)
            StopCoroutine(resumeGameplayInputCoroutine);

        resumeGameplayInputCoroutine = StartCoroutine(ResumeGameplayInputAfterFrame(lockId));
    }

    private IEnumerator ResumeGameplayInputAfterFrame(string lockId)
    {
        yield return null;
        resumeGameplayInputCoroutine = null;

        if (menuOpen)
            yield break;

        S_GameEvent.PopGameplayInputLock(lockId);
    }

    private void SelectMainMenuDefault()
    {
        Button buttonToSelect = FirstInteractableButton(StartButton, ReStartButton, ExitButton);
        SelectButton(buttonToSelect);
    }

    private void SelectButton(Button button)
    {
        if (!IsSelectable(button) || EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void ClearSelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private Button FirstInteractableButton(params Button[] buttons)
    {
        foreach (Button button in buttons)
        {
            if (IsSelectable(button))
                return button;
        }

        return null;
    }

    private bool IsSelectable(Button button)
    {
        return button != null && button.interactable && button.gameObject.activeInHierarchy;
    }

    private void ApplyAutomaticNavigation(Selectable selectable)
    {
        if (selectable == null) return;

        Navigation navigation = selectable.navigation;
        navigation.mode = Navigation.Mode.Automatic;
        selectable.navigation = navigation;
    }
}
