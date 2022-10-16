﻿using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class KnifePool : UdonSharpBehaviour
{
    [Header("Owner Information")]
    public VRCPlayerApi Owner;

    // Player tracking
    public Vector3 PlayerPosition;
    public Quaternion PlayerRotation;

    [Header("Prefab Components")]
    // Main script attached to player
    public PlayerController PlayerController;

    // Script attached to the EffectsContainer
    public EffectsContainer EffectsContainer;

    [Header("Knife Stats")]
    // Knife array
    public GameObject[] Knives = new GameObject[30];


    // Used to toggle the knife weapon
    [UdonSynced]
    public bool isKnifeOn = false;

    // Index used for iterating through the pool
    public int KnifeIndex;

    // Amount of damage done on hit
    [UdonSynced]
    public float Damage;

    // Attack frequency
    [UdonSynced]
    public float cooldown;

    // Counter for the cooldown
    public float CDCounter;

    // Distance knives get thrown
    [UdonSynced]
    public float Range;

    // Throw speed
    [UdonSynced]
    public float Force;

    // Amount of knives thrown at once
    [UdonSynced]
    public float Quantity;

    public bool ReadyToGo = false;

    // Script ran after the _OnOwnerSet script on the PlayerController
    public void _OnOwnerSet()
    {
        Owner = Networking.GetOwner(gameObject);
        PlayerController = transform.parent.GetComponent<PlayerController>();
        EffectsContainer = PlayerController.EffectsContainer;

        // Reset stats to base
        Damage = 1f;
        cooldown = 2f;
        Range = 100f;
        Force = 2f;
        Quantity = 1f;

        int index = 0;
        foreach (Transform child in transform)
        {
            Networking.SetOwner(Owner, child.gameObject);
            Knife Knife = child.GetComponent<Knife>();
            Knife.name = index.ToString() + "Knife";
            Knives[index] = Knife.gameObject;
            Knife._OnOwnerSet();
            index++;
        }
        ReadyToGo = true;
    }

    private void Update()
    {
        if (Owner == null || !ReadyToGo)
        {
            return;
        }
        // Tracking the players position
        PlayerPosition = Owner.GetPosition();
        PlayerRotation = Owner.GetRotation();

        // Used to toggle knife attacking
        // If isAttacking is false, stop attacking
        if (!isKnifeOn)
        {
            return;
        }
        // if isAttacking is true, begin the attack
        else
        {
            CDCounter -= Time.deltaTime;
            if (CDCounter <= 0)
            {
                CDCounter = cooldown;
                for (int i = 1; i <= Quantity; i++)
                {
                    Knives[KnifeIndex].SetActive(true);
                    if (KnifeIndex <= 28)
                    {
                        KnifeIndex++;
                    }
                    else
                    {
                        KnifeIndex = 0;
                    }
                }
            }
        }
    }

    public void TestingUI()
    {
        Debug.Log("In KnifePool");
        Debug.Log("isKnifeOn");
        Debug.Log(isKnifeOn);
    }
}
