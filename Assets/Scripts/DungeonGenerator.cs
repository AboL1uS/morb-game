using UnityEngine;
using System.Collections.Generic;

// Этот скрипт вешается на пустой GameObject "DungeonGenerator" в сцене
public class DungeonGenerator : MonoBehaviour
{

    [System.Serializable]
    public class RoomData
    {
        public Vector2Int gridPos;
        public GameObject instance;
        public bool isStartRoom;
        public bool isBossRoom;
        public List<Vector2Int> neighbors = new List<Vector2Int>();

        public RoomData(Vector2Int pos)
        {
            gridPos = pos;
        }
    }

    [Header("Префабы")]
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private GameObject corridorPrefab;

    [Header("Размеры генерации")]
    [SerializeField] private int roomCount = 8;
    [SerializeField] private int roomSpacing = 25;  // расстояние между центрами комнат
    [SerializeField] private int maxAttempts = 100;

    [Header("Сид (0 = случайный)")]
    [SerializeField] private int seed = 0;

    private Dictionary<Vector2Int, RoomData> _rooms = new Dictionary<Vector2Int, RoomData>();
    private GameObject _dungeonRoot;
    private PlayerController _player;

    private static readonly Vector2Int[] Directions = {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private void Start()
    {
        _player = FindFirstObjectByType<PlayerController>();

        int actualSeed = (seed == 0) ? Random.Range(1, 99999) : seed;
        Random.InitState(actualSeed);
        Debug.Log("Генерация подземелья. Сид: " + actualSeed);

        _dungeonRoot = new GameObject("=== DUNGEON ===");

        GenerateRooms();
        ConnectRooms();
        PlacePlayer();

        Debug.Log("Подземелье готово! Комнат: " + _rooms.Count);
    }

    private void GenerateRooms()
    {
        Vector2Int currentPos = Vector2Int.zero;
        PlaceRoom(currentPos, isStart: true);

        int placed = 1;
        int attempts = 0;

        while (placed < roomCount && attempts < maxAttempts)
        {
            attempts++;

            Vector2Int dir = Directions[Random.Range(0, Directions.Length)];
            Vector2Int newPos = currentPos + dir;

            if (!_rooms.ContainsKey(newPos))
            {
                PlaceRoom(newPos);
                placed++;
                Debug.Log("Комната " + placed + " на " + newPos);
            }

            currentPos = newPos;
        }

        MarkBossRoom();
    }

    private void PlaceRoom(Vector2Int gridPos, bool isStart = false)
    {
        Vector3 worldPos = new Vector3(
            gridPos.x * roomSpacing,
            0f,
            gridPos.y * roomSpacing
        );

        GameObject roomObj = Instantiate(roomPrefab, worldPos, Quaternion.identity);
        roomObj.transform.SetParent(_dungeonRoot.transform);
        roomObj.name = isStart ? "Room_START [0,0]" : $"Room [{gridPos.x},{gridPos.y}]";

        RoomData data = new RoomData(gridPos);
        data.instance = roomObj;
        data.isStartRoom = isStart;

        _rooms[gridPos] = data;

        // Цвет стартовой комнаты
        if (isStart)
        {
            var rend = roomObj.GetComponentInChildren<Renderer>();
            if (rend != null)
                rend.material.color = new Color(0.3f, 0.25f, 0.45f);
        }
    }

    private void MarkBossRoom()
    {
        RoomData bossCandidate = null;
        float maxDist = 0f;

        foreach (var pair in _rooms)
        {
            if (pair.Value.isStartRoom) continue;

            float dist = Mathf.Abs(pair.Key.x) + Mathf.Abs(pair.Key.y);
            if (dist > maxDist)
            {
                maxDist = dist;
                bossCandidate = pair.Value;
            }
        }

        if (bossCandidate != null)
        {
            bossCandidate.isBossRoom = true;
            bossCandidate.instance.name = "Room_BOSS";

            var rend = bossCandidate.instance.GetComponentInChildren<Renderer>();
            if (rend != null)
                rend.material.color = new Color(0.45f, 0.15f, 0.15f);

            Debug.Log("Комната босса: " + bossCandidate.gridPos);
        }
    }

    private void ConnectRooms()
    {
        var processedPairs = new HashSet<string>();

        foreach (var pair in _rooms)
        {
            Vector2Int pos = pair.Key;
            RoomData room = pair.Value;

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int neighborPos = pos + dir;

                if (!_rooms.ContainsKey(neighborPos)) continue;

                string pairKey = GetPairKey(pos, neighborPos);
                if (processedPairs.Contains(pairKey)) continue;

                processedPairs.Add(pairKey);

                room.neighbors.Add(neighborPos);
                _rooms[neighborPos].neighbors.Add(pos);

                BuildCorridor(pos, neighborPos, dir);
            }
        }
    }

    private void BuildCorridor(Vector2Int fromPos, Vector2Int toPos, Vector2Int dir)
    {
        Vector3 fromWorld = new Vector3(fromPos.x * roomSpacing, 0f, fromPos.y * roomSpacing);
        Vector3 toWorld = new Vector3(toPos.x * roomSpacing, 0f, toPos.y * roomSpacing);

        // Центр коридора — ровно между двумя комнатами
        Vector3 midPoint = (fromWorld + toWorld) / 2f;

        GameObject corridor = Instantiate(corridorPrefab, midPoint, Quaternion.identity);
        corridor.transform.SetParent(_dungeonRoot.transform);
        corridor.name = $"Corridor [{fromPos}→{toPos}]";

        // Длина коридора = расстояние между центрами - толщина стен с обеих сторон (по 1 unit)
        float corridorLength = roomSpacing - 2f;

        if (dir == Vector2Int.left || dir == Vector2Int.right)
        {
            // Горизонтальный коридор (вдоль X) — поворачиваем на 90°
            corridor.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        }
        // Вертикальный коридор (вдоль Z) — поворот не нужен

        // Растягиваем пол коридора
        // Plane при Scale.z=1 имеет длину 10 units → делим нужную длину на 10
        Transform floor = corridor.transform.Find("Floor");
        if (floor != null)
        {
            float scaleZ = corridorLength / 10f;
            floor.localScale = new Vector3(floor.localScale.x, 1f, scaleZ);
        }

        // Растягиваем боковые стены коридора
        AdjustCorridorWalls(corridor, corridorLength);
    }

    // Подгоняем боковые стены коридора под его длину
    private void AdjustCorridorWalls(GameObject corridor, float corridorLength)
    {
        Transform wallL = corridor.transform.Find("Wall_L");
        Transform wallR = corridor.transform.Find("Wall_R");

        if (wallL == null || wallR == null)
        {
            Debug.LogWarning("Corridor: Wall_L или Wall_R не найдены в префабе!");
            return;
        }

        // Cube при Scale.z=1 имеет длину 1 unit → просто присваиваем длину
        Vector3 wallScale = wallL.localScale;
        wallScale.z = corridorLength;
        wallL.localScale = wallScale;
        wallR.localScale = wallScale;
    }

    private void PlacePlayer()
    {
        if (_player == null)
        {
            Debug.LogWarning("DungeonGenerator: PlayerController не найден!");
            return;
        }

        foreach (var pair in _rooms)
        {
            if (pair.Value.isStartRoom)
            {
                Vector3 spawnPos = pair.Value.instance.transform.position + Vector3.up * 1f;
                _player.transform.position = spawnPos;
                Debug.Log("Игрок размещён: " + spawnPos);
                return;
            }
        }
    }

    private string GetPairKey(Vector2Int a, Vector2Int b)
    {
        if (a.x < b.x || (a.x == b.x && a.y < b.y))
            return $"{a.x},{a.y}|{b.x},{b.y}";
        else
            return $"{b.x},{b.y}|{a.x},{a.y}";
    }

    // Другие скрипты могут запросить данные комнат
    public RoomData GetRoom(Vector2Int gridPos)
    {
        return _rooms.ContainsKey(gridPos) ? _rooms[gridPos] : null;
    }

    public Dictionary<Vector2Int, RoomData> GetAllRooms()
    {
        return _rooms;
    }

    private void OnDrawGizmos()
    {
        if (_rooms == null) return;

        foreach (var pair in _rooms)
        {
            Vector3 worldPos = new Vector3(
                pair.Key.x * roomSpacing,
                0.5f,
                pair.Key.y * roomSpacing
            );

            if (pair.Value.isStartRoom) Gizmos.color = Color.green;
            else if (pair.Value.isBossRoom) Gizmos.color = Color.red;
            else Gizmos.color = Color.cyan;

            Gizmos.DrawWireCube(worldPos, Vector3.one * 3f);

            Gizmos.color = Color.yellow;
            foreach (Vector2Int neighborPos in pair.Value.neighbors)
            {
                Vector3 neighborWorld = new Vector3(
                    neighborPos.x * roomSpacing,
                    0.5f,
                    neighborPos.y * roomSpacing
                );
                Gizmos.DrawLine(worldPos, neighborWorld);
            }
        }
    }
}