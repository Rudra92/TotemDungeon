using UnityEngine.Audio;
using UnityEngine;
using System;

public class AudioManager : MonoBehaviour
{

    // singleton class
    public static AudioManager singleton;
    public Sound[] sounds;


    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if(singleton == null)
        {
            singleton = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach(Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();

            s.source.name = s.name;
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.spatialBlend = s.spatialBlend;
        }
    }

    public void Play(string name, Vector3? position = null)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Could not find Sound: " + name);
            return;
        }
        if(position != null)
        {
            s.source.transform.position = ((Vector3)position);
        }
        s.source.Play();

    }
}
