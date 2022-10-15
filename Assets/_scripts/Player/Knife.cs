﻿using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Knife : UdonSharpBehaviour
{
    [Header("Components")]
    public VRCPlayerApi Owner;
    public PlayerController PlayerController;
    public EffectsContainer EffectsContainer;
    public KnifePool KnifePool;
    public Rigidbody KnifeRB;

    [Header("Knife Info")]
    // Used to initialize where the knife will spawn in reference to the current player
    public Vector3 KnifePosition;
    public Quaternion KnifeRotation;

    // For tracking distance
    public float DistanceTravelled;

    [Header("Sounds and VFX")]
    // Used to detect if what was collided with was an enemy
    public bool HitEnemy;

    // Checks to see if what was hit was an inanimate object
    public bool HitIAO;

    // The effect when the knife hits an IAO
    public ParticleSystem Spark;

    // The effect for when a knife hits an enemy
    public ParticleSystem Blood;

    // The sound made when the player throws the knife
    private AudioClip Throw;

    // The sound when a knife hits an IAO
    private AudioClip HitIAOSound;

    // The sound when a knife hits an enemy
    private AudioClip HitEnemySound;

    // The sound made by the enemy when killed
    private AudioClip Kill;

    // The position to player the effects at
    public Vector3 EffectPosition;

    // for OnEnable functionality
    public bool ReadyToGo = false;

    // Ran after the owner has been set on this object
    public void _OnOwnerSet()
    {
        Debug.Log("In _OnOwnerSet in Knife");
        // Set AudioClips
        Owner = Networking.GetOwner(gameObject);
        KnifePool = transform.parent.GetComponent<KnifePool>();
        
        PlayerController = KnifePool.PlayerController;
        EffectsContainer = KnifePool.EffectsContainer;
        Throw = EffectsContainer.Throw.clip;
        HitIAOSound = EffectsContainer.IAOHit.clip;
        HitEnemySound = EffectsContainer.EnemyHit.clip;
        Kill = EffectsContainer.Kill.clip;
        gameObject.SetActive(false);
    }

    // When it's the knife's turn to be thrown
    private void OnEnable()
    {
        if (ReadyToGo == false)
        {
            return;
        }
        KnifePool = transform.parent.GetComponent<KnifePool>();
        KnifeRB = transform.GetComponent<Rigidbody>();
        DistanceTravelled = 0;
        HitEnemy = false;
        HitIAO = false;

        KnifePosition = new Vector3(KnifePool.PlayerPosition.x, 1, KnifePool.PlayerPosition.z);
        KnifeRotation = KnifePool.PlayerRotation;

        transform.SetPositionAndRotation(KnifePosition, KnifeRotation);
        KnifeRB.velocity = transform.forward * KnifePool.Force;
        AudioSource.PlayClipAtPoint(Throw, transform.position);
    }

    private void Update()
    {
        if (ReadyToGo == false)
        {
            return;
        }
        // Tracking Distance
        DistanceTravelled += Vector3.Distance(transform.position, KnifePosition);
        // Debug.Log();
        if (DistanceTravelled >= KnifePool.Range)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        EffectPosition = transform.position;
        if (other.name.Contains("IAE"))
        {
            HitIAO = true;
            gameObject.SetActive(false);
        }
        else if (other.name.Contains("Enemy"))
        {
            HitEnemy = true;
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        ReadyToGo = true;
    }
}