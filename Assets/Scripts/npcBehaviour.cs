using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(AIDestinationSetter))]
[RequireComponent(typeof(AIPath))]

public class npcBehaviour : MonoBehaviour
{

    [SerializeField] private Material seekingMat;
    [SerializeField] private Material foundMat;
    private GameObject player;
    private NystromGenerator levelGen; //gives access to rooms 
    [SerializeField] private float rayDistance;
    [SerializeField] private LayerMask whatToDetect;
    [SerializeField] private float fieldOfView;
    [SerializeField] private float timeToRecheck;
    [SerializeField] private float refreshRate;

    private GameController controller;

    private List<Rect> rooms;
    private HashSet<int> collectibleRooms;
    bool chasing = false;
    private AIDestinationSetter AIDst;
    private AIPath path;
    private bool isChasingPlayer;
    private Transform playerPos;
    private int currRoom = 0;
    private Renderer thisRenderer;
    private GameObject npcDst;
    private float chaseCounter;

    private float seekSpeed;
    private float chaseSpeed;

    void Start()
    {
        // initialise vital variables from config
        controller = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
        seekSpeed = controller.npcSeekSpeed;
        chaseSpeed = controller.npcChaseSpeed;

        thisRenderer = GetComponentInChildren<Renderer>();
        // initialise game object containing npc path destination
        npcDst = new GameObject();
        npcDst.name = "NPCDst";
        AIDst = GetComponent<AIDestinationSetter>();
        path = GetComponent<AIPath>();
        // initialise generator script and associated variables
        GameObject generator = GameObject.FindGameObjectWithTag("Generator");
        levelGen = generator.GetComponent<NystromGenerator>();
        rooms = levelGen.mRooms;
        collectibleRooms = levelGen.ImportantRoomsByIndex;
        // initialise useful variables
        isChasingPlayer = false;
        player = GameObject.FindGameObjectWithTag("Player");
        playerPos = GameObject.FindGameObjectWithTag("Player").transform;
        chaseCounter = timeToRecheck;

        StartCoroutine("behaviourRoutine");
    }

    IEnumerator behaviourRoutine()
    {
        for (; ; )
        {
            // execute block of code here
            updateBehaviour();
            yield return new WaitForSeconds(refreshRate);
        }
    }

    void updateBehaviour()
    {
        Debug.Log(chaseCounter);
        // if there is currently a path
        if (path.hasPath)
        {
            // if reaching the end
            if (path.remainingDistance < 2f)
            {
                // if not chasing the player
                if (!isChasingPlayer)
                {
                    setSeekingBehaviour();
                }
                else
                {
                    //end game
                    Debug.Log("Lose");
                }
            }
            else
            {

                if (isChasingPlayer)
                {
                    //check if player is still in line of sight
                    if (checkForPlayer())
                    {
                        //reset counter
                        chaseCounter = timeToRecheck;
                    } 
                    if (chaseCounter > 0)
                    {
                        chaseCounter -= refreshRate;
                    }
                    else
                    {
                        thisRenderer.material = seekingMat;
                        setSeekingBehaviour();
                    }
                } else
                {
                    checkForPlayer();
                }
                
                
            }
        }
        else
        {
            setSeekingBehaviour();

        }
    }

    // Update is called once per frame
    void Update()
    {
      
    }
    
 
    void setSeekingBehaviour()
    {
        Debug.Log("Look for room");
        chaseCounter = timeToRecheck;
        npcDst.transform.position = levelGen.getRoomCenter(currRoom);
        AIDst.target = npcDst.transform;
        path.maxSpeed = seekSpeed;
        currRoom = (currRoom + 1) % rooms.Count;
        isChasingPlayer = false;

    }

    bool checkForPlayer()
    {

        Vector3 rayDirection = player.transform.position - this.transform.position;

        RaycastHit hitInfo;

        if ((Vector3.Angle(rayDirection, transform.forward)) <= fieldOfView * 0.5f)
        {
            // Detect if player is within the field of view
            if (Physics.Raycast(this.transform.position, rayDirection, out hitInfo, rayDistance, whatToDetect))
            {
                if (hitInfo.collider != null)
                {
                    //Found a collider we need to check if it is a player
                    if (hitInfo.collider.CompareTag("Player"))
                    {
                        Debug.Log("See player");
                        isChasingPlayer = true;
                        thisRenderer.material = foundMat;
                        AIDst.target = player.transform;
                        path.maxSpeed = chaseSpeed;

                        return true;
                    }
                    
                }
            }
        }
        return false;

    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta; // the color used to detect the player in front
        Gizmos.DrawRay(this.transform.position, this.transform.forward * rayDistance);
        Gizmos.color = Color.yellow; // the color used to detect the player from behind
        Gizmos.DrawRay(this.transform.position, this.transform.forward * -2f );
    }

}
