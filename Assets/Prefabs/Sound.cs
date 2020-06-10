using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;

    public int tensionLevel;
    public int valenceLevel;
    public int arousalLevel;

    public AudioClip clip;
    
    [Range(0f, 1f)]
    public float volume = 0.04f;
    [Range(0.1f, 3f)]
    public float pitch = 1;
    public bool loop  = false;
    [Range(0f, 1f)]
    public float spatialBlend;
    
    [HideInInspector]
    public AudioSource source;
    [HideInInspector]
    public bool interval;
    [HideInInspector]
    public float startTime = 0;
    [HideInInspector]
    public float endTime = 1;

    
    public void initialise(int progression)
    {
        GameObject m_sound = new GameObject();
        m_sound.name = name;
        m_sound.AddComponent<SoundBehaviour>();

        tensionLevel = progression; 
        
    }

    public void PlayOneShot()
    {
        source.PlayOneShot(clip);
    }

}


