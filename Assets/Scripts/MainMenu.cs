using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    //Yeni Oyun
    public void PlayNewGame()
    {
        SaveManager.NewGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
    //Eski Oyundan Devam
    public void ContinueGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
    //Oyundan Çýk
    public void QuitTheGame()
    {
        Application.Quit();
    }
}
