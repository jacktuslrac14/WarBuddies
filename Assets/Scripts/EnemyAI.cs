using UnityEngine;
using Mirror;

public class EnemyAI : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float speed = 3f;
    private Transform target;

    [Header("Attack Settings")]
    public int damageAmount = 20; // Ginawa nating 20 para sa 100 HP bar
    public float attackSpeed = 1.5f; 
    private float nextAttackTime = 0f;

    void Start()
    {
        FindTarget();
    }

    [ServerCallback] 
    void Update()
    {
        if (target == null)
        {
            FindTarget();
            return;
        }

        // Paggalaw papunta sa Player
        Vector2 direction = (target.position - transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        // Flip ang Sprite depende sa direction
        if (direction.x > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (direction.x < 0) transform.localScale = new Vector3(-1, 1, 1);
    }

    void FindTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
    }

    [ServerCallback]
    void OnCollisionStay2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player") && Time.time >= nextAttackTime)
        {
            PlayerHealth playerHealth = other.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
                nextAttackTime = Time.time + attackSpeed;
            }
        }
    }

    // DITO ANG UPDATE: Kapag nawala ang zombie, bawasan ang counter sa WaveManager
    [ServerCallback]
    private void OnDestroy()
    {
        WaveManager waveManager = Object.FindFirstObjectByType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.ZombieDied();
        }
    }
}