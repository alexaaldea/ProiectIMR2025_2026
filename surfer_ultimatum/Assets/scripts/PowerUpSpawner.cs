using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    public GameObject[] powerUpPrefabs;
    public float spawnInterval = 10f;
    public Vector3 spawnAreaSize = new Vector3(10, 2, 10);

    public Transform player; 
    public float spawnDistanceAhead = 10f;


    private float nextSpawnTime;

    void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnPowerUp();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    void SpawnPowerUp()
{
    GameObject prefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];
    float trackLeft = -0.5f;
    float trackRight = 0.5f;

    float spawnX = Random.Range(trackLeft, trackRight);
    float spawnY = 0.1f;
    float spawnZ = player.position.z + 3f + Random.Range(0f, 2f);
    Vector3 spawnPosition = new Vector3(spawnX, spawnY, spawnZ);

    Instantiate(prefab, spawnPosition, Quaternion.identity);
}


}
