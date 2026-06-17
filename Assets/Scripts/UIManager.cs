using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Slider fillSlider;
    [SerializeField] private Image fillFillImage;
    [SerializeField] private Color safeColor = Color.green;
    [SerializeField] private Color dangerColor = Color.red;

    [Header("Game Over")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    // ── Panel visibility ─────────────────────────────────────────────────

    public void ShowMainMenu()  => SetPanels(main: true);
    public void ShowHUD()       => SetPanels(hud: true);
    public void ShowPauseMenu() => SetPanels(hud: true, pause: true);
    public void ShowGameOver(int score)
    {
        SetPanels(gameOver: true);
        if (finalScoreText) finalScoreText.text = $"Score: {score}";
        if (highScoreText)  highScoreText.text  = $"Best: {ScoreManager.Instance?.HighScore}";
    }

    private void SetPanels(bool main = false, bool hud = false, bool pause = false, bool gameOver = false)
    {
        if (mainMenuPanel)  mainMenuPanel.SetActive(main);
        if (hudPanel)       hudPanel.SetActive(hud);
        if (pausePanel)     pausePanel.SetActive(pause);
        if (gameOverPanel)  gameOverPanel.SetActive(gameOver);
    }

    // ── HUD updates (wire these to ScoreManager/SipMechanic events) ──────

    public void UpdateScore(int score)
    {
        if (scoreText) scoreText.text = score.ToString();
    }

    public void UpdateFill(float normalisedFill)
    {
        if (fillSlider) fillSlider.value = normalisedFill;
        if (fillFillImage)
            fillFillImage.color = Color.Lerp(safeColor, dangerColor, normalisedFill);
    }

    // ── Button callbacks (assign in Inspector) ───────────────────────────

    public void OnPlayPressed()   => GameManager.Instance?.StartGame();
    public void OnPausePressed()  => GameManager.Instance?.PauseGame();
    public void OnResumePressed() => GameManager.Instance?.ResumeGame();
    public void OnRestartPressed() => GameManager.Instance?.StartGame();
    public void OnMenuPressed()   => GameManager.Instance?.GoToMainMenu();
}
