using UnityEngine;
using UnityEngine.AI;          // нужен для NavMeshAgent

public abstract class BaseEnemy : MonoBehaviour
{
    protected enum EnemyState
    {
        Idle,       // стоит, ничего не делает
        Patrol,     // ходит по комнате случайным образом
        Chase,      // преследует игрока
        Attack,     // атакует (игрок в радиусе атаки)
        Dead        // мёртв
    }


    [Header("Характеристики")]
    [SerializeField] protected int maxHealth = 30;
    [SerializeField] protected int damage = 10;
    [SerializeField] protected float attackRange = 2f;   // радиус атаки
    [SerializeField] protected float detectionRange = 10f;  // радиус обнаружения
    [SerializeField] protected float attackCooldown = 1.5f; // пауза между атаками

    [Header("Патрулирование")]
    [SerializeField] protected float patrolRadius = 8f;   // радиус случайного блуждания
    [SerializeField] protected float patrolWaitTime = 2f;   // пауза между точками патруля


    protected int _currentHealth;
    protected EnemyState _state = EnemyState.Idle;
    protected Transform _playerTransform;
    protected NavMeshAgent _agent;

    // Таймеры
    protected float _attackTimer = 0f;  // сколько прошло с последней атаки
    protected float _patrolTimer = 0f;  // сколько стоим на точке патруля

    // Начальная позиция — для патрулирования
    protected Vector3 _spawnPosition;

    // Свойства (читать могут все, писать — только класс и наследники)
    public bool IsAlive => _currentHealth > 0;
    public int CurrentHealth => _currentHealth;
    public float HealthPercent => (float)_currentHealth / maxHealth;


    protected virtual void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        if (_agent == null)
            Debug.LogError(gameObject.name + ": NavMeshAgent не найден!");
    }

    protected virtual void Start()
    {
        _currentHealth = maxHealth;
        _spawnPosition = transform.position;

        // Ищем игрока в сцене
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            _playerTransform = player.transform;

        // Начинаем патрулировать
        _state = EnemyState.Patrol;

        Debug.Log(gameObject.name + " создан. HP: " + _currentHealth);
    }

    protected virtual void Update()
    {
        if (!IsAlive) return;

        // Обновляем таймеры
        _attackTimer += Time.deltaTime;
        _patrolTimer += Time.deltaTime;

        // Запускаем логику текущего состояния
        switch (_state)
        {
            case EnemyState.Idle: UpdateIdle(); break;
            case EnemyState.Patrol: UpdatePatrol(); break;
            case EnemyState.Chase: UpdateChase(); break;
            case EnemyState.Attack: UpdateAttack(); break;
        }
    }

    protected virtual void UpdateIdle()
    {
        // Через 1 секунду переходим к патрулированию
        if (_patrolTimer > 1f)
        {
            _state = EnemyState.Patrol;
            _patrolTimer = 0f;
        }
    }

    protected virtual void UpdatePatrol()
    {
        if (_playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        // Игрок в зоне обнаружения → переключаемся на преследование
        if (distToPlayer <= detectionRange)
        {
            _state = EnemyState.Chase;
            Debug.Log(gameObject.name + " обнаружил игрока!");
            return;
        }

        // Двигаемся к случайной точке вокруг спавна
        // Если агент дошёл (или стоит на месте) — выбираем новую точку
        if (!_agent.hasPath || _agent.remainingDistance < 0.5f)
        {
            if (_patrolTimer >= patrolWaitTime)
            {
                _patrolTimer = 0f;
                MoveToRandomPatrolPoint();
            }
        }
    }

    protected virtual void UpdateChase()
    {
        if (_playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        // Игрок убежал слишком далеко → возвращаемся к патрулированию
        if (distToPlayer > detectionRange * 1.5f)
        {
            _state = EnemyState.Patrol;
            _agent.SetDestination(_spawnPosition);
            return;
        }

        // Игрок в радиусе атаки → атакуем
        if (distToPlayer <= attackRange)
        {
            _state = EnemyState.Attack;
            _agent.ResetPath();  // останавливаемся
            return;
        }

        // Иначе продолжаем преследование
        _agent.SetDestination(_playerTransform.position);
    }

    protected virtual void UpdateAttack()
    {
        if (_playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        // Игрок вышел из радиуса атаки → снова гонимся
        if (distToPlayer > attackRange)
        {
            _state = EnemyState.Chase;
            return;
        }

        // Поворачиваемся к игроку
        LookAtPlayer();

        // Атакуем если кулдаун прошёл
        if (_attackTimer >= attackCooldown)
        {
            _attackTimer = 0f;
            Attack();       // вызываем абстрактный метод — каждый враг атакует по-своему
        }
    }

    protected abstract void Attack();

    // ОБЩИЕ МЕТОДЫ — одинаковы для всех врагов
    

    // Получение урона
    public virtual void TakeDamage(int dmg)
    {
        if (!IsAlive) return;

        _currentHealth -= dmg;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);

        Debug.Log(gameObject.name + " получил урон: " + dmg + " | HP: " + _currentHealth + "/" + maxHealth);

        // Вспышка красного цвета при попадании
        StartCoroutine(HitFlash());

        if (_currentHealth <= 0)
            Die();
    }

    // Смерть
    protected virtual void Die()
    {
        _state = EnemyState.Dead;
        _agent.ResetPath();
        _agent.enabled = false;

        Debug.Log(gameObject.name + " уничтожен!");

        // Позже: дроп предметов, частицы смерти, очки
        // Уничтожаем объект через 1 секунду (время для эффекта смерти)
        Destroy(gameObject, 1f);
    }

    // Поворот к игроку (только по оси Y)
    protected void LookAtPlayer()
    {
        if (_playerTransform == null) return;

        Vector3 direction = (_playerTransform.position - transform.position).normalized;
        direction.y = 0f;  // не наклоняемся вперёд/назад

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    // Движение к случайной точке патруля
    protected void MoveToRandomPatrolPoint()
    {
        // Случайная точка в радиусе patrolRadius от точки спавна
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        Vector3 randomPoint = _spawnPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

        // NavMesh.SamplePosition — находим ближайшую точку на NavMesh
        // (случайная точка может быть в стене)
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
    }

    // Эффект вспышки при попадании
    private System.Collections.IEnumerator HitFlash()
    {
        var renderers = GetComponentsInChildren<Renderer>();

        // Запоминаем оригинальные цвета
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].material.color;

        // Красная вспышка
        foreach (var r in renderers)
            r.material.color = Color.red;

        yield return new WaitForSeconds(0.12f);

        // Восстанавливаем цвета
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].material.color = originalColors[i];
    }

    private void OnDrawGizmosSelected()
    {
        // Зона обнаружения — жёлтая
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Зона атаки — красная
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}