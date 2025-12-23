using UnityEngine;
using Mirror;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject startMenuUI; 
    public GameObject gameLogo;    

    void Start()
    {
        // KUSANG HAHANAPIN ANG LOGO: Kung walang naka-drag, hahanapin nito ang "GameLogo"
        if (gameLogo == null)
        {
            gameLogo = GameObject.Find("GameLogo"); 
        }
        
        // KUSANG HAHANAPIN ANG START BUTTON: Kung wala ring naka-drag
        if (startMenuUI == null)
        {
            startMenuUI = GameObject.Find("StartButton");
        }

        // Ipakita ang menu at logo sa simula
        if (startMenuUI != null) startMenuUI.SetActive(true);
        if (gameLogo != null) gameLogo.SetActive(true);
        
        // I-pause ang laro hanggang hindi pa napipindot ang Start
        Time.timeScale = 0; 
    
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void StartGame()
    {
        // Siguraduhing mahanap uli bago i-disable para walang error
        if (gameLogo == null) gameLogo = GameObject.Find("GameLogo");
        if (startMenuUI == null) startMenuUI = GameObject.Find("StartButton");

        // Itatago na ang UI at Logo pag-click ng button
        if (startMenuUI != null) startMenuUI.SetActive(false);
        if (gameLogo != null) gameLogo.SetActive(false);
        
        // I-RESUME ANG LARO
        Time.timeScale = 1; 
                
        // Panatilihing visible ang cursor para sa Network Manager
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}