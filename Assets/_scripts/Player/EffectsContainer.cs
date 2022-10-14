﻿using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EffectsContainer : UdonSharpBehaviour
{
    public VRCPlayerApi Owner;
    public PlayerController PlayerController;
    public KnifePool KnifePool;
    public ParticleSystem LevelUpVisual;
    public AudioSource LevelUpAudio;
    public ParticleSystem Spark;
    public ParticleSystem Blood;
    public AudioSource Throw;
    public AudioSource IAOHit;
    public AudioSource EnemyHit;
    public AudioSource Kill;

    private void OnEnable() { }

    public void _OnOwnerSet()
    {
        // Set current player to owner
        Owner = Networking.GetOwner(gameObject);
        // Set ParticleSystems and Sounds
        LevelUpVisual = transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>();
        LevelUpAudio = transform.GetChild(0).GetChild(1).GetComponent<AudioSource>();
        Spark = transform.GetChild(1).GetComponent<ParticleSystem>();
        Blood = transform.GetChild(2).GetComponent<ParticleSystem>();
        Throw = transform.GetChild(3).GetComponent<AudioSource>();
        IAOHit = transform.GetChild(4).GetComponent<AudioSource>();
        EnemyHit = transform.GetChild(5).GetComponent<AudioSource>();
        Kill = transform.GetChild(6).GetComponent<AudioSource>();
    }

    public void LevelUp(Vector3 pos)
    {
        AudioClip bong = LevelUpAudio.clip;
        LevelUpVisual.Play();
        AudioSource.PlayClipAtPoint(bong, pos);
    }
}
