using UnityEngine;
using UnityEngine.Events;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int CurrentScore { get; private set; }
    public int HighScore { get; private set; }

    public UnityEvent<int> onScoreChanged;
    public UnityEvent<int> onHighScoreBeaten;

    private const string HighScoreKey = "OneMoreSip_HighScore";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    public void AddScore(int amount)
    {
        CurrentScore += amount;
        onScoreChanged?.Invoke(CurrentScore);

        if (CurrentScore > HighScore)
        {
            HighScore = CurrentScore;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            onHighScoreBeaten?.Invoke(HighScore);
        }
    }

    public void ResetScore()
    {
        CurrentScore = 0;
        onScoreChanged?.Invoke(CurrentScore);
    }
}
