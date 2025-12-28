using UnityEngine;
using Mirror;

public class EnemyAI : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float speed = 3f;
    private Transform target;
    public float searchInterval = 1f; 
    private float nextSearchTime = 0f;

    [Header("Attack Settings")]
    public int damageAmount = 20; 
    public float attackSpeed = 1.5f; 
    private float nextAttackTime = 0f;
    public float attackRange = 1.5f; 

    // SYNCING LOGIC: Ginagamit ang SyncVar para i-update ang lahat ng clients
    [SyncVar(hook = nameof(OnScaleChanged))]
    private Vector3 syncScale = Vector3.one;

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Ang hook na ito ay tatakbo sa bawat Client kapag nagbago ang syncScale sa Server
    void OnScaleChanged(Vector3 oldScale, Vector3 newScale)
    {
        transform.localScale = newScale;
    }

    [ServerCallback] 
    void Update()
    {
        if (Time.time >= nextSearchTime)
        {
            FindClosestTarget();
            nextSearchTime = Time.time + searchInterval;
        }

        if (target == null) 
        {
            if (anim != null) anim.SetFloat("Speed", 0f);
            return;
        }

        float distance = Vector2.Distance(transform.position, target.position);

        if (distance > attackRange)
        {
            transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
            if (anim != null) anim.SetFloat("Speed", speed);
        }
        else 
        {
            if (anim != null) anim.SetFloat("Speed", 0f);

            if (Time.time >= nextAttackTime)
            {
                if (anim != null) anim.SetTrigger("Attack");
                
                PlayerHealth ph = target.GetComponent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(damageAmount);
                
                nextAttackTime = Time.time + attackSpeed;
            }
        }

        // FLIP LOGIC FIX: Ina-update ang SyncVar sa Server
        if (target != null)
        {
            if (target.position.x > transform.position.x)
            {
                syncScale = new Vector3(1, 1, 1);
            }
            else if (target.position.x < transform.position.x)
            {
                syncScale = new Vector3(-1, 1, 1);
            }
        }
    }

    void FindClosestTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float shortestDistance = Mathf.Infinity;
        GameObject closestPlayer = null;

        foreach (GameObject p in players)
        {
            PlayerHealth ph = p.GetComponent<PlayerHealth>();
            if (ph != null && ph.livesLeft <= 0) continue; 
            float d = Vector2.Distance(transform.position, p.transform.position);
            if (d < shortestDistance) { shortestDistance = d; closestPlayer = p; }
        }
        if (closestPlayer != null) target = closestPlayer.transform;
        else target = null;
    }
}