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

    public int NodeCount;
    public int Height;
    public int Width;

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

        List<Vector2> nodes = GenerateNodes();

        for (int i = 0; i < nodes.Count; i++)
        {
            int targetIndex = i + 1;
            if (i == nodes.Count - 1) break;

            List<Vector2> pathNodes = TransformLineToCurve(GetPointsBetweenNodes(nodes[i], nodes[targetIndex]));

            foreach (Vector2 pathNode in pathNodes)
            {
                GroundTilemap.SetTile(new Vector3Int((int)pathNode.x, (int)pathNode.y, (int)GroundTilemap.transform.position.y), GroundTile);

                // path moves to the right or the left
                if (pathNode.x > nodes[i].x || pathNode.x < nodes[i].x)
                {
                    int wallSize = Random.Next(1, 3);

                    for (int j = 0; j < wallSize; j++)
                    {
                        GroundTilemap.SetTile(new Vector3Int((int)pathNode.x, (int)pathNode.y + j + 1, (int)GroundTilemap.transform.position.y), GroundTile);
                        GroundTilemap.SetTile(new Vector3Int((int)pathNode.x, (int)pathNode.y + j + 1, (int)GroundTilemap.transform.position.y), GroundTile);
                    }
                }

                // path moves up or down
                if (pathNode.y > nodes[i].y || pathNode.y < nodes[i].y)
                {
                    int wallSize = Random.Next(1, 3);

                    for (int j = 0; j < wallSize; j++)
                    {
                        GroundTilemap.SetTile(new Vector3Int((int)pathNode.x + j + 1, (int)pathNode.y, (int)GroundTilemap.transform.position.y), GroundTile);
                        GroundTilemap.SetTile(new Vector3Int((int)pathNode.x + j + 1, (int)pathNode.y, (int)GroundTilemap.transform.position.y), GroundTile);
                    }
                }
            }
        }

    }

    private List<Vector2> GenerateNodes()
    {
        List<Vector2> nodes = new List<Vector2>();
        for (int i = 0; i < NodeCount; i++)
        {
            Vector2 node = new Vector2(Random.Next(0, Width), Random.Next(0, Height));

            if (nodes.Any(x => x.x == node.x && x.y == node.y))
            {
                i--;
                continue;
            }

            nodes.Add(node);
        }

        return nodes;
    }

    //private Vector2 GetClosestNode(Vector2 sourceNode, List<Vector2> nodes)
    //{
    //    Vector2 closestNode = Vector2.zero;
    //    float distance = -1;

    //    foreach (Vector2 targetNode in nodes)
    //    {
    //        if (sourceNode == targetNode) continue;

    //        float newDistance = (targetNode - sourceNode).sqrMagnitude;

    //        if (newDistance < distance || distance == -1)
    //        {
    //            distance = newDistance;
    //            closestNode = targetNode;
    //        }
    //    }

    //    return closestNode;
    //}

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
            int nodeIndex = (i + 1) * curvePointDistance;
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
