//Jose Pablo Peñaloza 
//27/DEC/20
//Script makes the character animations respond to audio. Currently the character responds only at one band at a time.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimControl : MonoBehaviour
{
    bool isPlaying;

    Animator animator;

    [Range(0, 15)]
    //Stores which range of frequencies the object will respond to. 
    public int bandFrequency;
    [Range(0.0f, 1.0f)]
    //Minimum value that the band buffer has to exceed to make the object react
    public float threshold;
    
    //Audio Controller which it will respond to.
    public AudioController audioController;

    
    void Start()
    {
        animator = GetComponent<Animator>();
        isPlaying = false;

        if (audioController == null)
            Debug.LogWarning("An Audio Controller must be attached to the object.");

    }

    // Update is called once per frame
    void Update()
    {
        if (audioController != null) {
            SingleAnimationResponsive();
        }
    }

    public void SingleAnimationResponsive()
    {
        if (isPlaying == false && audioController.audioBandBuffer[bandFrequency] > threshold)
        {
            isPlaying = true;
            animator.SetBool("Check", true);//Accesses the audio controller script to get the highest region registered and sends it to the animator.
        }
        else if (audioController.audioBandBuffer[bandFrequency] < threshold)
        {
            animator.SetBool("Check", false);//Accesses the audio controller script to get the highest region registered and sends it to the animator.
            isPlaying = false;
        }
    }
}
