using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-10000)]
public sealed class GameAudioManager : SingletonMB<GameAudioManager>
{
    private const string audioLibraryResourcePath = "Audio/GameAudioLibrary";
    private const string headquartersSceneName = "GuildHeadquartersScene";
    private const string battleSceneName = "CombatStageScene";
    private const string battleAuthoringSceneName = "CombatStageAuthoring";

    private readonly Dictionary<AudioClip, float> nextAllowedSfxTimes = new Dictionary<AudioClip, float>();

    private GameAudioLibrary audioLibrary;
    private AudioSource bgmSource;
    private AudioSource sfxSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetBeforePlayMode()
    {
        ResetSingletonState();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        GameAudioManager manager = Instance;
        if (manager == null)
        {
            Debug.LogError("[GameAudioManager] Failed to create the global audio manager.");
        }
    }

    private void Awake()
    {
        audioLibrary = Resources.Load<GameAudioLibrary>(audioLibraryResourcePath);
        if (audioLibrary == null)
        {
            Debug.LogError($"[GameAudioManager] GameAudioLibrary was not found at Resources/{audioLibraryResourcePath}.", this);
        }

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        ApplySceneMusic(SceneManager.GetActiveScene());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void PlayHeroAction(HeroDefinition definition)
    {
        if (audioLibrary == null)
        {
            return;
        }

        PlaySFX(audioLibrary.GetHeroActionSound(definition), true);
    }

    public void PlayEnemyEncounter()
    {
        PlaySFX(audioLibrary != null ? audioLibrary.EnemyEncounterSound : null, true);
    }

    public void PlayEnemyDeath()
    {
        PlaySFX(audioLibrary != null ? audioLibrary.EnemyDeathSound : null, true);
    }

    public void PlayVictory()
    {
        PlaySFX(audioLibrary != null ? audioLibrary.VictorySound : null, false);
    }

    public void PlayDefeat()
    {
        PlaySFX(audioLibrary != null ? audioLibrary.DefeatSound : null, false);
    }

    public void PlayUIButtonClick()
    {
        PlaySFX(audioLibrary != null ? audioLibrary.UIButtonClickSound : null, false);
    }

    public void PlayEquipGear()
    {
        PlaySFX(audioLibrary != null ? audioLibrary.EquipGearSound : null, false);
    }

    public void PlayUnequipGear()
    {
        PlaySFX(audioLibrary != null ? audioLibrary.UnequipGearSound : null, false);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        nextAllowedSfxTimes.Clear();
        ApplySceneMusic(scene);
    }

    private void ApplySceneMusic(Scene scene)
    {
        if (audioLibrary == null)
        {
            return;
        }

        if (scene.name == headquartersSceneName)
        {
            PlayBGM(audioLibrary.HeadquartersBGM);
        }
        else if (scene.name == battleSceneName || scene.name == battleAuthoringSceneName)
        {
            PlayBGM(audioLibrary.BattleBGM);
        }
        else
        {
            StopBGM();
        }
    }

    private void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null)
        {
            StopBGM();
            return;
        }

        bgmSource.volume = audioLibrary.BGMVolume;
        if (bgmSource.clip == clip)
        {
            if (!bgmSource.isPlaying)
            {
                bgmSource.Play();
            }

            return;
        }

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    private void StopBGM()
    {
        if (bgmSource == null)
        {
            return;
        }

        bgmSource.Stop();
        bgmSource.clip = null;
    }

    private void PlaySFX(AudioClip clip, bool limitRepeatedSound)
    {
        if (audioLibrary == null || sfxSource == null || clip == null)
        {
            return;
        }

        if (limitRepeatedSound)
        {
            float currentTime = Time.unscaledTime;
            if (nextAllowedSfxTimes.TryGetValue(clip, out float nextAllowedTime) && currentTime < nextAllowedTime)
            {
                return;
            }

            nextAllowedSfxTimes[clip] = currentTime + audioLibrary.RepeatedSFXInterval;
        }

        sfxSource.PlayOneShot(clip, audioLibrary.SFXVolume);
    }
}
