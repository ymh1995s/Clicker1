using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// UI_GameClear: 버튼형 UI로, 게임 클리어(또는 엔드컨텐츠)를 실행하기 위해 큰 금액이 필요합니다.
// 하위 객체 NeedMoneyImage는 플레이어의 골드가 부족하면 활성화되고,
// NeedMoneyImage/Text에는 항상 "천만 골드 필요" 라고 표시됩니다.
// 권위 있는 수치는 항상 GameManager를 기준으로 확인합니다.
public class UI_GameClear : UI_Base
{
    [SerializeField] private Button _clickButton;

    // Required gold for game clear: 10,000,000 (천만)
    // This value is authoritative here for the button's requirement.
    private const long REQUIRED_GOLD = 10_000_000L;

    // UI for indicating not enough gold
    [SerializeField] private GameObject _needMoneyImage;
    [SerializeField] private TMP_Text _needMoneyText;

    protected override void Awake()
    {
        base.Awake();

        if (_clickButton == null)
        {
            var go = FindChildGameObject("ClickButton");
            if (go != null)
                _clickButton = go.GetComponent<Button>();
        }

        if (_clickButton == null)
            Debug.LogWarning($"[UI_GameClear] ClickButton not found for {gameObject.name}.", this);

        // Try to find NeedMoneyImage but tolerate missing child
        GameObject needGo = null;
        try
        {
            needGo = FindChildGameObject("NeedMoneyImage");
        }
        catch { needGo = null; }

        if (_needMoneyImage == null && needGo != null)
            _needMoneyImage = needGo;

        if (_needMoneyText == null && _needMoneyImage != null)
        {
            var textTransform = _needMoneyImage.transform.Find("Text");
            if (textTransform != null)
                _needMoneyText = textTransform.GetComponent<TMP_Text>();
            if (_needMoneyText == null)
                _needMoneyText = _needMoneyImage.GetComponentInChildren<TMP_Text>(true);
        }

        if (_needMoneyImage != null)
            _needMoneyImage.SetActive(false);

        // Bind events early
        BindUIEvents();
    }

    protected override void Start()
    {
        base.Start();
        RefreshUI();
    }

    void OnEnable()
    {
        BindUIEvents();
        RefreshUI();
        if (GameManager.Instance != null)
            GameManager.Instance.OnGoldChanged += RefreshUI;
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGoldChanged -= RefreshUI;
    }

    protected override void BindUIEvents()
    {
        if (_clickButton == null) return;
        _clickButton.onClick.RemoveAllListeners();
        _clickButton.onClick.AddListener(() => TryGameClear());
    }

    protected override void RefreshUI()
    {
        base.RefreshUI();

        if (_clickButton == null) return;

        bool hasEnough = false;
        if (GameManager.Instance != null)
            hasEnough = GameManager.Instance.Gold >= REQUIRED_GOLD;

        // NeedMoneyImage is active only when not enough gold
        if (_needMoneyImage != null)
        {
            _needMoneyImage.SetActive(!hasEnough);
            if (!hasEnough && _needMoneyText != null)
            {
                // Show requested numeric text instead of Korean word '천만'
                _needMoneyText.text = $"{REQUIRED_GOLD:N0} 골드 필요";
            }
        }

        // Button is interactable only when enough gold
        _clickButton.interactable = hasEnough;
        var colors = _clickButton.colors;
        colors.normalColor = hasEnough ? Color.white : Color.grey;
        _clickButton.colors = colors;
    }

    private void TryGameClear()
    {
        if (_clickButton == null) return;
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.Gold < REQUIRED_GOLD)
        {
            // not enough
            return;
        }

        // Deduct required gold and perform a placeholder "game clear" action.
        // Real game logic (analytics, save, scene change) should be invoked here.
        GameManager.Instance.Gold -= (int)REQUIRED_GOLD;

        Debug.Log("Game cleared: required gold consumed.");

        // Trigger UI_Animation's game clear clip to play immediately (non-looping)
        try
        {
            var anim = UnityEngine.Object.FindObjectOfType<UI_Animation>(true);
            if (anim != null)
            {
                // Play the clip and when finished perform rebirth
                anim.PlayGameClearImmediate(() =>
                {
                    // After the animation completes, perform rebirth and save
                    try
                    {
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.Rebirth();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"UI_GameClear: Rebirth failed - {ex}");
                    }
                });
            }
            else
            {
                Debug.LogWarning("UI_GameClear: UI_Animation instance not found. Cannot play game-clear clip.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"UI_GameClear: Failed to play game-clear animation - {ex}");
        }

        // TODO: Trigger actual game-clear flow (show UI, load next scene, grant rewards, etc.)
    }
}
