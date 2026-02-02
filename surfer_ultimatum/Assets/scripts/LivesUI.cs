using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LivesUI : MonoBehaviour
{
    private TextMeshProUGUI tmp;
    private XRCharacterPowerUpHandler powerUpHandler;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        powerUpHandler = FindObjectOfType<XRCharacterPowerUpHandler>();
        if (powerUpHandler != null)
        {
            powerUpHandler.OnLivesChanged += UpdateLives;
            UpdateLives(powerUpHandler.ExtraLives);
        }
        else
        {
            UpdateLives(0);
            Debug.LogWarning("[LivesUI] XRCharacterPowerUpHandler not found in scene.");
        }
    }

    private void UpdateLives(int lives)
    {
        if (tmp != null)
            tmp.text = "Vieți: " + lives;
    }

    void OnDestroy()
    {
        if (powerUpHandler != null)
            powerUpHandler.OnLivesChanged -= UpdateLives;
    }
}