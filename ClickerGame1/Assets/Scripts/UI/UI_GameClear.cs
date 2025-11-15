using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    // Input blocker and root canvas fade
    private GameObject _inputBlocker;
    private CanvasGroup _rootCanvasGroup;
    [SerializeField] private float _canvasFadeDuration = 0.5f;

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

        EnsureInputBlockerExists();
        EnsureRootCanvasGroupExists();
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

        // Block input immediately
        if (_inputBlocker != null)
            _inputBlocker.SetActive(true);

        // Trigger UI_Animation's game clear clip to play immediately (non-looping)
        try
        {
            var anim = UnityEngine.Object.FindObjectOfType<UI_Animation>(true);
            if (anim != null)
            {
                // Play the clip and when finished perform rebirth+fade sequence
                anim.PlayGameClearImmediate(() =>
                {
                    // Start coroutine to handle rebirth and canvas fade. Use Unity main thread context
                    try
                    {
                        StartCoroutine(GameClearCompleteSequence());
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"UI_GameClear: Failed to start completion sequence - {ex}");
                        // Ensure inputs are unblocked in case of failure
                        if (_inputBlocker != null) _inputBlocker.SetActive(false);
                    }
                });
            }
            else
            {
                Debug.LogWarning("UI_GameClear: UI_Animation instance not found. Cannot play game-clear clip.");
                // still perform rebirth sequence so game continues
                StartCoroutine(GameClearCompleteSequence());
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"UI_GameClear: Failed to play game-clear animation - {ex}");
            if (_inputBlocker != null) _inputBlocker.SetActive(false);
        }
    }

    private IEnumerator GameClearCompleteSequence()
    {
        // First fade out root canvas
        if (_rootCanvasGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(_rootCanvasGroup, 1f, 0f, _canvasFadeDuration * 2f));

        // small delay to ensure fade completed visually
        yield return new WaitForSeconds(0.05f);

        // Perform Rebirth (resets values while preserving crystals/characters) AFTER fade out
        try
        {
            if (GameManager.Instance != null)
                GameManager.Instance.Rebirth();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"UI_GameClear: Rebirth failed - {ex}");
        }

        // Award clear crystals based on character I/J contributions (ClearCrystalReward) AFTER rebirth recalculation
        try
        {
            if (GameManager.Instance != null)
            {
                int reward = GameManager.Instance.ClearCrystalReward;
                if (reward > 0)
                {
                    GameManager.Instance.Crystal += reward;
                    Debug.Log($"UI_GameClear: Awarded {reward} crystals for clear based on characters (I/J). Current crystals={GameManager.Instance.Crystal}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"UI_GameClear: Failed to award clear crystals - {ex}");
        }

        // small delay to ensure state settled
        yield return new WaitForSeconds(0.1f);

        // Fade back in
        if (_rootCanvasGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(_rootCanvasGroup, 0f, 1f, _canvasFadeDuration * 2f));

        // Notify GameManager that rebirth sequence is fully complete (including fades)
        try { GameManager.Instance?.NotifyRebirthSequenceComplete(); } catch { }

        // Unblock input
        if (_inputBlocker != null)
            _inputBlocker.SetActive(false);

        yield break;
    }

    private void EnsureInputBlockerExists()
    {
        if (_inputBlocker != null) return;
        // Find root canvas
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Look for existing blocker
        var existing = canvas.transform.Find("InputBlocker");
        if (existing != null)
        {
            _inputBlocker = existing.gameObject;
            _inputBlocker.SetActive(false);
            return;
        }

        // Create blocker
        var go = new GameObject("InputBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f); // transparent but blocks raycasts
        img.raycastTarget = true;
        go.SetActive(false);
        _inputBlocker = go;
    }

    private void EnsureRootCanvasGroupExists()
    {
        if (_rootCanvasGroup != null) return;
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        _rootCanvasGroup = canvas.GetComponent<CanvasGroup>();
        if (_rootCanvasGroup == null)
            _rootCanvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        // ensure alpha is 1 initially
        _rootCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null)
            yield break;
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        cg.alpha = to;
    }
}
