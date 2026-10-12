using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BossController : MonoBehaviour
{
    [Header("Health and animation")]
    [SerializeField, Min(1)] private int maxHealth = 300;
    [SerializeField] private Animator animator;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Image healthFill;
    [SerializeField] private Color highHealthColor = Color.green;
    [SerializeField] private Color mediumHealthColor = Color.yellow;
    [SerializeField] private Color lowHealthColor = Color.red;
    [Header("Projectile pool")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform[] muzzles;
    [SerializeField] private float bulletSpeed = 6f;
    [SerializeField] private int bulletDamage = 1;
    [SerializeField] private int poolDefaultCapacity = 24;
    [SerializeField] private int poolMaxSize = 96;
    [Header("Arena movement")]
    [SerializeField] private Vector2 arenaMin = new Vector2(-7f, 1f);
    [SerializeField] private Vector2 arenaMax = new Vector2(7f, 4f);
    [SerializeField] private float patternMoveSpeed = 3f;
    [Header("Transition")]
    [SerializeField, Min(0f)] private float invulnerabilityDuration = 2f;
    [SerializeField] private Renderer[] renderersToFlash;
    [SerializeField, Min(0.03f)] private float flashInterval = 0.12f;

    private ObjectPool<Bullet> bulletPool;
    private int currentHealth;
    private int currentPhase;
    private int nextPattern;
    private float nextShotTime;
    private bool invulnerable;
    private Coroutine transitionRoutine;

    private static readonly int HealthPhaseId = Animator.StringToHash("HealthPhase");
    private static readonly int PatternIndexId = Animator.StringToHash("PatternIndex");
    private static readonly int PhaseChangedId = Animator.StringToHash("PhaseChanged");
    private static readonly int NextPatternId = Animator.StringToHash("NextPattern");

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int CurrentPhase => currentPhase;
    public bool IsInvulnerable => invulnerable;

    private void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        if (animator == null) animator = GetComponent<Animator>();
        if (renderersToFlash == null || renderersToFlash.Length == 0)
            renderersToFlash = GetComponentsInChildren<Renderer>(true);
        bulletPool = new ObjectPool<Bullet>(CreateBullet, OnTakeBullet, OnReleaseBullet,
            OnDestroyBullet, true, Mathf.Max(1, poolDefaultCapacity), Mathf.Max(1, poolMaxSize));
        SetPhase(DeterminePhase(), false);
        UpdateHealthBar();
    }

    public void TakeDamage(int damage)
    {
        if (invulnerable || currentHealth <= 0 || damage <= 0) return;
        int oldPhase = currentPhase;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealthBar();
        int newPhase = DeterminePhase();
        if (newPhase > oldPhase)
        {
            SetPhase(newPhase, true);
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(PhaseTransition());
        }
        if (currentHealth == 0)
        {
            if (GameManager.Instance != null) GameManager.Instance.ShowVictory();
            enabled = false;
        }
    }

    private int DeterminePhase()
    {
        float ratio = (float)currentHealth / Mathf.Max(1, maxHealth);
        if (ratio > 0.5f) return 0;
        if (ratio >= 0.2f) return 1;
        return 2;
    }

    private void SetPhase(int phase, bool signalAnimator)
    {
        currentPhase = Mathf.Clamp(phase, 0, 2);
        nextPattern = 0;
        if (animator != null)
        {
            animator.SetInteger(HealthPhaseId, currentPhase);
            animator.SetInteger(PatternIndexId, nextPattern);
            animator.ResetTrigger(NextPatternId);
            if (signalAnimator) animator.SetTrigger(PhaseChangedId);
        }
    }

    private IEnumerator PhaseTransition()
    {
        invulnerable = true;
        float endTime = Time.time + invulnerabilityDuration;
        bool visible = false;

        while (Time.time < endTime)
        {
            visible = !visible;
            SetRenderersVisible(visible);
            yield return new WaitForSeconds(flashInterval);
        }

        SetRenderersVisible(true);
        invulnerable = false;
        transitionRoutine = null;
    }

    private void SetRenderersVisible(bool visible)
    {
        foreach (Renderer target in renderersToFlash) if (target != null) target.enabled = visible;
    }

    private void UpdateHealthBar()
    {
        float ratio = (float)currentHealth / Mathf.Max(1, maxHealth);
        if (healthBar != null) healthBar.value = ratio;
        if (healthFill != null)
            healthFill.color = ratio > 0.5f ? highHealthColor : ratio >= 0.2f ? mediumHealthColor : lowHealthColor;
    }

    /// <summary>Called every Animator state update by the pattern StateMachineBehaviours.</summary>
    public void RunPattern(BossAttackPattern pattern, float stateTime)
    {
        if (invulnerable || currentHealth <= 0) return;
        Vector2 playerPosition = FindPlayerPosition();
        switch (pattern)
        {
            case BossAttackPattern.AimedSingle:
                MoveTo(new Vector2(playerPosition.x, arenaMax.y));
                FireAtPlayer(0.7f, 1);
                break;
            case BossAttackPattern.ThreeWay:
                MoveHorizontally(stateTime);
                FireSpread(0.9f, 3, 20f);
                break;
            case BossAttackPattern.Sweep:
                MoveTo(new Vector2(Mathf.Lerp(arenaMin.x, arenaMax.x, Mathf.PingPong(stateTime * 0.35f, 1f)), arenaMax.y));
                FireFan(0.3f, 5, 60f);
                break;
            case BossAttackPattern.AimedBurst:
                MoveTo(new Vector2(playerPosition.x, arenaMax.y));
                FireBurst(1f, 3, 0.15f);
                break;
            case BossAttackPattern.CrossSpread:
                MoveHorizontally(stateTime * 1.35f);
                FireSpread(0.7f, 5, 30f);
                break;
            case BossAttackPattern.Rain:
                MoveTo(new Vector2(Mathf.Sin(stateTime * 1.7f) * arenaMax.x * 0.65f, arenaMax.y));
                FireRain(0.2f, 3);
                break;
            case BossAttackPattern.DashAndAim:
                MoveTo(new Vector2(Mathf.Sin(stateTime * 2.4f) * arenaMax.x * 0.8f, arenaMax.y - Mathf.PingPong(stateTime * 1.2f, 1.2f)));
                FireAtPlayer(0.4f, 2);
                break;
            case BossAttackPattern.DenseFan:
                MoveHorizontally(stateTime * 1.8f);
                FireFan(0.3f, 9, 60f);
                break;
            case BossAttackPattern.Spiral:
                MoveTo(new Vector2(Mathf.Sin(stateTime * 1.4f) * arenaMax.x * 0.7f, arenaMax.y));
                FireSpiral(0.1f, 2);
                break;
        }
    }

    public void AdvancePattern(int completedPhase)
    {
        if (completedPhase != currentPhase || invulnerable || currentHealth <= 0) return;
        nextPattern = (nextPattern + 1) % 3;
        if (animator != null)
        {
            animator.SetInteger(PatternIndexId, nextPattern);
            animator.SetTrigger(NextPatternId);
        }
    }

    private Vector2 FindPlayerPosition()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        return player != null ? player.transform.position : (Vector2)transform.position + Vector2.down;
    }

    private void MoveHorizontally(float phase)
    {
        float normalized = Mathf.PingPong(phase * patternMoveSpeed * 0.25f, 1f);
        MoveTo(new Vector2(Mathf.Lerp(arenaMin.x, arenaMax.x, normalized), Mathf.Clamp(transform.position.y, arenaMin.y, arenaMax.y)));
    }

    private void MoveTo(Vector2 destination)
    {
        transform.position = Vector2.MoveTowards(transform.position, destination, patternMoveSpeed * Time.deltaTime);
    }

    private void FireAtPlayer(float interval, int shots)
    {
        if (Time.time < nextShotTime) return;
        nextShotTime = Time.time + interval;
        Vector2 origin = MuzzlePosition(0);
        Vector2 direction = (FindPlayerPosition() - origin).normalized;
        for (int i = 0; i < shots; i++) Fire(direction);
    }

    private void FireBurst(float interval, int shots, float spreadStep)
    {
        if (Time.time < nextShotTime) return;
        nextShotTime = Time.time + interval;
        Vector2 direction = (FindPlayerPosition() - MuzzlePosition(0)).normalized;
        for (int i = 0; i < shots; i++)
        {
            float angle = (i - (shots - 1) * 0.5f) * spreadStep;
            Fire(Rotate(direction, angle));
        }
    }

    private void FireSpread(float interval, int count, float angle)
    {
        if (Time.time < nextShotTime) return;
        nextShotTime = Time.time + interval;
        Vector2 direction = Vector2.down;
        for (int i = 0; i < count; i++) Fire(Rotate(direction, (i - (count - 1) * 0.5f) * angle));
    }

    private void FireFan(float interval, int count, float angle)
    {
        if (Time.time < nextShotTime) return;
        nextShotTime = Time.time + interval;
        Vector2 aim = (FindPlayerPosition() - MuzzlePosition(0)).normalized;
        for (int i = 0; i < count; i++) Fire(Rotate(aim, (i - (count - 1) * 0.5f) * angle / Mathf.Max(1, count - 1)));
    }

    private int rainIndex;
    private void FireRain(float interval, int shots)
    {
        if (Time.time < nextShotTime) return;
        nextShotTime = Time.time + interval;
        for (int i = 0; i < shots; i++)
        {
            float x = Mathf.Lerp(arenaMin.x, arenaMax.x, ((rainIndex + i * 7) % 17) / 16f);
            FireFrom(new Vector2(x, transform.position.y), Vector2.down);
        }
        rainIndex = (rainIndex + 3) % 17;
    }

    private float spiralAngle;
    private void FireSpiral(float interval, int count)
    {
        if (Time.time < nextShotTime) return;
        nextShotTime = Time.time + interval;
        for (int i = 0; i < count; i++) Fire(Rotate(Vector2.down, spiralAngle + i * (360f / count)));
        spiralAngle = (spiralAngle + 17f) % 360f;
    }

    private Vector2 MuzzlePosition(int index) => muzzles is { Length: > 0 } && muzzles[index % muzzles.Length] != null
        ? muzzles[index % muzzles.Length].position : transform.position;
    private void Fire(Vector2 direction) => FireFrom(MuzzlePosition(0), direction);
    private void FireFrom(Vector2 origin, Vector2 direction)
    {
        Bullet bullet = bulletPool.Get();
        bullet.transform.SetPositionAndRotation(origin, Quaternion.identity);
        bullet.Launch(Bullet.Owner.Boss, direction, bulletSpeed, bulletDamage);
    }
    private static Vector2 Rotate(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(vector.x * Mathf.Cos(radians) - vector.y * Mathf.Sin(radians),
            vector.x * Mathf.Sin(radians) + vector.y * Mathf.Cos(radians));
    }

    private Bullet CreateBullet()
    {
        Bullet bullet = Instantiate(bulletPrefab);
        bullet.SetPool(bulletPool);
        return bullet;
    }
    private void OnTakeBullet(Bullet bullet) => bullet.gameObject.SetActive(true);
    private void OnReleaseBullet(Bullet bullet) => bullet.gameObject.SetActive(false);
    private void OnDestroyBullet(Bullet bullet) => Destroy(bullet.gameObject);
}
