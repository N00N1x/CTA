using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public void RestartGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);

    }

    public void QuitGame()
    {
        Debug.Log("You pressed the Quit Button");

            Application.Quit();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
    }

    public void LoadMenu(string menuSceneName)
    {
        SceneManager.LoadScene(menuSceneName);

    }

    public void StartGame(string Level0) 
    {

        SceneManager.LoadScene("Level0");

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




