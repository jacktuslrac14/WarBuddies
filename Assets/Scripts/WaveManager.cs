using UnityEngine;
using Mirror;
using System.Collections;
using TMPro;

public class WaveManager : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject zombiePrefab;

    [Header("Spawn Points")]
    public Transform spawnLeft;
    public Transform spawnRight;

    [Header("UI Elements")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI countdownText; 
    public GameObject winUI; // I-drag ang WinPanel dito sa Inspector

    [Header("Wave Settings")]
    [SyncVar] public int currentWave = 0;
    public int[] zombiesPerSide = { 2, 3, 4, 5, 6 }; 
    public float spawnInterval = 1.5f;
    public float waveBreakTime = 5f; 

    [SyncVar] public int totalZombiesAlive = 0;
    private bool isSpawning = false;
    private bool isWaitingNextWave = false;

    void Start()
    {
        // 1. Siguraduhing nahanap ang WinPanel kahit naka-disable
        if (winUI == null)
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
            {
                Transform winTransform = canvasObj.transform.Find("WinPanel");
                if (winTransform != null) winUI = winTransform.gameObject;
            }
        }

        // 2. I-disable ang lahat ng UI sa simula para hindi humarang sa Start Button
        if (waveText != null) waveText.gameObject.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false); 
        if (winUI != null) winUI.SetActive(false); 

        if (isServer)
        {
            currentWave = 0;
            totalZombiesAlive = 0;
            StartCoroutine(StartNextWaveWithDelay());
        }
    }

    [Server]
    public void ZombieDied()
    {
        totalZombiesAlive--;
        if (totalZombiesAlive < 0) totalZombiesAlive = 0;

        // "Strict Check" para hindi mag-trigger ang win UI habang may zombie pa
        if (totalZombiesAlive == 0 && !isSpawning && !isWaitingNextWave)
        {
            // Siguraduhin na wala na talagang objects na may tag na "Enemy"
            if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
            {
                currentWave++;
                if (currentWave < zombiesPerSide.Length)
                {
                    StartCoroutine(StartNextWaveWithDelay());
                }
                else
                {
                    RpcShowWinScreen();
                }
            }
        }
    }

    [ClientRpc]
    void RpcShowWinScreen()
    {
        if (winUI == null)
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
            {
                Transform winTransform = canvasObj.transform.Find("WinPanel");
                if (winTransform != null) winUI = winTransform.gameObject;
            }
        }

        if (winUI != null)
        {
            winUI.SetActive(true); // Lalabas na ang YOU WIN!
            Time.timeScale = 0; 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // --- Nanatili ang Spawn at UI Countdown logic mo rito ---
    [Server]
    IEnumerator StartNextWaveWithDelay()
    {
        isWaitingNextWave = true;
        float timeLeft = waveBreakTime;
        while (timeLeft > 0)
        {
            RpcUpdateCountdownUI(Mathf.CeilToInt(timeLeft)); 
            yield return new WaitForSeconds(1f);
            timeLeft--;
        }
        RpcHideCountdownUI(); 
        RpcShowWaveUI(currentWave + 1);
        yield return new WaitForSeconds(2f);
        isWaitingNextWave = false;
        StartCoroutine(SpawnWaveRoutine());
    }

    [Server]
    IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        int countToSpawn = zombiesPerSide[currentWave];
        for (int i = 0; i < countToSpawn; i++)
        {
            SpawnZombie(spawnLeft.position);
            SpawnZombie(spawnRight.position);
            yield return new WaitForSeconds(spawnInterval);
        }
        isSpawning = false; 
    }

    [Server]
    void SpawnZombie(Vector3 pos)
    {
        GameObject zombie = Instantiate(zombiePrefab, pos, Quaternion.identity);
        NetworkServer.Spawn(zombie);
        totalZombiesAlive++;
    }

    [ClientRpc] void RpcUpdateCountdownUI(int t) { if (countdownText != null) { countdownText.gameObject.SetActive(true); countdownText.text = "NEXT WAVE IN: " + t; } }
    [ClientRpc] void RpcHideCountdownUI() { if (countdownText != null) countdownText.gameObject.SetActive(false); }
    [ClientRpc] void RpcShowWaveUI(int w) { if (waveText != null) StartCoroutine(FlashWaveText(w)); }
    IEnumerator FlashWaveText(int w) { waveText.text = "WAVE " + w; waveText.gameObject.SetActive(true); yield return new WaitForSeconds(2f); waveText.gameObject.SetActive(false); }
}