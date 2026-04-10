using UnityEngine;

public class GhostEnemy : BaseEnemy
{
    [Header("Уникальные параметры призрака")]
    [SerializeField] private float dashSpeed = 12f;   // скорость рывка
    [SerializeField] private float dashDuration = 0.3f;  // длительность рывка
    [SerializeField] private float dashCooldown = 4f;    // пауза между рывками
    [SerializeField] private Color ghostColor = new Color(0.6f, 0.8f, 1f, 0.8f); // голубоватый

    private float _dashTimer = 0f;
    private bool _isDashing = false;
    private float _dashTimeLeft = 0f;

    protected override void Start()
    {
        base.Start();   // сначала выполняем Start() из BaseEnemy

        // Устанавливаем цвет призрака
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            r.material.color = ghostColor;

        Debug.Log("Призрак готов к охоте!");
    }

    protected override void Update()
    {
        base.Update();  // выполняем всю базовую логику состояний

        _dashTimer += Time.deltaTime;

        // Обрабатываем рывок если он активен
        if (_isDashing)
        {
            _dashTimeLeft -= Time.deltaTime;
            if (_dashTimeLeft <= 0f)
                EndDash();
        }
    }

    protected override void Attack()
    {
        if (_playerTransform == null) return;

        // Наносим урон напрямую через PlayerController
        var player = _playerTransform.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(damage);
            Debug.Log("Призрак атаковал! Урон: " + damage);
        }

        // Если кулдаун рывка прошёл — делаем рывок
        if (_dashTimer >= dashCooldown && !_isDashing)
        {
            StartDash();
        }
    }

    private void StartDash()
    {
        if (_playerTransform == null) return;

        _isDashing = true;
        _dashTimeLeft = dashDuration;
        _dashTimer = 0f;

        // Временно увеличиваем скорость NavMeshAgent
        _agent.speed = dashSpeed;

        Debug.Log("Призрак делает рывок!");
    }

    private void EndDash()
    {
        _isDashing = false;
        // Возвращаем нормальную скорость
        // Берём скорость из компонента NavMeshAgent (стандартная)
        _agent.speed = 3.5f;
    }

    protected override void Die()
    {
        Debug.Log("Призрак растворяется...");
        StartCoroutine(FadeAndDie());
    }

    private System.Collections.IEnumerator FadeAndDie()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        float elapsed = 0f;
        float fadetime = 0.6f;

        while (elapsed < fadetime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadetime);

            foreach (var r in renderers)
            {
                Color c = r.material.color;
                c.a = alpha;
                r.material.color = c;
            }
            yield return null;
        }

        // Вызываем базовую смерть (Destroy, очки и т.д.)
        base.Die();
    }
}