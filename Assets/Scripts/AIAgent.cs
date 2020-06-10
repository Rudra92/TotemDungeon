using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;
using System.IO;
using UnityEditor;

public class AIAgent : MonoBehaviour
{

    private NystromGenerator levelgen;
    private AIDestinationSetter destinationAI;
    private AIPath path;
    private SimpleSmoothModifier smoother;

    private GameObject AIDstContainer;
    private List<int> notVisited;
    private float fieldOfView = 140;
    private float viewDistance = 50;
    private LayerMask whatToDetect;

    private GameController gc;
    private int roomDst;

    private AudioManager am;

    void Awake()
    {
        gc = FindObjectOfType<GameController>();
        am = FindObjectOfType<AudioManager>();

        whatToDetect = LayerMask.GetMask("Walls", "Collectibles");
    }
    // Start is called before the first frame update
    void Start()
    {

        levelgen = FindObjectOfType<NystromGenerator>();
        path = gameObject.AddComponent<AIPath>();
        path.maxSpeed = gc.playerSpeed;
        path.rotationSpeed = 200;
        smoother = gameObject.AddComponent<SimpleSmoothModifier>();
        destinationAI = gameObject.AddComponent<AIDestinationSetter>();

        AIDstContainer = new GameObject();
        AIDstContainer.name = "AIPlayerDst";
        notVisited = Enumerable.Range(0, levelgen.mRooms.Count).ToList();
        notVisited.Remove(levelgen.getInitialRoom());

        covered = new List<Vector3>();

        SetNextDest();


        StartCoroutine(behaviourRoutine());
    }
    void Update()
    {
        /*
        foreach(int rid in notVisited)
        {
            levelgen.drawRoom(rid);
        }
        */
    }


    IEnumerator behaviourRoutine()
    {
        while(true)
        {
            if (gc.collected == gc.collectibles) break;
            UpdateBehaviour();
            yield return new WaitForSeconds(1);
        }

    }

    private void SetNextDest()
    {
        if (notVisited.Count == 0) Debug.Log("Problem");
        int randomIndex = UnityEngine.Random.Range(0, notVisited.Count);
        roomDst = notVisited[randomIndex];
        AIDstContainer.transform.position = levelgen.getRoomCenter(notVisited[randomIndex]);
        destinationAI.target = AIDstContainer.transform;
    }

    private void SetPrevDest()
    {
        
        AIDstContainer.transform.position = levelgen.getRoomCenter(roomDst);
        destinationAI.target = AIDstContainer.transform;
    }

    private void CheckAllCollectibles()
    {
        var collectibles = GameObject.FindGameObjectsWithTag("Collectible");
        foreach(var c in collectibles)
        {
            if (checkForCollectible(c)) return;
        }
    }

    private void CheckInsideNotVisited()
    {
        int id = -1;
        foreach(var rId in notVisited)
        {
            if(levelgen.IsInsideRoom(transform.position, rId))
            {
                id = rId;
                break;
            }
        }

        if(id != -1)
        {
            notVisited.Remove(id);
        }
    }
    private List<Vector3> covered;
    private void OnDrawGizmos()
    {
        
        Gizmos.color = Color.black;

        foreach (var v in covered)
        {
            Gizmos.DrawSphere(new Vector3(v.x, 6, v.z), 1f);
        }
        
    }

    private bool checkForCollectible(GameObject collectible)
    {

        Vector3 rayDirection = collectible.transform.position - gameObject.transform.position;

        RaycastHit hitInfo;

        if ((Vector3.Angle(rayDirection, transform.forward)) <= fieldOfView * 0.5f)
        {
            // Detect if player is within the field of view
            if (Physics.Raycast(this.transform.position, rayDirection, out hitInfo, viewDistance, whatToDetect))
            {
                if (hitInfo.collider != null)
                {
                    //Found a collider we need to check if it is a collectible
                    if (hitInfo.collider.CompareTag("Collectible"))
                    {

                        AIDstContainer.transform.position = collectible.transform.position;
                        destinationAI.target = AIDstContainer.transform;
                        return true;
                    }

                }
            }
        }
        return false;
    }

    private void UpdateBehaviour()
    {

        //if sees collectibles go for them
        CheckAllCollectibles();
        CheckInsideNotVisited();

        if(path.remainingDistance < 1f)
        {
            // check if player is inside room he was meant to go since he may find a collectible and go for it in another room
            // todo add way to get the room number he is in and remove it
            if(levelgen.IsInsideRoom(transform.position, roomDst))
            {
                // he is so we mark it as visited
                notVisited.Remove(roomDst);
                SetNextDest();
            } else
            {  
                // resume destination
                SetPrevDest();

            }
            
        }
    }

    public float EvaluateSound()
    {
        string savePath = "Level/Eval/";

        if (Directory.Exists(savePath))
        {
            //overwrite
            //FileUtil.DeleteFileOrDirectory(savePath);
        }
        Directory.CreateDirectory(savePath);

        string alg = "";
        if (gc.option1) alg += "1";
        if (gc.option2) alg += "2";
        if (gc.randomAlgorithm) alg += "random";

        string evalFile = "Level/Eval/" + alg  + ".eval";
        System.IO.StreamWriter evalWriter = new System.IO.StreamWriter(evalFile, true);
        

        //todo give weights
        float totalScore = 0f;

        int criterias = 5;
        //total number of tracks played (not counting theme)
        float countCriteria = 0f;
        //Repetition Criteria:
        // If sound is never listened to: -1  -- is it releavant since there is a previous criteria which checks if all sounds are listened to
        // if sound is listened to 1 time : +2
        // if sound is listened to 2-3 times : +0.5
        // if sound is listened to more than 3 times : -1
        // normalise to fit [0-1] - perfect value is number of different sounds * 2
        float repetitiveCriteria = 0f;
        float pathCoverageCriteria = 0f;
        float progressionCoherenceCriteria = 0f;


        int nbTracksPlayed = 0;
        Debug.Log(am.numberPlacedSounds);
        float maxCountCriteria = gc.option1 ? am.numberPlacedSounds * 4 : am.numberPlacedSounds;

        // iterate over sound map to compute some criterias
        foreach(var pair in am.soundMap)
        {

            string name = pair.Key;
            EvalContainer ec = pair.Value;

            int soundCount = ec.playCount;
            Debug.Log("Sound : " + name + " played " + ec.playCount + " times.");
            
            switch(soundCount)
            {
                case 0:
                    break;
                case 1:
                    repetitiveCriteria += 2;
                    break;
                case 2:
                    repetitiveCriteria += 1;
                    break;
                case 3:
                    repetitiveCriteria += 0.5f;
                    break;
                default:
                    repetitiveCriteria -= 1;
                    break;
            }

            // sound picked evaluation
            // is the sound played at the correct time with appropriate tension level
            // progression coherence criteria only applies if the sound was played at least once
            if (soundCount > 0)
            {
                
                nbTracksPlayed++;

                float currSoundCoherence = 0f;
                foreach( var lvl in ec.numCollectWhenPlayed)
                {
               
                    if(ec.classification == lvl)
                    {
                        currSoundCoherence += 2;
                    }

                    if (ec.classification == lvl + 1 || ec.classification == lvl-1 )
                    {
                        currSoundCoherence += 1;
                    }

                    // if the difference of progression when sound was played is too different then score is 0
                    Debug.Log("Sound : " + name + " classification : "+ ec.classification + " vs  " + lvl 
                        + " -> " + currSoundCoherence + " coherence.");

                }

                progressionCoherenceCriteria += (currSoundCoherence / soundCount);
                Debug.Log("Sound : " + name + " : " + progressionCoherenceCriteria + " coherence.");
            }

        }

        countCriteria = nbTracksPlayed/ maxCountCriteria;

        float maxRepetitiveCriteria = (nbTracksPlayed * 2);
        repetitiveCriteria = repetitiveCriteria < 0 ? 0 :  repetitiveCriteria / maxRepetitiveCriteria;

        float maxCoherenceCriteria = maxRepetitiveCriteria;
        progressionCoherenceCriteria /= maxCoherenceCriteria; 

        Debug.Log("Count Criteria score : " + countCriteria);
        Debug.Log("Repetitive Criteria score : " + repetitiveCriteria);
        Debug.Log("Level Coherence Criteria score : " + progressionCoherenceCriteria);

        evalWriter.WriteLine("Count Criteria score : " + countCriteria);
        evalWriter.WriteLine("Repetitive Criteria score : " + repetitiveCriteria);
        evalWriter.WriteLine("Level Coherence Criteria score : " + progressionCoherenceCriteria);


        //coverage - 
        //1. Total walkable level coverage



        //2. Important path coverage
        List<float> pathCoverage = new List<float>();
        var paths = levelgen.getPaths();
        var soundObjects = GameObject.FindGameObjectsWithTag("Sound");
        var gg = AstarPath.active.data.gridGraph;

        foreach (var p in paths)
        {
            float path_covered = 0; 
           
            var p_list = p.vectorPath;
            for (int i = 0; i < p_list.Count; i++)
            {
                foreach(var so in soundObjects)
                {
                    if( (p_list[i] - so.transform.position).magnitude < 5f)
                    {
                        path_covered++;
                        covered.Add(p_list[i]);
                        
                        break;
                    }
                }

            }
            pathCoverage.Add(path_covered / p_list.Count);
        }
                 
        foreach(var f in pathCoverage)
        {
            pathCoverageCriteria += f;
        }

        float avgPathCoverage = pathCoverageCriteria /= paths.Count;


        pathCoverageCriteria = MapValue(0f, 0.5f, 0f, 1f, pathCoverageCriteria);

        Debug.Log("Path Coverage Criteria score : avg coverage : " + avgPathCoverage + " ->  score:" + pathCoverageCriteria);
        evalWriter.WriteLine("Path Coverage Criteria score : avg coverage : " + avgPathCoverage + " ->  score:" + pathCoverageCriteria);



        // Overlap score
        Debug.Log("Overlapped " + am.getOverlapCount() + " times");
        float overlapCriteria = MapValue(0,am.numberPlacedSounds,1,0, am.getOverlapCount());
        Debug.Log("Overlap Criteria : " + overlapCriteria);
        evalWriter.WriteLine("Overlap Criteria : " + overlapCriteria);

        float weights = 
            gc.countCrit +
            gc.repCrit +
            gc.covCrit +
            gc.progCrit +
            gc.overCrit;

        totalScore += 
            countCriteria * gc.countCrit +
            repetitiveCriteria * gc.repCrit +
            pathCoverageCriteria * gc.covCrit +
            progressionCoherenceCriteria * gc.progCrit +
            overlapCriteria * gc.overCrit;
        totalScore /= weights;

        evalWriter.WriteLine("Total score : " + totalScore);
        evalWriter.WriteLine("------------------------------------------");
        evalWriter.Close();

        return totalScore;
    }

    private float MapValue(float a0, float a1, float b0, float b1, float a)
    {
        return b0 + (b1 - b0) * ((a - a0) / (a1 - a0));
    }
}
