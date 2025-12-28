using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class EnemyHealth : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int health = 100;
    
    public Slider healthSlider;
    private bool hasSentDeathSignal = false; // FLAG: Para hindi mag-negative ang wave count

    void Start()
    {
        if (healthSlider == null) healthSlider = GetComponentInChildren<Slider>();
        if (healthSlider != null)
        {
            healthSlider.maxValue = health;
            healthSlider.value = health;
        }
    }

    [Server]
    public void TakeDamage(int amount)
    {
        if (health <= 0) return;

        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    [Server]
    void Die()
    {
        if (hasSentDeathSignal) return;
        hasSentDeathSignal = true;

        WaveManager wm = Object.FindFirstObjectByType<WaveManager>();
        if (wm != null) 
        {
            wm.ZombieDied();
            Debug.Log("Zombie Died - Signal sent to WaveManager");
        }

        NetworkServer.Destroy(gameObject);
    }

    void OnHealthChanged(int oldHealth, int newHealth)
    {
        if (healthSlider != null) healthSlider.value = newHealth;
    }
}