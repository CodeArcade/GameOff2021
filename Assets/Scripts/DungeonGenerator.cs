using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class DungeonGenerator : MonoBehaviour
{
    public Tilemap GroundTilemap;
    public Tilemap WallTilemap;

    public TileBase GroundTile;
    public TileBase WallTile;

    public GameObject PlayerPrefab;
    public Camera PlayerCamera;

    public int Height;
    public int Width;

    // Dictionaries are not shown in inspector :(
    public List<GameObject> Rooms;
    public List<int> RoomCounts;

    public GameObject SpawnRoom;
    public GameObject BossRoom;

    public int PathSize;
    public int WallSize;

    public List<GameObject> PathEnemies;
    public int PathEnemySpawnChance;
    public int HordeSpawnChance;

    private System.Random Random { get; set; }
    private int GridOffset { get; set; } = 50;
    //  private int GridBorder { get; set; } = 100; // A*
    private GameObject Player { get; set; }

    private bool GenerationStarted { get; set; }
    private bool GenerationFinished { get; set; }

    public GameObject Ui;

    public GameObject LoadingScreen;
    private Text MainLoadingLabel;
    private Text SideLoadingLabel;

    private List<Vector2> OnlyPathNodes;
    private List<Vector2> AllGroundNodes;
    //   private bool[,] AllGroundNodesArray; // A*, true = has path, false = no path
    private List<RoomSpawner> Spawner = new List<RoomSpawner>();

    void Awake()
    {
        GenerateDungeon();
        MainLoadingLabel = LoadingScreen.GetComponentsInChildren<Text>().First(x => x.name == "MainText");
        SideLoadingLabel = LoadingScreen.GetComponentsInChildren<Text>().First(x => x.name == "SideText");
        LoadingScreen.SetActive(true);
    }

    private void FixedUpdate()
    {
        if (!GenerationStarted)
        {
            GenerationStarted = true;
            StartCoroutine(GenerateDungeon());
        }

        if (GenerationFinished)
        {
            StopAllCoroutines();
            LoadingScreen.SetActive(false);
        }
    }

    private System.Collections.IEnumerator GenerateDungeon()
    {
        GroundTilemap.ClearAllTiles();
        WallTilemap.ClearAllTiles();
        Random = new System.Random();
        OnlyPathNodes = new List<Vector2>();
        AllGroundNodes = new List<Vector2>();
        //  AllGroundNodesArray = new bool[Width + GridOffset + GridBorder, Height + GridOffset + GridBorder]; // A*

        Dictionary<Vector2, Bounds> rooms = new Dictionary<Vector2, Bounds>();

        Debug.Log($"Spawning player");
        MainLoadingLabel.text = "Spawning player";
        SideLoadingLabel.text = "● ○ ○ ○ ○ ○ ○ ○";
        yield return new WaitForSecondsRealtime(1);

        PlayerPrefab.GetComponent<ThirdPersonMovement>().mainCamera = PlayerCamera;
        Player = Instantiate(PlayerPrefab);
        Player.transform.position = new Vector3(4, 1.5f, 25);
        Player.SetActive(false);

        Ui.GetComponentInChildren<Roll>().Player = Player.GetComponent<ThirdPersonMovement>();

        Debug.Log($"Spawning rooms");
        MainLoadingLabel.text = "Generating rooms";
        SideLoadingLabel.text = "● ● ○ ○ ○ ○ ○ ○";
        yield return new WaitForSecondsRealtime(1);

        for (int i = 0; i < Rooms.Count; i++)
            AddRoom(Rooms[i], RoomCounts[i], rooms);

        Debug.Log($"Spawning main rooms");
        MainLoadingLabel.text = "Resting";
        SideLoadingLabel.text = "● ● ● ○ ○ ○ ○ ○";
        yield return new WaitForSecondsRealtime(1);

        AddRoomAtCoordinate(BossRoom, new Vector2(Width, Height), rooms);
        AddRoomAtCoordinate(SpawnRoom, new Vector2(0, 0), rooms);
        //   AddRoomAtCoordinate(SpawnRoom, new Vector2(GridBorder, GridBorder), rooms); // A*

        Debug.Log($"Adding paths");
        MainLoadingLabel.text = "Adding paths";
        SideLoadingLabel.text = "● ● ● ● ○ ○ ○ ○";
        yield return new WaitForSecondsRealtime(1);

        List<Vector2> roomCoordinates = rooms.Keys.Take(rooms.Count - 1).ToList();
        foreach (var room in rooms.Take(rooms.Count - 1))
            AddPathNodes(BuildPath(room.Key, GetClosestNode(room.Key, roomCoordinates)));

        Debug.Log($"Building walls");
        MainLoadingLabel.text = "Building walls";
        SideLoadingLabel.text = "● ● ● ● ● ○ ○ ○";
        yield return new WaitForSecondsRealtime(1);

        int intervall = AllGroundNodes.Count / 3;

        BuildWalls(0, intervall);

        MainLoadingLabel.text = "Still building walls";
        SideLoadingLabel.text = "● ● ● ● ● ● ○ ○";
        yield return new WaitForSecondsRealtime(1);
        BuildWalls(intervall, intervall * 2);

        MainLoadingLabel.text = "There are a lot of walls";
        SideLoadingLabel.text = "● ● ● ● ● ● ● ○";
        yield return new WaitForSecondsRealtime(1);
        BuildWalls(intervall * 2, AllGroundNodes.Count);

        Debug.Log($"Summoning creepy crawlers");
        MainLoadingLabel.text = "Summoning creepy crawlers";
        SideLoadingLabel.text = "● ● ● ● ● ● ● ●";
        yield return new WaitForSecondsRealtime(1);

        SpawnEnemies();

        Player.SetActive(true);
        GenerationFinished = true;
    }

    private void AddRoom(GameObject roomPrefab, int count, Dictionary<Vector2, Bounds> rooms)
    {
        for (int i = 0; i < count; i++)
            AddRoomAtCoordinate(roomPrefab, new Vector2(Random.Next(GridOffset, Width), Random.Next(GridOffset, Height)), rooms);
        //  AddRoomAtCoordinate(roomPrefab, new Vector2(Random.Next(GridOffset + GridBorder, Width), Random.Next(GridOffset + GridBorder, Height)), rooms); // A*
    }

    private Bounds? AddRoomAtCoordinate(GameObject roomPrefab, Vector2 coordinate, Dictionary<Vector2, Bounds> rooms)
    {
        Bounds? roomBounds = null;
        List<Vector2> roomNodes = new List<Vector2>();
        int spawnTries = 5;
        int spawnTryCounter = 0;
        Vector2 center = new Vector2();
        Vector3Int position = new Vector3Int();
        Room roomScript = null;
        List<RoomSpawner> spawner = new List<RoomSpawner>();

        while (spawnTryCounter != spawnTries)
        {
            position = new Vector3Int((int)coordinate.x, (int)coordinate.y, (int)GroundTilemap.transform.position.z);
            GameObject room = Instantiate(roomPrefab);
            roomScript = room.GetComponent<Room>();
            room.transform.position = GroundTilemap.CellToWorld(position);

            center = roomScript.GetCenter(GroundTilemap);
            roomNodes = roomScript.GetFloorTilePositions(GroundTilemap);
            roomBounds = roomScript.GetBounds(roomNodes, GroundTilemap);
            spawner = room.GetComponentsInChildren<RoomSpawner>().ToList();
            spawnTryCounter++;

            if (CanAddRoom(roomBounds.Value, rooms))
                break;
            else
            {
                Destroy(room);
            }
        }

        if (spawnTryCounter == spawnTries) return null;

        Spawner.AddRange(spawner);

        foreach (Vector2 roomNode in roomNodes)
            GroundTilemap.SetTile(new Vector3Int((int)roomNode.x, (int)roomNode.y, (int)GroundTilemap.transform.position.y), GroundTile);

        rooms.Add(new Vector2(position.x, position.y), roomBounds.Value);

        if (AllGroundNodes.Any())
            AddPathNodes(
                BuildPath(new Vector2(center.x, center.y),
                GetClosestNode(new Vector2(center.x, center.y), AllGroundNodes)));

        AddPathNodes(roomScript.GetBorderTilePositions(GroundTilemap));
        return roomBounds;
    }

    private void AddPathNode(Vector2 node)
    {
        if (AllGroundNodes.Any(x => x.x == node.x && x.y == node.y)) return;
        AllGroundNodes.Add(node);

        // A*
        //try
        //{
        //    AllGroundNodesArray[(int)node.x, (int)node.y] = true;
        //}
        //catch { Debug.LogError($"{node.x} / {node.y}"); }
    }

    private void AddPathNodes(List<Vector2> nodes)
    {
        foreach (Vector2 node in nodes) AddPathNode(node);
    }

    private bool CanAddRoom(Bounds roomBounds, Dictionary<Vector2, Bounds> rooms)
    {
        foreach (var bounds in rooms)
            if (bounds.Value.Intersects(roomBounds)) return false;

        return true;
    }

    private List<Vector2> BuildPath(Vector2 sourceNode, Vector2 targetNode)
    {
        List<Vector2> pathNodes = GetPointsBetweenNodes(sourceNode, targetNode);
        List<Vector2> allPathNodes = new List<Vector2>();

        foreach (Vector2 pathNode in pathNodes)
        {
            AddPathNode(pathNode);
            OnlyPathNodes.Add(pathNode);
            SetGroundTile(new Vector3Int((int)pathNode.x, (int)pathNode.y, (int)GroundTilemap.transform.position.y));

            for (int i = 0; i < PathSize; i++)
            {
                SetGroundTile(new Vector3Int((int)pathNode.x, (int)pathNode.y + i + 1, (int)GroundTilemap.transform.position.y));
                SetGroundTile(new Vector3Int((int)pathNode.x, (int)pathNode.y - i - 1, (int)GroundTilemap.transform.position.y));
                SetGroundTile(new Vector3Int((int)pathNode.x + i + 1, (int)pathNode.y, (int)GroundTilemap.transform.position.y));
                SetGroundTile(new Vector3Int((int)pathNode.x - i - 1, (int)pathNode.y, (int)GroundTilemap.transform.position.y));

                if (i == PathSize - 1)
                {
                    AddPathNode(new Vector2(pathNode.x + i + 1, pathNode.y));
                    AddPathNode(new Vector2(pathNode.x - i - 1, pathNode.y));
                    AddPathNode(new Vector2(pathNode.x, pathNode.y + i + 1));
                    AddPathNode(new Vector2(pathNode.x, pathNode.y - i - 1));
                }

                // A*
                //AddPathNode(new Vector2(pathNode.x + i + 1, pathNode.y));
                //AddPathNode(new Vector2(pathNode.x - i - 1, pathNode.y));
                //AddPathNode(new Vector2(pathNode.x, pathNode.y + i + 1));
                //AddPathNode(new Vector2(pathNode.x, pathNode.y - i - 1));
            }
        }

        return allPathNodes;
    }

    private void SpawnEnemies()
    {
        //  Grid grid = new Grid(Width, Height, AllGroundNodesArray); A*

        foreach (RoomSpawner spawner in Spawner)
            spawner.Spawn(new Vector2(Width, Height), GroundTilemap, Player);

        foreach (Vector2 pathNode in OnlyPathNodes)
        {
            if (Random.Next(1, 101) <= PathEnemySpawnChance)
            {
                Vector3 position = GroundTilemap.CellToWorld(new Vector3Int(
                    (int)pathNode.x,
                    (int)pathNode.y,
                    (int)GroundTilemap.transform.position.z));
                position += new Vector3(0, 1.5f, 0);
                SpawnEnemy(position, pathNode);

                if (Random.Next(1, 101) <= HordeSpawnChance)
                {
                    int count = Random.Next(1, 5);
                    for (int i = 0; i < count; i++)
                    {
                        switch (i)
                        {
                            case 0:
                                position = GroundTilemap.CellToWorld(new Vector3Int(
                                    (int)pathNode.x + 1,
                                    (int)pathNode.y,
                                    (int)GroundTilemap.transform.position.z));
                                break;
                            case 1:
                                position = GroundTilemap.CellToWorld(new Vector3Int(
                                    (int)pathNode.x - 1,
                                    (int)pathNode.y,
                                    (int)GroundTilemap.transform.position.z));
                                break;
                            case 2:
                                position = GroundTilemap.CellToWorld(new Vector3Int(
                                    (int)pathNode.x,
                                    (int)pathNode.y + 1,
                                    (int)GroundTilemap.transform.position.z));
                                break;
                            case 3:
                                position = GroundTilemap.CellToWorld(new Vector3Int(
                                    (int)pathNode.x,
                                    (int)pathNode.y - 1,
                                    (int)GroundTilemap.transform.position.z));
                                break;
                        }

                        position += new Vector3(0, 1.5f, 0);
                        SpawnEnemy(position, pathNode);
                    }
                }
            }
        }
    }

    private void SpawnEnemy(Vector3 position, Vector2 gridPosition, Grid grid = null)
    {
        // AStar aStar = new AStar();
        //// one run takes 30 seconds -> way to long
        // List<Vector2> path = aStar.FindPath(grid, gridPosition, new Vector2(Width, Height));

        int maxDistance = (int)Mathf.Sqrt((new Vector2(0, 0) - new Vector2(Width, Height)).sqrMagnitude);
        int distanceToBoss = (int)Mathf.Sqrt((gridPosition - new Vector2(Width, Height)).sqrMagnitude);

        Instantiate(PathEnemies[Random.Next(0, PathEnemies.Count)]).GetComponent<Enemy>().Spawn(position, (maxDistance - distanceToBoss) / 10, Player);
    }

    private void BuildWalls(int from, int to)
    {
        for (int f = from; f < to; f++)
            for (int i = 0; i < WallSize; i++)
            {
                if (!GroundTilemap.HasTile(new Vector3Int((int)AllGroundNodes[f].x + 1 + i, (int)AllGroundNodes[f].y, (int)GroundTilemap.transform.position.z)))
                {
                    SetWallTile(new Vector3Int((int)AllGroundNodes[f].x + 1 + i, (int)AllGroundNodes[f].y, (int)WallTilemap.transform.position.z));
                }

                if (!GroundTilemap.HasTile(new Vector3Int((int)AllGroundNodes[f].x - 1 - i, (int)AllGroundNodes[f].y, (int)GroundTilemap.transform.position.z)))
                {
                    SetWallTile(new Vector3Int((int)AllGroundNodes[f].x - 1 - i, (int)AllGroundNodes[f].y, (int)WallTilemap.transform.position.z));
                }

                if (!GroundTilemap.HasTile(new Vector3Int((int)AllGroundNodes[f].x, (int)AllGroundNodes[f].y + 1 + i, (int)GroundTilemap.transform.position.z)))
                {
                    SetWallTile(new Vector3Int((int)AllGroundNodes[f].x, (int)AllGroundNodes[f].y + 1 + i, (int)WallTilemap.transform.position.z));
                }

                if (!GroundTilemap.HasTile(new Vector3Int((int)AllGroundNodes[f].x, (int)AllGroundNodes[f].y - 1 - i, (int)GroundTilemap.transform.position.z)))
                {
                    SetWallTile(new Vector3Int((int)AllGroundNodes[f].x, (int)AllGroundNodes[f].y - 1 - i, (int)WallTilemap.transform.position.z));
                }
            }
    }

    private void SetWallTile(Vector3Int position) => SetTile(position, WallTile, WallTilemap);
    private void SetGroundTile(Vector3Int position) => SetTile(position, GroundTile, GroundTilemap);

    private void SetTile(Vector3Int position, TileBase tileToPlace, Tilemap tilemap) => tilemap.SetTile(position, tileToPlace);

    private Vector2 GetClosestNode(Vector2 sourceNode, List<Vector2> nodes)
    {
        Vector2 closestNode = Vector2.zero;
        float distance = -1;

        foreach (Vector2 targetNode in nodes)
        {
            if (sourceNode == targetNode) continue;

            float newDistance = (targetNode - sourceNode).sqrMagnitude;

            if (newDistance < distance || distance == -1)
            {
                distance = newDistance;
                closestNode = targetNode;
            }
        }

        return closestNode;
    }

    private List<Vector2> GetPointsBetweenNodes(Vector2 sourceNode, Vector2 targetNode)
    {
        List<Vector2> track = new List<Vector2>();
        int dx = (int)Mathf.Abs(targetNode.x - sourceNode.x);
        int dy = (int)Mathf.Abs(targetNode.y - sourceNode.y);

        int swaps = 0;
        if (dy > dx)
        {
            Swap(ref dx, ref dy);
            swaps = 1;
        }

        int a = Math.Abs(dy);
        int b = -Math.Abs(dx);

        double d = (2 * a) + b;
        int x = (int)sourceNode.x;
        int y = (int)sourceNode.y;
        track.Clear();
        track.Add(new Vector2(x, y));

        int s = 1;
        int q = 1;
        if (sourceNode.x > targetNode.x) q = -1;
        if (sourceNode.y > targetNode.y) s = -1;

        for (int k = 0; k < dx; k++)
        {
            if (d >= 0)
            {
                d = (2 * (a + b)) + d;
                y += s;
                x += q;
            }
            else
            {
                if (swaps == 1) y += s;
                else x += q;
                d = (2 * a) + d;
            }
            track.Add(new Vector2(x, y));
        }

        // to avoid sharp edges and therefore small hall ways

        Vector2 lastTrack = track.Last();

        for (int i = 0; i < PathSize; i++)
        {
            if (sourceNode.x < targetNode.x || sourceNode.x > targetNode.x)
            {
                track.Add(new Vector2(lastTrack.x + 1 + i, lastTrack.y));
                track.Add(new Vector2(lastTrack.x - 1 - i, lastTrack.y));
            }
            if (sourceNode.y < targetNode.y || sourceNode.y > targetNode.y)
            {
                track.Add(new Vector2(lastTrack.x, lastTrack.y + 1 + i));
                track.Add(new Vector2(lastTrack.x, lastTrack.y - 1 - i));
            }
        }

        return track;
    }

    private void Swap(ref int x, ref int y)
    {
        int temp = x;
        x = y;
        y = temp;
    }
}
