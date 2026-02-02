using UnityEngine;
using TMPro;

public class CoinGameManager : MonoBehaviour
{
    public static CoinGameManager Instance;

    [Header("UI")]
    public TextMeshProUGUI coinText;
    public string prefix = "Steluțe: ";

    [Header("Lives UI")]
    [Tooltip("Trage aici TextMeshProUGUI care va afișa viețile (ex: \"Vieți: 1\")")]
    public TextMeshProUGUI livesText;
    public string livesPrefix = "Vieți: ";

    [Header("Score")]
    public int currentCoins;
    public int totalCoinsLifetime;

    // reference to power-up handler for lives events
    private XRCharacterPowerUpHandler powerUpHandler;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        totalCoinsLifetime = PlayerPrefs.GetInt("TotalStars", 0);
        UpdateUI();
    }

    private void Start()
    {
        // Resolve powerUpHandler and subscribe to lives update event
        powerUpHandler = FindObjectOfType<XRCharacterPowerUpHandler>();
        if (powerUpHandler != null)
        {
            powerUpHandler.OnLivesChanged += OnLivesChanged;
            // set initial lives text immediately
            UpdateLivesUI(powerUpHandler.ExtraLives);
        }
        else
        {
            UpdateLivesUI(0);
            Debug.LogWarning("[CoinGameManager] XRCharacterPowerUpHandler not found in scene.");
        }
    }

    private void OnDestroy()
    {
        if (powerUpHandler != null)
            powerUpHandler.OnLivesChanged -= OnLivesChanged;
    }

    public void AddCoin(int amount)
    {
        currentCoins += amount;
        totalCoinsLifetime += amount;
        PlayerPrefs.SetInt("TotalStars", totalCoinsLifetime);
        UpdateUI();
    }

    public void ResetRunCoins()
    {
        currentCoins = 0;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (coinText != null)
            coinText.text = prefix + currentCoins;
    }

    // Called by XRCharacterPowerUpHandler via event
    private void OnLivesChanged(int newLives)
    {
        UpdateLivesUI(newLives);
    }

    private void UpdateLivesUI(int lives)
    {
        if (livesText != null)
            livesText.text = livesPrefix + lives;
    }
}