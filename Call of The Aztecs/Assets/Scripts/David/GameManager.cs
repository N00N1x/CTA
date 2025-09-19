using UnityEngine;
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
            Application.Quit();
        }

      
        public void LoadMenu(string menuSceneName)
        {
            SceneManager.LoadScene(menuSceneName);
        }

    public void StartGame(string Menu) // There was a imposter here >:(
    {

        SceneManager.LoadScene(Menu);


    }



    }





