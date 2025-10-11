using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GameAudio
{
    public class SoundEffectsManager : MonoBehaviour
    {
        private static SoundEffectsManager instance;
        public static SoundEffectsManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<SoundEffectsManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("SoundEffectsManager");
                        instance = go.AddComponent<SoundEffectsManager>();
                    }
                }
                return instance;
            }
        }

        [Header("Audio Mixer")]
        [SerializeField] AudioMixerGroup sfxMixerGroup;
        [SerializeField] AudioMixerGroup musicMixerGroup;
        [SerializeField] AudioMixerGroup masterMixerGroup;

        [Header("Pool Settings")]
        [SerializeField] int defaultPoolSize = 20;
        [SerializeField] GameObject audioSourcePrefab;

        [Header("Debug")]
        [SerializeField] bool showDebugLogs = false;

        private Dictionary<SoundEffectData, Queue<AudioSource>> soundPools;
        private List<AudioSource> activeAudioSources;
        private Transform poolParent;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        void Initialize()
        {
            soundPools = new Dictionary<SoundEffectData, Queue<AudioSource>>();
            activeAudioSources = new List<AudioSource>();

            // Create pool parent
            poolParent = new GameObject("AudioSourcePool").transform;
            poolParent.SetParent(transform);

            // Create default audio source prefab if not assigned
            if (audioSourcePrefab == null)
            {
                audioSourcePrefab = new GameObject("AudioSourceTemplate");
                audioSourcePrefab.AddComponent<AudioSource>();
                audioSourcePrefab.SetActive(false);
            }
        }

        public AudioSource PlaySound(SoundEffectData soundData, Vector3 position = default)
        {
            if (soundData == null || !soundData.CanPlay()) return null;

            AudioClip clip = soundData.GetRandomClip();
            if (clip == null) return null;

            AudioSource source = GetPooledAudioSource(soundData);
            if (source == null) return null;

            // Configure audio source
            ConfigureAudioSource(source, soundData, clip, position);

            // Play the sound
            source.Play();
            soundData.UpdateLastPlayTime();

            if (!soundData.loop)
            {
                StartCoroutine(ReturnToPoolAfterPlay(source, clip.length, soundData));
            }

            if (showDebugLogs)
                Debug.Log($"Playing sound: {clip.name} at {position}");

            return source;
        }

        public AudioSource PlaySoundAttached(SoundEffectData soundData, Transform parent, Vector3 localPosition = default)
        {
            AudioSource source = PlaySound(soundData, parent.position + localPosition);
            if (source != null)
            {
                source.transform.SetParent(parent);
                source.transform.localPosition = localPosition;
            }
            return source;
        }

        public void StopSound(AudioSource source)
        {
            if (source != null && source.isPlaying)
            {
                source.Stop();
                ReturnToPool(source, GetSoundDataFromSource(source));
            }
        }

        public void PlayOneShot(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;

            // Create temporary sound data for one-shot clips
            GameObject tempGO = new GameObject("OneShotAudio");
            tempGO.transform.position = position;
            AudioSource tempSource = tempGO.AddComponent<AudioSource>();

            tempSource.clip = clip;
            tempSource.volume = volume;
            tempSource.spatialBlend = 1f;
            tempSource.outputAudioMixerGroup = sfxMixerGroup;
            tempSource.Play();

            Destroy(tempGO, clip.length);
        }

        AudioSource GetPooledAudioSource(SoundEffectData soundData)
        {
            if (!soundPools.ContainsKey(soundData))
            {
                CreatePool(soundData);
            }

            Queue<AudioSource> pool = soundPools[soundData];

            if (pool.Count > 0)
            {
                AudioSource source = pool.Dequeue();
                source.gameObject.SetActive(true);
                activeAudioSources.Add(source);
                return source;
            }
            else
            {
                // Expand pool if needed
                return CreateNewAudioSource(soundData);
            }
        }

        void CreatePool(SoundEffectData soundData)
        {
            Queue<AudioSource> pool = new Queue<AudioSource>();

            int poolSize = soundData.poolSize > 0 ? soundData.poolSize : defaultPoolSize;

            for (int i = 0; i < poolSize; i++)
            {
                AudioSource source = CreateNewAudioSource(soundData);
                source.gameObject.SetActive(false);
                activeAudioSources.Remove(source);
                pool.Enqueue(source);
            }

            soundPools[soundData] = pool;
        }

        AudioSource CreateNewAudioSource(SoundEffectData soundData)
        {
            GameObject go = Instantiate(audioSourcePrefab, poolParent);
            go.name = $"AudioSource_{soundData.name}";

            AudioSource source = go.GetComponent<AudioSource>();
            if (source == null)
                source = go.AddComponent<AudioSource>();

            // Store reference to sound data
            go.tag = "PooledAudio";

            activeAudioSources.Add(source);

            return source;
        }

        void ConfigureAudioSource(AudioSource source, SoundEffectData soundData, AudioClip clip, Vector3 position)
        {
            source.clip = clip;
            source.volume = soundData.GetRandomVolume();
            source.pitch = soundData.GetRandomPitch();
            source.loop = soundData.loop;

            // 3D settings
            source.spatialBlend = soundData.is3D ? soundData.spatialBlend : 0f;
            source.minDistance = soundData.minDistance;
            source.maxDistance = soundData.maxDistance;
            source.rolloffMode = soundData.rolloffMode;

            // Position
            source.transform.position = position;
            source.transform.SetParent(poolParent);

            // Mixer group
            source.outputAudioMixerGroup = sfxMixerGroup;
        }

        System.Collections.IEnumerator ReturnToPoolAfterPlay(AudioSource source, float delay, SoundEffectData soundData)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool(source, soundData);
        }

        void ReturnToPool(AudioSource source, SoundEffectData soundData)
        {
            if (source == null) return;

            source.Stop();
            source.clip = null;
            source.transform.SetParent(poolParent);
            source.gameObject.SetActive(false);

            activeAudioSources.Remove(source);

            if (soundData != null && soundPools.ContainsKey(soundData))
            {
                soundPools[soundData].Enqueue(source);
            }
        }

        SoundEffectData GetSoundDataFromSource(AudioSource source)
        {
            // In a real implementation, you might store this mapping
            // For now, return null and let the source be destroyed
            return null;
        }

        public void StopAllSounds()
        {
            foreach (AudioSource source in activeAudioSources.ToArray())
            {
                if (source != null && source.isPlaying)
                {
                    source.Stop();
                }
            }
        }

        public void PauseAllSounds()
        {
            foreach (AudioSource source in activeAudioSources)
            {
                if (source != null && source.isPlaying)
                {
                    source.Pause();
                }
            }
        }

        public void UnpauseAllSounds()
        {
            foreach (AudioSource source in activeAudioSources)
            {
                if (source != null)
                {
                    source.UnPause();
                }
            }
        }

        void OnDestroy()
        {
            StopAllSounds();
        }
    }
}