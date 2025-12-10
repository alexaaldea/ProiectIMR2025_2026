using UnityEngine;
using System.Collections.Generic;

public class CoinSpawner : MonoBehaviour
{
    [Header("Start / End (direcția pistei)")]
    public Transform startPoint;          // CoinStart
    public Transform endPoint;            // CoinEnd

    [Header("Player")]
    public Transform player;              // XR Origin / Camera / Player

    [Header("Lane Settings")]
    [Tooltip("Distanța laterală între pista din mijloc și cele laterale")]
    public float laneOffset = 0.3f;       // EX: 0.3 => stânga = x-0.3, dreapta = x+0.3

    [Header("Spawn Settings")]
    public bool autoSpawnByTime = true;
    public float spawnInterval = 1.0f;    // secunde între spawn-uri
    public float spawnDistanceAhead = 15f;// cât de departe în fața player-ului
    public float randomZOffset = 2f;      // mică variație pe Z

    [Header("Mișcare pe Z spre player")]
    public float coinSpeed = 5f;          // unități / secundă

    [Header("Overlap check (opțional)")]
    public float checkRadius = 0.3f;
    public LayerMask obstacleMask;

    private List<StarCollectible> _spawned = new List<StarCollectible>();
    private float _nextSpawnTime;

    // ==== metoda folosită și de MapChunkExampleHook ====
    public void Spawn()
    {
        if (player == null)
        {
            Debug.LogWarning("CoinSpawner: Player nu este setat.");
            return;
        }

        if (startPoint == null || endPoint == null)
        {
            Debug.LogWarning("CoinSpawner: startPoint sau endPoint lipsesc.");
            return;
        }

        if (CoinPool.Instance == null)
        {
            Debug.LogWarning("CoinSpawner: CoinPool.Instance este null.");
            return;
        }

        // direcția drumului
        Vector3 dir = (endPoint.position - startPoint.position).normalized;

        // Z în fața playerului
        Vector3 forwardPos = player.position + dir * (spawnDistanceAhead + Random.Range(0f, randomZOffset));

        // 3 piste pe X
        float baseX = startPoint.position.x;

        int laneIndex = Random.Range(0, 3); // 0, 1, 2
        float laneX;

        switch (laneIndex)
        {
            case 0: // stânga
                laneX = baseX - laneOffset;
                break;
            case 1: // mijloc
                laneX = baseX;
                break;
            default: // 2, dreapta
                laneX = baseX + laneOffset;
                break;
        }

        // pentru debug: vezi în consolă pe ce pistă spawnează
        Debug.Log($"CoinSpawner: spawn laneIndex={laneIndex}, laneX={laneX}");

        // Y = solul (Y-ul lui startPoint)
        float y = startPoint.position.y;

        Vector3 spawnPos = new Vector3(laneX, y, forwardPos.z);

        if (Physics.CheckSphere(spawnPos, checkRadius, obstacleMask))
            return;

        StarCollectible star = CoinPool.Instance.GetStar(spawnPos, Quaternion.identity);
        if (star == null) return;

        _spawned.Add(star);

        // mișcare DOAR pe Z (nu urmărește playerul pe X/Y)
        CoinMoverZ mover = star.gameObject.GetComponent<CoinMoverZ>();
        if (mover == null) mover = star.gameObject.AddComponent<CoinMoverZ>();

        mover.speed = coinSpeed;
    }
    // ====================================================

    private void Update()
    {
        if (!autoSpawnByTime) return;
        if (player == null) return;

        if (Time.time >= _nextSpawnTime)
        {
            Spawn();
            _nextSpawnTime = Time.time + spawnInterval;
        }
    }

    public void ClearPrevious()
    {
        if (CoinPool.Instance == null) return;

        foreach (var star in _spawned)
        {
            if (star != null && star.gameObject.activeSelf)
                CoinPool.Instance.ReturnToPool(star);
        }
        _spawned.Clear();
    }

    // ===== component intern: mișcare strict pe axa Z =====
    private class CoinMoverZ : MonoBehaviour
    {
        public float speed = 5f;

        void Update()
        {
            transform.position += Vector3.back * speed * Time.deltaTime;
        }
    }
}