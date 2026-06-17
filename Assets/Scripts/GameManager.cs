using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { MainMenu, Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; }

    [Header("References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private ScoreManager scoreManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetState(GameState.MainMenu);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.MainMenu:
                OnMainMenu();
                break;
            case GameState.Playing:
                OnPlay();
                break;
            case GameState.Paused:
                OnPause();
                break;
            case GameState.GameOver:
                OnGameOver();
                break;
        }
    }

    private void OnMainMenu()
    {
        Time.timeScale = 1f;
        uiManager?.ShowMainMenu();
    }

    private void OnPlay()
    {
        Time.timeScale = 1f;
        scoreManager?.ResetScore();
        uiManager?.ShowHUD();
    }

    private void OnPause()
    {
        Time.timeScale = 0f;
        uiManager?.ShowPauseMenu();
    }

    private void OnGameOver()
    {
        Time.timeScale = 0f;
        uiManager?.ShowGameOver(scoreManager?.CurrentScore ?? 0);
    }

    public void StartGame()   => SetState(GameState.Playing);
    public void PauseGame()   => SetState(GameState.Paused);
    public void ResumeGame()  => SetState(GameState.Playing);
    public void EndGame()     => SetState(GameState.GameOver);
    public void GoToMainMenu() => SetState(GameState.MainMenu);
}
