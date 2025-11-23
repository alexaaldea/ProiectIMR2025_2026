using UnityEngine;
using System.Collections.Generic;

public class CoinSpawner : MonoBehaviour
{
    [Header("Spawn Area")]
    public Transform startPoint;
    public Transform endPoint;
    public int coinsPerLine = 10;
    public float verticalOffset = 0.5f;

    [Header("Random")]
    public bool randomize = false;
    public float laneWidth = 2f;
    public int lanes = 3;
    public float minSpacing = 1f;

    [Header("Avoid Overlap")]
    public float checkRadius = 0.3f;
    public LayerMask obstacleMask;

    public enum Pattern { LineCenter, MultiLaneSpread, ZigZag, RandomScatter }
    public Pattern pattern = Pattern.LineCenter;

    private List<StarCollectible> _spawned = new List<StarCollectible>();

    public void Spawn()
    {
        ClearPrevious();
        if (startPoint == null || endPoint == null) return;

        Vector3 a = startPoint.position;
        Vector3 b = endPoint.position;
        Vector3 dir = (b - a).normalized;
        float totalDist = Vector3.Distance(a, b);

        switch (pattern)
        {
            case Pattern.LineCenter: SpawnLine(a, dir, totalDist); break;
            case Pattern.MultiLaneSpread: SpawnMultiLane(a, dir, totalDist); break;
            case Pattern.ZigZag: SpawnZigZag(a, dir, totalDist); break;
            case Pattern.RandomScatter: SpawnRandom(a, dir, totalDist); break;
        }
    }

    private void SpawnLine(Vector3 start, Vector3 dir, float dist)
    {
        for (int i = 0; i < coinsPerLine; i++)
        {
            float t = (float)i / (coinsPerLine - 1);
            Vector3 pos = start + dir * dist * t;
            pos.y += verticalOffset;
            TrySpawnAt(pos);
        }
    }

    private void SpawnMultiLane(Vector3 start, Vector3 dir, float dist)
    {
        for (int lane = 0; lane < lanes; lane++)
        {
            float laneOffset = Mathf.Lerp(-laneWidth, laneWidth, lane / (float)(lanes - 1));
            for (int i = 0; i < coinsPerLine; i++)
            {
                float t = (float)i / (coinsPerLine - 1);
                Vector3 pos = start + dir * dist * t + Vector3.right * laneOffset;
                pos.y += verticalOffset;
                TrySpawnAt(pos);
            }
        }
    }

    private void SpawnZigZag(Vector3 start, Vector3 dir, float dist)
    {
        for (int i = 0; i < coinsPerLine; i++)
        {
            float t = (float)i / (coinsPerLine - 1);
            float zig = Mathf.Sin(t * Mathf.PI * 2f) * laneWidth;
            Vector3 pos = start + dir * dist * t + Vector3.right * zig;
            pos.y += verticalOffset;
            TrySpawnAt(pos);
        }
    }

    private void SpawnRandom(Vector3 start, Vector3 dir, float dist)
    {
        float step = dist / coinsPerLine;
        float current = 0f;
        while (current < dist)
        {
            Vector3 pos = start + dir * current;
            float randomLane = Random.Range(-laneWidth, laneWidth);
            pos += Vector3.right * randomLane;
            pos.y += verticalOffset;
            TrySpawnAt(pos);
            current += Random.Range(minSpacing, step + minSpacing);
        }
    }

    private void TrySpawnAt(Vector3 pos)
    {
        if (Physics.CheckSphere(pos, checkRadius, obstacleMask))
            return;

        if (CoinPool.Instance == null)
        {
            Debug.LogWarning("CoinSpawner: CoinPool.Instance este null. Amân spawn-ul.");
            return;
        }

        var star = CoinPool.Instance.GetStar(pos, Quaternion.identity);
        if (star != null)
            _spawned.Add(star);
    }

    public void ClearPrevious()
    {
        foreach (var star in _spawned)
        {
            if (star != null && star.gameObject.activeSelf)
                CoinPool.Instance.ReturnToPool(star);
        }
        _spawned.Clear();
    }
}