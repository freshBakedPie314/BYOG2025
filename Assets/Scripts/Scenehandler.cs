using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class Scenehandler : MonoBehaviour
{
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject LoaderCanvas;
    public Slider progressSliderl;
   
    
    void Start()
    {
        LoaderCanvas.SetActive(false);
    }
    public void LoadGame()
    {
        LoadScene("SampleScene");
    }
    public void QuitGame()
    {
        Application.Quit();

    }

    public void LoadCredits()
    {
        SceneManager.LoadScene("End_Credits");

    }

    public async void LoadScene(string sceneName)
    {
        var scene = SceneManager.LoadSceneAsync(sceneName);
        scene.allowSceneActivation = false;
        LoaderCanvas.SetActive(true);
        await Task.Delay(3000);
        do
        {
            await Task.Delay(100);
            progressSliderl.value = scene.progress;
        } while (scene.progress < 0.9f);
        await Task.Delay(1000);
        //LoaderCanvas.SetActive(false);
        scene.allowSceneActivation = true;

    }
}
