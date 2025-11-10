using System;
using UnityEngine;
using UnityEngine.UI;

// Manages a character collection frame UI with stars and a hide placeholder.
// Structure expected in the GameObject:
// - HideCharacter (GameObject)
// - Character (GameObject or Image)
// - Stars (GameObject)
//   - NonStar1..NonStar5
//   - Star1..Star5
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
    }

    private void Start()
    {
        // If SaveManager wasn't ready during Awake, load now in Start (safe order)
        if (!_loadedFromSave && SaveManager.Instance != null)
        {
            _currentStars = SaveManager.Instance.GetSavedCharacterStars(collectionId);
            _loadedFromSave = true;
            ApplyVisualState();
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
    }

    public int GetStars() => _currentStars;
}
