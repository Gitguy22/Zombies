using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EventManager : MonoBehaviour
{
    [SerializeField] GameObject mainMenu;
    [SerializeField] GameObject levelSelect;
    [SerializeField] GameObject map1Description;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EnableMainMenu()
    {
        mainMenu.SetActive(true);
    }

    public void DisableMainMenu()
    {
        mainMenu.SetActive(false);
    }

    public void EnableLevelSelect()
    {
        levelSelect.SetActive(true);
    }

    public void DisableLevelSelect()
    {
        levelSelect.SetActive(false);
    }

    public void LoadMap1()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void EnableMap1Description()
    {
        map1Description.SetActive(true);
    }

        public void DisableMap1Description()
    {
        map1Description.SetActive(false);
    }
}
