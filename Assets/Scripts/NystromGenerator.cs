using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

using Pathfinding;

public class NystromGenerator : MonoBehaviour {

    public static ReadOnlyCollection<Vector2Int> CardinalDirections =
    new List<Vector2Int> { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left }.AsReadOnly();

    enum TileType { None, Wall, RoomFloor, CorridorFloor, OpenDoor, ClosedDoor };

    public Grid grid;
    public Tilemap tilemap;
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



    public GameObject player;
    public GameObject npc;


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

    public List<Rect> mRooms { get; private set; }
    public HashSet<int> ImportantRoomsByIndex { get; private set; }
    public Vector3 getRoomCenter(int roomId)
    {
        Vector2 roomPos = mRooms[roomId].position;
        return new Vector3(roomPos.x + mRooms[roomId].width / 2f - .5f, 1f, roomPos.y + mRooms[roomId].height / 2f - .5f) * scale;
    }

    private ReadOnlyCollection<int> Directions;


    Rect mBounds;
    LevelGrid<int> mRegions;
    LevelGrid<TileType> mLevelGrid; // store the tiles we'll use for later placement
    int mCurrentRegion = -1;

    private GameObject Level;


	// Use this for initialization
	void Start () {


        if (seed != 0)
            Random.InitState(seed);

        mRooms = new List<Rect>();
        ImportantRoomsByIndex = new HashSet<int>();

        Level = new GameObject(); Level.name = "Level"; 

        Generate(dimensions, dimensions);

        placePlayer();

        placeNPC();

        placeCollectibles();

        if(AstarPath.active.enabled) AstarPath.active.Scan();
    }

    void placePlayer()
    {

        player = Instantiate(player);
        player.name = "NPC";
        player.tag = "Player";
        player.layer = 13;

        for (int i = 0; i < dimensions; i++)
        {
            for(int j = 0; j < dimensions; j++)
            {
                if(mLevelGrid[i,j] == TileType.RoomFloor )
                {
                    player.transform.position = new Vector3Int(i*scale, 4, j*scale);
                }
            }
        }
    }

    void placeNPC()
    {
        npc = Instantiate(npc);
        npc.name = "NPC";
        npc.tag = "NPC";
        npc.layer = 14;
        //place npc on the opposite end
        for (int i = dimensions - 1; i >=0 ; i--)
        {
            for (int j = dimensions - 1; j >= 0; j--)
            {
                if (mLevelGrid[i, j] == TileType.RoomFloor)
                {
                    npc.transform.position = new Vector3Int(i * scale, 4, j * scale);
                }
            }
        }

    }



    void placeCollectibles()
    {
        int collNum = 3;
        //choose 3 distinct random numbers
        while(ImportantRoomsByIndex.Count < collNum)
        {
            int roomIndex = UnityEngine.Random.Range(0, mRooms.Count);
            ImportantRoomsByIndex.Add(roomIndex);
            //Debug.Log(roomIndex + " from : " + mRooms.Count);

        }

        foreach (int i in ImportantRoomsByIndex)
        {
            //place collectible
            Vector3 collPos = getRoomCenter(i);
            GameObject collectible = Instantiate(collectible1, collPos, Quaternion.identity);
            collectible.name = "Totem Piece";
            collectible.layer = 12;
        }
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
                            break;
                        default:
                            //tilemap.SetTile(position, FloorTile);
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


}
