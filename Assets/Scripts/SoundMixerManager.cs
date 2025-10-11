using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

namespace GameAudio
{
    public class SoundMixerManager : MonoBehaviour
    {
        private static SoundMixerManager instance;
        public static SoundMixerManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<SoundMixerManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("SoundMixerManager");
                        instance = go.AddComponent<SoundMixerManager>();
                    }
                }
                return instance;
            }
        }

        [Header("Audio Mixer")]
        [SerializeField] AudioMixer mainMixer;

        [Header("Snapshot Transitions")]
        [SerializeField] AudioMixerSnapshot defaultSnapshot;
        [SerializeField] AudioMixerSnapshot pausedSnapshot;
        [SerializeField] AudioMixerSnapshot combatSnapshot;
        [SerializeField] float snapshotTransitionTime = 0.5f;

        [Header("Volume Settings")]
        [SerializeField] AnimationCurve volumeFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Music System")]
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioClip[] musicTracks;
        [SerializeField] float musicFadeTime = 2f;

        private Coroutine currentFadeCoroutine;
        private int currentMusicIndex = -1;

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
            // Load saved audio settings
            LoadAudioSettings();

            // Setup music source if not assigned
            if (musicSource == null)
            {
                GameObject musicGO = new GameObject("MusicSource");
                musicGO.transform.SetParent(transform);
                musicSource = musicGO.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.spatialBlend = 0f; // 2D sound
            }

            // Start default music
            if (musicTracks != null && musicTracks.Length > 0)
            {
                PlayMusic(0);
            }
        }

        void LoadAudioSettings()
        {
            SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 0.75f));
            SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 0.75f));
            SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 0.75f));
        }

        public void SetMasterVolume(float normalizedVolume)
        {
            float dbVolume = NormalizedToDecibels(normalizedVolume);
            mainMixer.SetFloat("MasterVolume", dbVolume);
        }

        public void SetSFXVolume(float normalizedVolume)
        {
            float dbVolume = NormalizedToDecibels(normalizedVolume);
            mainMixer.SetFloat("SFXVolume", dbVolume);
        }

        public void SetMusicVolume(float normalizedVolume)
        {
            float dbVolume = NormalizedToDecibels(normalizedVolume);
            mainMixer.SetFloat("MusicVolume", dbVolume);
        }

        public float GetMasterVolume()
        {
            float dbVolume;
            mainMixer.GetFloat("MasterVolume", out dbVolume);
            return DecibelsToNormalized(dbVolume);
        }

        public float GetSFXVolume()
        {
            float dbVolume;
            mainMixer.GetFloat("SFXVolume", out dbVolume);
            return DecibelsToNormalized(dbVolume);
        }

        public float GetMusicVolume()
        {
            float dbVolume;
            mainMixer.GetFloat("MusicVolume", out dbVolume);
            return DecibelsToNormalized(dbVolume);
        }

        float NormalizedToDecibels(float normalized)
        {
            normalized = Mathf.Clamp(normalized, 0.0001f, 1f);
            return Mathf.Log10(normalized) * 20f;
        }

        float DecibelsToNormalized(float db)
        {
            return Mathf.Pow(10f, db / 20f);
        }

        public void TransitionToSnapshot(string snapshotName, float transitionTime = -1f)
        {
            if (transitionTime < 0) transitionTime = snapshotTransitionTime;

            switch (snapshotName.ToLower())
            {
                case "default":
                    if (defaultSnapshot != null)
                        defaultSnapshot.TransitionTo(transitionTime);
                    break;
                case "paused":
                    if (pausedSnapshot != null)
                        pausedSnapshot.TransitionTo(transitionTime);
                    break;
                case "combat":
                    if (combatSnapshot != null)
                        combatSnapshot.TransitionTo(transitionTime);
                    break;
            }
        }

        public void PlayMusic(int trackIndex, bool fade = true)
        {
            if (musicTracks == null || trackIndex < 0 || trackIndex >= musicTracks.Length) return;

            if (currentFadeCoroutine != null)
                StopCoroutine(currentFadeCoroutine);

            currentFadeCoroutine = StartCoroutine(CrossfadeMusic(musicTracks[trackIndex], fade));
            currentMusicIndex = trackIndex;
        }

        public void PlayMusic(AudioClip clip, bool fade = true)
        {
            if (clip == null) return;

            if (currentFadeCoroutine != null)
                StopCoroutine(currentFadeCoroutine);

            currentFadeCoroutine = StartCoroutine(CrossfadeMusic(clip, fade));
        }

        public void StopMusic(bool fade = true)
        {
            if (currentFadeCoroutine != null)
                StopCoroutine(currentFadeCoroutine);

            if (fade)
                currentFadeCoroutine = StartCoroutine(FadeOutMusic());
            else
                musicSource.Stop();
        }

        public void PlayNextMusic(bool fade = true)
        {
            if (musicTracks == null || musicTracks.Length == 0) return;

            currentMusicIndex = (currentMusicIndex + 1) % musicTracks.Length;
            PlayMusic(currentMusicIndex, fade);
        }

        IEnumerator CrossfadeMusic(AudioClip newClip, bool fade)
        {
            if (!fade)
            {
                musicSource.clip = newClip;
                musicSource.volume = 1f;
                musicSource.Play();
                yield break;
            }

            // Fade out current music
            float startVolume = musicSource.volume;
            float timer = 0f;

            while (timer < musicFadeTime * 0.5f)
            {
                timer += Time.deltaTime;
                float t = timer / (musicFadeTime * 0.5f);
                musicSource.volume = Mathf.Lerp(startVolume, 0f, volumeFadeCurve.Evaluate(t));
                yield return null;
            }

            // Switch track
            musicSource.Stop();
            musicSource.clip = newClip;
            musicSource.Play();

            // Fade in new music
            timer = 0f;
            while (timer < musicFadeTime * 0.5f)
            {
                timer += Time.deltaTime;
                float t = timer / (musicFadeTime * 0.5f);
                musicSource.volume = Mathf.Lerp(0f, 1f, volumeFadeCurve.Evaluate(t));
                yield return null;
            }

            musicSource.volume = 1f;
        }

        IEnumerator FadeOutMusic()
        {
            float startVolume = musicSource.volume;
            float timer = 0f;

            while (timer < musicFadeTime)
            {
                timer += Time.deltaTime;
                float t = timer / musicFadeTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, volumeFadeCurve.Evaluate(t));
                yield return null;
            }

            musicSource.Stop();
            musicSource.volume = 1f;
        }

        public void DuckMusic(float duckAmount, float duckDuration)
        {
            StartCoroutine(DuckMusicCoroutine(duckAmount, duckDuration));
        }

        IEnumerator DuckMusicCoroutine(float duckAmount, float duration)
        {
            float originalVolume = musicSource.volume;
            float targetVolume = originalVolume * (1f - duckAmount);

            // Duck down
            float timer = 0f;
            while (timer < 0.1f)
            {
                timer += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(originalVolume, targetVolume, timer / 0.1f);
                yield return null;
            }

            // Hold
            yield return new WaitForSeconds(duration);

            // Return to normal
            timer = 0f;
            while (timer < 0.3f)
            {
                timer += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(targetVolume, originalVolume, timer / 0.3f);
                yield return null;
            }

            musicSource.volume = originalVolume;
        }

        public void OnGamePaused()
        {
            TransitionToSnapshot("paused");
            SoundEffectsManager.Instance.PauseAllSounds();
        }

        public void OnGameResumed()
        {
            TransitionToSnapshot("default");
            SoundEffectsManager.Instance.UnpauseAllSounds();
        }

        public void OnCombatStarted()
        {
            TransitionToSnapshot("combat");
        }

        public void OnCombatEnded()
        {
            TransitionToSnapshot("default");
        }
    }
}