using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
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

        SceneManager.LoadScene("AntonTestScene");

    }

    public void Options(string Options)
    {


    }
    public void OnpointerEnter(PointerEventData eventData)
    {



    }
    public void OnPointerExit(PointerEventData eventData)
    {



    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}




