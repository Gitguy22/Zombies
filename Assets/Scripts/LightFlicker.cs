using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightFlicker : MonoBehaviour {

    public GameObject lightElement;
    public int flickerDelay = 5;
    public float lightMaxIntesity = 5.0f;
    private int lightRandom = 0;
    public ParticleSystem lightParticle;
    public Color32 onColor = new Color32(255,255,128,255);
    public Color32 offColor = new Color32(23, 20, 10, 255);

    // Use this for initialization
    void Start () {
        ParticleSystem lightParticle = GetComponent<ParticleSystem>();
	}
	
	// Update is called once per frame
	void Update () {
        // Debug.Log(lightRandom);
        //lightElement.GetComponent<Light>().intensity = 1;
        lightRandom = Random.Range(0 , flickerDelay);
        if (1 == lightRandom) // WHEN LIGHT IS OFF
            {
            lightElement.GetComponent<Light>().intensity = 0.0f;
            lightParticle.startColor = offColor;
        }

        if (2 == lightRandom) //  WHEN LIGHT IS ON
            {
            lightElement.GetComponent<Light>().intensity = lightMaxIntesity;
            lightParticle.startColor = onColor;
        }
        
    }
}
