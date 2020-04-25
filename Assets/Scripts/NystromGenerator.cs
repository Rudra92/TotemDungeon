using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;


[RequireComponent(typeof(Seeker))]

public class NystromGenerator : MonoBehaviour {

    private string saveLevelPath = "Assets/Resources/Prefabs/SavedLevel/";
    private string saveRoomsPath = "Level/Rooms/";

    public static ReadOnlyCollection<Vector2Int> CardinalDirections =
    new List<Vector2Int> { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left }.AsReadOnly();

    enum TileType { None, Wall, RoomFloor, CorridorFloor, OpenDoor, ClosedDoor };

    private Grid grid;
    private Tilemap tilemap;
    public Tile WallTile;
    public Tile FloorTile;
    public Tile DoorTile;
    public Tile CorridorTile;

    int scale = 4;


    public GameObject wall;
    public GameObject floor;
    public GameObject door;
    public GameObject corridor;
    public GameObject collectible1;


    public bool showAllPaths;

    public float pathRecalculateRate = 1;
    public float distanceToRecalculate = 10;
    private Vector3 playerOldPosition;


    public GameObject PlayerPrefab;
    public GameObject npcPrefab;
    public GameObject endPlatformPrefab;

    private GameObject currPlayer, currNPC, currPlatform;


    private int[,] data;
    private int[,] regions;
    private int currRegion = -1;

    public int dimensions = 51;

    public int numRoomTries = 0;
    public int extraConnectorChance = 10;

    public int extraRoomSize = 0;
    public int windingPercent = 50;

    public bool removeDeadEnds = false;

    public int seed = 0;

    // essential containers
    public List<Rect> mRooms { get; private set; }
    private List<Vector3> doorPositions;


    private int collectibles = 5;
    public List<GameObject> mCollectibles { get; private set; }
    public HashSet<int> CollectibleRooms { get; private set; }
    private int finalRoomIdx;
    private List<Pathfinding.Path> importantPaths;

    public bool smoothPath = false;
    private GameController gameController;
    private SimpleSmoothModifier smoother;
    private HashSet<Vector3> intersections;

    private ReadOnlyCollection<int> Directions;


    Rect mBounds;
    LevelGrid<int> mRegions;
    LevelGrid<TileType> mLevelGrid; // store the tiles we'll use for later placement
    int mCurrentRegion = -1;

    private GameObject Level;
    private Seeker mSeeker;

    private bool pathComplete = false;
    private bool alreadySaved = false;

    private AudioManager audioManager;

    private void Awake()
    {
        audioManager = FindObjectOfType<AudioManager>();
        mSeeker = GetComponent<Seeker>();
        gameController = FindObjectOfType<GameController>();


        if (seed != 0)
                    Random.InitState(seed);

        mRooms = new List<Rect>();
        mCollectibles = new List<GameObject>();
        CollectibleRooms = new HashSet<int>();
        importantPaths = new List<Pathfinding.Path>();
        intersections = new HashSet<Vector3>();
    }

    // Use this for initialization
    void Start () {
       
    }

    void Update()
    {
        if (showAllPaths) ShowPaths();

        if (gameController.save && !alreadySaved)
        {
            SaveLevel();
            alreadySaved = true;
        }
    }

    private bool checkInsideRoom(Vector3 pos, Rect r)
    {
        Vector3 roomCenter = getRoomCenter(r);
        Vector3 TopLeft = new Vector3(roomCenter.x - r.width / 2 * scale, 4, roomCenter.z - r.height / 2 * scale);
        Vector3 BotRight = new Vector3(roomCenter.x + r.width / 2 * scale, 4, roomCenter.z + r.height / 2 * scale);

        if ((pos.x < TopLeft.x) ||
            (pos.x > BotRight.x) ||
            (pos.z < TopLeft.z) ||
            (pos.z > BotRight.z))
        {
            return false;
        }

        return true;
    }

    private bool nearDoor(Vector3 pos)
    {
        foreach(Vector3 p in doorPositions)
        {
            if( (p - pos).magnitude < 0.5f)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsInsideRoom(Vector3 pos, int roomIdx)
    {
        if (roomIdx < 0 || roomIdx >= mRooms.Count)
        {
            Debug.LogError("IsInsideRoom was given an invalid roomId");
            return false;
        }

        Rect r = mRooms[roomIdx];
        return checkInsideRoom(pos, r);     
    }

    public bool IsInsideRoom(Vector3 pos, Rect r)
    {
        
        return checkInsideRoom(pos, r);
    }

    private void smoothPaths()
    {
        if(smoother == null)
        {
            smoother = gameObject.AddComponent<SimpleSmoothModifier>();
        } 
        foreach(Pathfinding.Path p in importantPaths )
        {
           p.vectorPath = smoother.SmoothSimple(p.vectorPath);
        }
    } 

    private bool IsInsideAnyRoom(Vector3 pos)
    {
        foreach(Rect r in mRooms)
        {
            if (IsInsideRoom(pos, r))
            {
                return true;
            }
        }

        return false;
    }

    private int InsideRoomNumber(Vector3 pos)
    {
        for(int i = 0; i < mRooms.Count; i++)
        {
            if(IsInsideRoom(pos, i))
            {
                return i;
            }
        }

        return -1;
    }

    private Vector3 calculateRoomCenter(Rect room)
    {
        Vector2 roomPos = room.position;
        return new Vector3(roomPos.x + room.width / 2f - .5f, 1f, roomPos.y + room.height / 2f - .5f) * scale;
    }

    public Vector3 getRoomCenter(Rect room)
    {
        return calculateRoomCenter(room);
    }

    public Vector3 getRoomCenter(int roomId)
    {
        if (roomId < 0 || roomId >= mRooms.Count)
        {
            Debug.LogError("getRoomCenter was given an invalid roomId");
            return new Vector3();
        }
        Rect room = mRooms[roomId];
        return calculateRoomCenter(room);
    }

    public int getFinalRoom()
    {
        return finalRoomIdx;
    }

    private void placePlayer()
    {
        currPlayer = Instantiate(PlayerPrefab);
        currPlayer.name = "FPSPlayer";
        currPlayer.tag = "Player";
        // Player layer
        currPlayer.layer = 13;

        Vector3 pos = currPlatform.transform.position;
        // small offsets;
        pos.x += 3f;
        pos.y = 3;
        pos.z += 9;

        currPlayer.transform.position = pos;
        currPlayer.transform.forward = new Vector3(0, 0, -1);
        
        playerOldPosition = currPlayer.transform.position;
    }

    void placeCollectibles()
    {
        
        //choose 3 distinct random numbers representing important rooms
        while(CollectibleRooms.Count < collectibles)
        {
            int roomIndex;
            do
            {
                roomIndex = UnityEngine.Random.Range(0, mRooms.Count);
            } while (roomIndex  == finalRoomIdx);
            // retry if it is the final room
            CollectibleRooms.Add(roomIndex);
        }

        //place collectibles
        foreach (int i in CollectibleRooms)
        {
            Vector3 collPos = getRoomCenter(i);
            GameObject collectible = Instantiate(collectible1, collPos, Quaternion.identity);
            collectible.name = "Totem Piece";
            collectible.layer = 12;
            collectible.tag = "Collectible";
            mCollectibles.Add(collectible);
        }

    }

    private void placeNPC()
    {
        currNPC = Instantiate(npcPrefab);
        currNPC.name = "NPC";
        currNPC.tag = "NPC";
        currNPC.layer = 14;
        //place npc on the opposite end
        for (int i = dimensions - 1; i >=0 ; i--)
        {
            for (int j = dimensions - 1; j >= 0; j--)
            {
                if (mLevelGrid[i, j] == TileType.RoomFloor)
                {
                    currNPC.transform.position = new Vector3Int(i * scale, 4, j * scale);
                }
            }
        }

    }

    private void fillFinalRoom()
    {
        // pick large enough room
        for(int i = 0; i < mRooms.Count; i++)
        {
            Rect r = mRooms[i];

            if(r.width > 4 && r.height > 4)
            {
                finalRoomIdx = i;

                Vector3 platformPos = getRoomCenter(r);
                platformPos.x -= 3f;
                platformPos.y = 2.7f;
                platformPos.z -= 2f;
                currPlatform = Instantiate(endPlatformPrefab, platformPos, Quaternion.identity);
                currPlatform.transform.parent = Level.transform;
                currPlatform.name = "EndPlatform";
                // set platform child base layer for astar algorithm - layer Obstacle
                currPlatform.layer =  15;
                return;

            }
        }
    }

    public void OnPathComplete(Pathfinding.Path p)
    {
        pathComplete = true;
        // We got our path back
        if (p.error)
        {
            // Nooo, a valid path couldn't be found
            Debug.Log("No path found, restarting game");
            Restart();
        }
        

    }

    private void findSoundSpots()
    {
        
        // Find important intersections
        foreach(Pathfinding.Path p in importantPaths)
        {
            var p_list = p.vectorPath;
            for (int i = 0; i < p_list.Count; i++)
            {
                // if a position is a door position add intersection
                if (nearDoor(p_list[i]))
                {
                    intersections.Add(p_list[i]);
                    continue;
                }

                foreach (Pathfinding.Path q in importantPaths)
                {
                    if (p != q)
                    {
                        var q_list = q.vectorPath;
         
                            for (int j = 0; j < q_list.Count; j++)
                            {
                                bool previousEqual = false;
                                bool nextEqual = false;
                                bool p_prev_q_next = false;
                                bool q_prev_p_next = false;
                                bool currEqual = p_list[i] == q_list[j];

                                if(currEqual)
                                {
                                

                                    // check both previous
                                    if (i != 0 && j != 0)
                                    {
                                        //we are not at start we can check previous nodes
                                        previousEqual = p_list[i - 1] == q_list[j - 1];

                                    }
                                    else
                                    {
                                        continue;
                                    }
                                
                                    //check both next nodes
                                    if (i != p_list.Count - 1 && j != q_list.Count - 1)
                                    {
                                        nextEqual = p_list[i + 1] == q_list[j + 1];
                                    }
                                    else
                                    {
                                        continue;
                                    }


                                    //check permutations
                                    if (i != 0 && j != q_list.Count -  1)
                                    {
                                        p_prev_q_next = p_list[i + 1] == q_list[j - 1];

                                    }

                                    if (i != p_list.Count && j != 0)
                                    {
                                        //we are not at start we can check previous nodes
                                        q_prev_p_next = p_list[i - 1] == q_list[j + 1];

                                    }
                                

                                    if (!previousEqual ^ !nextEqual ^ !p_prev_q_next ^ !q_prev_p_next)
                                    {
                                        intersections.Add(p_list[i]);
          
                                    }
                                }
                            }
                        }
                    }
            }
        }
        Debug.Log("Raw number of spots : " + intersections.Count);
        for (int i = 0; i < 5; i++)
        {
            refineSoundSpots();
        }
        Debug.Log("Refined number of spots:" + intersections.Count);


    }

    private void refineSoundSpots()
    {
        HashSet<Vector3> toBeRemoved = new HashSet<Vector3>();
        HashSet<Vector3> toBeAdded = new HashSet<Vector3>();

        foreach(var b in intersections)
        {
            if (IsInsideAnyRoom(b))
            {
                toBeRemoved.Add(b);
            }
        }

        foreach (var v in toBeRemoved)
        {
            intersections.Remove(v);
        }

        foreach (Vector3 a in intersections)
        {
            Vector2 a_ = new Vector2(a.x, a.z);
            foreach(Vector3 b in intersections)
            {

                Vector2 b_ = new Vector2(b.x, b.z);
                if(a != b && !toBeRemoved.Contains(b))
                {
                    // check if they are close enough
                    if((a_- b_).magnitude < 5f)
                    {
                        toBeRemoved.Add(b);

                        toBeAdded.Add(new Vector3((a_.x + b_.x) / 2f, 2.5f, (a_.y + b_.y) / 2f) );
                    }
                }
            }
        }

        foreach(var v in toBeRemoved)
        {
            intersections.Remove(v);
        }

        foreach(var v in toBeAdded)
        {
            intersections.Add(v);
        }

     
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        if (intersections == null) return;

        foreach (Vector3 v in intersections)
        {
            if(!IsInsideAnyRoom(v))
                Gizmos.DrawSphere(v, 2f);
        }
    }

    private void ShowPaths()
    {
        foreach(Pathfinding.Path p in importantPaths)
        {
            for (int v = 0; v < p.vectorPath.Count - 1; v++)
            {
                Debug.DrawLine(p.vectorPath[v], p.vectorPath[v + 1], Color.magenta);
            }
        }

        
    }

    IEnumerator calculatePaths()
    {
        //  calculate paths from each collectible to another
        for (int i = 0; i < CollectibleRooms.Count; i++)
        {
            for (int j = 0; j < CollectibleRooms.Count; j++)
            {
                if (j <= i) continue;

                var point1 = getRoomCenter(CollectibleRooms.ElementAt(i));
                var point2 = getRoomCenter(CollectibleRooms.ElementAt(j));

                GraphNode node1 = AstarPath.active.GetNearest(point1, NNConstraint.Default).node;
                GraphNode node2 = AstarPath.active.GetNearest(point2, NNConstraint.Default).node;

                if (!PathUtilities.IsPathPossible(node1, node2))
                {
                    //Impossible game, restart
                    Restart();
                }                

                var path = mSeeker.StartPath(point1, point2, OnPathComplete);

                yield return StartCoroutine(path.WaitForPath());

                importantPaths.Add(path);
            }
        }

        // calculate paths from starting player position to each collectible 
        for (int j = 0; j < CollectibleRooms.Count; j++)
        {
            var path = mSeeker.StartPath(getRoomCenter(CollectibleRooms.ElementAt(j)), currPlayer.transform.position, OnPathComplete);

            yield return StartCoroutine(path.WaitForPath());

            importantPaths.Add(path);
            
        }

        // smooth if set on
        if(smoothPath)
        {
            smoothPaths();
        }

        //  calculate inersections
        findSoundSpots();
    }

    void printRegion(int[,] reg)
    {
        string s = "Regions  :" + System.Environment.NewLine;
        int rMax = reg.GetUpperBound(0);
        int cMax = reg.GetUpperBound(1);
        for (int i = 0; i < rMax; i++)
        {
            for (int j = 0; j < cMax; j++)
            {
                s += " " + reg[i, j] + " ";
            }
            s += System.Environment.NewLine;

        }

        Debug.Log(s);
    }

    void printMaze(int[,] maze)
    { 
                //print data
        string s = "Maze  :" + System.Environment.NewLine;
        int rMax = maze.GetUpperBound(0);
        int cMax = maze.GetUpperBound(1);
        for (int i = 0; i < rMax; i++)
        {
            for (int j = 0; j < cMax; j++)
            {
                if (maze[i, j] == 1)
                {
                    s += " | ";
                }
                else
                {
                    s += " _ ";
                }
            }
            s += System.Environment.NewLine;

        }

        Debug.Log(s);
    }

	void Generate(int width, int height)
    {

        if (width % 2 == 0 || height % 2 == 0)
        {
            throw new System.Exception("The stage must be odd-sized.");
        }
        Level = new GameObject(); Level.name = "Level";

        // setup Grid and tilemap for level generation
        GameObject Grid = new GameObject(); Grid.name = "Grid";
        Grid.transform.parent = Level.transform;
        grid = Grid.AddComponent<Grid>();
   
        GameObject Tilemap = new GameObject(); Tilemap.name = "Tilemap";
        Tilemap.transform.parent = Grid.transform;
        tilemap = Tilemap.AddComponent<Tilemap>();

        mBounds = new Rect(0, 0, width, height);

        mLevelGrid = new LevelGrid<TileType>(width, height);
        mLevelGrid.Fill(TileType.Wall);

        mRegions = new LevelGrid<int>(width, height);
        mRegions.Fill(-1);
        
        AddRooms();
       
        // Fill in all of the empty space with mazes.
        for (int y = 1; y < mBounds.height; y += 2)
        {
            for (int x = 1; x < mBounds.width; x += 2)
            {
                var pos = new Vector2Int(x, y);
                if (mLevelGrid[pos] != TileType.Wall) continue;

                GrowMaze(pos);
            }
        }

        ConnectRegions();

        if ( removeDeadEnds)
            RemoveDeadEnds();

        StartCoroutine(SetAllTiles());
    }

    IEnumerator SetAllTiles()
    {
        float wallOffset = 0.5f;
        // initialise needed lists
        doorPositions = new List<Vector3>();

        for (int x = 0; x < mLevelGrid.Width; x++)
            for (int y = 0; y < mLevelGrid.Height; y++)
            {
                {
                    var position = new Vector3Int(x, y, 0);
                    var roofPos = new Vector3Int(x, 2, y);
                    var realPos = new Vector3(x, 0, y);

                    //set roof
                    var Roof = Instantiate(corridor, roofPos * scale, Quaternion.identity);
                    Roof.name = "Floor";
                    Roof.transform.SetParent(Level.transform);
                    Roof.transform.localScale *= scale;
                    Roof.layer = 9;

                    switch (mLevelGrid[x,y])
                    {
                        case TileType.Wall:
                            tilemap.SetTile(position, WallTile);
                            
                            var Wall = Instantiate(wall, new Vector3(realPos.x, wallOffset, realPos.z) * scale, Quaternion.identity);
                            Wall.name = "Wall";
                            Wall.transform.SetParent(Level.transform);
                            Wall.transform.localScale *= scale;
                            Wall.layer = 11;
                            Wall.tag = "Wall";
                            break;
                        case TileType.RoomFloor:
                            tilemap.SetTile(position, FloorTile);
                            var Floor = Instantiate(floor, realPos * scale, Quaternion.identity);
                            Floor.name = "Floor";
                            Floor.transform.SetParent(Level.transform);
                            Floor.layer = 8;
                            Floor.transform.localScale *= scale;
                            break;
                        case TileType.CorridorFloor:
                            tilemap.SetTile(position, FloorTile);
                            var Corridor = Instantiate(corridor, realPos * scale, Quaternion.identity);
                            Corridor.name = "Corridor";
                            Corridor.transform.SetParent(Level.transform);
                            Corridor.layer = 8;
                            Corridor.transform.localScale *= scale;
                            break;
                        case TileType.OpenDoor:
                        case TileType.ClosedDoor:
                            tilemap.SetTile(position, CorridorTile);
                            var Door = Instantiate(door, realPos * scale, Quaternion.identity);
                            Door.name = "Door";
                            Door.transform.SetParent(Level.transform);
                            Door.transform.localScale *= scale;
                            // add this position to container
                            Vector3 doorCenterElevated = Door.transform.position;
                            doorCenterElevated.y = AstarPath.active.transform.position.y;
                            doorPositions.Add(doorCenterElevated);

                            break;
                        default:
                            tilemap.SetTile(position, FloorTile);
                            break;
                    }

                }
            }
       
        yield return null;
    }

    //Growing tree maze
    void GrowMaze(Vector2Int start)
    {
        var cells = new LinkedList<Vector2Int>();

        Vector2Int lastDir = Vector2Int.zero; //won't be a cardinal direction


        StartRegion();
        Carve(start, TileType.CorridorFloor);

        cells.AddFirst(start);

        while ( cells.Count != 0)
        {
            var cell = cells.Last.Value;

            //see which adjacent cells are open

            var unmadeCells =  new List<Vector2Int>();

            foreach ( var dir in CardinalDirections)
            {
                if (CanCarve(cell, dir)) unmadeCells.Add(dir);
            }

            if (unmadeCells.Count != 0)
            {
                Vector2Int dir;

                if (lastDir != Vector2Int.zero && unmadeCells.Contains(lastDir) && Random.Range(0, 100) > windingPercent)
                {
                    dir = lastDir;
                }
                else
                {
                    int idx = Random.Range(0, unmadeCells.Count);
                    dir = unmadeCells[idx];
                }


                Carve(cell + dir, TileType.CorridorFloor);
                Carve(cell + dir * 2, TileType.CorridorFloor);

                cells.AddLast(cell + dir * 2);
                lastDir = dir;
            }
            else
            {
                //no adjacent uncarved cells
                cells.RemoveLast();
                lastDir = Vector2Int.zero;
            }
        }

    }

    void AddRoomsMatrix()
    {
        for (int i = 0; i < numRoomTries; i++)
        {
            int size = Random.Range(1, 3 + extraRoomSize) * 2 + 1;
            int rectangularity = Random.Range(0, 1 + size / 2) * 2;

            int width = size;
            int height = size;

            if (Random.Range(1, 2) == 1)
            {
                width += rectangularity;
            }
            else
            {
                height += rectangularity;
            }

            int x = Random.Range(0, (mLevelGrid.Width - width) / 2) * 2 + 1;
            int y = Random.Range(0, (mLevelGrid.Height - height) / 2) * 2 + 1;

            Rect room = new Rect(x, y, width, height);

            bool overlaps = false;

            foreach (var other in mRooms)
            {
                if (room.Overlaps(other))
                {
                    overlaps = true;
                    break;
                }
            }

            if (overlaps) continue;

            mRooms.Add(room);

            startRegionMatrix();

            for (int m = 0; m <  width; m++)
            {
                for (int n = 0; n < height; n++)
                {
                    CarveMatrix(new Vector2Int(m, n));
                }

            }
        }
    }

    void AddRooms()
    {
        for (int i = 0; i < numRoomTries; i++)
        {
            int size = Random.Range(1, 3 + extraRoomSize) * 2 + 1;
            int rectangularity = Random.Range(0, 1 + size / 2) * 2;

            int width = size;
            int height = size;

            if (Random.Range(1, 2) == 1)
            {
                width += rectangularity;
            }
            else
            {
                height += rectangularity;
            }

            int x = Random.Range(0, (mLevelGrid.Width - width) / 2) * 2 + 1;
            int y = Random.Range(0, (mLevelGrid.Height - height) / 2) * 2 + 1;

            Rect room = new Rect(x, y, width, height);

            bool overlaps = false;

            foreach (var other in mRooms)
            {
                if (room.Overlaps(other))
                {
                    overlaps = true;
                    break;
                }
            }

            if (overlaps) continue;

            mRooms.Add(room);

            StartRegion();

            for (int m = x; m < x + width; m++)
            {
                for ( int n = y; n < y + height; n++)
                {
                    Carve(new Vector2Int(m,n));
                }

            }
        }
    }

    void ConnectRegions()
    {
        //find all the tiles that can connect two or more region

        Dictionary<Vector2Int, HashSet<int>> connectorRegions = new Dictionary<Vector2Int, HashSet<int>>();

        for (int y = 1; y < mBounds.height - 1; y++)
        {
                for (int x = 1; x < mBounds.width - 1; x++)
                {
                var pos = new Vector2Int(x, y);

                if (mLevelGrid[pos] != TileType.Wall) continue;

                var regions = new HashSet<int>();

                foreach( var dir in  CardinalDirections)
                {
                    var region = mRegions[pos + dir];
                    if ( region != -1) regions.Add(region);

                }

                if (regions.Count < 2) continue;

                connectorRegions[pos] = regions;

            }
        }

        var connectors = new HashSet<Vector2Int>(connectorRegions.Keys);
        // Keep track of which regions have been merged. This maps an original
        // region index to the one it has been merged to.
        var merged = new SortedDictionary<int, int>();
        var openRegions = new List<int>();
        for (int i = 0; i <= mCurrentRegion; i++)
        {
            merged.Add(i, i);
            openRegions.Add(i);
        }


        // Keep connecting regions until we're down to one.
        while (openRegions.Count > 0)
        {

            var idx = Random.Range(0, connectors.Count);
            Vector2Int connector = connectors.ElementAt(idx);

            // Carve the connection.
            AddJunction(connector);

            // Merge the connected regions. We'll pick one region (arbitrarily) and
            // map all of the other regions to its index.
            var regions = connectorRegions[connector].Select(region => merged[region]);

            var dest = regions.First();
            var sources = regions.Skip(1).ToList();

            // Merge all of the affected regions. We have to look at *all* of the
            // regions because other regions may have previously been merged with
            // some of the ones we're merging now.
            for (var i = 0; i <= mCurrentRegion; i++)
            {
                if (sources.Contains(merged[i]))
                {
                    merged[i] = dest;
                }
            }

            // The sources are no longer in use.
            openRegions.RemoveAll(sources.Contains);

            // Remove any connectors that aren't needed anymore.
            connectors.RemoveWhere(pos => {
                // Don't allow connectors right next to each other.
                if ((connector - pos).magnitude < 2) return true;

                // If the connector no long spans different regions, we don't need it.
                var local_regions = connectorRegions[pos].Select(region => merged[region]).ToList();

                if (local_regions.Count > 1) return false;

                // This connecter isn't needed, but connect it occasionally so that the
                // dungeon isn't singly-connected.

                if (OneIn(extraConnectorChance)) AddJunction(pos);

                return true;
            });
        }
    }

    void AddJunction(Vector2Int pos)
    {
        if (OneIn(4))
        {
            mLevelGrid[pos] = OneIn(3) ? TileType.OpenDoor : TileType.RoomFloor;
        }
        else
            mLevelGrid[pos] = TileType.ClosedDoor;
    }

    void RemoveDeadEnds()
    {
        bool done = false;

        while (!done)
        {
            done = true;

            for (int x = 1; x < mBounds.width-1; x++)
            {
                for (int y = 1; y < mBounds.height-1; y++)
                {
                    var pos = new Vector2Int(x, y);
                    if (mLevelGrid[pos] == TileType.Wall) continue;

                    int exits = 0;
                    foreach ( var dir in  CardinalDirections)
                    {
                        if (mLevelGrid[pos + dir] != TileType.Wall) exits++;
                    }

                    if (exits != 1) continue;

                    done = false;

                    mLevelGrid[pos] = TileType.Wall;
                }
            }
        }

    }

    /// Gets whether or not an opening can be carved from the given starting
    /// [Cell] at [pos] to the adjacent Cell facing [direction]. Returns `true`
    /// if the starting Cell is in bounds and the destination Cell is filled
    /// (or out of bounds).</returns>
    bool CanCarve(Vector2Int pos, Vector2Int direction )
    {
        if (!mBounds.Contains(pos + direction * 3)) return false;

        return mLevelGrid[pos + direction * 2] == TileType.Wall;
    }

    void StartRegion()
    {
        mCurrentRegion++;
    }

    void startRegionMatrix()
    {
        currRegion++;
    }

    void Carve(Vector2Int pos)
    {
        mLevelGrid[pos] = TileType.RoomFloor;
        mRegions[pos] = mCurrentRegion;
    }

    void CarveMatrix(Vector2Int pos)
    {
        data[pos.x, pos.y] = 0;
        regions[pos.x, pos.y] = currRegion;
    }

    void Carve(Vector2Int pos, TileType type = TileType.None)
    {
        if (type == TileType.None) type = TileType.RoomFloor;

        mLevelGrid[pos] = type;
        mRegions[pos] = mCurrentRegion;

    }

    public bool OneIn(int value)
    {
        return (Random.Range(0, value) == 1);
    }

    public void QuitGame()
    {
        // save any game data here
        #if UNITY_EDITOR
        // Application.Quit() does not work in the editor so
        // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif

    }

    private void Restart()
    {
        SceneManager.LoadScene("Main");
    }

    public void setupGame(int _collectibles, bool npc, bool load)
    {

        if (!load)
        {
            Generate(dimensions, dimensions);
        } else
        {
            Debug.Log("Loading Level from data");
            LoadLevel();

        }
        Debug.Log("Fill Initial Room..");
        fillFinalRoom();
        collectibles = _collectibles;


        Debug.Log("Place Collectibles..");
        placeCollectibles();

        Debug.Log("Initialise Player..");
        placePlayer();
        if(npc) placeNPC();

        AstarPath.FindAstarPath();
        AstarPath.active.Scan();

        StartCoroutine("calculatePaths");

    }

    // save and load a pre generated level
    private void LoadLevel()
    {
        mRooms = new List<Rect>();
        
        // Load saved Level prefab
        GameObject prefab = AssetDatabase.LoadAssetAtPath(saveLevelPath + "Level.prefab", typeof(GameObject)) as GameObject;

        if (prefab == null)
        {
            Debug.LogError("No files to load from");
            QuitGame();
            return;
        }
        Level = Instantiate(prefab);
        Level.name = "Level";

        //Load Rooms

        var info = new DirectoryInfo(saveRoomsPath);
        var roomFileInfo = info.GetFiles("*.room");
        foreach (var file in roomFileInfo)
        {
        
            StreamReader roomReader = new StreamReader(file.FullName);
            var sStrings = roomReader.ReadToEnd().Split(","[0]);

            Rect r = new Rect(
                float.Parse(sStrings[0]),
                float.Parse(sStrings[1]),
                float.Parse(sStrings[2]),
                float.Parse(sStrings[3])
                );
            mRooms.Add(r);
            
        }

        //load door positions
        var doorFileInfo = info.GetFiles("*.doorPos");
        doorPositions = new List<Vector3>();

        StreamReader doorReader = new StreamReader(doorFileInfo[0].FullName);
        var dStrings = doorReader.ReadToEnd().Split("\n"[0]);

        foreach(string s in dStrings)
        {
            if (s == "") { continue; }

            var values = s.Split(","[0]);
            Vector3 pos = new Vector3(
                    float.Parse(values[0]),
                    float.Parse(values[1]),
                    float.Parse(values[2])
                );
            doorPositions.Add(pos);

        }

    }

    private void SaveLevel()
    {
         
        //save Level
        string localPath = saveLevelPath + "Level.prefab";
     
        if (Directory.Exists(saveLevelPath))
        {
            //overwrite
            FileUtil.DeleteFileOrDirectory(saveLevelPath);
        }
        Directory.CreateDirectory(saveLevelPath);
        // Create the new Prefab.
        PrefabUtility.SaveAsPrefabAssetAndConnect(Level, localPath, InteractionMode.UserAction);


        //save rooms
        if (Directory.Exists(saveRoomsPath))
        {
            //overwrite
            FileUtil.DeleteFileOrDirectory(saveRoomsPath);
        }
        Directory.CreateDirectory(saveRoomsPath);
        foreach (var p in mRooms.Select((value, index) => new { value, index }) )
        {

            string filename = saveRoomsPath+p.index + ".room";
      
            //Write some text to the test.txt file
            StreamWriter writer = new StreamWriter(filename, true);

            writer.WriteLine(p.value.x +","+ p.value.y + ","+ p.value.width + "," + p.value.height);
   
            writer.Close();
        }

        //save door positions
        string file = saveRoomsPath + ".doorPos";
        StreamWriter doorWriter = new StreamWriter(file, true);
        foreach (var p in doorPositions)
        {
            doorWriter.WriteLine(p.x + "," + p.y + "," +p.z);
        }
        
        doorWriter.Close();
    }
}
