using UnityEngine;
using UnityEngine.InputSystem;   

public class PlayerController : MonoBehaviour
{

    [Header("Движение")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float maxSpeed = 12f;

    [Header("Характеристики")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float manaRegen = 5f;

    // Приватные поля — инкапсуляция ООП
    private int _currentHealth;
    private float _currentMana;

    // Свойства — внешний код только читает
    public int CurrentHealth => _currentHealth;
    public float CurrentMana => _currentMana;
    public bool IsAlive => _currentHealth > 0;

    // Компоненты
    private Rigidbody _rb;
    private Vector3 _moveInput;


    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        if (_rb == null)
            Debug.LogError("PlayerController: Rigidbody не найден на " + gameObject.name);
    }

    private void Start()
    {
        _currentHealth = maxHealth;
        _currentMana = maxMana;

        Debug.Log("Орб пробуждён! HP: " + _currentHealth + " | Мана: " + _currentMana);
    }

    private void Update()
    {
        HandleInput();
        RegenerateMana();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }


    private void HandleInput()
    {
        // Keyboard.current — текущее состояние клавиатуры (новый Input System)
        // .isPressed — удерживается ли клавиша прямо сейчас
        var kb = Keyboard.current;

        // Если клавиатура недоступна (например, мобильное устройство без неё)
        if (kb == null) return;

        float inputX = 0f;
        float inputZ = 0f;

        // Горизонталь: A / D или стрелки влево / вправо
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) inputX = -1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) inputX = 1f;

        // Вертикаль: W / S или стрелки вверх / вниз
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) inputZ = 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) inputZ = -1f;

        _moveInput = new Vector3(inputX, 0f, inputZ);

        // Нормализация — диагональ не быстрее прямого направления
        if (_moveInput.magnitude > 1f)
            _moveInput = _moveInput.normalized;
    }

    private void HandleMovement()
    {
        // Целевая скорость на основе ввода
        Vector3 targetVelocity = _moveInput * moveSpeed;

        // Сохраняем вертикальную скорость (гравитация работает)
        targetVelocity.y = _rb.linearVelocity.y;

        _rb.linearVelocity = targetVelocity;

        // Ограничение максимальной горизонтальной скорости
        Vector3 horizontal = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        if (horizontal.magnitude > maxSpeed)
        {
            Vector3 clamped = horizontal.normalized * maxSpeed;
            _rb.linearVelocity = new Vector3(clamped.x, _rb.linearVelocity.y, clamped.z);
        }
    }


    private void RegenerateMana()
    {
        if (_currentMana < maxMana)
        {
            _currentMana += manaRegen * Time.deltaTime;
            _currentMana = Mathf.Clamp(_currentMana, 0f, maxMana);
        }
    }


    public void TakeDamage(int damage)
    {
        if (!IsAlive) return;

        _currentHealth -= damage;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);

        Debug.Log("Урон: -" + damage + " | HP: " + _currentHealth + "/" + maxHealth);

        if (_currentHealth <= 0) Die();
    }

    public void Heal(int amount)
    {
        _currentHealth += amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);
        Debug.Log("Лечение: +" + amount + " | HP: " + _currentHealth + "/" + maxHealth);
    }

    // Возвращает true если маны хватило на заклинание
    public bool SpendMana(float amount)
    {
        if (_currentMana < amount)
        {
            Debug.Log("Недостаточно маны! Нужно: " + amount + " | Есть: " + _currentMana);
            return false;
        }

        _currentMana -= amount;
        return true;
    }

    private void Die()
    {
        Debug.Log("Орб уничтожен...");
        this.enabled = false;
        // Позже: эффект смерти + экран Game Over
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, _moveInput * 2f);
    }
}