using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// UI_CharacterGacha: consume gold to grant a star to a CharacterColloection frame.
// - gachaCost: serialized, default 100 (can be changed later)
// - If no eligible target found, gold is refunded.
public class UI_CharacterGacha : MonoBehaviour
{
    [Tooltip("Cost in gold to perform one gacha. Changeable in Inspector at runtime.")]
    [SerializeField] private int gachaCost = 100;

    [SerializeField] private Button gachaButton;

    // Optional: parent transform that contains the character frames (assign in Inspector to avoid expensive scans)
    [SerializeField] private Transform framesParent;

    private void Awake()
    {
        // Prefer a Button on the same GameObject (component might be attached directly to a Button)
        if (gachaButton == null)
        {
            gachaButton = GetComponent<Button>();
        }

        // Fallback: look for a child named "GachaButton"
        if (gachaButton == null)
        {
            var btnGo = transform.Find("GachaButton");
            if (btnGo != null) gachaButton = btnGo.GetComponent<Button>();
        }

        // If framesParent not set, try some common parent names in the scene
        if (framesParent == null)
        {
            string[] candidates = new[] { "CharactersTab", "Characters", "CharacterCollectionRoot", "CharacterCollection", "CharacterColloectionFrame", "CharacterCollectionFrame", "CharacterColloectionRoot" };
            foreach (var name in candidates)
            {
                var go = GameObject.Find(name);
                if (go != null)
                {
                    framesParent = go.transform;
                    break;
                }
            }
        }
    }

    private void OnEnable()
    {
        BindButton();
    }

    private void OnDisable()
    {
        UnbindButton();
    }

    private void BindButton()
    {
        if (gachaButton != null)
        {
            gachaButton.onClick.RemoveListener(OnGachaButtonClicked);
            gachaButton.onClick.AddListener(OnGachaButtonClicked);
        }
    }

    private void UnbindButton()
    {
        if (gachaButton != null)
        {
            gachaButton.onClick.RemoveListener(OnGachaButtonClicked);
        }
    }

    // wrapper with correct signature for Button.onClick
    private void OnGachaButtonClicked()
    {
        bool ok = TryGacha();
        Debug.Log(ok ? "UI_CharacterGacha: Gacha purchase SUCCESS" : "UI_CharacterGacha: Gacha purchase FAILED");
    }

    // Public API to attempt gacha. Returns true if a star was granted.
    public bool TryGacha()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("UI_CharacterGacha: GameManager not found.");
            Debug.Log("UI_CharacterGacha: Gacha failed - no GameManager instance.");
            return false;
        }

        // Determine current cost (allows gachaCost to be modified at runtime)
        int cost = Mathf.Max(0, gachaCost);

        if (GameManager.Instance.Gold < cost)
        {
            Debug.Log($"UI_CharacterGacha: Not enough gold. Need {cost}, have {GameManager.Instance.Gold}.");
            return false;
        }

        // Deduct gold now; if we fail to grant anything we'll refund
        GameManager.Instance.Gold -= cost;

        // Find target frames
        var all = GetAllFrames();
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning("UI_CharacterGacha: No CharacterColloection instances found in scene. Refunding gold.");
            GameManager.Instance.Gold += cost;
            Debug.Log("UI_CharacterGacha: Gacha failed - no frames found.");
            return false;
        }

        // Prefer those with stars < 5
        var eligible = all.Where(c => c != null && c.GetStars() < 5).ToArray();
        if (eligible.Length == 0)
        {
            Debug.Log("UI_CharacterGacha: All characters already max stars. Refunding gold.");
            GameManager.Instance.Gold += cost;
            Debug.Log("UI_CharacterGacha: Gacha failed - all maxed.");
            return false;
        }

        // Pick random eligible and add a star
        var chosen = eligible[Random.Range(0, eligible.Length)];
        string chosenId = chosen != null ? chosen.collectionId : "<unknown>";
        bool added = chosen.AddStar();
        if (!added)
        {
            // Shouldn't happen because we filtered <5, but just in case refund
            Debug.LogWarning($"UI_CharacterGacha: Failed to add star to chosen CharacterColloection '{chosenId}'. Refunding gold.");
            GameManager.Instance.Gold += cost;
            Debug.Log("UI_CharacterGacha: Gacha failed - add star failed.");
            return false;
        }

        // Success - log which character gained a star and its new star count
        int newStars = chosen.GetStars();
        Debug.Log($"UI_CharacterGacha: Gacha SUCCESS. Granted 1 star to '{chosenId}' (stars={newStars}).");
        return true;
    }

    // Try to collect frames from configured parent, common parents, or global find
    private CharacterColloection[] GetAllFrames()
    {
        if (framesParent != null)
        {
            var arr = framesParent.GetComponentsInChildren<CharacterColloection>(true);
            if (arr != null && arr.Length > 0) return arr;
        }

        // If framesParent not set or returned nothing, try to find by common singular names
        string[] candidates = new[] { "CharacterColloectionFrame", "CharacterCollectionFrame", "CharacterColloection", "CharacterCollection", "CharacterFrame", "Characters" };
        foreach (var name in candidates)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                var arr = go.GetComponentsInChildren<CharacterColloection>(true);
                if (arr != null && arr.Length > 0) return arr;
            }
        }

        // Fallback to global FindObjectsOfType
        return Object.FindObjectsOfType<CharacterColloection>(true);
    }

    // Allow updating the cost at runtime
    public void SetCost(int newCost)
    {
        gachaCost = Mathf.Max(0, newCost);
    }

    public int GetCost() => gachaCost;
}
