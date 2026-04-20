using UnityEngine;
using UnityEngine.InputSystem;

public class SpellSystem : MonoBehaviour
{
    [Header("Снаряд")]
    [SerializeField] private GameObject projectilePrefab;  // префаб Fireball
    [SerializeField] private float projectileSpeed = 14f;
    [SerializeField] private float projectileRange = 20f;
    [SerializeField] private int projectileDamage = 25;

    [Header("Кулдаун выстрела")]
    [SerializeField] private float fireCooldown = 0.35f;
    [SerializeField] private float fireManaCost = 8f;

    private PlayerController _player;
    private Animator _animator;   // для триггера анимации выстрела
    private Camera _camera;

    private float _cooldownTimer = 0f;
    private Vector3 _aimDirection;

    // ── Хэши анимаций — быстрее чем строки ──────────────────
    private static readonly int HashShoot = Animator.StringToHash("Shoot");
    private static readonly int HashHit = Animator.StringToHash("Hit");

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        _animator = GetComponent<Animator>();
        _camera = Camera.main;

        if (_player == null)
            Debug.LogError("SpellSystem: PlayerController не найден!");
        if (projectilePrefab == null)
            Debug.LogError("SpellSystem: Projectile Prefab не назначен в Inspector!");
    }

    private void Update()
    {
        if (!_player.IsAlive) return;

        _cooldownTimer += Time.deltaTime;

        UpdateAim();    // поворот орба к курсору
        HandleFire();   // обработка выстрела
    }

    private void UpdateAim()
    {
        // Позиция мыши на экране → луч в 3D
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _camera.ScreenPointToRay(mousePos);

        // Плоскость на высоте орба
        Plane plane = new Plane(Vector3.up, Vector3.up * transform.position.y);

        float enter;
        if (plane.Raycast(ray, out enter))
        {
            Vector3 aimPoint = ray.GetPoint(enter);
            _aimDirection = (aimPoint - transform.position).normalized;
            _aimDirection.y = 0f;

            // Поворачиваем орб к прицелу
            if (_aimDirection != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(_aimDirection);
        }
    }

    private void HandleFire()
    {
        if (!Mouse.current.leftButton.isPressed) return;
        if (_cooldownTimer < fireCooldown) return;

        // Проверяем ману
        if (!_player.SpendMana(fireManaCost))
        {
            Debug.Log("Мало маны!");
            return;
        }

        _cooldownTimer = 0f;
        FireProjectile();
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null) return;

        // Позиция спавна — чуть впереди орба
        Vector3 spawnPos = transform.position + _aimDirection * 0.7f;

        // Создаём снаряд
        GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        projObj.tag = "Projectile";

        // Инициализируем данными
        Projectile proj = projObj.GetComponent<Projectile>();
        if (proj != null)
            proj.Init(_aimDirection, projectileDamage, projectileSpeed, projectileRange);

        // Анимация выстрела на орбе
        if (_animator != null)
            _animator.SetTrigger(HashShoot);

        //// Звук выстрела
        //if (AudioManager.Instance != null)
        //    AudioManager.Instance.PlayShoot();
    }

  
    public void TriggerHitAnimation()
    {
        if (_animator != null)
            _animator.SetTrigger(HashHit);
    }

    // ── Gizmos — линия прицела ──────────────────────────────
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, _aimDirection * 3f);
    }
}