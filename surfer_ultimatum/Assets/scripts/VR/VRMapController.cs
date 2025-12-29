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

    [Header("Active Chunks")]
    public int maxActiveChunks = 12;

    [Header("Despawning Settings")]
    public float despawnDistance = 30f;

    [Header("Alignment Settings")]
    public Transform initialMapChunk;
    public bool useEndPointAlignment = true;

    [Header("Pooling Settings")]
    public int initialPoolSize = 8;

    [Header("Debug")]
    public bool debugMode = false;

    [Header("Distance Tracking")]
    public float distanceTraveled = 0f;

    private Queue<GameObject> activeChunks = new Queue<GameObject>();
    private List<Queue<GameObject>> chunkPools = new List<Queue<GameObject>>();

    private Vector3 movementDirection = Vector3.back;
    private float fixedPlayerZ = 0f;

    private GameObject initialChunkGO;

    private Transform chunkParent;
    private Vector3 initialLocalScale = Vector3.one;
    private Quaternion initialWorldRotation = Quaternion.identity;

    private GameObject lastChunk;

    private Vector3 nextSpawnPosition;

    private bool isInitialized = false;

    void Start()
    {
        FindVRComponents();

        if (mapChunkPrefabs == null || mapChunkPrefabs.Length == 0)
        {
            Debug.LogError("VRMapController: No mapChunkPrefabs assigned!");
            enabled = false;
            return;
        }
        for (int i = 0; i < mapChunkPrefabs.Length; i++)
        {
            if (mapChunkPrefabs[i] == null)
            {
                Debug.LogError($"VRMapController: mapChunkPrefabs[{i}] is NULL.");
                enabled = false;
                return;
            }
        }

        fixedPlayerZ = (vrHead != null) ? vrHead.position.z : 0f;

        initialChunkGO = initialMapChunk ? initialMapChunk.gameObject : null;
        chunkParent = initialMapChunk ? initialMapChunk.parent : transform;
        initialLocalScale = initialMapChunk ? initialMapChunk.localScale : Vector3.one;
        initialWorldRotation = initialMapChunk ? initialMapChunk.rotation : Quaternion.identity;

        InitializeChunkPools();

        distanceTraveled = 0f;

        if (initialChunkGO != null)
        {
            lastChunk = initialChunkGO;
            nextSpawnPosition = GetSafeChunkEndWorld(initialChunkGO);
        }
        else
        {
            lastChunk = null;
            nextSpawnPosition = new Vector3(0f, 0f, fixedPlayerZ + 10f);
        }

        while (activeChunks.Count < maxActiveChunks)
            SpawnChunk();

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        distanceTraveled += movementSpeed * Time.deltaTime;

        MoveChunks();
        DespawnBehindPlayer();
        EnsureChunksAhead();
    }

    void MoveChunks()
    {
        Vector3 delta = movementDirection * movementSpeed * Time.deltaTime;

        if (initialChunkGO != null && initialChunkGO.activeInHierarchy)
            initialChunkGO.transform.Translate(delta, Space.World);

        foreach (GameObject chunk in activeChunks)
        {
            if (chunk != null && chunk.activeInHierarchy)
                chunk.transform.Translate(delta, Space.World);
        }

        if (lastChunk != null)
            nextSpawnPosition = GetSafeChunkEndWorld(lastChunk);
    }

    void EnsureChunksAhead()
    {
        while (activeChunks.Count < maxActiveChunks)
            SpawnChunk();
    }

    void DespawnBehindPlayer()
    {
        while (activeChunks.Count > 0)
        {
            GameObject oldest = activeChunks.Peek();
            if (oldest == null || !oldest.activeInHierarchy)
            {
                activeChunks.Dequeue();
                continue;
            }

            float endZ = GetSafeChunkEndWorld(oldest).z;

            if (endZ < fixedPlayerZ - despawnDistance)
            {
                activeChunks.Dequeue();
                ReturnChunkToPoolByName(oldest);
            }
            else
            {
                break;
            }
        }

        if (activeChunks.Count == 0)
        {
            lastChunk = initialChunkGO;
            if (initialChunkGO != null)
                nextSpawnPosition = GetSafeChunkEndWorld(initialChunkGO);
        }
    }

    void SpawnChunk()
    {
        int chunkType = Random.Range(0, mapChunkPrefabs.Length);
        GameObject chunk = GetChunkFromPool(chunkType);
        if (chunk == null) return;

        chunk.transform.SetParent(chunkParent, false);
        chunk.transform.localScale = initialLocalScale;
        chunk.transform.rotation = initialWorldRotation;

        Vector3 attachPoint = (lastChunk != null) ? GetSafeChunkEndWorld(lastChunk) : nextSpawnPosition;

        MapChunkAlignment align = chunk.GetComponent<MapChunkAlignment>();

        if (useEndPointAlignment && align != null)
        {
            Vector3 startWorld = align.GetWorldStartPoint();
            Vector3 pivotToStart = startWorld - chunk.transform.position;

            chunk.transform.position = attachPoint - pivotToStart;
        }
        else
        {
            chunk.transform.position = attachPoint;
        }

        chunk.SetActive(true);
        activeChunks.Enqueue(chunk);

        lastChunk = chunk;
        nextSpawnPosition = GetSafeChunkEndWorld(lastChunk);

        if (debugMode)
            Debug.Log($"Spawned {chunk.name} at z={chunk.transform.position.z:F2}, next={nextSpawnPosition.z:F2}");
    }

    Vector3 GetSafeChunkEndWorld(GameObject chunk)
    {
        if (chunk == null) return nextSpawnPosition;

        MapChunkAlignment a = chunk.GetComponent<MapChunkAlignment>();
        if (useEndPointAlignment && a != null)
        {
            Vector3 start = a.GetWorldStartPoint();
            Vector3 end = a.GetWorldEndPoint();

            Vector3 trackForward = -movementDirection;
            float advance = Vector3.Dot(end - start, trackForward);

            if (advance < 0.01f || float.IsNaN(advance))
            {
                if (debugMode)
                    Debug.LogWarning($"{chunk.name}: END is not ahead of START. Using fallback chunkLength.");
                end = start + trackForward * chunkLength;
            }

            return end;
        }

        return chunk.transform.position + Vector3.forward * chunkLength;
    }


    void InitializeChunkPools()
    {
        chunkPools.Clear();

        foreach (GameObject prefab in mapChunkPrefabs)
        {
            Queue<GameObject> pool = new Queue<GameObject>();

            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject c = Instantiate(prefab);
                c.transform.SetParent(chunkParent, false);
                c.transform.localScale = initialLocalScale;
                c.transform.rotation = initialWorldRotation;
                c.SetActive(false);
                pool.Enqueue(c);
            }

            chunkPools.Add(pool);
        }
    }

    GameObject GetChunkFromPool(int chunkType)
    {
        if (chunkType < 0 || chunkType >= chunkPools.Count) return null;

        Queue<GameObject> pool = chunkPools[chunkType];

        foreach (GameObject c in pool)
        {
            if (c != null && !c.activeInHierarchy)
                return c;
        }

        GameObject created = Instantiate(mapChunkPrefabs[chunkType]);
        created.transform.SetParent(chunkParent, false);
        created.transform.localScale = initialLocalScale;
        created.transform.rotation = initialWorldRotation;
        created.SetActive(false);
        return created;
    }

    void ReturnChunkToPoolByName(GameObject chunk)
    {
        if (chunk == null) return;

        if (chunk == initialChunkGO) return;

        chunk.SetActive(false);

        for (int i = 0; i < mapChunkPrefabs.Length; i++)
        {
            if (chunk.name.StartsWith(mapChunkPrefabs[i].name))
            {
                if (!chunkPools[i].Contains(chunk))
                    chunkPools[i].Enqueue(chunk);
                return;
            }
        }
    }


    void FindVRComponents()
    {
        if (xrOrigin == null)
        {
            xrOrigin = GameObject.Find("XR Origin")?.transform
                    ?? GameObject.Find("XRInteractionSetup")?.transform;
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
    }

    void OnDrawGizmos()
    {
        if (!debugMode || !Application.isPlaying) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(nextSpawnPosition, 0.5f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(new Vector3(0, 0, fixedPlayerZ - despawnDistance), 0.35f);
    }


    public void StopMovement() => movementSpeed = 0f;

    public void StartMovement() => movementSpeed = 8f;

    public void SetMovementSpeed(float newSpeed) => movementSpeed = newSpeed;
}
