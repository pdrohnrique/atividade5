using UnityEngine;
using UnityEngine.Pool;

/// <summary>Reusable projectile shared by the player and boss pools.</summary>
[DisallowMultipleComponent]
public sealed class Bullet : MonoBehaviour
{
    public enum Owner { Player, Boss }

    [SerializeField] private float defaultSpeed = 10f;
    [SerializeField] private float defaultLifetime = 5f;
    [SerializeField] private int defaultDamage = 1;

    private IObjectPool<Bullet> pool;
    private float lifetimeRemaining;
    private Vector2 direction;
    private Owner owner;
    private int damage;
    private bool configured;

    public void SetPool(IObjectPool<Bullet> owningPool) => pool = owningPool;

    public void Launch(Owner bulletOwner, Vector2 travelDirection, float speed = -1f,
        int hitDamage = -1, float lifetime = -1f)
    {
        owner = bulletOwner;
        direction = travelDirection.sqrMagnitude > 0f ? travelDirection.normalized : Vector2.up;
        damage = hitDamage > 0 ? hitDamage : defaultDamage;
        lifetimeRemaining = lifetime > 0f ? lifetime : defaultLifetime;
        this.speed = speed > 0f ? speed : defaultSpeed;
        configured = true;
        gameObject.SetActive(true);
    }

    private float speed;

    private void OnEnable()
    {
        // A pooled projectile is configured immediately after Get().
        configured = false;
    }

    private void Update()
    {
        if (!configured) return;
        transform.position += (Vector3)(direction * (speed * Time.deltaTime));
        lifetimeRemaining -= Time.deltaTime;
        if (lifetimeRemaining <= 0f) ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == Owner.Player)
        {
            BossController boss = other.GetComponentInParent<BossController>();
            if (boss != null) { boss.TakeDamage(damage); ReturnToPool(); }
        }
        else
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null) { player.TakeDamage(damage); ReturnToPool(); }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner == Owner.Player)
        {
            BossController boss = other.GetComponentInParent<BossController>();
            if (boss != null) { boss.TakeDamage(damage); ReturnToPool(); }
        }
        else
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null) { player.TakeDamage(damage); ReturnToPool(); }
        }
    }

    public void ReturnToPool()
    {
        if (pool != null && gameObject.activeSelf) pool.Release(this);
        else gameObject.SetActive(false);
    }
}
