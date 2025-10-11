using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public string gameScene;
    bool sound = true;
    public TextMeshProUGUI soundToggleText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameScene);
    }
    
    public void toggleSound()
    {
        sound = !sound;
        soundToggleText.text = sound ? "SOUND: ON" : "SOUND: OFF";
    }
}
