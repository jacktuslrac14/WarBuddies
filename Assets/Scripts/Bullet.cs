using UnityEngine;
using Mirror;

public class Bullet : NetworkBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    public float destroyTime = 2f;
    
    private bool spent = false; // Safety flag para hindi tumagos sa iba

    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null && rb.velocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        if (isServer) Invoke(nameof(DestroySelf), destroyTime);
    }

    [ServerCallback]
    void OnTriggerEnter2D(Collider2D other)
    {
        // Kung nagamit na ang bala sa frame na ito, huwag nang ituloy
        if (spent) return;

        if (other.CompareTag("Enemy"))
        {
            EnemyHealth health = other.GetComponent<EnemyHealth>();
            if (health != null)
            {
                spent = true; // I-mark as spent agad para hindi na makatama ng iba
                health.TakeDamage(damage);
                Debug.Log("Hit " + other.name + ". Damage dealt: " + damage);
            }
            DestroySelf();
        }
    }

    [Server]
    void DestroySelf()
    {
        NetworkServer.Destroy(gameObject); // Siguradong burado sa lahat ng clients
    }
}