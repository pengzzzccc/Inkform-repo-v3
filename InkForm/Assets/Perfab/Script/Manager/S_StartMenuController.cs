using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class S_StartMenuController : MonoBehaviour
{
    private const string StartSceneName = "Start";
    private const string StartScenePathSuffix = "Assets/Scenes/For_game/Start.unity";
    private const string DefaultFirstLevelSceneName = "Playtest1";
    private static bool sceneHookRegistered;

    private Canvas canvas;
    private RectTransform mainMenuRoot;
    private RectTransform settingsPanel;
    private TMP_Text rebindStatusText;
    private Button cancelRebindButton;
    private Coroutine rebindStartCoroutine;
    private BindingButtonView activeRebind;
    private InputAction cancelAction;
    private Button startButton;

    private readonly List<BindingButtonView> bindingButtons = new List<BindingButtonView>();
    private readonly List<Button> settingsButtons = new List<Button>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        if (!sceneHookRegistered)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneHookRegistered = true;
        }

        TryCreateForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreateForScene(scene);
    }

    private static void TryCreateForScene(Scene scene)
    {
        if (!IsStartScene(scene))
            return;

        if (FindAnyObjectByType<S_StartMenuController>() != null)
            return;

        GameObject controller = new GameObject("StartMenuController");
        controller.AddComponent<S_StartMenuController>();
    }

    private static bool IsStartScene(Scene scene)
    {
        return string.Equals(scene.name, StartSceneName, StringComparison.OrdinalIgnoreCase)
            || scene.path.EndsWith(StartScenePathSuffix, StringComparison.OrdinalIgnoreCase);
    }

    private void Awake()
    {
        EnsurePersistentManagers();
        EnsureEventSystem();
        cancelAction = S_InputBindingManager.Instance.Actions.UI.Cancel;
        BuildStartMenu();
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        if (S_InputBindingManager.HasInstance)
            S_InputBindingManager.Instance.BindingsChanged += RefreshBindingLabels;
    }

    private void OnDisable()
    {
        if (S_InputBindingManager.HasInstance)
        {
            S_InputBindingManager.Instance.CancelRebind();
            S_InputBindingManager.Instance.BindingsChanged -= RefreshBindingLabels;
        }
    }

    private void Update()
    {
        if (settingsPanel != null
            && settingsPanel.gameObject.activeSelf
            && !S_InputBindingManager.Instance.IsRebinding
            && cancelAction != null
            && cancelAction.WasPressedThisFrame())
        {
            ShowMainMenu();
        }
    }

    private void EnsurePersistentManagers()
    {
        GameObject host = S_GameManager.Instance != null ? S_GameManager.Instance.gameObject : null;
        if (host == null)
        {
            S_GameManager existingGameManager = FindAnyObjectByType<S_GameManager>();
            host = existingGameManager != null ? existingGameManager.gameObject : new GameObject("PersistentManagers");

            if (existingGameManager == null)
                host.AddComponent<S_GameManager>();
        }

        if (!S_InputBindingManager.HasInstance && host.GetComponent<S_InputBindingManager>() == null)
            host.AddComponent<S_InputBindingManager>();

        if (S_AudioManager.Instance == null && FindAnyObjectByType<S_AudioManager>() == null)
            host.AddComponent<S_AudioManager>();
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }

        StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyModule != null)
            DestroyImmediate(legacyModule);

        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

        InputSystem_Actions actions = S_InputBindingManager.Instance.Actions;
        inputModule.actionsAsset = actions.asset;
        inputModule.point = InputActionReference.Create(actions.UI.Point);
        inputModule.leftClick = InputActionReference.Create(actions.UI.Click);
        inputModule.rightClick = InputActionReference.Create(actions.UI.RightClick);
        inputModule.middleClick = InputActionReference.Create(actions.UI.MiddleClick);
        inputModule.scrollWheel = InputActionReference.Create(actions.UI.ScrollWheel);
        inputModule.move = InputActionReference.Create(actions.UI.Navigate);
        inputModule.submit = InputActionReference.Create(actions.UI.Submit);
        inputModule.cancel = InputActionReference.Create(actions.UI.Cancel);
        inputModule.trackedDevicePosition = InputActionReference.Create(actions.UI.TrackedDevicePosition);
        inputModule.trackedDeviceOrientation = InputActionReference.Create(actions.UI.TrackedDeviceOrientation);
    }

    private void BuildStartMenu()
    {
        canvas = CreateCanvas();

        RectTransform background = CreateFullScreenImage("DeepSpaceBackground", canvas.transform, CreateDeepSpaceSprite(), Color.white);
        background.SetAsFirstSibling();

        RectTransform starRoot = CreateRect("StarField", canvas.transform);
        Stretch(starRoot);
        CreateStarField(starRoot);

        mainMenuRoot = CreateRect("MainMenu", canvas.transform);
        Stretch(mainMenuRoot);

        CreateInkformMascot(mainMenuRoot);

        startButton = CreateMenuButton(mainMenuRoot, "start", new Vector2(350f, 145f), new Vector2(300f, 58f));
        Button settingsButton = CreateMenuButton(mainMenuRoot, "Setting", new Vector2(455f, 0f), new Vector2(330f, 64f));
        Button exitButton = CreateMenuButton(mainMenuRoot, "Exit", new Vector2(305f, -155f), new Vector2(310f, 58f));

        startButton.onClick.AddListener(() =>
        {
            if (S_GameManager.Instance != null)
                S_GameManager.Instance.StartFreshGameFromMenu();
            else
                SceneManager.LoadScene(DefaultFirstLevelSceneName);
        });
        settingsButton.onClick.AddListener(ShowSettings);
        exitButton.onClick.AddListener(() => S_GameEvent.ExitGame());

        settingsPanel = CreateSettingsPanel(canvas.transform);
        settingsPanel.gameObject.SetActive(false);

        SelectButton(startButton);
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("StartMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas newCanvas = canvasObject.GetComponent<Canvas>();
        newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        newCanvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366f, 768f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return newCanvas;
    }

    private void CreateStarField(RectTransform parent)
    {
        Sprite dot = CreateCircleSprite(48, Color.white, Color.clear);
        Sprite gold = CreateStarSprite(64, new Color(1f, 0.9f, 0.05f, 1f));
        System.Random random = new System.Random(1487);

        for (int i = 0; i < 90; i++)
        {
            bool special = i % 9 == 0;
            GameObject star = new GameObject(special ? "GoldStar" : "Star", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(StartMenuStar));
            star.transform.SetParent(parent, false);

            RectTransform rect = star.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2((float)random.NextDouble(), (float)random.NextDouble());
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            float size = special ? random.Next(13, 28) : random.Next(2, 7);
            rect.sizeDelta = new Vector2(size, size);

            Image image = star.GetComponent<Image>();
            image.sprite = special ? gold : dot;
            image.color = special
                ? new Color(1f, 0.92f, 0.05f, 0.95f)
                : new Color(0.75f, 0.88f, 1f, UnityEngine.Random.Range(0.3f, 0.85f));
            image.raycastTarget = false;

            star.GetComponent<StartMenuStar>().Initialize(
                UnityEngine.Random.Range(0.18f, 0.65f),
                UnityEngine.Random.Range(0.5f, 3.5f),
                UnityEngine.Random.Range(0.15f, 0.9f),
                special);
        }
    }

    private void CreateInkformMascot(RectTransform parent)
    {
        GameObject mascot = new GameObject("InkformMascot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(InkformMascotView));
        mascot.transform.SetParent(parent, false);

        RectTransform rect = mascot.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.31f, 0.47f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(275f, 275f);

        Image body = mascot.GetComponent<Image>();
        body.sprite = CreateCircleSprite(256, new Color(0.005f, 0.005f, 0.008f, 1f), new Color(0.08f, 0.11f, 0.18f, 0.65f));
        body.color = Color.white;

        Button button = mascot.GetComponent<Button>();
        button.transition = Selectable.Transition.None;

        Image leftGlow = CreateMascotEye(mascot.transform, "LeftEyeGlow", new Vector2(-58f, 30f), new Vector2(56f, 40f), new Color(0.6f, 1f, 1f, 0.28f));
        Image rightGlow = CreateMascotEye(mascot.transform, "RightEyeGlow", new Vector2(58f, 22f), new Vector2(56f, 40f), new Color(0.6f, 1f, 1f, 0.28f));
        Image leftEye = CreateMascotEye(mascot.transform, "LeftEye", new Vector2(-58f, 30f), new Vector2(40f, 28f), Color.white);
        Image rightEye = CreateMascotEye(mascot.transform, "RightEye", new Vector2(58f, 22f), new Vector2(40f, 28f), Color.white);

        InkformMascotView view = mascot.GetComponent<InkformMascotView>();
        view.Initialize(rect, body, leftEye, rightEye, leftGlow, rightGlow);
        button.onClick.AddListener(view.PulseClick);
    }

    private Image CreateMascotEye(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject eye = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        eye.transform.SetParent(parent, false);

        RectTransform rect = eye.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = eye.GetComponent<Image>();
        image.sprite = CreateCircleSprite(96, Color.white, Color.clear);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Button CreateMenuButton(Transform parent, string label, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline), typeof(Shadow));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = CreateRoundedRectSprite(256, 64, 8);
        image.type = Image.Type.Sliced;
        image.color = new Color(0.92f, 0.96f, 0.95f, 0.96f);

        Outline outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0.02f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);

        Shadow shadow = buttonObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0.05f, 0.75f, 1f, 0.28f);
        shadow.effectDistance = new Vector2(0f, -4f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.92f, 0.96f, 0.95f, 0.96f);
        colors.highlightedColor = new Color(0.74f, 1f, 0.96f, 1f);
        colors.pressedColor = new Color(0.45f, 0.7f, 0.76f, 1f);
        colors.selectedColor = new Color(0.76f, 1f, 0.96f, 1f);
        colors.disabledColor = new Color(0.35f, 0.39f, 0.42f, 0.55f);
        button.colors = colors;
        button.targetGraphic = image;
        ApplyAutomaticNavigation(button);

        TMP_Text text = CreateText("Label", buttonObject.transform, label, 22f, TextAlignmentOptions.Center);
        RectTransform textRect = text.GetComponent<RectTransform>();
        Stretch(textRect);
        textRect.offsetMin = new Vector2(8f, 2f);
        textRect.offsetMax = new Vector2(-8f, -2f);
        text.color = new Color(0.06f, 0.08f, 0.12f, 1f);

        return button;
    }

    private RectTransform CreateSettingsPanel(Transform parent)
    {
        GameObject panelObject = new GameObject("SettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        panelObject.transform.SetParent(parent, false);

        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(900f, 640f);

        Image image = panelObject.GetComponent<Image>();
        image.sprite = CreateRoundedRectSprite(256, 256, 18);
        image.type = Image.Type.Sliced;
        image.color = new Color(0.025f, 0.03f, 0.085f, 0.94f);

        Outline outline = panelObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.48f, 0.88f, 1f, 0.58f);
        outline.effectDistance = new Vector2(2f, -2f);

        TMP_Text title = CreateText("Title", panel, "Settings", 34f, TextAlignmentOptions.Center);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -18f);
        titleRect.sizeDelta = new Vector2(-40f, 48f);

        CreateVolumeRow(panel, "BGM Volume", new Vector2(0f, -88f), true);
        CreateVolumeRow(panel, "SFX Volume", new Vector2(0f, -136f), false);

        TMP_Text controlsTitle = CreateText("ControlsTitle", panel, "Controls", 24f, TextAlignmentOptions.Left);
        RectTransform controlsTitleRect = controlsTitle.GetComponent<RectTransform>();
        controlsTitleRect.anchorMin = new Vector2(0f, 1f);
        controlsTitleRect.anchorMax = new Vector2(1f, 1f);
        controlsTitleRect.pivot = new Vector2(0.5f, 1f);
        controlsTitleRect.anchoredPosition = new Vector2(0f, -188f);
        controlsTitleRect.sizeDelta = new Vector2(-88f, 34f);

        RectTransform scroll = CreateControlsScroll(panel);
        AddBindingRows(scroll);
        CreateSettingsFooter(panel);
        RefreshBindingLabels();

        return panel;
    }

    private void CreateVolumeRow(RectTransform parent, string label, Vector2 position, bool bgm)
    {
        GameObject row = new GameObject(label.Replace(" ", string.Empty), typeof(RectTransform));
        row.transform.SetParent(parent, false);

        RectTransform rect = row.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(760f, 34f);

        TMP_Text text = CreateText("Label", row.transform, label, 18f, TextAlignmentOptions.Left);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(180f, 0f);

        Slider slider = CreateSlider(row.transform);
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(110f, 0f);
        sliderRect.sizeDelta = new Vector2(-240f, 24f);

        S_AudioManager audio = S_AudioManager.Instance;
        slider.value = audio != null ? (bgm ? audio.BgmVolume : audio.SfxVolume) : 1f;
        slider.onValueChanged.AddListener(value =>
        {
            if (S_AudioManager.Instance == null) return;
            if (bgm)
                S_AudioManager.Instance.SetBgmVolume(value);
            else
                S_AudioManager.Instance.SetSfxVolume(value);
        });
    }

    private Slider CreateSlider(Transform parent)
    {
        GameObject sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        RectTransform background = CreateFullScreenImage("Background", sliderObject.transform, CreateRoundedRectSprite(128, 16, 7), new Color(0.08f, 0.1f, 0.18f, 1f));
        RectTransform fillArea = CreateRect("Fill Area", sliderObject.transform);
        Stretch(fillArea);
        fillArea.offsetMin = new Vector2(3f, 3f);
        fillArea.offsetMax = new Vector2(-3f, -3f);

        RectTransform fill = CreateFullScreenImage("Fill", fillArea, CreateRoundedRectSprite(128, 16, 7), new Color(0.38f, 0.95f, 1f, 0.96f));
        RectTransform handleArea = CreateRect("Handle Slide Area", sliderObject.transform);
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(8f, 0f);
        handleArea.offsetMax = new Vector2(-8f, 0f);

        RectTransform handle = CreateFullScreenImage("Handle", handleArea, CreateCircleSprite(48, Color.white, new Color(0.35f, 1f, 1f, 0.5f)), Color.white);
        handle.sizeDelta = new Vector2(24f, 24f);

        slider.targetGraphic = handle.GetComponent<Image>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        return slider;
    }

    private RectTransform CreateControlsScroll(RectTransform parent)
    {
        GameObject scrollObject = new GameObject("ControlsScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(parent, false);

        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0.5f, 1f);
        scrollRectTransform.anchorMax = new Vector2(0.5f, 1f);
        scrollRectTransform.pivot = new Vector2(0.5f, 1f);
        scrollRectTransform.anchoredPosition = new Vector2(0f, -228f);
        scrollRectTransform.sizeDelta = new Vector2(790f, 315f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect);
        viewport.GetComponent<Image>().color = new Color(0.02f, 0.025f, 0.06f, 0.55f);
        viewport.GetComponent<Mask>().showMaskGraphic = true;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 28f;

        return contentRect;
    }

    private void AddBindingRows(RectTransform content)
    {
        AddBindingRow(content, "Move Up", BindingTarget.Keyboard("Move", "up", "Button"), null);
        AddBindingRow(content, "Move Down", BindingTarget.Keyboard("Move", "down", "Button"), null);
        AddBindingRow(content, "Move Left", BindingTarget.Keyboard("Move", "left", "Button"), null);
        AddBindingRow(content, "Move Right", BindingTarget.Keyboard("Move", "right", "Button"), null);
        AddBindingRow(content, "Move Stick", null, BindingTarget.Gamepad("Move", null, "Vector2"));
        AddBindingRow(content, "Jump", BindingTarget.Keyboard("Jump", null, "Button"), BindingTarget.Gamepad("Jump", null, "Button"));
        AddBindingRow(content, "Sprint", BindingTarget.Keyboard("Sprint", null, "Button"), BindingTarget.Gamepad("Sprint", null, "Button"));
        AddBindingRow(content, "Grip", BindingTarget.Keyboard("grep", null, "Button"), BindingTarget.Gamepad("grep", null, "Button"));
        AddBindingRow(content, "Hide", BindingTarget.Keyboard("Hide", null, "Button"), BindingTarget.Gamepad("Hide", null, "Button"));
        AddBindingRow(content, "Camera Control", BindingTarget.Keyboard("CameraControl", null, "Button"), BindingTarget.Gamepad("CameraControl", null, "Button"));
        AddBindingRow(content, "Open Menu", BindingTarget.Keyboard("OpenMenu", null, "Button", "<Keyboard>"), BindingTarget.Gamepad("OpenMenu", null, "Button"));
    }

    private void AddBindingRow(RectTransform parent, string label, BindingTarget keyboardTarget, BindingTarget gamepadTarget)
    {
        GameObject row = new GameObject(label.Replace(" ", string.Empty) + "Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        LayoutElement element = row.GetComponent<LayoutElement>();
        element.preferredHeight = 36f;

        TMP_Text labelText = CreateText("Label", row.transform, label, 16f, TextAlignmentOptions.Left);
        AddLayout(labelText.gameObject, 190f, 30f);

        CreateBindingButton(row.transform, keyboardTarget);
        CreateBindingButton(row.transform, gamepadTarget);
    }

    private void CreateBindingButton(Transform parent, BindingTarget target)
    {
        if (target == null)
        {
            TMP_Text placeholder = CreateText("Empty", parent, "-", 15f, TextAlignmentOptions.Center);
            AddLayout(placeholder.gameObject, 255f, 30f);
            return;
        }

        Button button = CreateSmallButton(parent, "-");
        AddLayout(button.gameObject, 255f, 30f);

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        BindingButtonView view = new BindingButtonView(button, text, target);
        bindingButtons.Add(view);
        settingsButtons.Add(button);
        button.onClick.AddListener(() => BeginRebind(view));
    }

    private void CreateSettingsFooter(RectTransform parent)
    {
        rebindStatusText = CreateText("RebindStatus", parent, string.Empty, 15f, TextAlignmentOptions.Center);
        RectTransform statusRect = rebindStatusText.GetComponent<RectTransform>();
        statusRect.anchorMin = statusRect.anchorMax = new Vector2(0.5f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.anchoredPosition = new Vector2(0f, 78f);
        statusRect.sizeDelta = new Vector2(600f, 24f);

        RectTransform footer = CreateRect("Footer", parent);
        footer.anchorMin = footer.anchorMax = new Vector2(0.5f, 0f);
        footer.pivot = new Vector2(0.5f, 0f);
        footer.anchoredPosition = new Vector2(0f, 28f);
        footer.sizeDelta = new Vector2(720f, 38f);

        HorizontalLayoutGroup layout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;

        Button reset = CreateSmallButton(footer, "Reset All");
        reset.onClick.AddListener(() =>
        {
            S_InputBindingManager.Instance.CancelRebind();
            S_InputBindingManager.Instance.ResetAllBindings();
            RefreshBindingLabels();
        });
        AddLayout(reset.gameObject, 160f, 34f);
        settingsButtons.Add(reset);

        cancelRebindButton = CreateSmallButton(footer, "Cancel Rebind");
        cancelRebindButton.onClick.AddListener(() => S_InputBindingManager.Instance.CancelRebind());
        cancelRebindButton.interactable = false;
        AddLayout(cancelRebindButton.gameObject, 170f, 34f);

        Button back = CreateSmallButton(footer, "Back");
        back.onClick.AddListener(ShowMainMenu);
        AddLayout(back.gameObject, 150f, 34f);
        settingsButtons.Add(back);
    }

    private Button CreateSmallButton(Transform parent, string label)
    {
        GameObject buttonObject = new GameObject(label.Replace(" ", string.Empty) + "Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = CreateRoundedRectSprite(192, 40, 7);
        image.type = Image.Type.Sliced;
        image.color = new Color(0.82f, 0.92f, 0.96f, 0.96f);

        Outline outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0.02f, 0.05f, 0.95f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.82f, 0.92f, 0.96f, 0.96f);
        colors.highlightedColor = new Color(0.58f, 1f, 0.96f, 1f);
        colors.pressedColor = new Color(0.35f, 0.63f, 0.68f, 1f);
        colors.selectedColor = new Color(0.62f, 1f, 0.96f, 1f);
        colors.disabledColor = new Color(0.24f, 0.28f, 0.33f, 0.65f);
        button.colors = colors;
        button.targetGraphic = image;
        ApplyAutomaticNavigation(button);

        TMP_Text text = CreateText("Text", buttonObject.transform, label, 14f, TextAlignmentOptions.Center);
        Stretch(text.GetComponent<RectTransform>());
        text.color = new Color(0.04f, 0.07f, 0.11f, 1f);
        return button;
    }

    private void ShowSettings()
    {
        mainMenuRoot.gameObject.SetActive(false);
        settingsPanel.gameObject.SetActive(true);
        RefreshBindingLabels();
        SelectFirstSettingsButton();
    }

    private void ShowMainMenu()
    {
        if (S_InputBindingManager.HasInstance)
            S_InputBindingManager.Instance.CancelRebind();

        settingsPanel.gameObject.SetActive(false);
        mainMenuRoot.gameObject.SetActive(true);
        activeRebind = null;
        SetSettingsInteractable(true);
        SelectButton(startButton);
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
        SetSettingsInteractable(false);
        view.Label.text = "press input...";

        if (rebindStatusText != null)
            rebindStatusText.text = "waiting for input";

        SelectButton(cancelRebindButton);

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
        SetSettingsInteractable(true);
        RefreshBindingLabels();

        if (rebindStatusText != null)
            rebindStatusText.text = "binding unavailable";
    }

    private void CompleteRebind()
    {
        BindingButtonView completed = activeRebind;
        activeRebind = null;
        SetSettingsInteractable(true);
        RefreshBindingLabels();
        SelectButton(completed != null ? completed.Button : null);

        if (rebindStatusText != null)
            rebindStatusText.text = "saved";
    }

    private void CancelRebind()
    {
        BindingButtonView cancelled = activeRebind;
        activeRebind = null;
        SetSettingsInteractable(true);
        RefreshBindingLabels();
        SelectButton(cancelled != null ? cancelled.Button : null);

        if (rebindStatusText != null)
            rebindStatusText.text = "cancelled";
    }

    private void SetSettingsInteractable(bool interactable)
    {
        foreach (Button button in settingsButtons)
        {
            if (button != null)
                button.interactable = interactable;
        }

        if (cancelRebindButton != null)
            cancelRebindButton.interactable = !interactable;
    }

    private void RefreshBindingLabels()
    {
        foreach (BindingButtonView view in bindingButtons)
        {
            if (view == null || view.Label == null || activeRebind == view)
                continue;

            BindingTarget target = view.Target;
            view.Label.text = S_InputBindingManager.Instance.GetBindingDisplayString(
                target.ActionName,
                target.BindingGroup,
                target.PartName,
                target.DevicePath);
        }
    }

    private void SelectFirstSettingsButton()
    {
        foreach (BindingButtonView view in bindingButtons)
        {
            if (view.Button != null && view.Button.interactable)
            {
                SelectButton(view.Button);
                return;
            }
        }
    }

    private void SelectButton(Button button)
    {
        if (button == null || EventSystem.current == null || !button.gameObject.activeInHierarchy)
            return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void ApplyAutomaticNavigation(Selectable selectable)
    {
        if (selectable == null) return;
        Navigation nav = selectable.navigation;
        nav.mode = Navigation.Mode.Automatic;
        selectable.navigation = nav;
    }

    private TMP_Text CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontSizeMin = 9f;
        tmp.fontSizeMax = size;
        tmp.enableAutoSizing = true;
        tmp.alignment = alignment;
        tmp.color = new Color(0.9f, 0.98f, 1f, 1f);
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

    private RectTransform CreateFullScreenImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        Stretch(rect);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private void AddLayout(GameObject target, float width, float height)
    {
        LayoutElement layout = target.GetComponent<LayoutElement>();
        if (layout == null)
            layout = target.AddComponent<LayoutElement>();

        layout.preferredWidth = width;
        layout.preferredHeight = height;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Sprite CreateDeepSpaceSprite()
    {
        const int width = 768;
        const int height = 432;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color top = new Color(0.055f, 0.04f, 0.16f, 1f);
        Color mid = new Color(0.035f, 0.08f, 0.2f, 1f);
        Color bottom = new Color(0.015f, 0.012f, 0.045f, 1f);

        for (int y = 0; y < height; y++)
        {
            float t = y / (height - 1f);
            Color vertical = t < 0.55f
                ? Color.Lerp(bottom, mid, t / 0.55f)
                : Color.Lerp(mid, top, (t - 0.55f) / 0.45f);

            for (int x = 0; x < width; x++)
            {
                float nx = (x - width * 0.34f) / width;
                float ny = (y - height * 0.48f) / height;
                float glow = Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny) * 2.3f) * 0.15f;
                Color pixel = vertical + new Color(glow * 0.2f, glow * 0.35f, glow, 0f);
                pixel.a = 1f;
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateCircleSprite(int size, Color fill, Color rim)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = (size - 1) * 0.5f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(radius + 1f - dist);
                float edge = Mathf.Clamp01((dist - radius * 0.78f) / (radius * 0.2f));
                Color color = Color.Lerp(fill, rim.a > 0f ? rim : fill, edge * rim.a);
                color.a *= alpha;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateStarSprite(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y) - center;
                float dist = p.magnitude;
                float horizontal = Mathf.Exp(-Mathf.Abs(p.y) * 0.42f) * Mathf.Clamp01(1f - Mathf.Abs(p.x) / (size * 0.48f));
                float vertical = Mathf.Exp(-Mathf.Abs(p.x) * 0.42f) * Mathf.Clamp01(1f - Mathf.Abs(p.y) / (size * 0.48f));
                float core = Mathf.Clamp01(1f - dist / (size * 0.13f));
                float alpha = Mathf.Clamp01(Mathf.Max(horizontal, vertical) * 0.82f + core);
                Color pixel = color;
                pixel.a *= alpha;
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateRoundedRectSprite(int width, int height, int radius)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color white = Color.white;
        Color clear = Color.clear;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float px = Mathf.Min(x, width - 1 - x);
                float py = Mathf.Min(y, height - 1 - y);
                float alpha = 1f;
                if (px < radius && py < radius)
                {
                    Vector2 corner = new Vector2(radius, radius);
                    Vector2 point = new Vector2(px, py);
                    alpha = Mathf.Clamp01(radius + 0.75f - Vector2.Distance(point, corner));
                }

                texture.SetPixel(x, y, alpha > 0f ? new Color(white.r, white.g, white.b, alpha) : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
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

public class StartMenuStar : MonoBehaviour
{
    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Vector2 basePosition;
    private float speed;
    private float phase;
    private float drift;
    private bool twinkle;

    public void Initialize(float speed, float phase, float drift, bool twinkle)
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        basePosition = rect.anchoredPosition;
        this.speed = speed;
        this.phase = phase;
        this.drift = drift;
        this.twinkle = twinkle;
    }

    private void Update()
    {
        if (rect == null)
            return;

        float time = Time.unscaledTime * speed + phase;
        rect.anchoredPosition = basePosition + new Vector2(Mathf.Sin(time * 0.7f), Mathf.Cos(time)) * drift;

        if (canvasGroup != null)
            canvasGroup.alpha = twinkle ? 0.55f + Mathf.Sin(time * 4.2f) * 0.28f : 0.55f + Mathf.Sin(time * 2.1f) * 0.12f;
    }
}

public class InkformMascotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private RectTransform root;
    private Image body;
    private Image leftEye;
    private Image rightEye;
    private Image leftGlow;
    private Image rightGlow;
    private bool hovered;
    private float clickPulse;

    public void Initialize(RectTransform root, Image body, Image leftEye, Image rightEye, Image leftGlow, Image rightGlow)
    {
        this.root = root;
        this.body = body;
        this.leftEye = leftEye;
        this.rightEye = rightEye;
        this.leftGlow = leftGlow;
        this.rightGlow = rightGlow;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PulseClick();
    }

    public void PulseClick()
    {
        clickPulse = 1f;
    }

    private void Update()
    {
        if (root == null)
            return;

        float time = Time.unscaledTime;
        clickPulse = Mathf.MoveTowards(clickPulse, 0f, Time.unscaledDeltaTime * 2.6f);

        float breathe = 1f + Mathf.Sin(time * 1.5f) * 0.025f;
        float hoverScale = hovered ? 1.045f : 1f;
        float pulseScale = 1f + Mathf.Sin(clickPulse * Mathf.PI) * 0.12f;
        root.localScale = Vector3.one * breathe * hoverScale * pulseScale;
        root.localRotation = Quaternion.Euler(0f, 0f, hovered ? Mathf.Sin(time * 3f) * 2.5f : Mathf.Sin(time * 1.1f) * 1.2f);

        float glow = hovered ? 0.55f : 0.28f;
        glow += Mathf.Sin(time * 3.8f) * 0.08f;
        Color glowColor = new Color(0.64f, 1f, 1f, Mathf.Clamp01(glow + clickPulse * 0.35f));

        if (leftGlow != null) leftGlow.color = glowColor;
        if (rightGlow != null) rightGlow.color = glowColor;

        float blink = Mathf.Sin(time * 0.85f) > 0.985f ? 0.25f : 1f;
        if (clickPulse > 0.65f)
            blink = 0.38f;

        SetEyeScale(leftEye, blink);
        SetEyeScale(rightEye, blink);

        if (body != null)
            body.color = hovered ? new Color(0.02f, 0.025f, 0.04f, 1f) : Color.white;
    }

    private void SetEyeScale(Image eye, float yScale)
    {
        if (eye == null)
            return;

        RectTransform rect = eye.GetComponent<RectTransform>();
        rect.localScale = new Vector3(1f, yScale, 1f);
    }
}
