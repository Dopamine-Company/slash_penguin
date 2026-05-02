//José Pablo Peñaloza Cobos / Mariana Gutiérrez Carreto
//19/JUL/20
//Controls the overall behaviour of the object. 

using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

[ExecuteInEditMode]
public class ShObjectBehaviour : MonoBehaviour
{
    //To keep the tab index regardless of play and editor mode
    [HideInInspector]
    public int toolbarTab;

    //To name your tabs and keep track of them
    [HideInInspector]
    public string currentTab;

    [HideInInspector]
    public Renderer myObject;

    [HideInInspector]
    public MaterialPropertyBlock block;

    //Frequencies
    [Range(0, 15)]
    //Stores which range of frequencies the object will respond to. 
    public int bandFrequency;

    [Range(0.0f, 1.0f)]
    //Minimum value that the band buffer has to exceed to make the object react
    public float threshold;
    //copy of the band buffer value for being able to modify it per object
    private float changeFactor;

    //Scales axis
    public bool X;
    public bool Y;
    public bool Z;
    //Starting scale of the object and its multiplier. 
    public float scaleMultiplier;
    private Vector3 startScale;

    //Rotation
    public bool rotate;
    public float speedMultiplier;
    //Rotation axis
    public bool RotateX;
    public bool RotateY;
    public bool RotateZ;

    //Color
    public bool color;
    public Color colorStart;
    public Color colorEnd;

    //Glow
    public bool enableGlow;
    public float glowIntensity;
    public Color glowColor;

    //Stores the audio clip file
    public AudioController audioController;

    private int sX = 0;
    private int sY = 0;
    private int sZ = 0;

    private int rX = 0;
    private int rY = 0;
    private int rZ = 0;


    //used for Gizmos
    Vector3 OriginalBounds;

    // Start is called before the first frame update
    void Start()
    {
        startScale = transform.localScale;
        OriginalBounds = transform.lossyScale;  //Gets the original bounds of the mesh for gizmos in play mode


        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        if (myObject == null)
        {
            myObject = gameObject.GetComponent<Renderer>();
        }
        if (audioController == null)
        {//Checks that the object has an audio controller to respond to 
            Debug.LogWarning("An Audio Controller must be attached to the object.");
        }

        myObject.GetPropertyBlock(block);//Uses a block in order to be able to change the independentely the material color
        block.SetColor("originalColor", colorStart);
        block.SetColor("glowColor", glowColor);
        block.SetColor("objectiveColor", colorEnd);
        myObject.SetPropertyBlock(block);


        sX = X ? 1 : 0;
        sY = Y ? 1 : 0;
        sZ = Z ? 1 : 0;


        if (RotateX)
            rX = 1;

        if (RotateY)
            rY = 1;

        if (RotateZ)
            rZ = 1;
        //Assigns the variables of control for rotation and scale a value to permit their change
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.isPlaying && audioController != null)
        {
            //If the aplication is playing & If an audio controller is attached & 
            if (audioController.audioBandBuffer[bandFrequency] >= threshold)
            {
                //When the value exceeds the threshold
                changeFactor = audioController.audioBandBuffer[bandFrequency];
            }
            else if (changeFactor > 0)
            {
                //When the value exceeds the threshold
                changeFactor = 0;
            }

            if (Application.isPlaying && audioController != null)
            {//Runs all the funtions that modify the object appereance
                myObject.GetPropertyBlock(block);
                transformObject();
                colorControl();
                rotationControl();
                glowManager();
                myObject.SetPropertyBlock(block);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {//Function that draws in the editor how big the game object will get when using the Scale Modifier     
        try
        {
            //Gets the game object mesh
            MeshFilter currentMeshFilter = (MeshFilter)gameObject.GetComponent("MeshFilter");
            Mesh currentMesh = currentMeshFilter.sharedMesh;

            //Verifies witch axis are being checked
            sX = X ? 1 : 0;
            sY = Y ? 1 : 0;
            sZ = Z ? 1 : 0;

            //Function thats translates local scale to lossy
            Vector3 localToLossy = R3Vector3(transform.localScale, transform.lossyScale, scaleMultiplier);//Rule of three for a vector 3 

            //Gets the scale for each axis 
            Vector3 desiredSize;
            if (scaleMultiplier >= 0)
            {
                desiredSize.x = OriginalBounds.x + localToLossy.x * scaleMultiplier * sX;
                desiredSize.y = OriginalBounds.y + localToLossy.y * scaleMultiplier * sY;
                desiredSize.z = OriginalBounds.z + localToLossy.z * scaleMultiplier * sZ;
            }
            else
            {
                desiredSize.x = OriginalBounds.x - localToLossy.x * scaleMultiplier * sX;
                desiredSize.y = OriginalBounds.y - localToLossy.y * scaleMultiplier * sY;
                desiredSize.z = OriginalBounds.z - localToLossy.z * scaleMultiplier * sZ;
            }
            //Creates the gizmo
            if (X || Y || Z)
                Gizmos.DrawWireMesh(currentMesh, -1, transform.position, transform.rotation, desiredSize);
        }
        catch
        {
            Debug.LogWarning("Could not get mesh");
        }
    }

    private Vector3 R3Vector3(Vector3 v1, Vector3 v2, float a)
    {
        //v1 = v2 
        //a  = ?
        Vector3 aVector;
        aVector.x = (v2.x * a) / v1.x;
        aVector.y = (v2.y * a) / v1.y;
        aVector.z = (v2.z * a) / v1.z;

        return aVector;
    }

    private void OnDrawGizmos()
    {
        myObject = gameObject.GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        myObject.GetPropertyBlock(block);//Uses a block in order to be able to change the independentely the material color
        block.SetColor("originalColor", colorStart);
        myObject.SetPropertyBlock(block);
    }

    private void transformObject()
    {
        //Transform the scale of the object on the selected axis
        if (audioController.audioBandBuffer[bandFrequency] > 0)
            transform.localScale = new Vector3((changeFactor * scaleMultiplier * sX) + startScale.x, (changeFactor * scaleMultiplier * sY) + startScale.y, (changeFactor * scaleMultiplier * sZ) + startScale.z);
    }

    private void colorControl()
    {
        if (color)
        {
            //Control the shader so the object can change color
            block.SetFloat("colorIntensity", changeFactor);
            
        }
    }

    private void glowManager()
    {
        if (enableGlow)
        {
            //Control the shader so the object can glow
            block.SetFloat("glowIntensity", changeFactor * glowIntensity);
        }
        else
        {
            block.SetFloat("glowIntensity", 0);
        }
    }

    private void rotationControl()
    {
        if (rotate)
        {
            //Rotation control of the object in the X, Y and Z axis. 
            transform.Rotate(changeFactor * speedMultiplier * rX, changeFactor * speedMultiplier * rY, changeFactor * speedMultiplier * rZ);
        }

    }

}
