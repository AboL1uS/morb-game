using UnityEngine;
using UnityEngine.AI;

public class BossEnemy : BaseEnemy
{
    // Фазы босса — меняются по мере потери HP
    private enum BossPhase { PhaseOne, PhaseTwo, PhaseThree }

    [Header("Параметры босса")]
    [SerializeField] private int phase2Threshold = 60;   // % HP для фазы 2
    [SerializeField] private int phase3Threshold = 30;   // % HP для фазы 3
    [SerializeField] private float summonCooldown = 8f;   // пауза между призывами
    [SerializeField] private GameObject ghostPrefab;        // префаб призрака для призыва

    private BossPhase _currentPhase = BossPhase.PhaseOne;
    private float _summonTimer = 0f;
    private bool _phase2Triggered = false;
    private bool _phase3Triggered = false;

    protected override void Start()
    {
        // Боссу больше HP и урона
        maxHealth = 200;
        damage = 20;
        attackRange = 3f;
        detectionRange = 20f;  // видит дальше обычных врагов

        base.Start();

        // Цвет босса — тёмно-красный с пульсацией
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            r.material.color = new Color(0.7f, 0.1f, 0.1f);

        // NavMeshAgent босса медленнее но мощнее
        if (_agent != null)
        {
            _agent.speed = 2.5f;
            _agent.radius = 0.8f;
        }

        Debug.Log("БОСС пробуждён! Бегите!");
    }

    protected override void Update()
    {
        base.Update();

        _summonTimer += Time.deltaTime;

        // Проверяем переход между фазами
        CheckPhaseTransition();
    }

    private void CheckPhaseTransition()
    {
        int healthPercent = Mathf.RoundToInt(HealthPercent * 100f);

        // Фаза 2: HP < 60%
        if (healthPercent <= phase2Threshold && !_phase2Triggered)
        {
            _phase2Triggered = true;
            _currentPhase = BossPhase.PhaseTwo;
            EnterPhaseTwo();
        }

        // Фаза 3: HP < 30%
        if (healthPercent <= phase3Threshold && !_phase3Triggered)
        {
            _phase3Triggered = true;
            _currentPhase = BossPhase.PhaseThree;
            EnterPhaseThree();
        }
    }

    private void EnterPhaseTwo()
    {
        Debug.Log("БОСС входит в фазу 2! Становится быстрее!");
        if (_agent != null)
            _agent.speed = 4f;   // ускоряется

        attackCooldown = 1f;     // атакует чаще

        // Меняем цвет — оранжевый
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            r.material.color = new Color(0.8f, 0.4f, 0.0f);
    }

    private void EnterPhaseThree()
    {
        Debug.Log("БОСС входит в фазу 3! Ярость!");
        if (_agent != null)
            _agent.speed = 6f;   // очень быстрый

        attackCooldown = 0.6f;   // атакует очень часто
        damage = 35;     // больше урона

        // Меняем цвет — ярко-красный
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            r.material.color = new Color(1f, 0.05f, 0.05f);
    }

    protected override void Attack()
    {
        var player = _playerTransform?.GetComponent<PlayerController>();

        switch (_currentPhase)
        {
            case BossPhase.PhaseOne:
                // Обычный удар
                player?.TakeDamage(damage);
                Debug.Log("Босс ударил! Урон: " + damage);
                break;

            case BossPhase.PhaseTwo:
                // Двойной удар
                player?.TakeDamage(damage);
                player?.TakeDamage(damage / 2);
                Debug.Log("Босс нанёс двойной удар!");

                // Призываем призрака если кулдаун прошёл
                if (_summonTimer >= summonCooldown)
                {
                    _summonTimer = 0f;
                    SummonGhost();
                }
                break;

            case BossPhase.PhaseThree:
                // Тройной удар + призыв
                player?.TakeDamage(damage);
                player?.TakeDamage(damage);
                player?.TakeDamage(damage / 2);
                Debug.Log("Босс в ярости! Тройной удар!");

                if (_summonTimer >= summonCooldown * 0.5f)
                {
                    _summonTimer = 0f;
                    SummonGhost();
                    SummonGhost();  // два призрака
                }
                break;
        }
    }

    private void SummonGhost()
    {
        if (ghostPrefab == null)
        {
            Debug.LogWarning("BossEnemy: ghostPrefab не назначен в Inspector!");
            return;
        }

        // Спавним призрака рядом с боссом
        Vector3 spawnOffset = Random.insideUnitSphere * 3f;
        spawnOffset.y = 0f;
        Vector3 spawnPos = transform.position + spawnOffset;

        GameObject ghost = Instantiate(ghostPrefab, spawnPos, Quaternion.identity);
        Debug.Log("Босс призвал призрака!");
    }

    protected override void Die()
    {
        Debug.Log("БОСС ПОВЕРЖЕН! Уровень завершён!");
        // Позже: открыть дверь на следующий этаж, дроп редкого лута
        StartCoroutine(BossDeathSequence());
    }

    private System.Collections.IEnumerator BossDeathSequence()
    {
        // Несколько вспышек перед уничтожением
        var renderers = GetComponentsInChildren<Renderer>();

        for (int i = 0; i < 5; i++)
        {
            foreach (var r in renderers)
                r.material.color = Color.white;
            yield return new WaitForSeconds(0.1f);

            foreach (var r in renderers)
                r.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
        }

        base.Die();
    }
}