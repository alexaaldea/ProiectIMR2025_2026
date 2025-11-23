using UnityEngine;
using System.Collections.Generic;

public class CoinPool : MonoBehaviour
{
    public static CoinPool Instance;

    [Header("Pool Settings")]
    public StarCollectible starPrefab;
    public int initialAmount = 50;
    public bool expandable = true;

    private readonly Queue<StarCollectible> _pool = new Queue<StarCollectible>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Prewarm();
    }

    private void Prewarm()
    {
        for (int i = 0; i < initialAmount; i++)
        {
            var star = Instantiate(starPrefab, transform);
            star.gameObject.SetActive(false);
            _pool.Enqueue(star);
        }
    }

    public StarCollectible GetStar(Vector3 position, Quaternion rotation)
    {
        StarCollectible star;
        if (_pool.Count > 0) star = _pool.Dequeue();
        else
        {
            if (!expandable) return null;
            star = Instantiate(starPrefab, transform);
        }

        star.transform.SetPositionAndRotation(position, rotation);
        star.gameObject.SetActive(true);
        return star;
    }

    public void ReturnToPool(StarCollectible star)
    {
        star.gameObject.SetActive(false);
        _pool.Enqueue(star);
    }
}