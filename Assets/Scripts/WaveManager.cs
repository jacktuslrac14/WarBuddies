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

    [Header("Wave Settings")]
    [SyncVar] public int currentWave = 0;
    public int[] zombiesPerSide = { 2, 3, 4, 5, 6 }; 
    public float spawnInterval = 1.5f;
    public float waveBreakTime = 5f; 

    [SyncVar] public int totalZombiesAlive = 0;
    private bool isWaitingNextWave = false;
    private bool gameEnded = false;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (waveText != null) RpcHideWaveUI();
        if (countdownText != null) RpcHideCountdownUI(); 
        
        currentWave = 0;
        totalZombiesAlive = 0;
        gameEnded = false;
        StartCoroutine(StartNextWaveWithDelay());
    }

    // --- SOLUTION TO YOUR ERROR: CheckGameOverCondition ---
    [Server]
    public void CheckGameOverCondition()
    {
        if (gameEnded) return;

        PlayerHealth[] allPlayers = Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        bool anyoneAlive = false;

        foreach (PlayerHealth p in allPlayers)
        {
            if (p.livesLeft > 0)
            {
                anyoneAlive = true;
                break;
            }
        }

        if (!anyoneAlive)
        {
            EndGame(false);
        }
    }

    [Server]
    public void ZombieDied()
    {
        if (gameEnded) return;
        
        totalZombiesAlive--;
        if (totalZombiesAlive < 0) totalZombiesAlive = 0;

        Debug.Log($"Zombie Died. Remaining: {totalZombiesAlive}");

        if (totalZombiesAlive == 0 && !isWaitingNextWave)
        {
            GameObject[] remainingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (remainingEnemies.Length <= 1) 
            {
                currentWave++;
                if (currentWave < zombiesPerSide.Length)
                    StartCoroutine(StartNextWaveWithDelay());
                else
                    EndGame(true);
            }
        }
    }

    [Server]
    IEnumerator StartNextWaveWithDelay()
    {
        if (gameEnded) yield break;
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
        if (gameEnded) yield break;
        
        int countToSpawn = zombiesPerSide[currentWave];
        totalZombiesAlive = countToSpawn * 2; 

        for (int i = 0; i < countToSpawn; i++)
        {
            SpawnZombie(spawnLeft.position);
            SpawnZombie(spawnRight.position);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    [Server]
    void SpawnZombie(Vector3 pos)
    {
        if (zombiePrefab == null) return;
        GameObject zombie = Instantiate(zombiePrefab, pos, Quaternion.identity);
        NetworkServer.Spawn(zombie);
    }

    // --- UI RPCs ---
    [ClientRpc] void RpcUpdateCountdownUI(int t) { if (countdownText != null) { countdownText.gameObject.SetActive(true); countdownText.text = "NEXT WAVE IN: " + t; } }
    [ClientRpc] void RpcHideCountdownUI() { if (countdownText != null) countdownText.gameObject.SetActive(false); }
    [ClientRpc] void RpcHideWaveUI() { if (waveText != null) waveText.gameObject.SetActive(false); }
    [ClientRpc] void RpcShowWaveUI(int w) { if (waveText != null) StartCoroutine(FlashWaveText(w)); }
    
    IEnumerator FlashWaveText(int w) 
    { 
        waveText.text = "WAVE " + w; 
        waveText.gameObject.SetActive(true); 
        yield return new WaitForSeconds(2f); 
        waveText.gameObject.SetActive(false); 
    }

    [Server] public void EndGame(bool isWin) { if (gameEnded) return; gameEnded = true; RpcShowGameResult(isWin); }
    [ClientRpc] void RpcShowGameResult(bool isWin) { Time.timeScale = 0; PlayerHealth localPlayer = NetworkClient.localPlayer?.GetComponent<PlayerHealth>(); if (localPlayer != null) { if (isWin) localPlayer.ShowWinScreen(); else localPlayer.ShowLoseScreen(); } }
}