using UnityEngine;
using TMPro;

public class CoinGameManager : MonoBehaviour
{
    public static CoinGameManager Instance;

    [Header("UI")]
    public TextMeshProUGUI coinText;
    public string prefix = "Steluțe: ";

    [Header("Score")]
    public int currentCoins;
    public int totalCoinsLifetime;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        totalCoinsLifetime = PlayerPrefs.GetInt("TotalStars", 0);
        UpdateUI();
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
}