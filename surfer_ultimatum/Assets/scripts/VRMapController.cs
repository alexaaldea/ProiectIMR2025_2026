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
    public float spawnDistance = 25f;
    public float despawnDistance = 25f;
    public float movementSpeed = 5f;

    [Header("Alignment Settings")]
    public Transform initialMapChunk;
    public bool useEndPointAlignment = true;

    [Header("Pooling Settings")]
    public int initialPoolSize = 3;

    [Header("Debug")]
    public bool debugMode = true;

    private Queue<GameObject> activeChunks = new Queue<GameObject>();
    private List<Queue<GameObject>> chunkPools = new List<Queue<GameObject>>();
    private Vector3 movementDirection = Vector3.back;
    private Vector3 nextSpawnPosition;

    void Start()
    {
        Debug.Log("=== VRMapController Starting ===");
        FindVRComponents();
        InitializeChunkPools();

        // Set initial spawn position
        if (initialMapChunk != null)
        {
            if (useEndPointAlignment)
            {
                MapChunkAlignment alignment = initialMapChunk.GetComponent<MapChunkAlignment>();
                if (alignment != null)
                {
                    nextSpawnPosition = alignment.GetWorldEndPoint();
                    Debug.Log($"Using alignment end point: {nextSpawnPosition}");
                }
                else
                {
                    nextSpawnPosition = initialMapChunk.position + Vector3.forward * chunkLength;
                    Debug.Log($"No alignment script, using chunk length: {nextSpawnPosition}");
                }
            }
            else
            {
                nextSpawnPosition = initialMapChunk.position + Vector3.forward * chunkLength;
                Debug.Log($"Using simple positioning: {nextSpawnPosition}");
            }
        }
        else
        {
            nextSpawnPosition = Vector3.zero;
            Debug.LogWarning("No initial map chunk assigned! Using default position.");
        }

        SpawnInitialChunks();
        Debug.Log($"Initial setup complete. Active chunks: {activeChunks.Count}");
    }

    void FindVRComponents()
    {
        Debug.Log("Finding VR components...");

        if (xrOrigin == null)
        {
            xrOrigin = GameObject.Find("XR Origin")?.transform;
            if (xrOrigin == null)
            {
                xrOrigin = GameObject.Find("XRInteractionSetup")?.transform;
                Debug.Log(xrOrigin != null ? "Found XRInteractionSetup" : "No XR Origin found");
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
            Debug.Log(vrHead != null ? $"Found VR Head: {vrHead.name}" : "No VR Head found");
        }

        if (vrHead == null)
        {
            vrHead = transform;
            Debug.LogWarning("VR Head not found, using controller transform");
        }
    }

    void InitializeChunkPools()
    {
        Debug.Log($"Initializing pools with {mapChunkPrefabs.Length} prefab types");

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

                if (debugMode) Debug.Log($"Created pool object: {chunk.name}");
            }

            chunkPools.Add(pool);
        }

        Debug.Log($"Created {chunkPools.Count} pools");
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
        if (vrHead == null)
        {
            Debug.LogWarning("VR Head is null in Update!");
            return;
        }

        MoveChunks();
        CheckSpawning();
        CheckDespawning();

        // Debug controls
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Manual spawn triggered");
            SpawnChunk();
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

    void CheckSpawning()
    {
        float headZ = vrHead.position.z;
        float triggerZ = headZ + spawnDistance;

        if (debugMode)
        {
            Debug.Log($"Head Z: {headZ}, Trigger Z: {triggerZ}, Next Spawn Z: {nextSpawnPosition.z}");
        }

        if (triggerZ > nextSpawnPosition.z)
        {
            Debug.Log($"Spawning triggered! Head at {headZ}, need chunk at {nextSpawnPosition.z}");
            SpawnChunk();
        }
    }

    void CheckDespawning()
    {
        if (activeChunks.Count == 0) return;

        float headZ = vrHead.position.z;

        while (activeChunks.Count > 0)
        {
            GameObject firstChunk = activeChunks.Peek();
            if (firstChunk == null || !firstChunk.activeInHierarchy)
            {
                activeChunks.Dequeue();
                continue;
            }

            float chunkZ = firstChunk.transform.position.z;

            if (chunkZ < headZ - despawnDistance)
            {
                Debug.Log($"Despawning chunk at Z: {chunkZ}");
                DespawnOldestChunk();
            }
            else
            {
                break;
            }
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
        chunk.transform.position = nextSpawnPosition;
        chunk.SetActive(true);
        activeChunks.Enqueue(chunk);

        Debug.Log($"Spawned chunk '{chunk.name}' at {chunk.transform.position}");

        // Update next spawn position
        MapChunkAlignment alignment = chunk.GetComponent<MapChunkAlignment>();
        if (alignment != null)
        {
            nextSpawnPosition = alignment.GetWorldEndPoint();
            Debug.Log($"Next spawn position (from alignment): {nextSpawnPosition}");
        }
        else
        {
            nextSpawnPosition += Vector3.forward * chunkLength;
            Debug.Log($"Next spawn position (from length): {nextSpawnPosition}");
        }
    }

    GameObject GetChunkFromPool(int chunkType)
    {
        if (chunkType < 0 || chunkType >= chunkPools.Count)
        {
            Debug.LogError($"Invalid chunk type: {chunkType}");
            return null;
        }

        Queue<GameObject> pool = chunkPools[chunkType];

        if (pool.Count > 0)
        {
            GameObject chunk = pool.Dequeue();
            Debug.Log($"Retrieved chunk from pool: {chunk.name}");
            return chunk;
        }
        else
        {
            Debug.Log("Pool empty, creating new chunk");
            GameObject newChunk = Instantiate(mapChunkPrefabs[chunkType]);
            newChunk.transform.SetParent(transform);
            return newChunk;
        }
    }

    void DespawnOldestChunk()
    {
        if (activeChunks.Count > 0)
        {
            GameObject chunkToDespawn = activeChunks.Dequeue();

            if (chunkToDespawn != null)
            {
                for (int i = 0; i < mapChunkPrefabs.Length; i++)
                {
                    if (chunkToDespawn.name.StartsWith(mapChunkPrefabs[i].name))
                    {
                        ReturnChunkToPool(i, chunkToDespawn);
                        break;
                    }
                }
            }
        }
    }

    void ReturnChunkToPool(int chunkType, GameObject chunk)
    {
        if (chunkType < 0 || chunkType >= chunkPools.Count) return;

        chunk.SetActive(false);
        chunkPools[chunkType].Enqueue(chunk);
        Debug.Log($"Returned chunk to pool: {chunk.name}");
    }

    void OnDrawGizmos()
    {
        if (!debugMode) return;

        // Draw spawn position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(nextSpawnPosition, 1f);

        // Draw spawn range
        if (vrHead != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 spawnTriggerPos = vrHead.position + Vector3.forward * spawnDistance;
            Gizmos.DrawWireSphere(spawnTriggerPos, 0.5f);
            Gizmos.DrawLine(vrHead.position, spawnTriggerPos);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(nextSpawnPosition + Vector3.up * 2f, $"Next Spawn\n{nextSpawnPosition.z}");
        if (vrHead != null)
        {
            Vector3 spawnTriggerPos = vrHead.position + Vector3.forward * spawnDistance;
            UnityEditor.Handles.Label(spawnTriggerPos + Vector3.up * 2f, $"Spawn Trigger\n{spawnTriggerPos.z}");
        }
#endif
    }
}