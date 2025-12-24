using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Stats")]
    [SyncVar(hook = nameof(OnHPChanged))] public int currentHP = 100; 
    [SyncVar(hook = nameof(OnLivesChanged))] public int livesLeft = 5; 

    [SyncVar] private bool isInvincible = false; 
    private float nextDamageTime;
    public float damageCooldown = 0.8f; 

    [Header("UI References")]
    private TextMeshProUGUI livesText; 
    private GameObject loseUI;
    private GameObject winUI; 
    private Slider hpSlider; 

    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(DelayedFindUI());
    }

    IEnumerator DelayedFindUI()
    {
        yield return new WaitForSeconds(0.2f);
        FindUIElements();
        if (isLocalPlayer)
        {
            UpdateLivesUI(livesLeft);
            UpdateHealthBar(currentHP);
        }
    }

    void FindUIElements()
    {
        if (hpSlider == null) hpSlider = GetComponentInChildren<Slider>(true);
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj != null)
        {
            if (livesText == null) livesText = canvasObj.transform.Find("LivesText")?.GetComponent<TextMeshProUGUI>();
            if (loseUI == null) loseUI = canvasObj.transform.Find("LosePanel")?.gameObject;
            if (winUI == null) winUI = canvasObj.transform.Find("WinPanel")?.gameObject;
        }
    }

    // --- MGA FUNCTIONS NA HINAHANAP NG WAVEMANAGER ---
    public void ShowWinScreen() 
    { 
        if (winUI == null) FindUIElements();
        if (winUI != null) winUI.SetActive(true); 
        FreezePlayer(); 
    }

    public void ShowLoseScreen() 
    { 
        if (loseUI == null) FindUIElements();
        if (loseUI != null) loseUI.SetActive(true); 
        FreezePlayer(); 
    }

    void FreezePlayer()
    {
        var controller = GetComponent<MonoBehaviour>();
        if (controller != null) controller.enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) { rb.velocity = Vector2.zero; rb.simulated = false; }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    [Server]
    public void TakeDamage(int amount)
    {
        if (isInvincible || livesLeft <= 0) return;
        if (Time.time < nextDamageTime) return;
        nextDamageTime = Time.time + damageCooldown;
        currentHP -= amount;
        if (currentHP <= 0)
        {
            currentHP = 0; 
            isInvincible = true; 
            livesLeft--;
            if (livesLeft > 0) StartCoroutine(ServerRespawn());
            else FindObjectOfType<WaveManager>().EndGame(false);
        }
    }

    IEnumerator ServerRespawn()
    {
        currentHP = 100;
        RpcTeleportPlayer();
        yield return new WaitForSeconds(2.0f);
        isInvincible = false;
    }

    void OnLivesChanged(int oldL, int newL) { if (isLocalPlayer) UpdateLivesUI(newL); }
    void OnHPChanged(int oldH, int newH) { if (isLocalPlayer) UpdateHealthBar(newH); }
    void UpdateLivesUI(int currentLives) { if (livesText != null) livesText.text = "LIVES: " + currentLives; }
    void UpdateHealthBar(int hp) { if (hpSlider != null) hpSlider.value = hp; }

    [ClientRpc]
    void RpcTeleportPlayer()
    {
        NetworkStartPosition[] spawnPoints = Object.FindObjectsByType<NetworkStartPosition>(FindObjectsSortMode.None);
        if (spawnPoints.Length > 0) transform.position = spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position;
    }
}