using UnityEngine;

// ============================================================
// СНАРЯД — вешается на префаб Fireball
// Летит вперёд, при попадании спавнит эффект и звук
// ============================================================
public class Projectile : MonoBehaviour
{
    // ── Данные снаряда (устанавливаются через Init) ──────────
    private float _speed = 14f;
    private int _damage = 25;
    private float _range = 20f;
    private Vector3 _direction;
    private Vector3 _startPos;

    // ── Компоненты ───────────────────────────────────────────
    private Rigidbody _rb;

    [Header("Эффект попадания")]
    [SerializeField] private GameObject hitEffectPrefab;  // перетащи HitEffect из Prefabs/Effects

    // ============================================================
    // ИНИЦИАЛИЗАЦИЯ — вызывается сразу после Instantiate
    // ============================================================
    public void Init(Vector3 direction, int damage, float speed, float range)
    {
        _direction = direction.normalized;
        _damage = damage;
        _speed = speed;
        _range = range;
        _startPos = transform.position;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // Задаём скорость через Rigidbody
        if (_rb != null)
            _rb.linearVelocity = _direction * _speed;

        // Поворачиваем снаряд по направлению
        if (_direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(_direction);
    }

    private void Update()
    {
        // Уничтожаем если улетел слишком далеко
        if (Vector3.Distance(_startPos, transform.position) >= _range)
            DestroyProjectile(transform.position);
    }

    // ============================================================
    // ПОПАДАНИЕ — срабатывает когда снаряд входит в триггер
    // ============================================================
    private void OnTriggerEnter(Collider other)
    {
        // Игнорируем самого игрока и другие снаряды
        if (other.CompareTag("Player")) return;
        if (other.CompareTag("Projectile")) return;

        // Проверяем попал ли в врага
        BaseEnemy enemy = other.GetComponent<BaseEnemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(_damage);

            // Воспроизводим звук попадания
            //if (AudioManager.Instance != null)
            //    AudioManager.Instance.PlayEnemyHit();
        }

        // Спавним эффект и уничтожаем снаряд
        DestroyProjectile(transform.position);
    }

    // ============================================================
    // УНИЧТОЖЕНИЕ СНАРЯДА с эффектом
    // ============================================================
    private void DestroyProjectile(Vector3 pos)
    {
        // Спавним эффект попадания
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, pos, Quaternion.identity);

        Destroy(gameObject);
    }
}