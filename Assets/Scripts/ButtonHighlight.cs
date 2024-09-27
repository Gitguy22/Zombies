using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public Color normalTextColor = Color.white;
    public Color highlightedTextColor = Color.red;
    public AudioClip selectSound;  // Assign in Inspector for selection sound
    public AudioClip clickSound;   // Assign in Inspector for click sound
    private AudioSource audioSource;
    private TMP_Text buttonText;

    void Start()
    {
        buttonText = GetComponentInChildren<TMP_Text>();
        buttonText.color = normalTextColor;
        audioSource = GetComponent<AudioSource>(); // Make sure to attach AudioSource to the button
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        buttonText.color = highlightedTextColor;
        PlaySound(selectSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        buttonText.color = normalTextColor;
    }

    public void OnSelect(BaseEventData eventData)
    {
        buttonText.color = highlightedTextColor;
        PlaySound(selectSound);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        buttonText.color = normalTextColor;
    }

    // Play sound when the button is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        PlaySound(clickSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}