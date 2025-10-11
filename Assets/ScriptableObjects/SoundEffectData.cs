using UnityEngine;

namespace GameAudio
{
    [CreateAssetMenu(fileName = "SoundEffectData", menuName = "Audio/Sound Effect Data")]
    public class SoundEffectData : ScriptableObject
    {
        [Header("Audio Clips")]
        public AudioClip[] clips;

        [Header("Volume Settings")]
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0f, 0.2f)]
        public float volumeVariation = 0.05f;

        [Header("Pitch Settings")]
        [Range(0.1f, 3f)]
        public float pitch = 1f;
        [Range(0f, 0.2f)]
        public float pitchVariation = 0.05f;

        [Header("3D Settings")]
        public bool is3D = true;
        [Range(0f, 1f)]
        public float spatialBlend = 1f;
        public float minDistance = 1f;
        public float maxDistance = 50f;
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        [Header("Pooling")]
        public int poolSize = 5;

        [Header("Special Settings")]
        public bool loop = false;
        public float cooldown = 0f; // Minimum time between plays

        private float lastPlayTime;

        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        public float GetRandomVolume()
        {
            return volume + Random.Range(-volumeVariation, volumeVariation);
        }

        public float GetRandomPitch()
        {
            return pitch + Random.Range(-pitchVariation, pitchVariation);
        }

        public bool CanPlay()
        {
            if (cooldown <= 0) return true;
            return Time.time - lastPlayTime >= cooldown;
        }

        public void UpdateLastPlayTime()
        {
            lastPlayTime = Time.time;
        }
    }
}