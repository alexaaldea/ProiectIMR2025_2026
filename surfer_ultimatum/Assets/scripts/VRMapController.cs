using UnityEngine;
using System.Collections.Generic;

public class VRMapController : MonoBehaviour
{
    [Header("VR Settings")]
    public Transform xrOrigin;
    public Transform vrHead;

    [Header("Chunk Settings")]
    public GameObject[] mapChunkPrefabs;
    public float chunkLength = 12.88f;
    public float movementSpeed = 8f;

    [Header("Timer Spawning Settings")]
    public float spawnInterval = 1.0f; // Spawn a chunk every 1 second
    public int maxActiveChunks = 12; // Maximum number of active chunks

    [Header("Despawning Settings")]
    public float despawnDistance = 30f; // Despawn chunks this far behind player

    [Header("Alignment Settings")]
    public Transform initialMapChunk;
    public bool useEndPointAlignment = true;

    [Header("Pooling Settings")]
    public int initialPoolSize = 8;

    [Header("Debug")]
    public bool debugMode = false;

    private Queue<GameObject> activeChunks = new Queue<GameObject>();
    private List<Queue<GameObject>> chunkPools = new List<Queue<GameObject>>();
    private Vector3 movementDirection = Vector3.back;
    private Vector3 nextSpawnPosition;
    private bool isInitialized = false;
    private float fixedPlayerZ = 0f;
    private float spawnTimer = 0f;

    void Start()
    {
        Debug.Log("=== VRMapController Starting ===");
        FindVRComponents();
        InitializeChunkPools();

        // Set fixed player position
        if (vrHead != null)
        {
            fixedPlayerZ = vrHead.position.z;
            Debug.Log($"Fixed player Z position: {fixedPlayerZ}");
        }

        // Set initial spawn position
        if (initialMapChunk != null)
        {
            activeChunks.Enqueue(initialMapChunk.gameObject);
            Debug.Log($"Added initial chunk to active chunks: {initialMapChunk.name}");

            if (useEndPointAlignment)
            {
                MapChunkAlignment alignment = initialMapChunk.GetComponent<MapChunkAlignment>();
                if (alignment != null)
                {
                    nextSpawnPosition = alignment.GetWorldEndPoint();
                }
                else
                {
                    nextSpawnPosition = initialMapChunk.position + Vector3.forward * chunkLength;
                }
            }
            else
            {
                nextSpawnPosition = initialMapChunk.position + Vector3.forward * chunkLength;
            }
        }
        else
        {
            nextSpawnPosition = Vector3.forward * 20f;
        }

        Debug.Log($"Initial spawn position: {nextSpawnPosition}");

        // Spawn initial chunks immediately
        SpawnInitialChunks();
        isInitialized = true;

        Debug.Log($"Setup complete. Active chunks: {activeChunks.Count}, Next spawn at: {nextSpawnPosition.z}");
    }

    public void ResetMovement()
{
    // If your script uses speed, reset it here safely
    if (this.GetType().GetField("speed") != null)
    {
        this.GetType().GetField("speed").SetValue(this, 2f); // default speed
    }

    // If your script has a bool that controls movement, reset it
    if (this.GetType().GetField("isMoving") != null)
    {
        this.GetType().GetField("isMoving").SetValue(this, true);
    }

    Debug.Log("VRMapController reset.");
}



    void FindVRComponents()
    {
        if (xrOrigin == null)
        {
            xrOrigin = GameObject.Find("XR Origin")?.transform;
            if (xrOrigin == null)
            {
                xrOrigin = GameObject.Find("XRInteractionSetup")?.transform;
            }
        }

        if (vrHead == null)
        {
            vrHead = Camera.main?.transform;
            if (vrHead == null)
            {
                Camera cam = FindObjectOfType<Camera>();
                if (cam != null) vrHead = cam.transform;
            }
        }

        if (vrHead == null)
        {
            vrHead = transform;
        }
    }

    void InitializeChunkPools()
    {
        if (mapChunkPrefabs.Length == 0)
        {
            Debug.LogError("No map chunk prefabs assigned!");
            return;
        }

        foreach (GameObject chunkPrefab in mapChunkPrefabs)
        {
            if (chunkPrefab == null)
            {
                Debug.LogError("One of the map chunk prefabs is null!");
                continue;
            }

            Queue<GameObject> pool = new Queue<GameObject>();

            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject chunk = Instantiate(chunkPrefab);
                chunk.SetActive(false);
                chunk.transform.SetParent(transform);
                pool.Enqueue(chunk);
            }

            chunkPools.Add(pool);
        }

        Debug.Log($"Created {chunkPools.Count} pools with {initialPoolSize} chunks each");
    }

    void SpawnInitialChunks()
    {
        Debug.Log($"Spawning {initialPoolSize} initial chunks...");

        for (int i = 0; i < initialPoolSize; i++)
        {
            SpawnChunk();
        }

        Debug.Log($"Finished spawning initial chunks. Total active: {activeChunks.Count}");
    }

    void Update()
    {
        if (!isInitialized) return;

        MoveChunks();
        CheckTimerSpawning();
        CheckDespawning();

        // Debug controls
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Manual spawn triggered");
            SpawnChunk();
        }

        if (Input.GetKeyDown(KeyCode.Equals))
        {
            movementSpeed += 2f;
            Debug.Log($"Speed increased to: {movementSpeed}");
        }

        if (Input.GetKeyDown(KeyCode.Minus))
        {
            movementSpeed = Mathf.Max(1f, movementSpeed - 2f);
            Debug.Log($"Speed decreased to: {movementSpeed}");
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            spawnInterval = Mathf.Max(0.1f, spawnInterval - 0.1f);
            Debug.Log($"Spawn interval decreased to: {spawnInterval:F1}s");
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            spawnInterval += 0.1f;
            Debug.Log($"Spawn interval increased to: {spawnInterval:F1}s");
        }
    }

    void MoveChunks()
    {
        foreach (GameObject chunk in activeChunks)
        {
            if (chunk != null && chunk.activeInHierarchy)
            {
                chunk.transform.Translate(movementDirection * movementSpeed * Time.deltaTime);
            }
        }
    }

    void CheckTimerSpawning()
    {
        // Timer-based spawning - spawn a chunk every spawnInterval seconds
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f; // Reset timer

            // Only spawn if we haven't reached the maximum chunk limit
            if (activeChunks.Count < maxActiveChunks)
            {
                if (debugMode) Debug.Log($"Timer spawn - Chunk {activeChunks.Count + 1}/{maxActiveChunks} at interval {spawnInterval:F1}s");
                SpawnChunk();
            }
        }
    }

    void CheckDespawning()
    {
        if (activeChunks.Count == 0) return;

        // Check all chunks and mark ones that should be despawned
        List<GameObject> chunksToDespawn = new List<GameObject>();

        foreach (GameObject chunk in activeChunks)
        {
            if (chunk == null || !chunk.activeInHierarchy) continue;

            float chunkEndZ = GetChunkEndZ(chunk);

            // Despawn chunk if it's far behind the player
            if (chunkEndZ < fixedPlayerZ - despawnDistance)
            {
                chunksToDespawn.Add(chunk);
            }
        }

        // Despawn marked chunks
        foreach (GameObject chunk in chunksToDespawn)
        {
            if (debugMode) Debug.Log($"Despawning chunk at Z: {GetChunkEndZ(chunk):F1}");
            DespawnSpecificChunk(chunk);
        }
    }

    void DespawnSpecificChunk(GameObject chunkToDespawn)
    {
        Queue<GameObject> newQueue = new Queue<GameObject>();
        bool found = false;

        while (activeChunks.Count > 0)
        {
            GameObject chunk = activeChunks.Dequeue();
            if (chunk == chunkToDespawn)
            {
                found = true;
                ReturnChunkToPoolByName(chunk);
            }
            else
            {
                newQueue.Enqueue(chunk);
            }
        }

        activeChunks = newQueue;

        if (!found && debugMode)
        {
            Debug.LogWarning("Tried to despawn chunk that wasn't in active chunks");
        }
    }

    float GetChunkEndZ(GameObject chunk)
    {
        MapChunkAlignment alignment = chunk.GetComponent<MapChunkAlignment>();
        if (alignment != null)
        {
            return alignment.GetWorldEndPoint().z;
        }
        else
        {
            return chunk.transform.position.z + chunkLength;
        }
    }

    void SpawnChunk()
    {
        if (mapChunkPrefabs.Length == 0)
        {
            Debug.LogError("Cannot spawn: No prefabs assigned!");
            return;
        }

        int chunkType = Random.Range(0, mapChunkPrefabs.Length);
        GameObject chunk = GetChunkFromPool(chunkType);

        if (chunk == null)
        {
            Debug.LogError("Failed to get chunk from pool!");
            return;
        }

        // Position the chunk
        MapChunkAlignment chunkAlignment = chunk.GetComponent<MapChunkAlignment>();

        if (chunkAlignment != null && useEndPointAlignment)
        {
            Vector3 startPointWorld = chunkAlignment.GetWorldStartPoint();
            Vector3 localOffset = startPointWorld - chunk.transform.position;
            chunk.transform.position = nextSpawnPosition - localOffset;
        }
        else
        {
            chunk.transform.position = nextSpawnPosition;
        }

        chunk.SetActive(true);
        activeChunks.Enqueue(chunk);

        if (debugMode) Debug.Log($"Spawned '{chunk.name}' at Z: {chunk.transform.position.z:F1} (Total active: {activeChunks.Count})");

        // Update next spawn position
        if (chunkAlignment != null && useEndPointAlignment)
        {
            nextSpawnPosition = chunkAlignment.GetWorldEndPoint();
        }
        else
        {
            nextSpawnPosition = chunk.transform.position + Vector3.forward * chunkLength;
        }
    }

    GameObject GetChunkFromPool(int chunkType)
    {
        if (chunkType < 0 || chunkType >= chunkPools.Count) return null;

        Queue<GameObject> pool = chunkPools[chunkType];

        // Look for available chunk in pool
        foreach (GameObject chunk in pool)
        {
            if (chunk != null && !chunk.activeInHierarchy)
            {
                return chunk;
            }
        }

        // If no available chunk found in pool, create a new one
        if (debugMode) Debug.Log("No available chunk in pool, creating new one");
        GameObject newChunk = Instantiate(mapChunkPrefabs[chunkType]);
        newChunk.transform.SetParent(transform);
        return newChunk;
    }

    void ReturnChunkToPoolByName(GameObject chunk)
    {
        // Don't pool the initial chunk from hierarchy - destroy it instead
        if (chunk == initialMapChunk?.gameObject)
        {
            if (debugMode) Debug.Log("Destroying initial hierarchy chunk (not pooling)");
            Destroy(chunk);
            return;
        }

        for (int i = 0; i < mapChunkPrefabs.Length; i++)
        {
            if (chunk.name.StartsWith(mapChunkPrefabs[i].name))
            {
                ReturnChunkToPool(i, chunk);
                break;
            }
        }
    }

    void ReturnChunkToPool(int chunkType, GameObject chunk)
    {
        if (chunkType < 0 || chunkType >= chunkPools.Count) return;

        chunk.SetActive(false);

        if (!chunkPools[chunkType].Contains(chunk))
        {
            chunkPools[chunkType].Enqueue(chunk);
            if (debugMode) Debug.Log($"Returned chunk to pool: {chunk.name}");
        }
    }

    void OnDrawGizmos()
    {
        if (!debugMode || !Application.isPlaying) return;

        // Draw spawn position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(nextSpawnPosition, 1f);

        // Draw despawn range
        Gizmos.color = Color.magenta;
        Vector3 despawnTriggerPos = new Vector3(0, 0, fixedPlayerZ - despawnDistance);
        Gizmos.DrawWireSphere(despawnTriggerPos, 0.5f);

        // Draw player position
        Gizmos.color = Color.green;
        Vector3 playerPos = new Vector3(0, 0, fixedPlayerZ);
        Gizmos.DrawWireSphere(playerPos, 0.3f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(nextSpawnPosition + Vector3.up * 2f, $"Next Spawn\n{nextSpawnPosition.z:F1}");
        
        Vector3 despawnLabelPos = new Vector3(0, 0, fixedPlayerZ - despawnDistance);
        UnityEditor.Handles.Label(despawnLabelPos + Vector3.up * 2f, $"Despawn\n{despawnLabelPos.z:F1}");
        
        Vector3 playerLabelPos = new Vector3(0, 0, fixedPlayerZ);
        UnityEditor.Handles.Label(playerLabelPos + Vector3.up * 2f, $"Player\n{playerLabelPos.z:F1}");
        
        // Show timer info
        UnityEditor.Handles.Label(playerLabelPos + Vector3.up * 4f, $"Timer: {spawnTimer:F1}/{spawnInterval:F1}s\nChunks: {activeChunks.Count}/{maxActiveChunks}");
#endif
    }

    // Public methods for game control
    public void SetMovementSpeed(float newSpeed)
    {
        movementSpeed = newSpeed;
    }

    public void SetSpawnInterval(float newInterval)
    {
        spawnInterval = Mathf.Max(0.1f, newInterval);
    }

    public void StopMovement()
    {
        movementSpeed = 0f;
    }

    public void StartMovement()
    {
        movementSpeed = 8f;
    }
}