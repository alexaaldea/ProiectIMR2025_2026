using UnityEngine;

public class MapChunkExampleHook : MonoBehaviour
{
    public CoinSpawner coinSpawner;

    private void Start()
    {
        // Start se execută după Awake-urile tuturor obiectelor active
        if (coinSpawner != null)
        {
            coinSpawner.Spawn();
        }
        else
        {
            Debug.LogWarning("MapChunkExampleHook: coinSpawner nu este asignat.");
        }
    }
}