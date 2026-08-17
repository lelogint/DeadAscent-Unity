using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class SoundHandler : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;

    [SerializeField] private AudioClip land1, land2;
    [SerializeField] private AudioClip jump1, jump2;
    [SerializeField] private AudioClip doubleJump;
    [SerializeField] private AudioClip step1, step2, step3, step4;
    [SerializeField] private AudioClip slash1, slash2, slash3, slash4;
    [SerializeField] private AudioClip slashHit1, slashHit2, slashHit3; // To make sound variations more visible I declare them specifically with a comma (,) between
    [SerializeField] private AudioClip sheath;
    [SerializeField] private AudioClip getItem;
    [SerializeField] private AudioClip dash1, dash2;
    [SerializeField] private AudioClip coinGet;
    [SerializeField] private AudioClip uiBack, uiSelect;
    [SerializeField] private AudioClip hit1, hit2;
    [SerializeField] private AudioClip hitGround;
    [SerializeField] private AudioClip swordRecoil1, swordRecoil2, swordRecoil3;

    private Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    void Awake()
    {
        clips = new Dictionary<string, AudioClip>()
        {
        {"Landed1", land1},
        {"Landed2", land2},
        {"Jump1", jump1},
        {"Jump2", jump2},
        {"DoubleJump", doubleJump},
        {"Step1", step1},
        {"Step2", step2},
        {"Step3", step3},
        {"Step4", step4},
        {"Slash1", slash1},
        {"Slash2", slash2},
        {"Slash3", slash3},
        {"Slash4", slash4},
        {"SlashHit1", slashHit1},
        {"SlashHit2", slashHit2},
        {"SlashHit3", slashHit3},
        {"Sheath", sheath},
        {"GetItem", getItem},
        {"Dash1", dash1},
        {"Dash2", dash2},
        {"CoinGet", coinGet},
        {"UiBack", uiBack},
        {"UiSelect", uiSelect},
        {"Hit1", hit1},
        {"Hit2", hit2},
        {"HitGround", hitGround},
        {"SwordRecoil1", swordRecoil1},
        {"SwordRecoil2", swordRecoil2},
        {"SwordRecoil3", swordRecoil3},
        };
    }

    private float SoundVolume(float volume)
    {
        float soundVolume = (volume >= 0) ? volume : 1f; // If volume isn't defined 
        return soundVolume;
    }

    public void RandomSound(string name, int min, int max, float volume) // If there is multiple sounds for one action, random generation is done
    {
        int randomInt = Random.Range(1, max + 1); // Add one because the range is maximally inclusive
        string stringAudio = name + randomInt.ToString();
        audioSource.PlayOneShot(clips[stringAudio], SoundVolume(volume));
    }

    public void SwingSounds(int num)
    {
        int randomInt = Random.Range(1, 3);
        if (num == 2) // This because there is 2 variations of sword swing left and sword swing right, so we add 2 if it is on the second swing
        {
            randomInt += 2;
        }
        string stringAudio = "Slash" + randomInt.ToString();
        audioSource.PlayOneShot(clips[stringAudio], .8f);
    }

    public void PlaySound(string soundName, float volume) // Play sound once
    {
        audioSource.PlayOneShot(clips[soundName], SoundVolume(volume));
    }
}
