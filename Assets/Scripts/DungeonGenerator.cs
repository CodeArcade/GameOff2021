using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    public List<GameObject> PathEnemies;
    public int PathEnemySpawnChance;
    public int HordeSpawnChance;

    private System.Random Random { get; set; }

    void Awake()
    {
        GenerateDungeon();
    }

    private void GenerateDungeon()
    {
        GroundTilemap.ClearAllTiles();
        WallTilemap.ClearAllTiles();
        Random = new System.Random();

        List<Vector2> allPathNodes = new List<Vector2>();
        Dictionary<Vector2, Bounds> rooms = new Dictionary<Vector2, Bounds>();

        for (int i = 0; i < Rooms.Count; i++)
            AddRoom(Rooms[i], RoomCounts[i], rooms, allPathNodes);

        AddRoomAtCoordinate(BossRoom, new Vector2(Width + 50, Height + 50), rooms, allPathNodes);
        AddRoomAtCoordinate(SpawnRoom, new Vector2(-50, -50), rooms, allPathNodes);

        List<Vector2> roomCoordinates = rooms.Keys.Take(rooms.Count - 1).ToList();
        foreach (var room in rooms.Take(rooms.Count - 1))
            AddPathNodes(allPathNodes, BuildPath(room.Key, GetClosestNode(room.Key, roomCoordinates)));

        BuildWalls(allPathNodes);

        PlayerPrefab.GetComponent<ThirdPersonMovement>().mainCamera = PlayerCamera;
        GameObject player = Instantiate(PlayerPrefab);

        player.transform.position = new Vector3(-45, 1.5f, -23);
    }

    private void AddRoom(GameObject roomPrefab, int count, Dictionary<Vector2, Bounds> rooms, List<Vector2> allPathNodes)
    {
        for (int i = 0; i < count; i++)
        {
            Debug.Log($"Spawning room {i + 1} / {count}");
            AddRoomAtCoordinate(roomPrefab, new Vector2(Random.Next(0, Width), Random.Next(0, Height)), rooms, allPathNodes);
        }
    }

    private Bounds? AddRoomAtCoordinate(GameObject roomPrefab, Vector2 coordinate, Dictionary<Vector2, Bounds> rooms, List<Vector2> allPathNodes)
    {
        Bounds? roomBounds = null;
        List<Vector2> roomNodes = new List<Vector2>();
        int spawnTries = 5;
        int spawnTryCounter = 0;
        Vector2 center = new Vector2();
        Room roomScript = null;
        Vector3Int position = new Vector3Int();

        while (spawnTryCounter != spawnTries)
        {
            position = new Vector3Int((int)coordinate.x, (int)coordinate.y, (int)GroundTilemap.transform.position.z);
            GameObject room = Instantiate(roomPrefab);
            roomScript = room.GetComponent<Room>();
            room.transform.position = GroundTilemap.CellToWorld(position);

            center = roomScript.GetCenter(GroundTilemap);
            roomNodes = roomScript.GetFloorTilePositions(GroundTilemap);
            roomBounds = roomScript.GetBounds(roomNodes, GroundTilemap);
            spawnTryCounter++;

            if (CanAddRoom(roomBounds.Value, rooms))
                break;
            else
            {
                Destroy(room);
            }
        }

        if (spawnTryCounter == spawnTries) return null;

        foreach (Vector2 roomNode in roomNodes)
            GroundTilemap.SetTile(new Vector3Int((int)roomNode.x, (int)roomNode.y, (int)GroundTilemap.transform.position.y), GroundTile);

        rooms.Add(new Vector2(position.x, position.y), roomBounds.Value);

        if (allPathNodes.Any())
            AddPathNodes(allPathNodes,
                BuildPath(new Vector2(center.x, center.y),
                GetClosestNode(new Vector2(center.x, center.y), allPathNodes)));

        AddPathNodes(allPathNodes, roomScript.GetBorderTilePositions(GroundTilemap));

        return roomBounds;
    }

    private void AddPathNode(List<Vector2> pathNodes, Vector2 node)
    {
        if (pathNodes.Any(x => x.x == node.x && x.y == node.y)) return;
        pathNodes.Add(node);
    }

    private void AddPathNodes(List<Vector2> pathNodes, List<Vector2> nodes)
    {
        foreach (Vector2 node in nodes) AddPathNode(pathNodes, node);
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
            AddPathNode(allPathNodes, pathNode);
            GroundTilemap.SetTile(new Vector3Int((int)pathNode.x, (int)pathNode.y, (int)GroundTilemap.transform.position.y), GroundTile);

            if (Random.Next(1, 101) <= PathEnemySpawnChance)
            {
                GameObject enemy = PathEnemies[Random.Next(0, PathEnemies.Count)];
                Vector3 position = GroundTilemap.CellToWorld(new Vector3Int(
                    (int)pathNode.x,
                    (int)pathNode.y,
                    (int)GroundTilemap.transform.position.z));
                position += new Vector3(0, 1.5f, 0);

                Instantiate(enemy).transform.position = position;

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
                                position += new Vector3(0, 1.5f, 0);
                                break;
                            case 1:
                                position = GroundTilemap.CellToWorld(new Vector3Int(
                                    (int)pathNode.x - 1,
                                    (int)pathNode.y,
                                    (int)GroundTilemap.transform.position.z));
                                position += new Vector3(0, 1.5f, 0);
                                break;
                            case 2:
                                position = GroundTilemap.CellToWorld(new Vector3Int(
                                    (int)pathNode.x,
                                    (int)pathNode.y + 1,
                                    (int)GroundTilemap.transform.position.z));
                                position += new Vector3(0, 1.5f, 0);
                                break;
                            case 3:
                                position = GroundTilemap.CellToWorld(new Vector3Int(
                                    (int)pathNode.x,
                                    (int)pathNode.y - 1,
                                    (int)GroundTilemap.transform.position.z));
                                position += new Vector3(0, 1.5f, 0);
                                break;
                        }

                        Instantiate(enemy).transform.position = position;
                    }
                }
            }

            for (int i = 0; i < PathSize; i++)
            {
                GroundTilemap.SetTile(new Vector3Int((int)pathNode.x, (int)pathNode.y + i + 1, (int)GroundTilemap.transform.position.y), GroundTile);
                GroundTilemap.SetTile(new Vector3Int((int)pathNode.x, (int)pathNode.y - i - 1, (int)GroundTilemap.transform.position.y), GroundTile);
                GroundTilemap.SetTile(new Vector3Int((int)pathNode.x + i + 1, (int)pathNode.y, (int)GroundTilemap.transform.position.y), GroundTile);
                GroundTilemap.SetTile(new Vector3Int((int)pathNode.x - i - 1, (int)pathNode.y, (int)GroundTilemap.transform.position.y), GroundTile);

                if (i == PathSize - 1)
                {
                    AddPathNode(allPathNodes, new Vector2(pathNode.x - i - 1, pathNode.y));
                    AddPathNode(allPathNodes, new Vector2(pathNode.x + i + 1, pathNode.y));
                    AddPathNode(allPathNodes, new Vector2(pathNode.x, pathNode.y - i - 1));
                    AddPathNode(allPathNodes, new Vector2(pathNode.x, pathNode.y + i + 1));
                }
            }
        }

        return allPathNodes;
    }

    private void BuildWalls(List<Vector2> nodes)
    {
        int counter = 0;

        foreach (var pathNode in nodes)
        {
            Debug.Log($"Building Wall { counter++ } / {nodes.Count}");

            if (!GroundTilemap.HasTile(new Vector3Int((int)pathNode.x + 1, (int)pathNode.y, (int)GroundTilemap.transform.position.z)))
                WallTilemap.SetTile(new Vector3Int((int)pathNode.x + 1, (int)pathNode.y, (int)WallTilemap.transform.position.z), WallTile);

            if (!GroundTilemap.HasTile(new Vector3Int((int)pathNode.x - 1, (int)pathNode.y, (int)GroundTilemap.transform.position.z)))
                WallTilemap.SetTile(new Vector3Int((int)pathNode.x - 1, (int)pathNode.y, (int)WallTilemap.transform.position.z), WallTile);

            if (!GroundTilemap.HasTile(new Vector3Int((int)pathNode.x, (int)pathNode.y + 1, (int)GroundTilemap.transform.position.z)))
                WallTilemap.SetTile(new Vector3Int((int)pathNode.x, (int)pathNode.y + 1, (int)WallTilemap.transform.position.z), WallTile);

            if (!GroundTilemap.HasTile(new Vector3Int((int)pathNode.x, (int)pathNode.y - 1, (int)GroundTilemap.transform.position.z)))
                WallTilemap.SetTile(new Vector3Int((int)pathNode.x, (int)pathNode.y - 1, (int)WallTilemap.transform.position.z), WallTile);
        }
    }

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
