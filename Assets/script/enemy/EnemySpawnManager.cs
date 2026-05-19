using UnityEngine;

public sealed class EnemySpawnManager : MonoBehaviour
{
    [SerializeField] private enemy01 enemyPrefab;
    [SerializeField] private Transform center;
    [SerializeField] private int spawnCount = 4;
    [SerializeField] private float radius = 25f;
    [SerializeField] private float spawnHeight = 1f;

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawnManager has no enemyPrefab assigned.");
            return;
        }

        Vector3 centerPosition = center != null ? center.position : transform.position;

        for (int i = 0; i < spawnCount; i++)
        {
            float angle = i * Mathf.PI * 2f / spawnCount;
            Vector3 position = centerPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            position.y = spawnHeight;

            enemy01 enemy = Instantiate(enemyPrefab, position, Quaternion.identity, transform);
            enemy.name = $"enemy01_{i + 1:00}";
        }
    }
}
