using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class EndCreditsController : MonoBehaviour
{
    public RectTransform creditsContent;
    public float scrollSpeed = 50f;
    public TextAsset creditsFile; // optional text file
    public GameObject creditTextPrefab;
    private bool scrolling = true;

    void Start()
    {
        // Load credits dynamically
        GenerateCredits();

        // Start scrolling
        StartCoroutine(ScrollCredits());
    }

    void GenerateCredits()
    {
        if (creditsFile != null)
        {
            string[] lines = creditsFile.text.Split('\n');
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                GameObject txtObj = Instantiate(creditTextPrefab, creditsContent);
                txtObj.GetComponent<TMP_Text>().text = line.Trim();
            }
        }
        else
        {
            // Example fallback data
            List<string> credits = new List<string> {
                "Anshuman \"AnsxhuGod\" Behera - 3D",
                "Animesh \"\" Tripathy - Programming ",
                "Bidyendu \"RUSTGOD\" Das - Programming and 3D",
                "Aditya \"Enigma\" Rout - Programming",
                "Sumit \"ASURZURVAN\" Kumar Sahu - Sound",
                "Special Thanks: You!"
            };

            foreach (string entry in credits)
            {
                GameObject txtObj = Instantiate(creditTextPrefab, creditsContent);
                txtObj.GetComponent<TMP_Text>().text = entry;
            }
        }
    }

    System.Collections.IEnumerator ScrollCredits()
    {
        while (scrolling)
        {
            creditsContent.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;

            // stop when finished scrolling off screen
            if (creditsContent.anchoredPosition.y > creditsContent.sizeDelta.y + 1000f)
            {
                scrolling = false;
                EndCreditsFinished();
            }

            yield return null;
        }
    }

    void EndCreditsFinished()
    {
        // Fade to menu or restart
        Debug.Log("Credits finished!");
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
