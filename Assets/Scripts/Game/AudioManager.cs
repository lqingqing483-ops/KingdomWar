using UnityEngine;

namespace KingdomWar.Game
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("AudioManager");
                    instance = obj.AddComponent<AudioManager>();
                }
                return instance;
            }
        }

        private AudioSource bgmSource;
        private AudioSource sfxSource;
        private float bgmVolume = 1.0f;
        private float sfxVolume = 1.0f;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);

                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;

                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;

                LoadVolumeSettings();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void LoadVolumeSettings()
        {
            bgmVolume = PlayerPrefs.GetFloat("Audio_BGM_Volume", 1.0f);
            sfxVolume = PlayerPrefs.GetFloat("Audio_SFX_Volume", 1.0f);
            ApplyVolume();
        }

        private void ApplyVolume()
        {
            if (bgmSource != null)
                bgmSource.volume = bgmVolume;
            if (sfxSource != null)
                sfxSource.volume = sfxVolume;
        }

        private void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat("Audio_BGM_Volume", bgmVolume);
            PlayerPrefs.SetFloat("Audio_SFX_Volume", sfxVolume);
            PlayerPrefs.Save();
        }

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null || bgmSource == null) return;
            bgmSource.clip = clip;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            if (bgmSource != null)
                bgmSource.volume = bgmVolume;
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        public void SetVolume(float volume)
        {
            SetBGMVolume(volume);
            SetSFXVolume(volume);
            AudioListener.volume = volume;
        }

        public float GetBGMVolume()
        {
            return bgmVolume;
        }

        public float GetSFXVolume()
        {
            return sfxVolume;
        }

        public void Save()
        {
            SaveVolumeSettings();
        }

        public void Load()
        {
            LoadVolumeSettings();
        }

        public void StopBGM()
        {
            if (bgmSource != null)
                bgmSource.Stop();
        }
    }
}
