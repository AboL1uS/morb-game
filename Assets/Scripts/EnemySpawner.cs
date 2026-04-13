using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Префабы врагов")]
    [SerializeField] private GameObject ghostPrefab;   // префаб GhostEnemy
    [SerializeField] private GameObject bossPrefab;    // префаб BossEnemy

    [Header("Настройки спавна")]
    [SerializeField] private int enemiesPerRoom = 2;     // врагов на комнату
    [SerializeField] private float spawnDelay = 0.5f;  // задержка после генерации

    private DungeonGenerator _generator;
    private NavMeshSurface _navMeshSurface;

    private void Start()
    {
        _generator = GetComponent<DungeonGenerator>();
        _navMeshSurface = GetComponent<NavMeshSurface>();

        if (_generator == null)
        {
            Debug.LogError("EnemySpawner: DungeonGenerator не найден на этом объекте!");
            return;
        }

        if (_navMeshSurface == null)
        {
            Debug.LogError("EnemySpawner: NavMeshSurface не найден! Добавь его на этот объект.");
            return;
        }

        // Запускаем через корутину — ждём один кадр чтобы DungeonGenerator
        // успел создать все комнаты в своём Start()
        StartCoroutine(SpawnAfterGeneration());
    }

    private IEnumerator SpawnAfterGeneration()
    {
        // Ждём spawnDelay секунд — за это время DungeonGenerator
        // создаёт все комнаты через Instantiate()
        yield return new WaitForSeconds(spawnDelay);

        // Строим NavMesh поверх всех созданных объектов
        Debug.Log("Строим NavMesh...");
        _navMeshSurface.BuildNavMesh();
        Debug.Log("NavMesh построен!");

        // Ещё один кадр — ждём пока NavMesh применится
        yield return new WaitForEndOfFrame();

        // Теперь спавним врагов
        SpawnEnemies();
    }

    private void SpawnEnemies()
    {
        var allRooms = _generator.GetAllRooms();

        if (allRooms == null || allRooms.Count == 0)
        {
            Debug.LogWarning("EnemySpawner: комнаты не найдены!");
            return;
        }

        foreach (var pair in allRooms)
        {
            DungeonGenerator.RoomData room = pair.Value;

            // В стартовой комнате врагов нет — игрок появляется здесь
            if (room.isStartRoom) continue;

            // В комнате босса спавним только босса
            if (room.isBossRoom)
            {
                SpawnBoss(room);
                continue;
            }

            // В обычных комнатах спавним призраков
            SpawnGhostsInRoom(room);
        }

        Debug.Log("Враги расставлены!");
    }

    private void SpawnGhostsInRoom(DungeonGenerator.RoomData room)
    {
        if (ghostPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: ghostPrefab не назначен!");
            return;
        }

        Vector3 roomCenter = room.instance.transform.position;

        for (int i = 0; i < enemiesPerRoom; i++)
        {
            // Случайная позиция внутри комнаты (в радиусе 6 юнитов от центра)
            Vector3 spawnPos = GetRandomPointInRoom(roomCenter, 6f);

            if (spawnPos == Vector3.zero) continue;  // не нашли точку на NavMesh

            GameObject ghost = Instantiate(ghostPrefab, spawnPos, Quaternion.identity);
            ghost.name = $"Ghost [{room.gridPos}] #{i + 1}";
        }
    }

    private void SpawnBoss(DungeonGenerator.RoomData room)
    {
        if (bossPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: bossPrefab не назначен!");
            return;
        }

        Vector3 roomCenter = room.instance.transform.position;
        Vector3 spawnPos = roomCenter + Vector3.up * 1f;

        GameObject boss = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        boss.name = "BOSS";

        Debug.Log("Босс размещён в комнате: " + room.gridPos);
    }

    private Vector3 GetRandomPointInRoom(Vector3 center, float radius)
    {
        // Пробуем несколько раз найти точку на NavMesh
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 randomPoint = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 2f, NavMesh.AllAreas))
            {
                return hit.position + Vector3.up * 1f;  // чуть выше пола
            }
        }

        Debug.LogWarning("EnemySpawner: не удалось найти точку на NavMesh рядом с " + center);
        return Vector3.zero;
    }
}