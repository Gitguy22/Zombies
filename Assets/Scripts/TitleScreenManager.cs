using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleScreenManager : MonoBehaviour
{
    public AudioSource audio;
    public Image image;

    [Header("Fade Settings")]
    public float fadeDuration = 1.5f;

    private float originalVolume;

    // Start is called before the first frame update
    void Start()
    {
        // Store the original audio volume
        if (audio != null)
            originalVolume = audio.volume;

        // Make sure the fade image starts transparent if it should
        if (image != null)
            image.color = new Color(image.color.r, image.color.g, image.color.b, 0f);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public IEnumerator FadeToMenuScene()
    {
        float elapsedTime = 0f;

        // Get starting values
        float startVolume = audio != null ? audio.volume : 0f;
        float startAlpha = image != null ? image.color.a : 0f;

        // Fade out audio and fade in black screen
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;

            // Fade out audio volume
            if (audio != null)
            {
                audio.volume = Mathf.Lerp(startVolume, 0f, t);
            }

            // Fade in black screen (increase alpha)
            if (image != null)
            {
                Color currentColor = image.color;
                currentColor.a = Mathf.Lerp(startAlpha, 1f, t);
                image.color = currentColor;
            }

            yield return null;
        }

        // Ensure final values are set
        if (audio != null)
            audio.volume = 0f;

        if (image != null)
        {
            Color finalColor = image.color;
            finalColor.a = 1f;
            image.color = finalColor;
        }

        // Wait a brief moment before loading scene
        yield return new WaitForSeconds(0.2f);

        // Load the menu scene
        SceneManager.LoadScene("Menu");
    }

    public void LoadMenuScene()
    {
        Debug.Log("LoadMenuScene called - starting fade");
        // Start the fade coroutine instead of directly loading the scene
        StartCoroutine(FadeToMenuScene());
    }
}