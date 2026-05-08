using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Debug.Log("You pressed the Quit Button");

        Application.Quit();
    }

    public void LoadMenu(string menuSceneName)
    {
        SceneManager.LoadScene(menuSceneName);

    }

    public void StartGame(string Level0)
    {

        SceneManager.LoadScene("AssembleScene");

    }

    public void MainMenu(string MainMenu)
    {

        SceneManager.LoadScene("MainMenu");
    }


    public void Options(string Options)
    {

        SceneManager.LoadScene("Options");
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}