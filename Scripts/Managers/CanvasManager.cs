using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    public static CanvasManager instance;

    [Header("UI Canvases")]
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private Canvas gameCanvas;
    [SerializeField] private Canvas gameOverCanvas;
    [SerializeField] private Canvas marketCanvas;
    [SerializeField] private Canvas settingsCanvas;
    [SerializeField] private Canvas pausedCanvas;

    [Header("UI Elements")]
    [SerializeField] private Image dimImage;
    [SerializeField] private ScrollRect marketScrollRect;
    [Header("Game Over Scene")]
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI highscoreText;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Method to show only the specified canvas and hide all others
    public void ShowCanvas(GameState state)
    {
        HideAllCanvases();

        switch (state)
        {
            case GameState.Menu:
                ShowMenuCanvas();
                break;
            case GameState.Playing:
                ShowGameCanvas();
                break;
            case GameState.GameOver:
                ShowGameOverCanvas();
                break;
            case GameState.Market:
                ShowMarketCanvas();
                break;
            case GameState.Settings:
                ShowSettingsCanvas();
                break;
            case GameState.Paused:
                ShowPausedCanvas();
                break;
        }
    }

    private void HideAllCanvases()
    {
        menuCanvas.enabled = false;
        gameCanvas.enabled = false;
        gameOverCanvas.enabled = false;
        marketCanvas.enabled = false;
        settingsCanvas.enabled = false;
        pausedCanvas.enabled = false;

        dimImage.enabled = false;
    }

    public void GetScores()
    {
        highscoreText.text = SaveManager.Instance.GetSaveData().highestScore.ToString();
        scoreText.text = Slingshot.instance.GetScore().ToString();
    }

    private void ShowMenuCanvas()
    {
        dimImage.enabled = true;
        menuCanvas.enabled = true;
    }

    private void ShowGameCanvas()
    {
        gameCanvas.enabled = true;
    }

    private void ShowPausedCanvas()
    {
        dimImage.enabled = true;
        pausedCanvas.enabled = true;
    }

    private void ShowGameOverCanvas()
    {
        gameOverCanvas.enabled = true;
        dimImage.enabled = true;
    }

    private void ShowMarketCanvas()
    {
        marketCanvas.enabled = true;
        dimImage.enabled = true;
        // Reset scroll to top
        if (marketScrollRect != null)
        {
            marketScrollRect.normalizedPosition = new Vector2(0, 1);
        }
    }

    private void ShowSettingsCanvas()
    {
        settingsCanvas.enabled = true;
        dimImage.enabled = true;
    }

    // Individual canvas control methods (if you need more granular control)
    public void EnableDimImage() => dimImage.enabled = true;
    public void DisableDimImage() => dimImage.enabled = false;

    // Getter methods for specific canvases (if needed by other scripts)
    public Canvas GetMenuCanvas() => menuCanvas;
    public Canvas GetGameCanvas() => gameCanvas;
    public Canvas GetGameOverCanvas() => gameOverCanvas;
    public Canvas GetMarketCanvas() => marketCanvas;
    public Canvas GetSettingsCanvas() => settingsCanvas;
    public Canvas GetPauseCanvas() => pausedCanvas;
    public ScrollRect GetMarketScrollRect() => marketScrollRect;
}