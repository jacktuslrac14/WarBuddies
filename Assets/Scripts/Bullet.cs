using UnityEngine;
using Mirror;

public class Bullet : NetworkBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    public float destroyTime = 2f;

    void Start()
    {
        // Sisirain ang bala pagkatapos ng ilang segundo para hindi mag-lag
        if (isServer) Invoke(nameof(DestroySelf), destroyTime);
    }

    [ServerCallback]
    void OnTriggerEnter2D(Collider2D other)
    {
        // Dapat ang Zombie mo ay may Tag na "Enemy"
        if (other.CompareTag("Enemy"))
        {
            EnemyHealth health = other.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
            DestroySelf();
        }
    }

    [Server]
    void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }
}