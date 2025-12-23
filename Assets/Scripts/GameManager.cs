using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public void RetryGame()
    {
        // 1. Ibalik ang oras sa normal (dahil naka-pause sa Win/Lose)
        Time.timeScale = 1f;

        // 2. I-check kung Host o Client
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            // Kung ikaw ang Host, i-restart ang scene para sa lahat
            NetworkManager.singleton.ServerChangeScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            // Kung sakaling client lang, i-reload ang current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit(); // Gagana lang ito kapag naka-build na ang laro
    }
}