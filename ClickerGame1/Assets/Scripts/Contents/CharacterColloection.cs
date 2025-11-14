using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Manages a character collection frame UI with stars and a hide placeholder.
// Structure expected in the GameObject:
// - HideCharacter (GameObject)
// - Character (GameObject or Image)
// - Stars (GameObject)
//   - NonStar1..NonStar5
//   - Star1..Star5
// - HideExplain (GameObject)
// - Icon (GameObject)
// - ExplainTxt (TMP_Text)
// - ValueTxt (TMP_Text)
// The frame has an identifier 'collectionId' used for saving/loading. Star count ranges 0..5.
public class CharacterColloection : MonoBehaviour
{
    // 10 enum values A..J exposed in the Inspector
    public enum CharacterId
    {
        A, B, C, D, E, F, G, H, I, J
    }

    [Header("Identification")]
    [SerializeField]
    private CharacterId collectionIdEnum = CharacterId.A; // choose in Inspector

    // Provide a string view for existing SaveManager integration
    public string collectionId => collectionIdEnum.ToString();

    [Header("References")]
    [SerializeField] private GameObject hideCharacter;
    [SerializeField] private GameObject characterImage;
    [SerializeField] private GameObject starsRoot;

    // New UI explain fields
    [SerializeField] private GameObject hideExplain;
    [SerializeField] private GameObject icon;
    [SerializeField] private TMP_Text explainTxt;
    [SerializeField] private TMP_Text valueTxt;

    // internal current star count (0..5)
    [SerializeField] private int _currentStars = 0;

    private GameObject[] nonStars = new GameObject[5];
    private GameObject[] stars = new GameObject[5];

    // track whether we already loaded saved value to avoid double-loading
    private bool _loadedFromSave = false;

    private void Awake()
    {
        // Attempt to auto-find children by name if not assigned
        if (hideCharacter == null) hideCharacter = FindChild("HideCharacter");
        if (characterImage == null) characterImage = FindChild("Character");
        if (starsRoot == null) starsRoot = FindChild("Stars");

        // New UI auto-find
        if (hideExplain == null) hideExplain = FindChild("HideExplain");
        if (icon == null) icon = FindChild("Icon");
        if (explainTxt == null)
        {
            var et = transform.Find("ExplainTxt") ?? transform.Find("ExplainText");
            if (et != null) explainTxt = et.GetComponent<TMP_Text>();
        }
        if (valueTxt == null)
        {
            var vt = transform.Find("ValueTxt") ?? transform.Find("ValueText");
            if (vt != null) valueTxt = vt.GetComponent<TMP_Text>();
        }

        // populate star arrays
        if (starsRoot != null)
        {
            for (int i = 0; i < 5; i++)
            {
                nonStars[i] = FindChildInParent(starsRoot, $"NonStar{i+1}");
                stars[i] = FindChildInParent(starsRoot, $"Star{i+1}");
            }
        }

        // Load saved state if SaveManager already initialized
        if (SaveManager.Instance != null)
        {
            _currentStars = SaveManager.Instance.GetSavedCharacterStars(collectionId);
            _loadedFromSave = true;
        }

        ApplyVisualState();

        // Notify GameManager of current stars so effects apply on startup
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateCharacterStars(collectionId, _currentStars);
        }
    }

    private void Start()
    {
        // If SaveManager wasn't ready during Awake, load now in Start (safe order)
        if (!_loadedFromSave && SaveManager.Instance != null)
        {
            _currentStars = SaveManager.Instance.GetSavedCharacterStars(collectionId);
            _loadedFromSave = true;
            ApplyVisualState();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdateCharacterStars(collectionId, _currentStars);
            }
        }
    }

    private void OnEnable()
    {
        // subscribe to SaveManager load event so late-loaded SaveManager can update visuals
        if (SaveManager.Instance != null)
            SaveManager.Instance.OnLoaded += OnSaveDataLoaded;
    }

    private void OnDisable()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.OnLoaded -= OnSaveDataLoaded;
    }

    private void OnSaveDataLoaded()
    {
        if (SaveManager.Instance == null) return;
        _currentStars = SaveManager.Instance.GetSavedCharacterStars(collectionId);
        ApplyVisualState();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateCharacterStars(collectionId, _currentStars);
        }
    }

    private GameObject FindChild(string name)
    {
        var t = transform.Find(name);
        return t != null ? t.gameObject : null;
    }

    private GameObject FindChildInParent(GameObject parent, string name)
    {
        if (parent == null) return null;
        var t = parent.transform.Find(name);
        return t != null ? t.gameObject : null;
    }

    private void ApplyVisualState()
    {
        // Default: if we have no saved info (stars == 0), show HideCharacter and NonStars active
        bool hasInfo = _currentStars > 0;

        if (hideCharacter != null) hideCharacter.SetActive(!hasInfo);
        if (characterImage != null) characterImage.SetActive(hasInfo);

        for (int i = 0; i < 5; i++)
        {
            if (nonStars[i] != null) nonStars[i].SetActive(!hasInfo || i >= _currentStars);
            if (stars[i] != null) stars[i].SetActive(hasInfo && i < _currentStars);
        }

        // Explain UI behavior
        if (_currentStars <= 0)
        {
            if (hideExplain != null) hideExplain.SetActive(true);
            if (icon != null) icon.SetActive(false);
            if (explainTxt != null) explainTxt.gameObject.SetActive(false);
            if (valueTxt != null) valueTxt.gameObject.SetActive(false);
        }
        else
        {
            if (hideExplain != null) hideExplain.SetActive(false);
            if (icon != null) icon.SetActive(true);
            if (explainTxt != null) explainTxt.gameObject.SetActive(true);
            if (valueTxt != null) valueTxt.gameObject.SetActive(true);

            // update explain/value text based on character type and star count
            UpdateExplainAndValue();
        }
    }

    private void UpdateExplainAndValue()
    {
        string id = collectionIdEnum.ToString();
        int s = Mathf.Clamp(_currentStars, 0, 5);

        string value = "";

        switch (id)
        {
            case "A":
            case "B":
                // Keep explainTxt as configured in Inspector; only update value text
                value = s > 0 ? $"+{GetStartGoldForStars(s):N0}" : "";
                break;
            case "C":
            case "D":
                value = s > 0 ? $"+{(int)(GetGpcPercentForStars(s) * 100)}%" : "";
                break;
            case "E":
            case "F":
                value = s > 0 ? $"+{(int)(GetGpsPercentForStars(s) * 100)}%" : "";
                break;
            case "G":
            case "H":
                value = s > 0 ? $"+{GetCpmForStars(s)} /m" : "";
                break;
            case "I":
            case "J":
                value = s > 0 ? $"+{GetClearCrystalForStars(s):N0}" : "";
                break;
            default:
                value = "";
                break;
        }

        // Do not overwrite explainTxt (preserve Inspector value)
        if (valueTxt != null) valueTxt.text = value;
    }

    private int GetStartGoldForStars(int s)
    {
        switch (s)
        {
            case 1: return 1000;
            case 2: return 1200;
            case 3: return 1500;
            case 4: return 1700;
            case 5: return 2000;
            default: return 0;
        }
    }

    private double GetGpcPercentForStars(int s)
    {
        switch (s)
        {
            case 1: return 0.10;
            case 2: return 0.12;
            case 3: return 0.15;
            case 4: return 0.17;
            case 5: return 0.20;
            default: return 0.0;
        }
    }

    private double GetGpsPercentForStars(int s)
    {
        return GetGpcPercentForStars(s);
    }

    private int GetCpmForStars(int s)
    {
        switch (s)
        {
            case 1: return 5;
            case 2: return 6;
            case 3: return 8;
            case 4: return 9;
            case 5: return 10;
            default: return 0;
        }
    }

    private int GetClearCrystalForStars(int s)
    {
        switch (s)
        {
            case 1: return 100;
            case 2: return 120;
            case 3: return 150;
            case 4: return 170;
            case 5: return 200;
            default: return 0;
        }
    }

    // Called externally (e.g. on click events) to add a star.
    // Returns true if a star was added (state changed), false if already maxed.
    public bool AddStar()
    {
        if (_currentStars >= 5) return false;
        _currentStars = Mathf.Clamp(_currentStars + 1, 0, 5);
        ApplyVisualState();

        // Persist
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetSavedCharacterStars(collectionId, _currentStars);
        }

        // Notify GameManager of change
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateCharacterStars(collectionId, _currentStars);
        }

        return true;
    }

    // Optional external setter
    public void SetStars(int stars)
    {
        _currentStars = Mathf.Clamp(stars, 0, 5);
        ApplyVisualState();
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetSavedCharacterStars(collectionId, _currentStars);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateCharacterStars(collectionId, _currentStars);
        }
    }

    public int GetStars() => _currentStars;
}
