using UnityEngine;
using Mirror;
using UnityEngine.UI; 

public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;
    
    [Header("Boundary Settings")]
    public float minX = -100f; 
    public float maxX = 100f;  

    [Header("Combat Settings")]
    public GameObject bulletPrefab;
    public Transform shootPoint;

    [Header("Health Settings")]
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int health = 100;
    public float damageCooldown = 1.0f; 
    public Slider healthSlider; 
    private float lastDamageTime;

    private Rigidbody2D rb;
    private Animator anim; // Para sa animations
    private bool isGrounded;
    private float direction = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>(); // Kunin ang animator component
        
        transform.position = new Vector3(transform.position.x, transform.position.y, 0);

        if (healthSlider != null)
        {
            healthSlider.maxValue = 100;
            healthSlider.value = health;
            healthSlider.transform.localScale = Vector3.one;

            Image bg = healthSlider.transform.Find("Background")?.GetComponent<Image>();
            Image fill = healthSlider.fillRect?.GetComponent<Image>();
            
            if (bg != null) { bg.sprite = null; bg.color = Color.black; }
            if (fill != null) fill.sprite = null;

            RectTransform fillArea = healthSlider.transform.Find("Fill Area")?.GetComponent<RectTransform>();
            if (fillArea != null)
            {
                fillArea.offsetMin = Vector2.zero;
                fillArea.offsetMax = Vector2.zero;
            }
        }

        Canvas playerCanvas = GetComponentInChildren<Canvas>();
        if (playerCanvas != null)
        {
            playerCanvas.worldCamera = Camera.main;
            playerCanvas.sortingLayerName = "UI";
            playerCanvas.sortingOrder = 10;
        }

        if (isLocalPlayer)
        {
            gameObject.tag = "Player";
            if (Time.timeScale != 0) SetCameraTarget();
        }
    }

    void SetCameraTarget()
    {
        CameraFollow cam = Object.FindFirstObjectByType<CameraFollow>();
        if (cam != null) cam.target = this.transform;
    }

    void Update()
    {
        if (Time.timeScale == 0) return;

        if (healthSlider != null && healthSlider.transform.parent != null)
        {
            healthSlider.transform.parent.rotation = Quaternion.identity;
            Vector3 currentScale = transform.localScale;
            healthSlider.transform.parent.localScale = new Vector3(currentScale.x > 0 ? 1 : -1, 1, 1);
        }

        if (!isLocalPlayer) return;

        CameraFollow cam = Object.FindFirstObjectByType<CameraFollow>();
        if (cam != null && cam.target == null) cam.target = this.transform;

        HandleMovement();
        HandleJump();
        HandleShooting();

        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        transform.position = new Vector3(clampedX, transform.position.y, 0); 
    }

    void HandleMovement()
    {
        if (rb == null) return;
        float moveX = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(moveX * moveSpeed, rb.velocity.y);

        // SYNC WALK/IDLE ANIMATION
        if (anim != null)
        {
            // Gamitin ang velocity magnitude para sa Speed parameter
            anim.SetFloat("Speed", Mathf.Abs(moveX)); 
        }

        if (moveX > 0) 
        {
            direction = 1f;
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (moveX < 0) 
        {
            direction = -1f;
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    void HandleJump()
    {
        if (rb == null) return;
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            isGrounded = false;
        }
    }

    void HandleShooting()
    {
        if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
        {
            if (bulletPrefab != null && shootPoint != null)
            {
                CmdShoot(direction);
                // I-play ang animation sa local player agad para mabilis ang response
                if (anim != null) anim.SetTrigger("Shoot");
            }
        }
    }

    [Command]
    void CmdShoot(float shootDir)
    {
        if (bulletPrefab == null) return;

        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
        bullet.transform.position = new Vector3(bullet.transform.position.x, bullet.transform.position.y, 0);

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null) bulletRb.velocity = new Vector2(shootDir * 15f, 0);

        NetworkServer.Spawn(bullet);
        
        // I-sync ang putok sa ibang players
        RpcOnShoot();
        
        Destroy(bullet, 2.0f);
    }

    [ClientRpc]
    void RpcOnShoot()
    {
        // I-play ang shoot animation sa lahat maliban sa local player (kasi nag-play na sa local)
        if (!isLocalPlayer && anim != null)
        {
            anim.SetTrigger("Shoot");
        }
    }

    [Server]
    public void TakeDamage(int amount)
    {
        if (Time.time < lastDamageTime + damageCooldown) return;

        lastDamageTime = Time.time;
        health -= amount;
        health = Mathf.Max(0, health);

        if (health <= 0) Respawn();
    }

    [Server]
    void Respawn()
    {
        health = 100;
        Transform startPos = NetworkManager.singleton.GetStartPosition();
        transform.position = startPos != null ? startPos.position : Vector3.zero;
    }

    void OnHealthChanged(int oldHealth, int newHealth)
    {
        if (healthSlider != null) healthSlider.value = newHealth;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Floor")) isGrounded = true;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isServer && collision.gameObject.CompareTag("Enemy")) TakeDamage(10);
    }
}