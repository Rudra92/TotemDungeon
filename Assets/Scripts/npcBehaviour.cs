using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using UnityEngine.SceneManagement;

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
        collectibleRooms = levelGen.CollectibleRooms;
        // initialise useful variables
        isChasingPlayer = false;
        player = GameObject.FindGameObjectWithTag("Player");
        playerPos = GameObject.FindGameObjectWithTag("Player").transform;
        chaseCounter = timeToRecheck;

        //Start NPC behaviour
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

        GraphNode node1 = AstarPath.active.GetNearest(transform.position, NNConstraint.Default).node;
        GraphNode node2 = AstarPath.active.GetNearest(npcDst.transform.position, NNConstraint.Default).node;

        if (!PathUtilities.IsPathPossible(node1, node2))
        {
            Debug.Log("NPC Impossible path");
            //todo  change behaviour if game not possible
            SceneManager.LoadScene("Main");
            
        }

        path.maxSpeed = seekSpeed;
        do {
            currRoom = (currRoom + 1) % rooms.Count;
        } while (currRoom == levelGen.getFinalRoom());
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

}
