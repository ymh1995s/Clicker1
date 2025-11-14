using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Summary:
// This is the UI_ClickerGame class, implemented as a subclass of UI_Base. It manages the UI interactions
// for the Clicker Game, including handling button clicks to activate tabs, updating gold values based on
// GPC and GPS, and synchronizing UI text elements with game state variables in GameManager.
// Additionally, it uses the FindChildGameObject method from UI_Base to automatically connect serialized fields
// to their corresponding child GameObjects.

public class UI_ClickerGame : UI_Base
{
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private Button _charactersButton;
    [SerializeField] private Button _bmButton;
    [SerializeField] private Button _optionButton;
    [SerializeField] private GameObject _upgradeTab;
    [SerializeField] private GameObject _charactersTab;
    [SerializeField] private GameObject _bmTab;
    [SerializeField] private GameObject _optionTab;
    [SerializeField] private GameObject _gameArea;
    [SerializeField] private TMP_Text _goldPerClickText;
    [SerializeField] private TMP_Text _goldPerSecText;
    [SerializeField] private TMP_Text _goldText;

    // Crystal UI
    [SerializeField] private TMP_Text _crystalText;
    [SerializeField] private TMP_Text _crystalPerMinText;

    private float _goldIncrementTimer;
    private Vector2 _lastClickScreenPos;

    // update timer for crystal display
    private float _crystalUpdateTimer = 0f;
    private const float CRYSTAL_UPDATE_INTERVAL = 0.5f; // seconds

    protected override void Awake()
    {
        base.Awake();

        // Automatically connect serialized fields using FindChildGameObject (use Optional variant to avoid exceptions)
        var go = FindChildGameObjectOptional("UpgradeButton");
        if (go != null) _upgradeButton = go.GetComponent<Button>();
        go = FindChildGameObjectOptional("CharactersButton");
        if (go != null) _charactersButton = go.GetComponent<Button>();
        go = FindChildGameObjectOptional("BMButton");
        if (go != null) _bmButton = go.GetComponent<Button>();
        go = FindChildGameObjectOptional("OptionButton");
        if (go != null) _optionButton = go.GetComponent<Button>();
        var tmp = FindChildGameObjectOptional("UpgradeTab"); if (tmp != null) _upgradeTab = tmp;
        tmp = FindChildGameObjectOptional("CharactersTab"); if (tmp != null) _charactersTab = tmp;
        tmp = FindChildGameObjectOptional("BMTab"); if (tmp != null) _bmTab = tmp;
        tmp = FindChildGameObjectOptional("OptionTab"); if (tmp != null) _optionTab = tmp;
        var area = FindChildGameObjectOptional("GameArea"); if (area != null) _gameArea = area;

        var t = FindChildGameObjectOptional("GoldPerClickText"); if (t != null) _goldPerClickText = t.GetComponent<TMP_Text>();
        t = FindChildGameObjectOptional("GoldPerSecText"); if (t != null) _goldPerSecText = t.GetComponent<TMP_Text>();
        t = FindChildGameObjectOptional("GoldText"); if (t != null) _goldText = t.GetComponent<TMP_Text>();

        // crystal texts (optional children)
        var ctGo = FindChildGameObjectOptional("CrystalText");
        if (ctGo != null) _crystalText = ctGo.GetComponent<TMP_Text>();
        var cpmGo = FindChildGameObjectOptional("CrystalTextPerMinText") ?? FindChildGameObjectOptional("CrystalPerMinText");
        if (cpmGo != null) _crystalPerMinText = cpmGo.GetComponent<TMP_Text>();
    }

    protected override void Start()
    {
        base.Start();
        BindUIEvents();
        RefreshUI();
    }

    protected override void Update()
    {
        base.Update();
        UpdateGoldPerSecond();

        // update crystal display periodically
        _crystalUpdateTimer += Time.deltaTime;
        if (_crystalUpdateTimer >= CRYSTAL_UPDATE_INTERVAL)
        {
            _crystalUpdateTimer = 0f;
            UpdateCrystalText();
            UpdateCrystalPerMinText();
        }

        // Handle input for click/tap to add gold at that screen position
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            if (IsPointerOverGameArea(pos) && !IsPointerBlockedByOtherUI(pos)) OnPlayAreaClick(pos);
        }
        if (Touchscreen.current != null)
        {
            foreach (var t in Touchscreen.current.touches)
            {
                if (t.press.wasPressedThisFrame)
                {
                    Vector2 pos = t.position.ReadValue();
                    if (IsPointerOverGameArea(pos) && !IsPointerBlockedByOtherUI(pos)) OnPlayAreaClick(pos);
                }
            }
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 pos = Input.mousePosition;
            if (IsPointerOverGameArea(pos) && !IsPointerBlockedByOtherUI(pos)) OnPlayAreaClick(pos);
        }
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    Vector2 pos = t.position;
                    if (IsPointerOverGameArea(pos) && !IsPointerBlockedByOtherUI(pos)) OnPlayAreaClick(pos);
                }
            }
        }
#endif
    }

    protected override void BindUIEvents()
    {
        base.BindUIEvents();

        // Bind buttons to their respective actions (null-checks to avoid runtime exceptions)
        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.RemoveAllListeners();
            _upgradeButton.onClick.AddListener(() => ActivateTab(_upgradeTab));
        }

        if (_charactersButton != null)
        {
            _charactersButton.onClick.RemoveAllListeners();
            _charactersButton.onClick.AddListener(() => ActivateTab(_charactersTab));
        }

        if (_bmButton != null)
        {
            _bmButton.onClick.RemoveAllListeners();
            _bmButton.onClick.AddListener(() => ActivateTab(_bmTab));
        }

        if (_optionButton != null)
        {
            _optionButton.onClick.RemoveAllListeners();
            _optionButton.onClick.AddListener(() => ActivateTab(_optionTab));
        }

        // Do NOT bind GameArea button here. Input is handled in Update to capture click position.
    }

    protected override void RefreshUI()
    {
        base.RefreshUI();

        // Update UI text elements with GameManager values
        UpdateGoldText();
        UpdateGoldPerClickText();
        UpdateGoldPerSecText();

        // Update crystal texts as well
        UpdateCrystalText();
        UpdateCrystalPerMinText();

        // Note: GameManager subscriptions are handled in OnEnable/OnDisable to avoid double subscriptions
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGoldChanged += UpdateGoldText;
            GameManager.Instance.OnGoldPerClickChanged += UpdateGoldPerClickText;
            GameManager.Instance.OnGoldPerSecondChanged += UpdateGoldPerSecText;
            GameManager.Instance.OnCrystalChanged += UpdateCrystalText; // subscribe to crystal changes
        }
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGoldChanged -= UpdateGoldText;
            GameManager.Instance.OnGoldPerClickChanged -= UpdateGoldPerClickText;
            GameManager.Instance.OnGoldPerSecondChanged -= UpdateGoldPerSecText;
            GameManager.Instance.OnCrystalChanged -= UpdateCrystalText; // unsubscribe
        }
    }

    private bool IsPointerOverGameArea(Vector2 screenPosition)
    {
        if (_gameArea == null) return false;
        RectTransform areaRect = _gameArea.GetComponent<RectTransform>();
        if (areaRect == null) return false;

        Canvas canvas = _gameArea.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera) ? canvas.worldCamera : null;

        return RectTransformUtility.RectangleContainsScreenPoint(areaRect, screenPosition, cam);
    }

    private bool IsPointerBlockedByOtherUI(Vector2 screenPosition)
    {
        // If there's no EventSystem, nothing can block
        if (EventSystem.current == null) return false;

        var ped = new PointerEventData(EventSystem.current) { position = screenPosition };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);
        if (results.Count == 0) return false;

        // Topmost raycast result determines which UI element is on top at that position
        var top = results[0].gameObject;
        if (top == null) return false;

        // If the topmost object is the game area or a child of it, then it's not blocked
        if (_gameArea != null && (top == _gameArea || top.transform.IsChildOf(_gameArea.transform)))
            return false;

        // Otherwise it's blocked by some other UI element
        return true;
    }

    void OnDestroy()
    {
        // Ensure unsubscription
        OnDisable();
    }

    private void ActivateTab(GameObject tab)
    {
        if (tab == null) return;

        _upgradeTab.SetActive(tab == _upgradeTab);
        _charactersTab.SetActive(tab == _charactersTab);
        _bmTab.SetActive(tab == _bmTab);
        _optionTab.SetActive(tab == _optionTab);
    }

    // Called when player clicks/taps the play area (or anywhere allowed)
    private void OnPlayAreaClick(Vector2 screenPosition)
    {
        _lastClickScreenPos = screenPosition;

        if (GameManager.Instance == null) return;

        GameManager.Instance.Gold += GameManager.Instance.GoldPerClick;

        // Spawn gold effect at the click position
        if (GoldEffectManager.Instance != null)
        {
            GoldEffectManager.Instance.SpawnAtScreen(screenPosition);
        }
    }

    private void UpdateGoldPerSecond()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.GoldPerSecond <= 0) return;

        _goldIncrementTimer += Time.deltaTime;
        if (_goldIncrementTimer >= 1f)
        {
            int increments = Mathf.FloorToInt(_goldIncrementTimer);
            GameManager.Instance.Gold += GameManager.Instance.GoldPerSecond * increments;
            _goldIncrementTimer -= increments;
        }
    }

    private GameObject FindChildGameObjectOptional(string name)
    {
        try
        {
            return FindChildGameObject(name);
        }
        catch { return null; }
    }

    private void UpdateGoldText()
    {
        if (_goldText == null || GameManager.Instance == null) return;
        _goldText.text = GameManager.Instance.Gold.ToString("N0");
    }

    private void UpdateGoldPerClickText()
    {
        if (_goldPerClickText == null || GameManager.Instance == null) return;
        _goldPerClickText.text = GameManager.Instance.GoldPerClick.ToString("N0");
    }

    private void UpdateGoldPerSecText()
    {
        if (_goldPerSecText == null || GameManager.Instance == null) return;
        _goldPerSecText.text = GameManager.Instance.GoldPerSecond.ToString("N0");
    }

    private void UpdateCrystalText()
    {
        if (_crystalText == null || GameManager.Instance == null) return;
        // Format: current crystal number
        _crystalText.text = GameManager.Instance.Crystal.ToString("N0");
        // removed newline to keep single-line display
    }

    private void UpdateCrystalPerMinText()
    {
        if (_crystalPerMinText == null || GameManager.Instance == null) return;
        // Format: CPM value with short suffix '/m' on the same line (no newline)
        _crystalPerMinText.text = $"{GameManager.Instance.CPM:N0}/m";
    }
}
