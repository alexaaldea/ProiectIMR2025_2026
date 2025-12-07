using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public GameObject gameOverScreen;

    private bool isGameOver = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void GameOver()
{
    if (isGameOver) return;
    isGameOver = true;

    // STOP MOVEMENT IMMEDIATELY
    VRMapController map = FindObjectOfType<VRMapController>();
    if (map != null)
        map.StopMovement();

    // STOP SPAWNING
    SphereSpawner spawner = FindObjectOfType<SphereSpawner>();
    if (spawner != null)
        spawner.enabled = false;

    // SHOW UI
    if (gameOverScreen != null)
        gameOverScreen.SetActive(true);

    Debug.Log("=== GAME OVER ===");
}

}
