using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// https://gist.github.com/DotNetCoreTutorials/08b0210616769e81034f53a6a420a6d9
/// </summary>
public class AStar
{
    public List<Vector2> FindPath(Grid grid, Vector2 startTile, Vector2 finishTile)
    {
        Tile start = new Tile
        {
            Y = (int)startTile.y,
            X = (int)startTile.x
        };

        Tile finish = new Tile
        {
            Y = (int)finishTile.y,
            X = (int)finishTile.x
        };

        start.SetDistance(finish.X, finish.Y);

        List<Tile> activeTiles = new List<Tile>
        {
            start
        };
        List<Tile> visitedTiles = new List<Tile>();

        List<Vector2> path = new List<Vector2>();

        while (activeTiles.Any())
        {
            Tile checkTile = activeTiles.OrderBy(x => x.CostDistance).First();

            if (checkTile.X == finish.X && checkTile.Y == finish.Y)
            {
                //We found the destination and we can be sure (Because the the OrderBy above)
                //That it's the most low cost option. 
                Tile tile = checkTile;
                while (true)
                {
                    if (grid.Cells[tile.Y, tile.X]) path.Add(new Vector2(tile.X, tile.Y));

                    tile = tile.Parent;
                    if (tile == null) return path;
                }
            }

            visitedTiles.Add(checkTile);
            activeTiles.Remove(checkTile);

            List<Tile> walkableTiles = GetWalkableTiles(grid, checkTile, finish);

            foreach (Tile walkableTile in walkableTiles)
            {
                if (Contains(walkableTile, visitedTiles)) continue;

                //It's already in the active list, but that's OK, maybe this new tile has a better value (e.g. We might zigzag earlier but this is now straighter). 
                if (Contains(walkableTile, activeTiles))
                {
                    Tile existingTile = activeTiles.First(x => x.X == walkableTile.X && x.Y == walkableTile.Y);
                    if (existingTile.CostDistance > checkTile.CostDistance)
                    {
                        activeTiles.Remove(existingTile);
                        activeTiles.Add(walkableTile);
                    }
                }
                else
                {
                    //We've never seen this tile before so add it to the list. 
                    activeTiles.Add(walkableTile);
                }
            }
        }

        return new List<Vector2>();
    }

    private bool Contains(Tile targetTile, List<Tile> source)
    {
        foreach (Tile tile in source) 
            if (tile.X == targetTile.X && tile.Y == targetTile.Y) return true;

        return false;
    }

    private List<Tile> GetWalkableTiles(Grid grid, Tile currentTile, Tile targetTile)
    {

        List<Tile> possibleTiles = new List<Tile>()
            {
                new Tile { X = currentTile.X, Y = currentTile.Y - 1, Parent = currentTile, Cost = currentTile.Cost + 1 },
                new Tile { X = currentTile.X, Y = currentTile.Y + 1, Parent = currentTile, Cost = currentTile.Cost + 1},
                new Tile { X = currentTile.X - 1, Y = currentTile.Y, Parent = currentTile, Cost = currentTile.Cost + 1 },
                new Tile { X = currentTile.X + 1, Y = currentTile.Y, Parent = currentTile, Cost = currentTile.Cost + 1 },
            };

        possibleTiles.ForEach(tile => tile.SetDistance(targetTile.X, targetTile.Y));

        int maxX = grid.Width;
        int maxY = grid.Height;

        return possibleTiles
                .Where(tile => tile.X >= 0 && tile.X <= maxX && tile.Y >= 0 && tile.Y <= maxY && grid.Cells[tile.X, tile.Y])
                .ToList();
    }
}

public class Grid
{
    public bool[,] Cells;
    public int Width;
    public int Height;

    public Grid(int width, int height, bool[,] tiles)
    {
        Width = width;
        Height = height;

        Cells = tiles;
    }
}

public class Cell
{
    public bool IsWalkable { get; set; }
}

public class Tile
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Cost { get; set; }
    public int Distance { get; set; }
    public int CostDistance => Cost + Distance;
    public Tile Parent { get; set; }

    //The distance is essentially the estimated distance, ignoring walls to our target. 
    //So how many tiles left and right, up and down, ignoring walls, to get there. 
    public void SetDistance(int targetX, int targetY)
    {
        this.Distance = Math.Abs(targetX - X) + Math.Abs(targetY - Y);
    }
}