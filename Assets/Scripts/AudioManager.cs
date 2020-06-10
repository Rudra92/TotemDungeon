using UnityEngine.Audio;
using UnityEngine;
using System;
using UnityEditor;
using System.Collections.Generic;

public class EvalContainer
{
    public int playCount = 0;
    public int classification = 0;
    public List<int> numCollectWhenPlayed = new List<int>();
}

public class AudioManager : MonoBehaviour
{
    // singleton class
    public static AudioManager singleton;

    public int numberPlacedSounds = 10;
    public Sound theme;

    public Sound[] sounds;

    [HideInInspector]
    public Dictionary<String, EvalContainer> soundMap; // vector contains play count

    [HideInInspector]
    private NystromGenerator levelGen;

    private GameObject soundContainer;

    private GameController gc;

    private int overlapCount = 0;

    private List<float> distances;
    private List<Vector3> placed;
    private HashSet<Vector3> soundPlacementsRandom;
    private HashSet<Vector3> soundPlacementsOption1;
    private HashSet<Vector3> soundPlacementsOption2;
    private void OnDrawGizmos()
    {

        
        Gizmos.color = Color.magenta;
        if(soundPlacementsOption1 != null)
        {
            foreach (var v1 in soundPlacementsOption1)
            {
                Gizmos.DrawSphere(v1, 2f);
            }
        }
        
        
        Gizmos.color = Color.magenta;
        if (soundPlacementsOption2 != null)
        {
            foreach (var v2 in soundPlacementsOption2)
            {
                Gizmos.DrawSphere(v2, 2f);
            }
        }
        
        
        Gizmos.color = Color.green;
        if (soundPlacementsRandom != null)
        {
            foreach (var v3 in soundPlacementsRandom)
            {
                Gizmos.DrawSphere(v3, 2f);
            }
        }
        /*
        // draw circles around collectibles
        foreach(var d in distances)
        {
            foreach(var rid in levelGen.CollectibleRooms)
            {
                Gizmos.DrawWireSphere(levelGen.getRoomCenter( levelGen.mRooms[rid]), d);
                    
            }
        }
        */
    }

    // code taken at : http://csharphelper.com/blog/2014/09/determine-where-two-circles-intersect-in-c/ 
    private int FindCircleCircleIntersections(Vector2 c0, float r0, Vector2 c1, float r1, out Vector2 intersection1, out Vector2 intersection2)
    {
        // Find the distance between the centers.
        double dx = c0.x - c1.x;
        double dy = c0.y - c1.y;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        if (Math.Abs(dist - (r0 + r1)) < 0.00001)
        {
            intersection1 = Vector2.Lerp(c0, c1, r0 / (r0 + r1));
            intersection2 = intersection1;
            return 1;
        }

        // See how many solutions there are.
        if (dist > r0 + r1)
        {
            // No solutions, the circles are too far apart.
            intersection1 = new Vector2(float.NaN, float.NaN);
            intersection2 = new Vector2(float.NaN, float.NaN);
            return 0;
        }
        else if (dist < Math.Abs(r0 - r1))
        {
            // No solutions, one circle contains the other.
            intersection1 = new Vector2(float.NaN, float.NaN);
            intersection2 = new Vector2(float.NaN, float.NaN);
            return 0;
        }
        else if ((dist == 0) && (r0 == r1))
        {
            // No solutions, the circles coincide.
            intersection1 = new Vector2(float.NaN, float.NaN);
            intersection2 = new Vector2(float.NaN, float.NaN);
            return 0;
        }
        else
        {
            // Find a and h.
            double a = (r0 * r0 -
                        r1 * r1 + dist * dist) / (2 * dist);
            double h = Math.Sqrt(r0 * r0 - a * a);

            // Find P2.
            double cx2 = c0.x + a * (c1.x - c0.x) / dist;
            double cy2 = c0.y + a * (c1.y - c0.y) / dist;

            // Get the points P3.
            intersection1 = new Vector2(
                (float)(cx2 + h * (c1.y - c0.y) / dist),
                (float)(cy2 - h * (c1.x - c0.x) / dist));
            intersection2 = new Vector2(
                (float)(cx2 - h * (c1.y - c0.y) / dist),
                (float)(cy2 + h * (c1.x - c0.x) / dist));

            return 2;
        }
    }

    void Awake()
    {
        soundMap = new Dictionary<String, EvalContainer>();
        levelGen = FindObjectOfType<NystromGenerator>();
        gc = FindObjectOfType<GameController>();
        //DontDestroyOnLoad(gameObject);
        if(singleton == null)
        {
            singleton = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // containers for algorithms  of placement
        placed = new List<Vector3>();
        soundPlacementsOption1 = new HashSet<Vector3>();
        soundPlacementsOption2 = new HashSet<Vector3>();
        soundPlacementsRandom = new HashSet<Vector3>();

        distances = new List<float>() {
                levelGen.dimensions / 8 * levelGen.scale,
                levelGen.dimensions / 4 * levelGen.scale,
                levelGen.dimensions * 3 / 8 * levelGen.scale,
                levelGen.dimensions * .5f * levelGen.scale
            };


        if (theme != null) SetSource(theme);

        //initialise sounds from audio database
        foreach(Sound s in sounds)
        {
            //Add audio source to each sound
            SetSource(s);
        }

        //initialise sound container
        soundContainer = new GameObject(); soundContainer.name = "Sound Container";


        
    }

    public void checkOverlap()
    {
        if (IsAnySoundPlaying()) overlapCount++;
    }

    public int getOverlapCount()
    {
        return overlapCount;
    }

    private void SetSource(Sound s)
    {
        //there will be one source by audio clip, but there may be multiple objects triggering this source
        //Add audio source to s
        s.source = gameObject.AddComponent<AudioSource>();

        s.source.clip = s.clip;
        s.source.volume = s.volume;
        s.source.pitch = s.pitch;
        s.source.loop = s.loop;
        s.source.spatialBlend = s.spatialBlend;

        // add sound to map
        EvalContainer ec = new EvalContainer();
        ec.classification = calculateProgression(s);
        soundMap.Add(s.name, ec);
    }

    public void incrementSoundPlayedCount(Sound s, int progressionAtTime)
    {
        soundMap[s.name].playCount ++;
        soundMap[s.name].numCollectWhenPlayed.Add(progressionAtTime);
    }

    public void PlayTheme()
    {
        if (theme != null) theme.source.Play();
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
    public bool IsAnySoundPlaying()
    {
        foreach (var s in sounds)
        {
            if (s.source.isPlaying) return true;
        }

        return false;
    }

    private float GetClosestCollectiblePos(Vector3 pos)
    {
        GameObject[] currCollectibles = GameObject.FindGameObjectsWithTag("Collectible");
        float closestDistance = float.MaxValue;

        foreach(var c in currCollectibles)
        {
            Vector3 cPos = c.transform.position;
            float distance = (cPos - pos).magnitude;
            if(distance < closestDistance)
            {
                closestDistance = distance;
            }
        }

        return closestDistance;
    }

    // function that takes the distance from collectible to sound,
    // calculates the valence level and returns a sound with same valence level (levels from 1-4).
    // There are 4 valence levels for 4 threshold distances
    private Sound SelectSoundFromDistance(float dist)
    {
        Sound ret = new Sound();

        if(dist < distances[0])
        {
            // first valence level
            var valSounds = FindByValenceLevel(1);
            int soundId = UnityEngine.Random.Range(0, valSounds.Count); ;
            ret = valSounds[soundId];

        }
        else if (dist < distances[1])
        {
            // second valence level
            var valSounds = FindByValenceLevel(2);
            int soundId = UnityEngine.Random.Range(0, valSounds.Count); ;
            ret = valSounds[soundId];
        }
        else if (dist < distances[2])
        {
            // third valence level
            var valSounds = FindByValenceLevel(3);
            int soundId = UnityEngine.Random.Range(0, valSounds.Count); ;
            ret = valSounds[soundId];
        }
        else
        { 
            // last valence level
            var valSounds = FindByValenceLevel(4);
            int soundId = UnityEngine.Random.Range(0, valSounds.Count); ;
            ret = valSounds[soundId];
        } 

        return ret;
    }

    // Functions for finding all sounds with given classification level
    private List<Sound> FindByValenceLevel(int level)
    {
        List<Sound> sameLevel = new List<Sound>();
        foreach(var s in sounds)
        {
            if (s.valenceLevel == level)
                sameLevel.Add(s);
        }

        return sameLevel;
    }
    private List<Sound> FindByTensionLevel(int level)
    {
        List<Sound> sameLevel = new List<Sound>();
        foreach (var s in sounds)
        {
            if (s.tensionLevel == level)
                sameLevel.Add(s);
        }

        return sameLevel;
    }
    ////////////////////////////////////////////////////
    
    private int calculateProgression(Sound sound)
    {
        return sound.tensionLevel - 1;
    }
    
    // Functions for creating the game object in the Unity scene, 
    // these will create the sound object with associated script and add them to a container
    private void CreateSoundObject(Sound s, Vector3 soundPos)
    {
        GameObject sound = new GameObject();
        sound.tag = "Sound";
        sound.transform.parent = soundContainer.transform;
        SoundBehaviour soundBehaviour = sound.AddComponent<SoundBehaviour>();
        soundBehaviour.Initialise(s, soundPos);
    }

    private void CreateSoundObject(int idx, Vector3 soundPos)
    {
        GameObject sound = new GameObject();
        sound.tag = "Sound";
        sound.transform.parent = soundContainer.transform;
        SoundBehaviour soundBehaviour = sound.AddComponent<SoundBehaviour>();
        soundBehaviour.Initialise(sounds[idx], soundPos);
    }
    /////////////////^///////////////////////////////////////////////////
    private void placeRandomSound()
    {
        Vector2 bounds = levelGen.getLevelBoundaries();
        // find position randomly
        float x = UnityEngine.Random.Range(0f, bounds.x - 1); ;
        float y = UnityEngine.Random.Range(0f, 6f);
        float z = UnityEngine.Random.Range(0f, bounds.y - 1);
        Vector3 soundPos = new Vector3(x, y, z);
        // pick sound randomly
        int idx = UnityEngine.Random.Range(0, sounds.Length - 1);
        //initialise sound object
        soundPlacementsRandom.Add(soundPos);
        CreateSoundObject(idx, soundPos);
    }

    // ALGORITHMS FUNCTIONS ///////////////
    // RANDOM OPTION : this algorithm places and selects the exact number of set sounds randomly.
    public void BaselineAlgorithm()
    {
        if (sounds.Length == 0) return;
        for(int i = 0; i < numberPlacedSounds; i++)
        {
            placeRandomSound();
        }

    }

    // OPTION 1 : this algorithm will pick random sounds and place them using paths and intersections,
    // uses player progression to select sounds. At first progression is 0 and after collecting items the sounds are updated.
    public void pathsAlgorithm()
    {
        //option1
        Debug.Log("option1");
        if (sounds.Length == 0) return;
        int placed = 0;

        // setup tension sounds by current progression
        int currentProgression = gc.collected + 1;
        var tensionSounds = FindByTensionLevel(currentProgression);

        foreach(var sPos in levelGen.getSoundPlacements())
        {

            if (placed == numberPlacedSounds) break;
            soundPlacementsOption1.Add(sPos);
            placed++;

            /*
            // pick sound randomly
            int idx = UnityEngine.Random.Range(0, sounds.Length);
            //initialise sound object
            CreateSoundObject(idx, sPos);
            */

            // pick a sound from the array of sounds with same tension level
            int soundId = UnityEngine.Random.Range(0, tensionSounds.Count); ;
            Sound s = tensionSounds[soundId];
            CreateSoundObject(s, sPos);


        }

        //update number of actually placed sounds
        numberPlacedSounds = placed;
        

    }
    public void updatePathsAlgorithm()
    {
        if (gc.collected == gc.collectibles) return;
        // destroy all sounds
        var sceneSounds = GameObject.FindGameObjectsWithTag("Sound");
        foreach(var s in sceneSounds)
        {
            Destroy(s);
        }

        // replace sounds
        pathsAlgorithm();
    }
    // OPTION 2 : this algorithm places sound according to distance to important rooms, uses valence to select sounds.
    public void distanceAlgorithm()
    {
        Debug.Log("option2");

        HashSet<Vector3> soundPlacementsOption2Tmp = new HashSet<Vector3>();

        // place sound inside collectible room, augment radius to place less impactful sounds
        foreach (var roomId in levelGen.CollectibleRooms)
        {
            var pos = levelGen.getRoomCenter(roomId);
            placed.Add(pos);
            soundPlacementsOption2Tmp.Add(pos);
        }

        // calculate intersections of circles centered in the collectible rooms to calculate possible sound placements
        if (placed != null)
        {
            foreach (var v1 in placed)
            {
                foreach (var v2 in placed)
                {
                    if (v1 != v2)
                    {
                        var c0 = new Vector2(v1.x, v1.z);
                        var c1 = new Vector2(v2.x, v2.z);

                        foreach (var r in distances)
                        {
                            Vector2 out1, out2;

                            int sols = FindCircleCircleIntersections(
                                    c0, r,
                                    c1, r,
                                    out out1, out out2
                                );

                            if (sols == 1)
                            {
                                if (levelGen.IsPosValid(out1))
                                {
                                    soundPlacementsOption2Tmp.Add(new Vector3(out1.x, 6, out1.y));
                                }
                            }
                            if (sols == 2)
                            {
                                if (levelGen.IsPosValid(out1))
                                {
                                    soundPlacementsOption2Tmp.Add(new Vector3(out1.x, 6, out1.y));
                                }
                                if (levelGen.IsPosValid(out2))
                                {
                                    soundPlacementsOption2Tmp.Add(new Vector3(out2.x, 6, out2.y));
                                }
                            }
                        }
                    }
                }

            }
        }
        Debug.Log("Raw number of spots:" + soundPlacementsOption2Tmp.Count);

        //refine if wanted
        if (levelGen.refinePlacement)
        {
           
            int iter = 1;
            float closeFactor = levelGen.soundCloseness;
            while (iter <= levelGen.refineIterations)
            {
                iter++;
                levelGen.refineSoundSpots(closeFactor, soundPlacementsOption2Tmp, 2);
                //closeFactor *= 0.95f;
            }

            Debug.Log("Refined number of spots:" + soundPlacementsOption2Tmp.Count);

            //replace sounds in middle of important rooms
            foreach (var roomId in levelGen.CollectibleRooms)
            {
                var pos = levelGen.getRoomCenter(roomId);
                placed.Add(pos);
                soundPlacementsOption2Tmp.Add(pos);
            }

            //refine twice

            levelGen.refineSoundSpots(levelGen.soundCloseness, soundPlacementsOption2Tmp, 2);
            levelGen.refineSoundSpots(levelGen.soundCloseness, soundPlacementsOption2Tmp, 2);

        }

        //select and place sounds
        int placedSounds = 0;

        foreach (var sPos in soundPlacementsOption2Tmp)
        {

            if (placedSounds == numberPlacedSounds) break;
            soundPlacementsOption2.Add(sPos);
            placedSounds++;

            /*
            // pick sound randomly
            int idx = UnityEngine.Random.Range(0, sounds.Length);
            //initialise sound object
            CreateSoundObject(idx, sPos);
            */

            //pick sound according to valence, e.g. to distance to closest collectible
            float closestDist = GetClosestCollectiblePos(sPos);
            Sound s = SelectSoundFromDistance(closestDist);
            CreateSoundObject(s, sPos);
        }

        //update number of actually placed sounds
        numberPlacedSounds = placedSounds;
    }

}


