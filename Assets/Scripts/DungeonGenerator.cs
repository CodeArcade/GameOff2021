using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

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

    public GameObject EggRoom;
    public int EggRoomCount;

    private System.Random Random { get; set; }

    private void Awake()
    {
        GenerateDungeon();
    }

    private void GenerateDungeon()
    {
        GroundTilemap.ClearAllTiles();
        WallTilemap.ClearAllTiles();
        Random = new System.Random();

        List<Vector2> allPathNodes = new List<Vector2>();
        List<Bounds> rooms = new List<Bounds>();
        Vector2 centerNode = new Vector2(Random.Next(0, Width), Random.Next(0, Height));

        AddPathNode(allPathNodes, centerNode);

        AddRoom(EggRoom, EggRoomCount, rooms, allPathNodes);


        BuildWalls(allPathNodes);

        PlayerPrefab.GetComponent<ThirdPersonMovement>().mainCamera = PlayerCamera;
        GameObject player = Instantiate(PlayerPrefab);
        player.transform.position = GroundTilemap.CellToWorld(new Vector3Int((int)centerNode.x, (int)centerNode.y, (int)GroundTilemap.transform.position.z));
        player.transform.position += new Vector3(0, 3, 0);
    }

    private void AddRoom(GameObject roomPrefab, int count, List<Bounds> rooms, List<Vector2> allPathNodes)
    {
        for (int i = 0; i < count; i++)
        {
            Bounds? roomBounds = null;
            List<Vector2> roomNodes = new List<Vector2>();
            int spawnTries = 5;
            int spawnTryCounter = 0;
            while (spawnTryCounter != spawnTries)
            {
                Vector3Int position = new Vector3Int(Random.Next(0, Width), Random.Next(0, Height), (int)GroundTilemap.transform.position.z);
                GameObject room = Instantiate(roomPrefab);
                Room roomScript = room.GetComponent<Room>();

                room.transform.position = GroundTilemap.CellToWorld(position);
                roomNodes = roomScript.GetFloorTilePositions(GroundTilemap);
                roomBounds = roomScript.Bounds(roomNodes);
                spawnTryCounter++;

                if (CanAddRoom(roomBounds.Value, rooms))
                    break;
                else
                    Destroy(room);
            }

            if (spawnTryCounter == spawnTries) continue;

            foreach (Vector2 roomNode in roomNodes)
                GroundTilemap.SetTile(new Vector3Int((int)roomNode.x, (int)roomNode.y, (int)GroundTilemap.transform.position.y), GroundTile);

            rooms.Add(roomBounds.Value);

            AddPathNodes(allPathNodes,
                BuildPath(new Vector2(roomBounds.Value.center.x, roomBounds.Value.center.y),
                GetClosestNode(new Vector2(roomBounds.Value.center.x, roomBounds.Value.center.y), allPathNodes)));

            AddPathNodes(allPathNodes, roomNodes);
        }
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

    private bool CanAddRoom(Bounds roomBounds, List<Bounds> rooms)
    {
        foreach (Bounds bounds in rooms)
            if (bounds.Intersects(roomBounds)) return false;

        return true;
    }

    private List<Vector2> BuildPath(Vector2 sourceNode, Vector2 targetNode)
    {
        List<Vector2> pathNodes = TransformLineToCurve(GetPointsBetweenNodes(sourceNode, targetNode));
        List<Vector2> allPathNodes = new List<Vector2>();

        foreach (Vector2 pathNode in pathNodes)
        {
            AddPathNode(allPathNodes, pathNode);
            GroundTilemap.SetTile(new Vector3Int((int)pathNode.x, (int)pathNode.y, (int)GroundTilemap.transform.position.y), GroundTile);

            // path moves to the right or the left
            if (pathNode.x > sourceNode.x || pathNode.x < sourceNode.x)
            {
                int wallSize = Random.Next(1, 3);

                for (int j = 0; j < wallSize; j++)
                {
                    AddPathNode(allPathNodes, new Vector2(pathNode.x, pathNode.y + j + 1));
                    GroundTilemap.SetTile(new Vector3Int((int)pathNode.x, (int)pathNode.y + j + 1, (int)GroundTilemap.transform.position.y), GroundTile);

                    AddPathNode(allPathNodes, new Vector2(pathNode.x, pathNode.y - j - 1));
                    GroundTilemap.SetTile(new Vector3Int((int)pathNode.x, (int)pathNode.y - j - 1, (int)GroundTilemap.transform.position.y), GroundTile);
                }
            }

            // path moves up or down
            if (pathNode.y > sourceNode.y || pathNode.y < sourceNode.y)
            {
                int wallSize = Random.Next(1, 3);

                for (int j = 0; j < wallSize; j++)
                {
                    AddPathNode(allPathNodes, new Vector2(pathNode.x + j + 1, pathNode.y));
                    GroundTilemap.SetTile(new Vector3Int((int)pathNode.x + j + 1, (int)pathNode.y, (int)GroundTilemap.transform.position.y), GroundTile);

                    AddPathNode(allPathNodes, new Vector2(pathNode.x - j - 1, pathNode.y));
                    GroundTilemap.SetTile(new Vector3Int((int)pathNode.x - j - 1, (int)pathNode.y, (int)GroundTilemap.transform.position.y), GroundTile);
                }
            }
        }

        return allPathNodes;
    }

    private void BuildWalls(List<Vector2> nodes)
    {
        foreach (var pathNode in nodes)
        {
            WallTilemap.SetTile(new Vector3Int((int)pathNode.x, (int)pathNode.y, (int)WallTilemap.transform.position.z), null);

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

        return track;
    }

    private void Swap(ref int x, ref int y)
    {
        int temp = x;
        x = y;
        y = temp;
    }

    private List<Vector2> TransformLineToCurve(List<Vector2> line)
    {
        if (line.Count < 10) return line;

        List<Vector2> curve = new List<Vector2>();
        int curvePointCount = (line.Count() / 5) + 3;
        curvePointCount = curvePointCount % 2 == 0 ? curvePointCount + 1 : curvePointCount;
        int curvePointDistance = line.Count / curvePointCount;
        bool direction = Random.Next(0, 2) == 1; // true = down | right / false = up / left
        int curvePointCounter = 0;
        Vector2 lastNode = line.First();
        int curveSize = 1;

        curve.Add(line.First());

        for (int i = 0; i < curvePointCount; i += curveSize)
        {
            int nodeIndex = Math.Min((i + 1) * curvePointDistance, curvePointCount - 1);
            int numberCount = 0;
            bool increaseCurve = curvePointCounter <= GetSum(curvePointCount, ref numberCount) / numberCount;
            Vector2 newNode = new Vector2(line[nodeIndex].x, line[nodeIndex].y);

            if (line.First().x > line.Last().x || line.First().x < line.Last().x)
            {
                newNode += new Vector2(0, direction ? (i + 1) * curveSize : (-i - 1) * curveSize);
                if (!increaseCurve) newNode = new Vector2(newNode.x, direction ? lastNode.y - curveSize : lastNode.y + curveSize);
            }

            if (line.First().y > line.Last().y || line.First().y < line.Last().y)
            {
                newNode += new Vector2(direction ? (i + 1) * curveSize : (-i - 1) * curveSize, 0);
                if (!increaseCurve) newNode = new Vector2(direction ? lastNode.x - curveSize : lastNode.x + curveSize, newNode.y);
            }

            curve.AddRange(GetPointsBetweenNodes(lastNode, newNode));

            curvePointCounter++;
            lastNode = newNode;
        }

        curve.AddRange(GetPointsBetweenNodes(lastNode, line.Last()));

        return curve;
    }

    private int GetSum(int number, ref int numberCount)
    {
        int result = 1;
        numberCount = 0;
        while (number != 1)
        {
            result += number;
            number -= 1;
            numberCount++;
        }
        return result;
    }
}
