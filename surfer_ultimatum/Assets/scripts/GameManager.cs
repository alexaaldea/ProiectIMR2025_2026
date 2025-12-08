using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public GameObject gameOverScreen;
    public TextMeshProUGUI coinsCollectedText;

    [Header("High Scores UI")]
    public TextMeshProUGUI best1Text;
    public TextMeshProUGUI best2Text;
    public TextMeshProUGUI best3Text;


    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void GameOver()
{
    if (isGameOver) return;
    isGameOver = true;

    int score = CoinGameManager.Instance.currentCoins;

    // 1. Update the top 3 scores
    UpdateBestScores(score);

    // 2. Update UI
    UpdateBestScoreUI();

    // Already existing code
    if (coinsCollectedText != null)
    {
        coinsCollectedText.text = "Steluțe colectate: " + score;
    }

    if (gameOverScreen != null) 
        gameOverScreen.SetActive(true);
}


    private void UpdateBestScores(int newScore)
{
    int best1 = PlayerPrefs.GetInt("BestScore1", 0);
    int best2 = PlayerPrefs.GetInt("BestScore2", 0);
    int best3 = PlayerPrefs.GetInt("BestScore3", 0);

    // If better than Best 1
    if (newScore > best1)
    {
        best3 = best2;
        best2 = best1;
        best1 = newScore;
    }
    // If better than Best 2
    else if (newScore > best2)
    {
        best3 = best2;
        best2 = newScore;
    }
    // If better than Best 3
    else if (newScore > best3)
    {
        best3 = newScore;
    }

    // Save back to PlayerPrefs
    PlayerPrefs.SetInt("BestScore1", best1);
    PlayerPrefs.SetInt("BestScore2", best2);
    PlayerPrefs.SetInt("BestScore3", best3);

    PlayerPrefs.Save();
}

private void UpdateBestScoreUI()
{
    int best1 = PlayerPrefs.GetInt("BestScore1", 0);
    int best2 = PlayerPrefs.GetInt("BestScore2", 0);
    int best3 = PlayerPrefs.GetInt("BestScore3", 0);

    if (best1Text != null) best1Text.text = "Best 1: " + best1;
    if (best2Text != null) best2Text.text = "Best 2: " + best2;
    if (best3Text != null) best3Text.text = "Best 3: " + best3;
}
public void PlayAgain()
{
    Scene scene = SceneManager.GetActiveScene();
    SceneManager.LoadScene(scene.name);
}



}
