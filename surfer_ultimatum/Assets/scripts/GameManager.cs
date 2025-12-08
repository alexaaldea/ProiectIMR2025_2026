using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public GameObject gameOverScreen;
    public TextMeshProUGUI coinsCollectedText;

    [Header("High Score UI")]
    public TextMeshProUGUI bestScoreText;

    [Header("Distance UI")]
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI bestDistanceText;

    private bool isGameOver = false;
    private VRMapController map;
    private SphereSpawner spawner;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Cache references
        map = FindObjectOfType<VRMapController>();
        spawner = FindObjectOfType<SphereSpawner>();

        // Load & show best distance
        float bestDist = PlayerPrefs.GetFloat("BestDistance", 0f);
        if (bestDistanceText != null)
            bestDistanceText.text = "Record: " + bestDist.ToString("F1") + " m";

        // Load best score
        UpdateBestScoreUI();
    }

    private void Update()
    {
        if (!isGameOver && map != null && distanceText != null)
        {
            distanceText.text = "Distanță: " + map.distanceTraveled.ToString("F1") + " m";
        }
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        int score = CoinGameManager.Instance.currentCoins;

        // Stop movement & spawner
        if (map != null) map.StopMovement();
        if (spawner != null) spawner.enabled = false;

        // Update best distance
        if (map != null)
            UpdateBestDistance(map.distanceTraveled);

        // Show final distance
        if (distanceText != null)
            distanceText.text = "Distanță: " + map.distanceTraveled.ToString("F1") + " m";

        // Update best score
        UpdateBestScore(score);
        UpdateBestScoreUI();

        // Show coins collected
        if (coinsCollectedText != null)
            coinsCollectedText.text = "Steluțe colectate: " + score;

        // Show Game Over screen
        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);
    }

    private void UpdateBestDistance(float newDistance)
    {
        float best = PlayerPrefs.GetFloat("BestDistance", 0f);

        if (newDistance > best)
        {
            best = newDistance;
            PlayerPrefs.SetFloat("BestDistance", best);
            PlayerPrefs.Save();
        }

        if (bestDistanceText != null)
            bestDistanceText.text = "Record: " + best.ToString("F1") + " m";
    }

    private void UpdateBestScore(int newScore)
    {
        int best = PlayerPrefs.GetInt("BestScore", 0);

        if (newScore > best)
        {
            best = newScore;
            PlayerPrefs.SetInt("BestScore", best);
            PlayerPrefs.Save();
        }
    }

    private void UpdateBestScoreUI()
    {
        int best = PlayerPrefs.GetInt("BestScore", 0);
        if (bestScoreText != null)
            bestScoreText.text = "Best: " + best;
    }

    public void PlayAgain()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }
}
