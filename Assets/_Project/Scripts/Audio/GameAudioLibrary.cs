using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class HeroSoundEntry
{
    [SerializeField] private HeroDefinition heroDefinition;
    [SerializeField] private AudioClip actionSound;

    public HeroDefinition HeroDefinition => heroDefinition;
    public AudioClip ActionSound => actionSound;
}

[CreateAssetMenu(fileName = "GameAudioLibrary", menuName = "Scriptable Objects/Audio/Game Audio Library")]
public sealed class GameAudioLibrary : ScriptableObject
{
    [Header("Background Music")]
    [SerializeField] private AudioClip headquartersBGM;
    [SerializeField] private AudioClip battleBGM;

    [Header("Hero Sounds")]
    [SerializeField] private List<HeroSoundEntry> heroSounds = new List<HeroSoundEntry>();

    [Header("Enemy Sounds")]
    [SerializeField] private AudioClip enemyEncounterSound;
    [SerializeField] private AudioClip enemyDeathSound;

    [Header("Stage Result Sounds")]
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip defeatSound;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip uiButtonClickSound;
    [SerializeField] private AudioClip equipGearSound;
    [SerializeField] private AudioClip unequipGearSound;

    [Header("Mix")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Min(0f)] private float repeatedSfxInterval = 0.04f;

    public AudioClip HeadquartersBGM => headquartersBGM;
    public AudioClip BattleBGM => battleBGM;
    public AudioClip EnemyEncounterSound => enemyEncounterSound;
    public AudioClip EnemyDeathSound => enemyDeathSound;
    public AudioClip VictorySound => victorySound;
    public AudioClip DefeatSound => defeatSound;
    public AudioClip UIButtonClickSound => uiButtonClickSound;
    public AudioClip EquipGearSound => equipGearSound;
    public AudioClip UnequipGearSound => unequipGearSound;
    public float BGMVolume => bgmVolume;
    public float SFXVolume => sfxVolume;
    public float RepeatedSFXInterval => repeatedSfxInterval;

    public AudioClip GetHeroActionSound(HeroDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        for (int i = 0; i < heroSounds.Count; i++)
        {
            HeroSoundEntry entry = heroSounds[i];
            if (entry != null && entry.HeroDefinition == definition)
            {
                return entry.ActionSound;
            }
        }

        return null;
    }
}
