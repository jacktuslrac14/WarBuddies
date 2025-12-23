using UnityEngine;
using Mirror;
using UnityEngine.UI; 

public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;
    
    [Header("Boundary Settings")]
    // NILAKIHAN ANG DEFAULT: Mula 8 ginawang 100 para hindi ka maharangan
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
    private bool isGrounded;
    private float direction = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // SIGURADONG LILITAW: Pilitin ang Player sa Z=0
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
            // Check kung naka-pause (Menu)
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
        // PIGILAN ANG LOGIC KUNG NAKA-PAUSE (MENU)
        if (Time.timeScale == 0) return;

        if (healthSlider != null && healthSlider.transform.parent != null)
        {
            healthSlider.transform.parent.rotation = Quaternion.identity;
            Vector3 currentScale = transform.localScale;
            healthSlider.transform.parent.localScale = new Vector3(currentScale.x > 0 ? 1 : -1, 1, 1);
        }

        if (!isLocalPlayer) return;

        // Auto-assign camera target kung nag-resume na ang laro
        CameraFollow cam = Object.FindFirstObjectByType<CameraFollow>();
        if (cam != null && cam.target == null) cam.target = this.transform;

        HandleMovement();
        HandleJump();
        HandleShooting();

        // BOUNDARY FIX: Nililimitahan ang pwesto ng player
        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        transform.position = new Vector3(clampedX, transform.position.y, 0); 
    }

    void HandleMovement()
    {
        if (rb == null) return;
        float moveX = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(moveX * moveSpeed, rb.velocity.y);

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
            // SAFE CHECK: Para hindi mag-error ang laro kung walang bala
            if (bulletPrefab != null && shootPoint != null)
            {
                CmdShoot(direction);
            }
        }
    }

    [Command]
    void CmdShoot(float shootDir)
    {
        if (bulletPrefab == null) return;

        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
        
        // Pilitin ang bala na mapunta sa Z=0 para lilitaw ito sa screen
        bullet.transform.position = new Vector3(bullet.transform.position.x, bullet.transform.position.y, 0);

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null) bulletRb.velocity = new Vector2(shootDir * 15f, 0);

        NetworkServer.Spawn(bullet);
        Destroy(bullet, 2.0f);
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