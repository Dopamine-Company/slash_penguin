//José Pablo Peñaloza Cobos / Mariana Gutierrez Carreto
//03/JUL/21
//Controls the overall behaviour of the particle system. 
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleBehaviour : MonoBehaviour
{
    //AUDIO CONTROLLER VARIABLES//
    //Frequencies
    [Range(0, 15)]
    //Stores which range of frequencies the object will respond to. 
    public int bandFrequency;
    [Range(0.0f, 1.0f)]
    //Minimun value that the band buffer has to exceed to make the object react
    public float threshold;
    //Boolean that activates the behaviour 
    public bool enable;
    //copy of the band buffer value for being able to modify it per object
    private float changeFactor;
    //Stores the audio controller
    public AudioController audioController;

    //Asks for the number of particles that will be emited per second while the emitter is activated
    public float particlesToEmit;

    private ParticleSystem particles;

    private void OnDrawGizmosSelected()
    {
        if (audioController == null)
        {//Checks that the object has an audio controller to respond to 
            Debug.LogWarning("An Audio Controller must be attached to the object.");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        particles = GetComponent<ParticleSystem>();

    }

    // Update is called once per frame
    void Update()
    {
        if (audioController != null)
            emitWithSound();
    }



    private void emitWithSound()
    {
        var em = particles.emission; //Creates a variable to handle the emission of the partivle system
        em.enabled = true;           //Enables the emissive to true 
        if (audioController.audioBandBuffer[bandFrequency] >= threshold && enable) //If the particle system was enabled and the value is greater than the threshold 
            em.rateOverTime = particlesToEmit;                                     //The amount of particles emited are created 
        else
            em.rateOverTime = 0;                                                   //If not the emission is set to 0.
    }
}
