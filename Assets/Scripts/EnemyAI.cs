using UnityEngine;
using Mirror;

public class EnemyAI : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float speed = 3f;
    private Transform target;
    public float searchInterval = 1f; // Mag-scan ng bagong target bawat segundo
    private float nextSearchTime = 0f;

    [Header("Attack Settings")]
    public int damageAmount = 20; 
    public float attackSpeed = 1.5f; 
    private float nextAttackTime = 0f;

    [ServerCallback] 
    void Update()
    {
        // STEP 1: Maghanap ng pinakamalapit na player periodically
        if (Time.time >= nextSearchTime)
        {
            FindClosestTarget();
            nextSearchTime = Time.time + searchInterval;
        }

        if (target == null) return;

        // STEP 2: Paggalaw papunta sa napiling target
        Vector2 direction = (target.position - transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        // Flip ang Sprite depende sa direction
        if (direction.x > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (direction.x < 0) transform.localScale = new Vector3(-1, 1, 1);
    }

    // BAGONG LOGIC: Humanap ng pinakamalapit na player sa lahat ng naka-connect
    void FindClosestTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float shortestDistance = Mathf.Infinity;
        GameObject closestPlayer = null;

        foreach (GameObject p in players)
        {
            // Siguraduhin na buhay pa ang player bago habulin (Optionally check livesLeft > 0)
            float distanceToPlayer = Vector2.Distance(transform.position, p.transform.position);
            
            if (distanceToPlayer < shortestDistance)
            {
                shortestDistance = distanceToPlayer;
                closestPlayer = p;
            }
        }

        if (closestPlayer != null)
        {
            target = closestPlayer.transform;
        }
    }

    [ServerCallback]
    void OnCollisionStay2D(Collision2D other)
    {
        // Check kung Player ang nabangga
        if (other.gameObject.CompareTag("Player") && Time.time >= nextAttackTime)
        {
            PlayerHealth playerHealth = other.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // TakeDamage ay tatakbo sa Server side
                playerHealth.TakeDamage(damageAmount);
                nextAttackTime = Time.time + attackSpeed;
            }
        }
    }

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