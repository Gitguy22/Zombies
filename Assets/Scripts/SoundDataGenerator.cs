using UnityEngine;
using System.Collections.Generic;
using GameAudio;

[System.Serializable]
public class SoundMapping
{
    public string fieldName;
    public AudioClip audioClip;
    public AudioClip[] audioClips;
    public SoundEffectData generatedData;
    public bool isArray;

    public SoundMapping(string name, AudioClip clip)
    {
        fieldName = name;
        audioClip = clip;
        isArray = false;
    }

    public SoundMapping(string name, AudioClip[] clips)
    {
        fieldName = name;
        audioClips = clips;
        isArray = true;
    }
}

public class SoundDataGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public string targetFolderPath = "Assets/Audio/SoundEffectData";
    public bool autoAssignToComponents = true;
    public bool useDefaultSettings = true;

    [Header("Default Sound Settings")]
    public float defaultVolume = 1f;
    public float defaultVolumeVariation = 0.05f;
    public float defaultPitch = 1f;
    public float defaultPitchVariation = 0.05f;
    public bool defaultIs3D = true;
    public float defaultMinDistance = 1f;
    public float defaultMaxDistance = 50f;
    public int defaultPoolSize = 5;

    [Header("Found Audio Clips")]
    public List<SoundMapping> foundSounds = new List<SoundMapping>();

    [Header("Generated Data")]
    public List<SoundEffectData> generatedSoundData = new List<SoundEffectData>();
}