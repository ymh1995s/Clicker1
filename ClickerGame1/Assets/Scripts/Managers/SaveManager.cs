using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// SaveManager: periodically saves/loading game state to a JSON file.
public class SaveManager : Singleton<SaveManager>
{
    public event Action OnLoaded;

    [SerializeField] private float _saveInterval = 10f;
    public float SaveInterval { get => _saveInterval; set => _saveInterval = value; }

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, "save_game.json");

    [Serializable]
    private class UpgradeEntry
    {
        public string key;
        public int level;
    }

    [Serializable]
    private class PurchasedEntry
    {
        public string key;
        public bool purchased;
    }

    [Serializable]
    private class CharacterEntry
    {
        public string id;
        public int stars;
    }

    [Serializable]
    private class CooldownEntry
    {
        public string key;
        public float remaining;
    }

    [Serializable]
    private class SaveData
    {
        public int gold;
        public int goldPerClick;
        public int goldPerSecond;
        public int crystal; // persist crystals across sessions and rebirths
        public List<UpgradeEntry> gpcUpgrades = new List<UpgradeEntry>();
        public List<UpgradeEntry> gpsUpgrades = new List<UpgradeEntry>();
        public List<PurchasedEntry> purchasedGpcItems = new List<PurchasedEntry>();
        public List<CharacterEntry> characterCollections = new List<CharacterEntry>();
        public List<CooldownEntry> cooldowns = new List<CooldownEntry>();
    }

    // Keep loaded save data in-memory so other systems can query/update it.
    private SaveData _currentData = null;

    protected void Awake()
    {
        Load();
    }

    protected virtual void Start()
    {
        // Ensure default autosave interval
        _saveInterval = 10f;
        StartCoroutine(AutoSaveRoutine());
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_saveInterval);
            Save();
        }
    }

    public void Save()
    {
        try
        {
            var data = new SaveData();

            if (GameManager.Instance != null)
            {
                data.gold = GameManager.Instance.Gold;
                data.goldPerClick = GameManager.Instance.GoldPerClick;
                data.goldPerSecond = GameManager.Instance.GoldPerSecond;
                data.crystal = GameManager.Instance.Crystal; // save crystal

                try
                {
                    foreach (var kv in GameManager.Instance.GPCUpgrades)
                    {
                        data.gpcUpgrades.Add(new UpgradeEntry { key = kv.Key.ToString(), level = kv.Value });
                    }
                }
                catch { }

                try
                {
                    foreach (var kv in GameManager.Instance.GPSUpgrades)
                    {
                        data.gpsUpgrades.Add(new UpgradeEntry { key = kv.Key.ToString(), level = kv.Value });
                    }
                }
                catch { }

                try
                {
                    foreach (var kv in GameManager.Instance.PurchasedGPCItems)
                    {
                        data.purchasedGpcItems.Add(new PurchasedEntry { key = kv.Key.ToString(), purchased = kv.Value });
                    }
                }
                catch { }

                // Save remaining cooldowns for each GPC tier so they can be restored on load
                try
                {
                    foreach (EGPCUpgradeType t in Enum.GetValues(typeof(EGPCUpgradeType)))
                    {
                        float rem = 0f;
                        try { rem = GameManager.Instance.GetRemainingCooldown(t); } catch { rem = 0f; }
                        data.cooldowns.Add(new CooldownEntry { key = t.ToString(), remaining = rem });
                    }
                }
                catch { }
            }

            // Gather character collection states from all CharacterColloection components in the scene (include inactive)
            try
            {
                var frames = UnityEngine.Object.FindObjectsOfType<CharacterColloection>(true);
                foreach (var frame in frames)
                {
                    if (frame == null) continue;
                    var id = frame.collectionId;
                    if (string.IsNullOrEmpty(id)) continue;
                    int stars = frame.GetStars();
                    data.characterCollections.Add(new CharacterEntry { id = id, stars = stars });
                }
            }
            catch { }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SaveFilePath, json, Encoding.UTF8);

            _currentData = data;

#if UNITY_EDITOR
            Debug.Log($"SaveManager: Saved to {SaveFilePath}");
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveManager: Failed to save - {ex}");
        }
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(SaveFilePath))
            {
#if UNITY_EDITOR
                Debug.Log("SaveManager: No save file found.");
#endif
                _currentData = new SaveData();
                return;
            }

            string json = File.ReadAllText(SaveFilePath, Encoding.UTF8);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("SaveManager: Failed to parse save file.");
#endif
                _currentData = new SaveData();
                return;
            }

            _currentData = data;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.Gold = data.gold;
                GameManager.Instance.GoldPerClick = data.goldPerClick;
                GameManager.Instance.GoldPerSecond = data.goldPerSecond;

                // Load crystal and ensure it persists across rebirths
                try
                {
                    GameManager.Instance.Crystal = data.crystal;
                }
                catch { }

                try
                {
                    GameManager.Instance.GPCUpgrades.Clear();
                    foreach (var e in data.gpcUpgrades)
                    {
                        if (Enum.TryParse(typeof(EGPCUpgradeType), e.key, out var parsed))
                        {
                            GameManager.Instance.GPCUpgrades[(EGPCUpgradeType)parsed] = e.level;
                        }
                    }
                }
                catch { }

                try
                {
                    GameManager.Instance.GPSUpgrades.Clear();
                    foreach (var e in data.gpsUpgrades)
                    {
                        if (Enum.TryParse(typeof(EGPSUpgradeType), e.key, out var parsed))
                        {
                            GameManager.Instance.GPSUpgrades[(EGPSUpgradeType)parsed] = e.level;
                        }
                    }
                }
                catch { }

                try
                {
                    GameManager.Instance.PurchasedGPCItems.Clear();
                    foreach (EGPCUpgradeType t in Enum.GetValues(typeof(EGPCUpgradeType)))
                    {
                        GameManager.Instance.PurchasedGPCItems[t] = false;
                    }

                    foreach (var p in data.purchasedGpcItems)
                    {
                        if (Enum.TryParse(typeof(EGPCUpgradeType), p.key, out var parsed))
                        {
                            GameManager.Instance.PurchasedGPCItems[(EGPCUpgradeType)parsed] = p.purchased;
                        }
                    }
                }
                catch { }

                // Restore cooldown remaining times if saved
                try
                {
                    if (data.cooldowns != null)
                    {
                        foreach (var c in data.cooldowns)
                        {
                            if (Enum.TryParse(typeof(EGPCUpgradeType), c.key, out var parsed))
                            {
                                try
                                {
                                    GameManager.Instance.SetRemainingCooldown((EGPCUpgradeType)parsed, c.remaining);
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }

                // Trigger property setters to notify listeners
                GameManager.Instance.Gold = GameManager.Instance.Gold;
                GameManager.Instance.GoldPerClick = GameManager.Instance.GoldPerClick;
                GameManager.Instance.GoldPerSecond = GameManager.Instance.GoldPerSecond;
            }

            // After loading save data, apply saved stars to any present CharacterColloection components
            try
            {
                if (data.characterCollections != null && data.characterCollections.Count > 0)
                {
                    var frames = UnityEngine.Object.FindObjectsOfType<CharacterColloection>(true);
                    foreach (var entry in data.characterCollections)
                    {
                        if (string.IsNullOrEmpty(entry.id)) continue;
                        foreach (var frame in frames)
                        {
                            if (frame == null) continue;
                            if (string.Equals(frame.collectionId, entry.id, StringComparison.OrdinalIgnoreCase))
                            {
                                // Use public API to set stars which will update visuals and persist if needed
                                frame.SetStars(entry.stars);
                                break;
                            }
                        }
                    }
                }
            }
            catch { }

#if UNITY_EDITOR
            Debug.Log($"SaveManager: Loaded save from {SaveFilePath}");
#endif

            OnLoaded?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveManager: Failed to load - {ex}");
        }
    }

    public void DeleteSaveFile()
    {
        try
        {
            // Preserve crystals across delete (rebirth) operations
            int preservedCrystal = 0;
            try
            {
                if (_currentData != null)
                    preservedCrystal = _currentData.crystal;
                else if (GameManager.Instance != null)
                    preservedCrystal = GameManager.Instance.Crystal;
            }
            catch { preservedCrystal = 0; }

            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
#if UNITY_EDITOR
                Debug.Log($"SaveManager: Deleted save file {SaveFilePath}");
#endif
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("SaveManager: No save file to delete.");
#endif
            }

            try
            {
                if (GameManager.Instance != null)
                {
                    var gmType = GameManager.Instance.GetType();
                    var initMethod = gmType.GetMethod("InitializeGameData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (initMethod != null)
                    {
                        initMethod.Invoke(GameManager.Instance, null);
                    }

                    // Restore preserved crystal value and persist immediately
                    try
                    {
                        GameManager.Instance.Crystal = preservedCrystal;
                        Save();
                    }
                    catch { }
                }
            }
            catch { }
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveManager: Failed to delete save - {ex}");
        }
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) Save();
    }

    // Public API for other scripts to query or update character collection stars
    public int GetSavedCharacterStars(string id)
    {
        if (_currentData == null) _currentData = new SaveData();
        if (string.IsNullOrEmpty(id)) return 0;
        try
        {
            var e = _currentData.characterCollections.Find(x => string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
            return e != null ? Mathf.Clamp(e.stars, 0, 5) : 0;
        }
        catch { return 0; }
    }

    public void SetSavedCharacterStars(string id, int stars)
    {
        if (_currentData == null) _currentData = new SaveData();
        if (string.IsNullOrEmpty(id)) return;
        try
        {
            var e = _currentData.characterCollections.Find(x => string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
            if (e == null)
            {
                e = new CharacterEntry { id = id, stars = Mathf.Clamp(stars, 0, 5) };
                _currentData.characterCollections.Add(e);
            }
            else
            {
                e.stars = Mathf.Clamp(stars, 0, 5);
            }
            // Save immediately to persist the change
            Save();
        }
        catch { }
    }
}
