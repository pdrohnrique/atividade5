using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public sealed class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField, Range(0f, 0.5f)] private float lowerHalfTop = 0.5f;
    [SerializeField] private Vector2 viewportPadding = new Vector2(0.04f, 0.04f);
    [Header("Shooting")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField, Range(0.2f, 1f)] private float shotInterval = 0.35f;
    [SerializeField] private float bulletSpeed = 12f;
    [SerializeField] private int bulletDamage = 1;
    [SerializeField] private int poolDefaultCapacity = 12;
    [SerializeField] private int poolMaxSize = 48;
    [Header("Lives")]
    [SerializeField, Min(1)] private int maxHits = 4;

    private ObjectPool<Bullet> bulletPool;
    private int hitsTaken;
    private float nextShotTime;
    private bool isDead;
    private Rigidbody2D body;
    private Vector2 moveInput;

    // HUD has three icons; the fourth hit is the fatal hit described by the assignment.
    public int RemainingLives => Mathf.Clamp(3 - hitsTaken, 0, 3);
    public bool IsDead => isDead;

    private void Awake()
    {
        if (gameplayCamera == null) gameplayCamera = Camera.main;
        body = GetComponent<Rigidbody2D>();
        bulletPool = new ObjectPool<Bullet>(CreateBullet, OnTakeBullet, OnReleaseBullet,
            OnDestroyBullet, true, Mathf.Max(1, poolDefaultCapacity), Mathf.Max(1, poolMaxSize));
        if (maxHits < 4) maxHits = 4;
    }

    private void Update()
    {
        if (isDead || (GameManager.Instance != null && !GameManager.Instance.GameplayActive)) return;
        moveInput = ReadMovement().normalized;
        if (IsFiring() && Time.time >= nextShotTime) Shoot();
    }

    private static Vector2 ReadMovement()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;
        float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
        return new Vector2(x, y);
    }

    private static bool IsFiring()
    {
        Keyboard keyboard = Keyboard.current;
        return (keyboard != null && (keyboard.spaceKey.isPressed || keyboard.leftCtrlKey.isPressed))
            || (Mouse.current != null && Mouse.current.leftButton.isPressed);
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        Vector2 next = (Vector2)transform.position + moveInput * (moveSpeed * Time.fixedDeltaTime);
        next = ClampToLowerScreen(next);
        if (body != null) body.MovePosition(next);
        else transform.position = next;
    }

    private Vector2 ClampToLowerScreen(Vector2 position)
    {
        if (gameplayCamera == null) return position;
        float depth = Mathf.Abs(gameplayCamera.transform.position.z - transform.position.z);
        Vector3 bottomLeft = gameplayCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
        Vector3 topRight = gameplayCamera.ViewportToWorldPoint(new Vector3(1f, lowerHalfTop, depth));
        float halfWidth = 0.5f;
        float halfHeight = 0.5f;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            halfWidth = col.bounds.extents.x;
            halfHeight = col.bounds.extents.y;
        }
        return new Vector2(
            Mathf.Clamp(position.x, bottomLeft.x + halfWidth + viewportPadding.x, topRight.x - halfWidth - viewportPadding.x),
            Mathf.Clamp(position.y, bottomLeft.y + halfHeight + viewportPadding.y, topRight.y - halfHeight - viewportPadding.y));
    }

    private void Shoot()
    {
        nextShotTime = Time.time + Mathf.Clamp(shotInterval, 0.2f, 1f);
        Bullet bullet = bulletPool.Get();
        bullet.transform.SetPositionAndRotation(muzzle != null ? muzzle.position : transform.position, Quaternion.identity);
        bullet.Launch(Bullet.Owner.Player, Vector2.up, bulletSpeed, bulletDamage);
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

    public void TakeDamage(int damage = 1)
    {
        if (isDead || damage <= 0) return;
        hitsTaken = Mathf.Min(maxHits, hitsTaken + damage);
        if (GameManager.Instance != null) GameManager.Instance.UpdateLives(RemainingLives);
        if (hitsTaken >= maxHits)
        {
            isDead = true;
            if (body != null) body.linearVelocity = Vector2.zero;
            if (GameManager.Instance != null) GameManager.Instance.ShowDefeat();
            else gameObject.SetActive(false);
        }
    }
}
