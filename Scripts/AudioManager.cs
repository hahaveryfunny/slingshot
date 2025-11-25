using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;

    public bool loop = false;

    [Range(0f, 1f)]
    public float volume = 1f;

    [HideInInspector]
    public AudioSource source;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Volume Sliders")]
    //public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Volume Icons")]
    //public Image[] masterVolumeIcons = new Image[2];  // [0] = mute, [1] = on
    public Image[] musicVolumeIcons = new Image[2];   // [0] = mute, [1] = on
    public Image[] sfxVolumeIcons = new Image[2];     // [0] = mute, [1] = on


    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    [Range(0f, 1f)]
    public float musicVolume = 1f;

    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Header("Music")]
    public Sound[] musicTracks;

    [Header("Sound Effects")]
    public Sound[] bounceSFX;
    public Sound[] wrongHitSFX;
    public Sound[] poofSFX;

    [Header("UI Sounds")]
    public Sound uiForward;
    public Sound uiBack;
    public Sound uiPurchase;
    public Sound uiPick;

    // Current playing music
    private Sound currentMusic;

    void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }



    void Start()
    {
        // Set sliders to saved values
        if (SaveManager.Instance != null && SaveManager.Instance.IsReady())
        {
            //masterVolumeSlider.value = SaveManager.Instance.GetMasterVolume();
            musicVolumeSlider.value = SaveManager.Instance.GetMusicVolume();
            sfxVolumeSlider.value = SaveManager.Instance.GetSFXVolume();

        }
        int index = Random.Range(0, musicTracks.Length);
        PlayMusic(musicTracks[index]);
        UpdateVolumeIcons();
    }


    public void UpdateVolumeIcons()
    {
        // // Show/hide master volume icons
        // int masterIndex = masterVolume == 0 ? 0 : 1;
        // masterVolumeIcons[0].gameObject.SetActive(masterIndex == 0);
        // masterVolumeIcons[1].gameObject.SetActive(masterIndex == 1);

        // Show/hide music volume icons
        int musicIndex = musicVolume == 0 ? 0 : 1;
        musicVolumeIcons[0].gameObject.SetActive(musicIndex == 0);
        musicVolumeIcons[1].gameObject.SetActive(musicIndex == 1);

        // Show/hide sfx volume icons
        int sfxIndex = sfxVolume == 0 ? 0 : 1;
        sfxVolumeIcons[0].gameObject.SetActive(sfxIndex == 0);
        sfxVolumeIcons[1].gameObject.SetActive(sfxIndex == 1);
    }


    void InitializeAudio()
    {
        // Create AudioSource components for each sound
        CreateAudioSources(musicTracks);
        CreateAudioSources(bounceSFX);
        CreateAudioSources(poofSFX);
        CreateAudioSources(wrongHitSFX);
        CreateAudioSource(uiForward);
        CreateAudioSource(uiBack);
        CreateAudioSource(uiPurchase);
        CreateAudioSource(uiPick);
    }

    //bu fonksiyon inspectorda value değiştiğine çalışıyor ve inspectordan ses değiştirmek için zorunlu
    void OnValidate()
    {
        if (Application.isPlaying && instance != null && currentMusic != null)
        {
            UpdateAllVolumes();
        }
    }

    void CreateAudioSources(Sound[] sounds)
    {
        foreach (Sound sound in sounds)
        {
            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.clip = sound.clip;
            sound.source.loop = sound.loop;
        }
    }
    void CreateAudioSource(Sound sound)
    {
        sound.source = gameObject.AddComponent<AudioSource>();
        sound.source.clip = sound.clip;
        sound.source.loop = sound.loop;
    }

    // =============================================================================
    // PUBLIC METHODS - Call these from other scripts
    // =============================================================================

    public void PlayMusic(Sound music)
    {
        if (music != null)
        {
            // Stop current music
            if (currentMusic != null && currentMusic.source.isPlaying)
            {
                currentMusic.source.Stop();
            }
            // Play new music
            currentMusic = music;
            music.source.volume = music.volume * musicVolume * masterVolume;
            music.source.Play();
        }
        else
        {
            Debug.LogWarning($"Music '{name}' not found!");
        }
    }

    public void StopMusic()
    {
        if (currentMusic != null)
        {
            currentMusic.source.Stop();
            currentMusic = null;
        }
    }

    public void PlaySFX(Sound sfx)
    {
        if (sfx != null)
        {
            sfx.source.volume = sfx.volume * sfxVolume * masterVolume;
            sfx.source.PlayOneShot(sfx.clip);
        }
        else
        {
            Debug.LogWarning($"SFX3 not found!");
        }
    }

    public void PlayUI(Sound ui)
    {
        if (ui != null)
        {
            ui.source.volume = ui.volume * sfxVolume * masterVolume;
            ui.source.PlayOneShot(ui.clip);
        }
        else
        {
            Debug.LogWarning($"UI Sound '{name}' not found!");
        }
    }

    // =============================================================================
    // VOLUME CONTROLS
    // =============================================================================

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
        UpdateVolumeIcons();

    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateMusicVolume();
        UpdateVolumeIcons();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateVolumeIcons();
        // SFX volumes are applied when played
    }

    public void MuteAll(bool mute)
    {
        SetMasterVolume(mute ? 0f : 1f);
    }

    public void MuteMusic(bool mute)
    {
        SetMusicVolume(mute ? 0f : 1f);
    }

    // =============================================================================
    // FADE EFFECTS
    // =============================================================================

    public void FadeInMusic(string name, float fadeTime = 1f)
    {
        Sound music = FindSound(musicTracks, name);
        if (music != null)
        {
            StartCoroutine(FadeIn(music, fadeTime));
        }
    }

    public void FadeOutMusic(float fadeTime = 1f)
    {
        if (currentMusic != null)
        {
            StartCoroutine(FadeOut(currentMusic, fadeTime));
        }
    }

    public void CrossfadeMusic(string newMusicName, float fadeTime = 1f)
    {
        Sound newMusic = FindSound(musicTracks, newMusicName);
        if (newMusic != null)
        {
            StartCoroutine(Crossfade(currentMusic, newMusic, fadeTime));
        }
    }

    // =============================================================================
    // HELPER METHODS
    // =============================================================================

    Sound FindSound(Sound[] sounds, string name)
    {
        foreach (Sound sound in sounds)
        {
            if (sound.name == name)
                return sound;
        }
        return null;
    }

    void UpdateAllVolumes()
    {
        UpdateMusicVolume();
        // SFX volumes are updated when played
    }

    void UpdateMusicVolume()
    {
        if (currentMusic != null)
        {
            currentMusic.source.volume = currentMusic.volume * musicVolume * masterVolume;
        }
    }

    // =============================================================================
    // COROUTINES FOR FADING
    // =============================================================================

    IEnumerator FadeIn(Sound music, float fadeTime)
    {
        // Stop current music
        if (currentMusic != null && currentMusic.source.isPlaying)
        {
            currentMusic.source.Stop();
        }

        currentMusic = music;
        music.source.volume = 0f;
        music.source.Play();

        float targetVolume = music.volume * musicVolume * masterVolume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / fadeTime;
            music.source.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        music.source.volume = targetVolume;
    }

    IEnumerator FadeOut(Sound music, float fadeTime)
    {
        float startVolume = music.source.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / fadeTime;
            music.source.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        music.source.Stop();
        music.source.volume = startVolume;
        currentMusic = null;
    }

    IEnumerator Crossfade(Sound oldMusic, Sound newMusic, float fadeTime)
    {
        // Start new music at volume 0
        newMusic.source.volume = 0f;
        newMusic.source.Play();

        float oldStartVolume = oldMusic != null ? oldMusic.source.volume : 0f;
        float newTargetVolume = newMusic.volume * musicVolume * masterVolume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / fadeTime;

            // Fade out old music
            if (oldMusic != null)
            {
                oldMusic.source.volume = Mathf.Lerp(oldStartVolume, 0f, t);
            }

            // Fade in new music
            newMusic.source.volume = Mathf.Lerp(0f, newTargetVolume, t);

            yield return null;
        }

        // Cleanup
        if (oldMusic != null)
        {
            oldMusic.source.Stop();
            oldMusic.source.volume = oldMusic.volume * musicVolume * masterVolume;
        }

        newMusic.source.volume = newTargetVolume;
        currentMusic = newMusic;
    }
}