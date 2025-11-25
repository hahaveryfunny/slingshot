using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public List<string> ownedCharacterIds = new List<string>();
    public string selectedCharacterId = "";
    public int currency = 1000;
    public int highestScore;

    // Audio Settings - matching your AudioManager
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private SaveData currentSave;
    private string savePath;
    private bool isInitialized = false;

    private void Awake()
    {
        // Ensure this runs first and only once
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize immediately
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        LoadGame();
        isInitialized = true;
        // Apply audio settings after loading
        ApplyAudioSettings();
        Debug.Log("SaveManager initialized successfully");
    }

    public SaveData GetSaveData()
    {
        // Safety check
        if (!isInitialized || currentSave == null)
        {
            Debug.LogWarning("SaveManager not ready, returning default data");
            return new SaveData();
        }
        return currentSave;
    }

    public bool IsReady()
    {
        return isInitialized && currentSave != null;
    }

    public void SaveGame()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("SaveManager not initialized, cannot save");
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(currentSave, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"Game saved to: {savePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }

    public void LoadGame()
    {
        try
        {
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                currentSave = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"Save file loaded successfully from {savePath}");
            }
            else
            {
                // New save with defaults
                currentSave = new SaveData();
                currentSave.currency = 1000;
                currentSave.ownedCharacterIds.Add("default");
                currentSave.selectedCharacterId = "default";
                currentSave.highestScore = 0;
                SaveGame();
                Debug.Log("New save file created");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            currentSave = new SaveData();
        }
    }

    public void SetMasterVolume(float volume)
    {
        if (!isInitialized) return;

        currentSave.masterVolume = Mathf.Clamp01(volume);

        // Apply to AudioManager if it exists
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetMasterVolume(volume);
        }

        SaveGame(); // Auto-save when settings change
    }

    public void SetMusicVolume(float volume)
    {
        if (!isInitialized) return;

        currentSave.musicVolume = Mathf.Clamp01(volume);

        // Apply to AudioManager if it exists
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetMusicVolume(volume);
        }

        SaveGame(); // Auto-save when settings change
    }

    public void SetSFXVolume(float volume)
    {
        if (!isInitialized) return;

        currentSave.sfxVolume = Mathf.Clamp01(volume);

        // Apply to AudioManager if it exists
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetSFXVolume(volume);
        }

        SaveGame(); // Auto-save when settings change
    }

    public float GetMasterVolume() => isInitialized ? currentSave.masterVolume : 1f;
    public float GetMusicVolume() => isInitialized ? currentSave.musicVolume : 1f;
    public float GetSFXVolume() => isInitialized ? currentSave.sfxVolume : 1f;

    private void ApplyAudioSettings()
    {
        // Wait a frame to ensure AudioManager is initialized
        StartCoroutine(ApplyAudioSettingsCoroutine());
    }

    private System.Collections.IEnumerator ApplyAudioSettingsCoroutine()
    {
        // Wait until AudioManager is available
        while (AudioManager.instance == null)
        {
            yield return null;
        }

        // Apply saved audio settings
        AudioManager.instance.SetMasterVolume(currentSave.masterVolume);
        AudioManager.instance.SetMusicVolume(currentSave.musicVolume);
        AudioManager.instance.SetSFXVolume(currentSave.sfxVolume);

        Debug.Log($"Audio settings applied - Master: {currentSave.masterVolume}, Music: {currentSave.musicVolume}, SFX: {currentSave.sfxVolume}");
    }
}