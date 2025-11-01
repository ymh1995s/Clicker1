// 2025-11-01 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

    private float _goldIncrementTimer;

    protected override void Awake()
    {
        base.Awake();

        // Automatically connect serialized fields using FindChildGameObject
        _upgradeButton = FindChildGameObject("UpgradeButton").GetComponent<Button>();
        _charactersButton = FindChildGameObject("CharactersButton").GetComponent<Button>();
        _bmButton = FindChildGameObject("BMButton").GetComponent<Button>();
        _optionButton = FindChildGameObject("OptionButton").GetComponent<Button>();
        _upgradeTab = FindChildGameObject("UpgradeTab");
        _charactersTab = FindChildGameObject("CharactersTab");
        _bmTab = FindChildGameObject("BMTab");
        _optionTab = FindChildGameObject("OptionTab");
        _gameArea = FindChildGameObject("GameArea");
        _goldPerClickText = FindChildGameObject("GoldPerClickText").GetComponent<TMP_Text>();
        _goldPerSecText = FindChildGameObject("GoldPerSecText").GetComponent<TMP_Text>();
        _goldText = FindChildGameObject("GoldText").GetComponent<TMP_Text>();
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
    }

    protected override void BindUIEvents()
    {
        base.BindUIEvents();

        // Bind buttons to their respective actions
        _upgradeButton.onClick.RemoveAllListeners();
        _upgradeButton.onClick.AddListener(() => ActivateTab(_upgradeTab));

        _charactersButton.onClick.RemoveAllListeners();
        _charactersButton.onClick.AddListener(() => ActivateTab(_charactersTab));

        _bmButton.onClick.RemoveAllListeners();
        _bmButton.onClick.AddListener(() => ActivateTab(_bmTab));

        _optionButton.onClick.RemoveAllListeners();
        _optionButton.onClick.AddListener(() => ActivateTab(_optionTab));

        // Bind GameArea click event
        _gameArea.GetComponent<Button>().onClick.RemoveAllListeners();
        _gameArea.GetComponent<Button>().onClick.AddListener(() => AddGold(GameManager.Instance.GoldPerClick));
    }

    protected override void RefreshUI()
    {
        base.RefreshUI();

        // Update UI text elements with GameManager values
        UpdateGoldText();
        UpdateGoldPerClickText();
        UpdateGoldPerSecText();

        // Subscribe to GameManager events
        GameManager.Instance.OnGoldChanged += UpdateGoldText;
        GameManager.Instance.OnGoldPerClickChanged += UpdateGoldPerClickText;
        GameManager.Instance.OnGoldPerSecondChanged += UpdateGoldPerSecText;
    }

    private void ActivateTab(GameObject tab)
    {
        _upgradeTab.SetActive(false);
        _charactersTab.SetActive(false);
        _bmTab.SetActive(false);
        _optionTab.SetActive(false);

        tab.SetActive(true);
    }

    private void AddGold(int amount)
    {
        GameManager.Instance.Gold += amount;
    }

    private void UpdateGoldPerSecond()
    {
        _goldIncrementTimer += Time.deltaTime;
        if (_goldIncrementTimer >= 1f)
        {
            _goldIncrementTimer = 0f;
            GameManager.Instance.Gold += GameManager.Instance.GoldPerSecond;
        }
    }

    private void UpdateGoldText()
    {
        _goldText.text = GameManager.Instance.Gold.ToString();
    }

    private void UpdateGoldPerClickText()
    {
        _goldPerClickText.text = $"GPC: {GameManager.Instance.GoldPerClick}";
    }

    private void UpdateGoldPerSecText()
    {
        _goldPerSecText.text = $"GPS: {GameManager.Instance.GoldPerSecond}";
    }
}
