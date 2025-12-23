using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class EnemyHealth : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int health = 100;
    
    public Slider healthSlider;

    void Start()
    {
        // Kusang hahanapin ang Slider para hindi mo na kailangang i-drag
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
        health -= amount;
        if (health <= 0)
        {
            WaveManager wm = Object.FindFirstObjectByType<WaveManager>();
            if (wm != null) wm.ZombieDied();
            NetworkServer.Destroy(gameObject);
        }
    }

    void OnHealthChanged(int oldHealth, int newHealth)
    {
        if (healthSlider != null) healthSlider.value = newHealth;
    }
}