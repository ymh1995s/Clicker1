using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// SaveManager: periodically saves/loading game state to a JSON file.
public class SaveManager : Singleton<SaveManager>
{
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
    private class SaveData
    {
        public int gold;
        public int goldPerClick;
        public int goldPerSecond;
        public List<UpgradeEntry> gpcUpgrades = new List<UpgradeEntry>();
        public List<UpgradeEntry> gpsUpgrades = new List<UpgradeEntry>();
    }

    // Awake is not overridden because base Singleton may not expose a virtual Awake
    protected void Awake()
    {
        Load();
    }

    protected virtual void Start()
    {
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

                // GPC upgrades
                try
                {
                    foreach (var kv in GameManager.Instance.GPCUpgrades)
                    {
                        data.gpcUpgrades.Add(new UpgradeEntry { key = kv.Key.ToString(), level = kv.Value });
                    }
                }
                catch { }

                // GPS upgrades
                try
                {
                    foreach (var kv in GameManager.Instance.GPSUpgrades)
                    {
                        data.gpsUpgrades.Add(new UpgradeEntry { key = kv.Key.ToString(), level = kv.Value });
                    }
                }
                catch { }
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SaveFilePath, json, Encoding.UTF8);
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
                return;
            }

            string json = File.ReadAllText(SaveFilePath, Encoding.UTF8);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("SaveManager: Failed to parse save file.");
#endif
                return;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.Gold = data.gold;
                GameManager.Instance.GoldPerClick = data.goldPerClick;
                GameManager.Instance.GoldPerSecond = data.goldPerSecond;

                // Apply GPC upgrades
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

                // Apply GPS upgrades
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

                // Trigger property setters to notify listeners
                GameManager.Instance.Gold = GameManager.Instance.Gold;
                GameManager.Instance.GoldPerClick = GameManager.Instance.GoldPerClick;
                GameManager.Instance.GoldPerSecond = GameManager.Instance.GoldPerSecond;
            }

#if UNITY_EDITOR
            Debug.Log($"SaveManager: Loaded save from {SaveFilePath}");
#endif
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

            // Optionally reinitialize in-memory data if GameManager exposes a method
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
}
