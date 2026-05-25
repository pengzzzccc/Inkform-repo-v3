using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class S_UIManager : MonoBehaviour
{
    private const string PauseMenuInputLockId = "UIManager.PauseMenu";
    private const string DeathUIInputLockId = "UIManager.DeathUI";

    public static S_UIManager Instance { get; private set; }
    private const string DefaultFirstLevelSceneName = "Playtest1";

    [SerializeField] private GameObject background;
    [SerializeField] private Button StartButton;
    [SerializeField] private Button ReStartButton;
    [SerializeField] private Button ExitButton;
    [SerializeField] private Button ControlsButton;
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
    private GameObject controlsPanel;
    private TMP_Text rebindStatusText;
    private Button cancelRebindButton;
    private Coroutine rebindStartCoroutine;
    private BindingButtonView activeRebind;
    private Coroutine resumeGameplayInputCoroutine;
    private Coroutine energyUIBuildCoroutine;
    private bool isBuildingEnergyUI;
    private bool hasPendingEnergyValue;
    private float pendingEnergyCurrent = 1f;
    private float pendingEnergyMax = 1f;

    private readonly List<GameObject> mainMenuObjects = new List<GameObject>();
    private readonly List<Button> controlsPanelButtons = new List<Button>();
    private readonly List<BindingButtonView> bindingButtons = new List<BindingButtonView>();
    private RectTransform bindingRowsScrollRoot;

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

        S_InputBindingManager inputBindingManager = S_InputBindingManager.Instance;
        if (inputBindingManager == null)
        {
            Debug.LogError("[UIManager] Missing S_InputBindingManager. Place UIManager under ManagerRoot.prefab with InputBindingManager.");
            enabled = false;
            return;
        }

        InputSystem_Actions actions = inputBindingManager.Actions;
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
            StartButton.onClick.AddListener(RequestFreshGameStart);
        }

        if (ReStartButton != null)
            ReStartButton.onClick.AddListener(() => S_GameEvent.GameReStart());

        if (ExitButton != null)
            ExitButton.onClick.AddListener(() => S_GameEvent.ExitGame());

        ApplyAutomaticNavigation(StartButton);
        ApplyAutomaticNavigation(ReStartButton);
        ApplyAutomaticNavigation(ExitButton);

        EnsureControlsUIBuilt();
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

        if (openMemu != null && openMemu.WasPressedThisFrame() && !S_InputBindingManager.Instance.IsRebinding)
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

        if (!toggledMenu && menuOpen && cancelAction != null && cancelAction.WasPressedThisFrame() && !S_InputBindingManager.Instance.IsRebinding)
        {
            if (IsControlsPanelVisible())
                CloseControlsPanel();
            else
                HideUI();
        }

        if (menuOpen && !S_InputBindingManager.Instance.IsRebinding)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                if (IsControlsPanelVisible())
                    SelectControlsDefault();
                else
                    SelectMainMenuDefault();
            }

            // Auto-scroll when selected button is outside viewport
            if (IsControlsPanelVisible())
                ScrollToSelected();
        }
    }

    void OnEnable()
    {
        S_GameEvent.OnGameStart += HideAllGameplayBlockingUI;
        S_GameEvent.OnGameRestart += HideAllGameplayBlockingUI;
        S_GameEvent.OnPlayerDied += ShowDeathUI;
        S_GameEvent.OnSuspicionValueChanged += UpdateSuspicionBar;
        S_GameEvent.OnSuspicionChanged += UpdateSuspicionBar;
        S_GameEvent.OnKeyCountChanged += UpdateKeyCount;
        S_GameEvent.OnPlayerEnergyChanged += UpdateEnergyBar;

        if (S_InputBindingManager.HasInstance)
            S_InputBindingManager.Instance.BindingsChanged += RefreshBindingLabels;
    }

    void OnDisable()
    {
        S_GameEvent.OnGameStart -= HideAllGameplayBlockingUI;
        S_GameEvent.OnGameRestart -= HideAllGameplayBlockingUI;
        S_GameEvent.OnPlayerDied -= ShowDeathUI;
        S_GameEvent.OnSuspicionValueChanged -= UpdateSuspicionBar;
        S_GameEvent.OnSuspicionChanged -= UpdateSuspicionBar;
        S_GameEvent.OnKeyCountChanged -= UpdateKeyCount;
        S_GameEvent.OnPlayerEnergyChanged -= UpdateEnergyBar;

        if (S_InputBindingManager.HasInstance)
            S_InputBindingManager.Instance.BindingsChanged -= RefreshBindingLabels;
    }

    void ShowUI()
    {
        if (deathPanelOpen)
            return;

        if (background == null) return;

        EnsureControlsUIBuilt();
        PauseGameplayInput(PauseMenuInputLockId);
        background.SetActive(true);
        menuOpen = true;
        SetControlsPanelVisible(false);
        Canvas.ForceUpdateCanvases();
        SelectMainMenuDefault();
    }

    void HideUI()
    {
        if (background == null) return;

        if (S_InputBindingManager.HasInstance)
            S_InputBindingManager.Instance.CancelRebind();

        background.SetActive(false);
        menuOpen = false;
        SetControlsPanelVisible(false);
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
        SetControlsPanelVisible(false);

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
        S_GameEvent.RestartCurrentLevelRequested();
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

    private void RequestFreshGameStart()
    {
        S_GameEvent.StartFreshGameRequested();

        if (S_GameManager.Instance == null)
            SceneManager.LoadScene(DefaultFirstLevelSceneName);
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

    private void BuildControlsUI()
    {
        if (background == null || controlsPanel != null) return;

        mainMenuObjects.Clear();
        for (int i = 0; i < background.transform.childCount; i++)
            mainMenuObjects.Add(background.transform.GetChild(i).gameObject);

        CreateControlsButton();
        controlsPanel = CreateControlsPanel(background.transform);
        controlsPanel.SetActive(false);

        CreateTitle("controls", controlsPanel.transform);
        bindingRowsScrollRoot = CreateControlsScrollRect(controlsPanel.transform);
        CreateHeaderRow(bindingRowsScrollRoot.transform);

        AddBindingRow(
            "Move Up",
            BindingTarget.Keyboard("Move", "up", "Button"),
            null);
        AddBindingRow(
            "Move Down",
            BindingTarget.Keyboard("Move", "down", "Button"),
            null);
        AddBindingRow(
            "Move Left",
            BindingTarget.Keyboard("Move", "left", "Button"),
            null);
        AddBindingRow(
            "Move Right",
            BindingTarget.Keyboard("Move", "right", "Button"),
            null);
        AddBindingRow(
            "Move Stick",
            null,
            BindingTarget.Gamepad("Move", null, "Vector2"));
        AddBindingRow(
            "Jump",
            BindingTarget.Keyboard("Jump", null, "Button"),
            BindingTarget.Gamepad("Jump", null, "Button"));
        AddBindingRow(
            "Sprint",
            BindingTarget.Keyboard("Sprint", null, "Button"),
            BindingTarget.Gamepad("Sprint", null, "Button"));
        AddBindingRow(
            "Grip",
            BindingTarget.Keyboard("grep", null, "Button"),
            BindingTarget.Gamepad("grep", null, "Button"));
        AddBindingRow(
            "Hide",
            BindingTarget.Keyboard("Hide", null, "Button"),
            BindingTarget.Gamepad("Hide", null, "Button"));
        AddBindingRow(
            "Camera Control",
            BindingTarget.Keyboard("CameraControl", null, "Button"),
            BindingTarget.Gamepad("CameraControl", null, "Button"));
        AddBindingRow(
            "Open Menu",
            BindingTarget.Keyboard("OpenMenu", null, "Button", "<Keyboard>"),
            BindingTarget.Gamepad("OpenMenu", null, "Button"));

        CreateFooterRow(controlsPanel.transform);
        RefreshBindingLabels();
    }

    private void EnsureControlsUIBuilt()
    {
        if (controlsPanel == null)
            BuildControlsUI();
    }

    private void CreateControlsButton()
    {
        if (ControlsButton == null && ExitButton != null)
        {
            ControlsButton = Instantiate(ExitButton, ExitButton.transform.parent);
            ControlsButton.name = "Button_controls";
            SetButtonText(ControlsButton, "controls");

            RectTransform controlsRect = ControlsButton.GetComponent<RectTransform>();
            RectTransform exitRect = ExitButton.GetComponent<RectTransform>();
            if (controlsRect != null && exitRect != null)
                controlsRect.anchoredPosition = exitRect.anchoredPosition + new Vector2(0f, -60f);
        }

        if (ControlsButton == null) return;

        ControlsButton.onClick.RemoveAllListeners();
        ControlsButton.onClick.AddListener(OpenControlsPanel);
        ApplyAutomaticNavigation(ControlsButton);

        if (!mainMenuObjects.Contains(ControlsButton.gameObject))
            mainMenuObjects.Add(ControlsButton.gameObject);
    }

    private GameObject CreateControlsPanel(Transform parent)
    {
        GameObject panel = new GameObject("ControlsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.12f, 0.08f);
        rect.anchorMax = new Vector2(0.88f, 0.92f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.04f, 0.06f, 0.08f, 0.92f);
        image.raycastTarget = false;

        return panel;
    }

    private void CreateTitle(string text, Transform parent)
    {
        TMP_Text title = CreateText("Title", parent, text, 26f, TextAlignmentOptions.Center);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -10f);
        titleRect.sizeDelta = new Vector2(-40f, 36f);
    }

    private void CreateHeaderRow(Transform parent)
    {
        GameObject row = CreateRow("HeaderRow", parent, 24f);
        TMP_Text action = CreateText("Action", row.transform, "Action", 16f, TextAlignmentOptions.Left);
        TMP_Text keyboard = CreateText("KeyboardMouse", row.transform, "Keyboard / Mouse", 16f, TextAlignmentOptions.Center);
        TMP_Text gamepad = CreateText("Gamepad", row.transform, "Gamepad", 16f, TextAlignmentOptions.Center);

        AddLayoutElement(action.gameObject, 180f, -1f);
        AddLayoutElement(keyboard.gameObject, 220f, -1f);
        AddLayoutElement(gamepad.gameObject, 220f, -1f);
    }

    private void AddBindingRow(string label, BindingTarget keyboardTarget, BindingTarget gamepadTarget)
    {
        GameObject row = CreateRow(label.Replace(" ", string.Empty) + "Row", bindingRowsScrollRoot.transform, 32f);

        TMP_Text labelText = CreateText("Label", row.transform, label, 16f, TextAlignmentOptions.Left);
        AddLayoutElement(labelText.gameObject, 180f, -1f);

        CreateBindingButton(row.transform, keyboardTarget);
        CreateBindingButton(row.transform, gamepadTarget);
    }

    private void CreateBindingButton(Transform parent, BindingTarget target)
    {
        if (target == null)
        {
            TMP_Text placeholder = CreateText("Empty", parent, "-", 16f, TextAlignmentOptions.Center);
            AddLayoutElement(placeholder.gameObject, 220f, 28f);
            return;
        }

        Button button = CreateButton(target.ActionName + "Button", parent, "-");
        AddLayoutElement(button.gameObject, 220f, 28f);

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        BindingButtonView view = new BindingButtonView(button, text, target);
        bindingButtons.Add(view);
        controlsPanelButtons.Add(button);

        button.onClick.AddListener(() => BeginRebind(view));
    }

    private void CreateFooterRow(Transform parent)
    {
        rebindStatusText = CreateText("RebindStatus", parent, string.Empty, 15f, TextAlignmentOptions.Center);
        RectTransform statusRect = rebindStatusText.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.anchoredPosition = new Vector2(0f, 32f);
        statusRect.sizeDelta = new Vector2(-40f, 22f);

        GameObject footerContainer = new GameObject("FooterContainer", typeof(RectTransform));
        footerContainer.transform.SetParent(parent, false);
        RectTransform footerRect = footerContainer.GetComponent<RectTransform>();
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.anchoredPosition = new Vector2(0f, 6f);
        footerRect.sizeDelta = new Vector2(-40f, 30f);

        HorizontalLayoutGroup hlg = footerContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 14f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;

        Button resetButton = CreateButton("ResetAllButton", footerContainer.transform, "reset all");
        resetButton.onClick.AddListener(() =>
        {
            S_InputBindingManager.Instance.CancelRebind();
            S_InputBindingManager.Instance.ResetAllBindings();
            RefreshBindingLabels();
        });
        AddLayoutElement(resetButton.gameObject, 150f, 28f);
        controlsPanelButtons.Add(resetButton);

        cancelRebindButton = CreateButton("CancelRebindButton", footerContainer.transform, "cancel");
        cancelRebindButton.onClick.AddListener(() => S_InputBindingManager.Instance.CancelRebind());
        AddLayoutElement(cancelRebindButton.gameObject, 170f, 28f);
        cancelRebindButton.interactable = false;

        Button backButton = CreateButton("BackButton", footerContainer.transform, "back");
        backButton.onClick.AddListener(CloseControlsPanel);
        AddLayoutElement(backButton.gameObject, 150f, 28f);
        controlsPanelButtons.Add(backButton);
    }

    private GameObject CreateRow(string name, Transform parent, float height)
    {
        GameObject row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);

        RectTransform rect = row.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, height);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        LayoutElement layoutElement = row.GetComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;
        layoutElement.flexibleHeight = 0f;

        return row;
    }

    private RectTransform CreateControlsScrollRect(Transform parent)
    {
        GameObject scrollObject = new GameObject("BindingsScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(parent, false);

        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(18f, 58f);
        scrollRect.offsetMax = new Vector2(-18f, -46f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(0.02f, 0.025f, 0.06f, 0.08f);
        viewportImage.raycastTarget = false;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 3f;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect sr = scrollObject.GetComponent<ScrollRect>();
        sr.viewport = viewportRect;
        sr.content = contentRect;
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 28f;
        sr.movementType = ScrollRect.MovementType.Elastic;

        return contentRect;
    }

    private void ScrollToSelected()
    {
        if (bindingRowsScrollRoot == null) return;

        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected == null) return;

        ScrollRect scroll = bindingRowsScrollRoot.GetComponentInParent<ScrollRect>();
        if (scroll == null || scroll.viewport == null) return;

        RectTransform selectedRect = selected.GetComponent<RectTransform>();
        if (selectedRect == null) return;

        RectTransform content = scroll.content;
        RectTransform viewport = scroll.viewport;

        // selected position relative to content
        Vector3 selectedWorldPos = selectedRect.position;
        Vector3 contentWorldPos = content.position;

        float viewportHeight = viewport.rect.height;
        float contentHeight = content.rect.height;

        if (contentHeight <= viewportHeight) return;

        // convert to content-local Y
        float selectedLocalY = content.InverseTransformPoint(selectedWorldPos).y;
        // selectedLocalY is negative (content pivot top), so selected is at -selectedLocalY from top

        float selectedFromTop = -selectedLocalY;
        float selectedFromBottom = contentHeight - selectedFromTop;

        float currentScrollPos = content.anchoredPosition.y;
        float visibleTop = currentScrollPos;
        float visibleBottom = currentScrollPos + viewportHeight;
        float padding = 40f;

        // check if selected is outside visible area
        if (selectedFromTop < visibleTop + padding)
        {
            float newY = Mathf.Max(0f, selectedFromTop - padding);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, newY);
        }
        else if (selectedFromBottom < (contentHeight - visibleBottom) + padding)
        {
            float newY = Mathf.Min(contentHeight - viewportHeight, selectedFromTop - viewportHeight + padding);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, newY);
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

    private void AddFlexibleSpace(Transform parent)
    {
        GameObject space = new GameObject("Space", typeof(RectTransform), typeof(LayoutElement));
        space.transform.SetParent(parent, false);
        space.GetComponent<LayoutElement>().flexibleWidth = 1f;
    }

    private void AddLayoutElement(GameObject target, float preferredWidth, float preferredHeight)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = target.AddComponent<LayoutElement>();

        if (preferredWidth >= 0f)
            layoutElement.preferredWidth = preferredWidth;

        if (preferredHeight >= 0f)
            layoutElement.preferredHeight = preferredHeight;
    }

    private void OpenControlsPanel()
    {
        EnsureControlsUIBuilt();
        SetControlsPanelVisible(true);
        if (controlsPanel != null)
            controlsPanel.transform.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();
        RefreshControlsPanelLayout();
        ResetControlsScrollPosition();
        SelectControlsDefault();
    }

    private void CloseControlsPanel()
    {
        if (S_InputBindingManager.HasInstance)
            S_InputBindingManager.Instance.CancelRebind();

        SetControlsPanelVisible(false);
        Canvas.ForceUpdateCanvases();
        SelectButton(ControlsButton != null ? ControlsButton : StartButton);
    }

    private void SetControlsPanelVisible(bool visible)
    {
        if (controlsPanel != null)
            controlsPanel.SetActive(visible);

        foreach (GameObject menuObject in mainMenuObjects)
        {
            if (menuObject == controlsPanel)
                continue;

            if (menuObject != null)
                menuObject.SetActive(!visible);
        }

        if (!visible)
        {
            activeRebind = null;
            SetControlsInteractable(true);
            if (rebindStatusText != null)
                rebindStatusText.text = string.Empty;
        }
        else
        {
            RefreshBindingLabels();
        }
    }

    private void RefreshControlsPanelLayout()
    {
        if (bindingRowsScrollRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(bindingRowsScrollRoot);

        RectTransform controlsPanelRect = controlsPanel != null ? controlsPanel.GetComponent<RectTransform>() : null;
        if (controlsPanelRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(controlsPanelRect);

        Canvas.ForceUpdateCanvases();
    }

    private void ResetControlsScrollPosition()
    {
        if (bindingRowsScrollRoot == null) return;

        ScrollRect scroll = bindingRowsScrollRoot.GetComponentInParent<ScrollRect>();
        if (scroll == null) return;

        scroll.StopMovement();
        scroll.verticalNormalizedPosition = 1f;

        RectTransform content = scroll.content;
        if (content != null)
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
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

    private void BeginRebind(BindingButtonView view)
    {
        if (rebindStartCoroutine != null)
            StopCoroutine(rebindStartCoroutine);

        rebindStartCoroutine = StartCoroutine(StartRebindAfterFrame(view));
    }

    private IEnumerator StartRebindAfterFrame(BindingButtonView view)
    {
        yield return null;
        rebindStartCoroutine = null;
        StartRebind(view);
    }

    private void StartRebind(BindingButtonView view)
    {
        activeRebind = view;
        SetControlsInteractable(false);
        view.Label.text = "press input...";
        SelectButton(cancelRebindButton);

        if (rebindStatusText != null)
            rebindStatusText.text = "waiting for input";

        bool started = S_InputBindingManager.Instance.StartInteractiveRebind(
            view.Target.ActionName,
            view.Target.BindingGroup,
            view.Target.PartName,
            view.Target.DevicePath,
            view.Target.ExpectedControlType,
            CompleteRebind,
            CancelRebind);

        if (started) return;

        activeRebind = null;
        SetControlsInteractable(true);
        RefreshBindingLabels();

        if (rebindStatusText != null)
            rebindStatusText.text = "binding unavailable";
    }

    private void CompleteRebind()
    {
        BindingButtonView completedRebind = activeRebind;
        activeRebind = null;
        SetControlsInteractable(true);
        RefreshBindingLabels();
        SelectButton(completedRebind != null ? completedRebind.Button : null);

        if (rebindStatusText != null)
            rebindStatusText.text = "saved";
    }

    private void CancelRebind()
    {
        BindingButtonView cancelledRebind = activeRebind;
        activeRebind = null;
        SetControlsInteractable(true);
        RefreshBindingLabels();
        SelectButton(cancelledRebind != null ? cancelledRebind.Button : null);

        if (rebindStatusText != null)
            rebindStatusText.text = "cancelled";
    }

    private void SetControlsInteractable(bool interactable)
    {
        foreach (Button button in controlsPanelButtons)
        {
            if (button != null)
                button.interactable = interactable;
        }

        if (cancelRebindButton != null)
            cancelRebindButton.interactable = !interactable;
    }

    private void RefreshBindingLabels()
    {
        foreach (BindingButtonView bindingButton in bindingButtons)
        {
            if (bindingButton == null || bindingButton.Label == null) continue;
            if (activeRebind == bindingButton) continue;

            BindingTarget target = bindingButton.Target;
            bindingButton.Label.text = S_InputBindingManager.Instance.GetBindingDisplayString(
                target.ActionName,
                target.BindingGroup,
                target.PartName,
                target.DevicePath);
        }
    }

    private void SetButtonText(Button button, string text)
    {
        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = text;
            return;
        }

        Text uiText = button.GetComponentInChildren<Text>(true);
        if (uiText != null)
            uiText.text = text;
    }

    private bool IsControlsPanelVisible()
    {
        return controlsPanel != null && controlsPanel.activeSelf;
    }

    private void SelectMainMenuDefault()
    {
        Button buttonToSelect = FirstInteractableButton(StartButton, ReStartButton, ControlsButton, ExitButton);
        SelectButton(buttonToSelect);
    }

    private void SelectControlsDefault()
    {
        foreach (BindingButtonView bindingButton in bindingButtons)
        {
            if (IsSelectable(bindingButton.Button))
            {
                SelectButton(bindingButton.Button);
                return;
            }
        }

        Button buttonToSelect = FirstInteractableButton(cancelRebindButton);
        if (buttonToSelect == null)
            buttonToSelect = FirstInteractableButton(controlsPanelButtons.ToArray());

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

    private class BindingTarget
    {
        public string ActionName { get; private set; }
        public string BindingGroup { get; private set; }
        public string PartName { get; private set; }
        public string DevicePath { get; private set; }
        public string ExpectedControlType { get; private set; }

        public static BindingTarget Keyboard(string actionName, string partName, string expectedControlType, string devicePath = null)
        {
            return new BindingTarget
            {
                ActionName = actionName,
                BindingGroup = "Keyboard&Mouse",
                PartName = partName,
                DevicePath = devicePath,
                ExpectedControlType = expectedControlType
            };
        }

        public static BindingTarget Gamepad(string actionName, string partName, string expectedControlType)
        {
            return new BindingTarget
            {
                ActionName = actionName,
                BindingGroup = "Gamepad",
                PartName = partName,
                DevicePath = "<Gamepad>",
                ExpectedControlType = expectedControlType
            };
        }
    }

    private class BindingButtonView
    {
        public Button Button { get; private set; }
        public TMP_Text Label { get; private set; }
        public BindingTarget Target { get; private set; }

        public BindingButtonView(Button button, TMP_Text label, BindingTarget target)
        {
            Button = button;
            Label = label;
            Target = target;
        }
    }
}
