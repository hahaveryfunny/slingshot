using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// GameState enum remains the same
public enum GameState
{
    Uninitialized,
    Menu,
    Playing,
    GameOver,
    Market,
    Settings,
    Paused,
}

// Interface remains the same
public interface IGameState
{
    void Enter();
    void Exit();
    void Update();
}

public class GameManager : MonoBehaviour
{
    float deltaTime = 0f;
    public static GameManager instance;

    [Header("Game References")]
    [SerializeField] public SingleBanManager singleBanManager;
    public Transform slingshotLocation;

    [Header("Menu Settings")]
    [SerializeField] public float tilingIncreaseSpeed;
    [SerializeField] public float maxTilingThreshold;

    // Current spawned slingshot
    private GameObject currentSlingshot;
    private Slingshot slingshotScript;

    // State Machine Variables
    private GameState currentState = GameState.Uninitialized;
    private GameState previousState = GameState.Uninitialized;
    private Dictionary<GameState, IGameState> states;

    // Public properties
    public GameState CurrentState => currentState;
    public GameState PreviousState => PreviousState;

    public bool IsGameOver => currentState == GameState.GameOver;

    void Awake()
    {
        Application.targetFrameRate = -1;
        if (instance == null)
        {
            instance = this;
            InitializeStateMachine();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ChangeState(GameState.Menu);
    }

    void Update()
    {
        states[currentState]?.Update();
    }

    // Currency management methods
    public int GetCurrency()
    {
        return SaveManager.Instance.GetSaveData().currency;
    }

    public bool CanAfford(int cost)
    {
        return GetCurrency() >= cost;
    }

    public void SpendCurrency(int amount)
    {
        var saveData = SaveManager.Instance.GetSaveData();
        if (saveData.currency >= amount)
        {
            saveData.currency -= amount;
            SaveManager.Instance.SaveGame();
        }
    }

    public void AddCurrency(int amount)
    {
        var saveData = SaveManager.Instance.GetSaveData();
        saveData.currency += amount;
        SaveManager.Instance.SaveGame();
    }

    public void UpdateHighscore(int score)
    {
        var saveData = SaveManager.Instance.GetSaveData();
        if (score < saveData.highestScore) return;
        saveData.highestScore = score;
        SaveManager.Instance.SaveGame();
    }



    void InitializeStateMachine()
    {
        states = new Dictionary<GameState, IGameState>
        {
            { GameState.Menu, new MenuState(this) },
            { GameState.Playing, new PlayingState(this) },
            { GameState.GameOver, new GameOverState(this) },
            { GameState.Market, new MarketState(this) },
            { GameState.Settings, new SettingsState(this) },
            { GameState.Paused, new PausedState(this) },
        };
    }

    public void ChangeState(GameState newState)
    {
        Debug.Log($"From {currentState} to {newState}");
        if (currentState == newState && currentState != GameState.Uninitialized)
            return;

        previousState = currentState;

        // Exit current state
        if (states.ContainsKey(currentState))
        {
            states[currentState].Exit();
        }

        // Change to new state
        currentState = newState;

        // Enter new state
        if (states.ContainsKey(currentState))
        {
            states[currentState].Enter();
        }
    }




    // Public methods for UI buttons
    public void StartGame() => ChangeState(GameState.Playing);
    public void GameOver() => ChangeState(GameState.GameOver);
    public void GoToMenu() => ChangeState(GameState.Menu);
    public void GoToMarket() => ChangeState(GameState.Market);
    public void RestartGame() => ChangeState(GameState.Playing);
    public void GoToSettings() => ChangeState(GameState.Settings);
    public void PauseGame() => ChangeState(GameState.Paused);
    public void ResumeGame() => ChangeState(GameState.Playing);
    public void ExitSettings()
    {
        if (previousState == GameState.Paused)
        {
            ChangeState(GameState.Paused);
        }
        else if (previousState == GameState.Menu)
        {
            ChangeState(GameState.Menu);
        }
        else
        {
            Debug.LogError("WHERE DO. you want to go? brada");
            return;
        }
    }


    // Helper methods for game systems
    public void SetTimeScale(float scale) => Time.timeScale = scale;

    // Updated slingshot management methods
    public void SpawnSelectedSlingshot()
    {
        // Clear existing slingshot first
        ClearCurrentSlingshot();

        // Get selected character ID from save data
        string selectedId = SaveManager.Instance.GetSaveData().selectedCharacterId;

        if (string.IsNullOrEmpty(selectedId))
        {
            Debug.LogWarning("No selected character ID found in save data");
            return;
        }

        // Get the prefab from CharacterManager
        GameObject slingshotPrefab = CharacterManager.Instance.GetCharacterPrefab(selectedId);

        if (slingshotPrefab == null)
        {
            Debug.LogError($"No slingshot prefab found for ID: {selectedId}");
            return;
        }

        // Spawn the slingshot at the designated spawn point
        currentSlingshot = Instantiate(slingshotPrefab, slingshotLocation.position, slingshotLocation.rotation);
        slingshotScript = CharacterManager.Instance.GetSlingshotController();

        if (slingshotScript == null)
        {
            Destroy(currentSlingshot);
            currentSlingshot = null;
            return;
        }
    }

    public void ReturnAllProjectiles()
    {
        if (ProjectilePool.Instance != null)
        {
            ProjectilePool.Instance.ReturnAllProjectiles();
        }
        else
        {
            Debug.Log("there is no projectile pool instance");
        }
    }

    private static int clearCallCount = 0;

    public void ClearCurrentSlingshot()
    {
        clearCallCount++;

        if (currentSlingshot != null)
        {
            Destroy(currentSlingshot);
            currentSlingshot = null;
            slingshotScript = null;
        }
        else
        {
            Debug.Log("No slingshot to clear");
        }
    }

    public void EnableSlingshot()
    {
        if (slingshotScript != null)
        {
            slingshotScript.enabled = true;
        }
        else
        {
            Debug.LogWarning("No current slingshot to enable!");
        }
    }

    public void DisableSlingshot()
    {
        if (slingshotScript != null)
        {
            slingshotScript.enabled = false;
        }
    }
}

// ============================================================================
// REFACTORED STATE IMPLEMENTATIONS
// ============================================================================

public class MenuState : IGameState
{
    private GameManager gameManager;

    public MenuState(GameManager gm)
    {
        gameManager = gm;
    }

    public void Enter()
    {

        // Handle UI through CanvasManager
        CanvasManager.instance.ShowCanvas(GameState.Menu);

        // Reset game systems
        gameManager.ClearCurrentSlingshot();
        gameManager.SetTimeScale(1f);
        gameManager.singleBanManager.gameObject.SetActive(false);

        // Clear any remaining game objects
        if (SingleBanManager.instance != null)
        {
            SingleBanManager.instance.ClearGame();
        }
        if (ProjectilePool.Instance != null)
        {
            ProjectilePool.Instance.ReturnAllProjectiles();
        }
        gameManager.SpawnSelectedSlingshot();
    }

    public void Exit()
    {
        // CanvasManager handles UI cleanup automatically when switching states
    }

    public void Update()
    {
        // Menu update logic if needed
    }
}

public class PlayingState : IGameState
{
    private GameManager gameManager;

    public PlayingState(GameManager gm)
    {
        gameManager = gm;
    }

    public void Enter()
    {
        if (SingleBanManager.instance != null)
        {
            SingleBanManager.instance.ClearGame();
        }
        Slingshot.instance.ResetScore();

        // Handle UI through CanvasManager
        CanvasManager.instance.ShowCanvas(GameState.Playing);

        // Start game systems
        gameManager.SetTimeScale(1f);
        gameManager.singleBanManager.gameObject.SetActive(true);

        // Spawn and enable the selected slingshot
        //gameManager.SpawnSelectedSlingshot();
        gameManager.EnableSlingshot();

        // Start ban manager
        if (SingleBanManager.instance != null)
        {
            SingleBanManager.instance.StartSetUp();
        }
    }

    public void Exit()
    {
        gameManager.DisableSlingshot();

        // Clear ALL projectiles (both in slingshot and launched)
        gameManager.ReturnAllProjectiles();
    }

    public void Update()
    {
        // Game logic updates
    }
}

public class GameOverState : IGameState
{
    private GameManager gameManager;

    public GameOverState(GameManager gm)
    {
        gameManager = gm;
    }

    public void Enter()
    {
        // Stop game
        gameManager.DisableSlingshot();
        gameManager.SetTimeScale(0f);

        // Handle UI through CanvasManager
        CanvasManager.instance.GetScores();
        CanvasManager.instance.ShowCanvas(GameState.GameOver);
    }

    public void Exit()
    {
        AudioManager.instance.PlaySFX(AudioManager.instance.uiForward);
    }

    public void Update()
    {
        // Game over screen logic
    }
}

public class SettingsState : IGameState
{
    private GameManager gameManager;

    public SettingsState(GameManager gm)
    {
        gameManager = gm;
    }

    public void Enter()
    {
        AudioManager.instance.PlaySFX(AudioManager.instance.uiForward);

        // Handle UI through CanvasManager
        CanvasManager.instance.ShowCanvas(GameState.Settings);
    }

    public void Exit()
    {
        AudioManager.instance.PlaySFX(AudioManager.instance.uiBack);
    }

    public void Update()
    {
        // Settings logic updates
    }
}

public class MarketState : IGameState
{
    private GameManager gameManager;

    public MarketState(GameManager gm)
    {
        gameManager = gm;
    }

    public void Enter()
    {
        AudioManager.instance.PlaySFX(AudioManager.instance.uiForward);

        // Handle UI through CanvasManager (includes scroll reset)
        CanvasManager.instance.ShowCanvas(GameState.Market);
    }

    public void Exit()
    {
        AudioManager.instance.PlaySFX(AudioManager.instance.uiForward);
    }

    public void Update()
    {
        // Market logic updates
    }
}


// ============================================================================
// NEW PAUSE STATE IMPLEMENTATIONS
// ============================================================================

public class PausedState : IGameState
{
    private GameManager gameManager;

    public PausedState(GameManager gm)
    {
        gameManager = gm;
    }

    public void Enter()
    {
        // Pause the game
        gameManager.SetTimeScale(0f);

        // Disable slingshot input
        gameManager.DisableSlingshot();

        // Show pause UI through CanvasManager
        CanvasManager.instance.ShowCanvas(GameState.Paused);

        // Play pause sound effect if available
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.uiBack);
        }
    }

    public void Exit()
    {
        // Play unpause sound effect if available
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.uiForward);
        }
    }

    public void Update()
    {
        // Pause screen logic - can handle additional input here if needed
    }
}
