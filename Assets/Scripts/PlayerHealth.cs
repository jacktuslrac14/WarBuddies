using UnityEngine;
using Mirror;
using System.Collections;
using TMPro;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Stats")]
    [SyncVar(hook = nameof(OnHPChanged))] public int currentHP = 100; 
    [SyncVar(hook = nameof(OnLivesChanged))] public int livesLeft = 5; 

    private bool isDead = false; 

    [Header("UI References")]
    public TextMeshProUGUI livesText; 
    public GameObject loseUI; 

    void Start()
    {
        // Hanapin ang Canvas sa scene
        GameObject canvasObj = GameObject.Find("Canvas");

        if (canvasObj != null)
        {
            // UPDATE: Hahanapin ang LosePanel kahit naka-disable ito sa Hierarchy
            if (loseUI == null)
            {
                Transform loseTransform = canvasObj.transform.Find("LosePanel");
                if (loseTransform != null) loseUI = loseTransform.gameObject;
            }

            // UPDATE: Hahanapin ang LivesText sa loob ng Canvas
            if (livesText == null)
            {
                Transform livesTransform = canvasObj.transform.Find("LivesText");
                if (livesTransform != null) livesText = livesTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        // Siguraduhin na tago ang Lose UI sa umpisa
        if (loseUI != null) loseUI.SetActive(false);
        
        UpdateLivesUI(livesLeft);
    }

    [Server]
    public void TakeDamage(int amount)
    {
        if (livesLeft <= 0 || isDead) return;
        currentHP -= amount;

        if (currentHP <= 0)
        {
            currentHP = 0; 
            isDead = true; 
            StartCoroutine(HostDeathSequence()); 
        }
    }

    IEnumerator HostDeathSequence()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.15f); 

        if (isServer)
        {
            livesLeft--; 

            if (livesLeft > 0)
            {
                RpcTeleportPlayer();
                currentHP = 100;
                yield return new WaitForSeconds(1.5f);
                isDead = false; 
            }
            else
            {
                RpcShowLoseScreen();
            }
        }
    }

    void OnLivesChanged(int oldLives, int newLives) => UpdateLivesUI(newLives);
    void OnHPChanged(int oldHP, int newHP) { }

    void UpdateLivesUI(int currentLives)
    {
        if (livesText != null) livesText.text = "LIVES: " + currentLives;
    }

    [ClientRpc]
    void RpcTeleportPlayer()
    {
        NetworkStartPosition[] spawnPoints = Object.FindObjectsByType<NetworkStartPosition>(FindObjectsSortMode.None);
        if (spawnPoints.Length > 0)
        {
            transform.position = spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position;
        }
    }

    [ClientRpc]
    void RpcShowLoseScreen()
    {
        // Double check kung nahanap ang UI object para sa clients
        if (loseUI == null)
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
            {
                Transform loseTransform = canvasObj.transform.Find("LosePanel");
                if (loseTransform != null) loseUI = loseTransform.gameObject;
            }
        }
        
        if (loseUI != null)
        {
            loseUI.SetActive(true); // Lalabas na ang YOU LOSE!
            Time.timeScale = 0; 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}