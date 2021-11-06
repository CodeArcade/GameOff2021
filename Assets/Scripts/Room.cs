using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Room : MonoBehaviour
{
    public Tilemap GroundTilemap;
    public GameObject Center;

    public Vector2 GetCenter(Tilemap mainTilemap)
    {
        Vector3 position = mainTilemap.WorldToCell(Center.transform.position);
        return new Vector2(position.x, position.y);
    }

    public Bounds Bounds(Tilemap tilemap, Tilemap mainTilemap)
    {
        return GetBounds(GetTiles(GroundTilemap, mainTilemap), mainTilemap);
    }

    public Bounds GetBounds(List<Vector2> tiles, Tilemap mainTilemap)
    {
        int x = (int)tiles.Min(x => x.x);
        int y = (int)tiles.Min(x => x.y);
        int width = (int)Mathf.Abs(tiles.Max(x => x.x) - x);
        int height = (int)Mathf.Abs(tiles.Max(x => x.y) - x);
        Vector2 center = GetCenter(mainTilemap);

        return new Bounds(new Vector3(center.x, center.y, 0), new Vector3(width, height, 0));
    }


    public List<Vector2> GetFloorTilePositions(Tilemap mainTilemap)
    {
        return GetTiles(GroundTilemap, mainTilemap);
    }

    public List<Vector2> GetBorderTilePositions(Tilemap mainTilemap)
    {
        List<Vector2> tiles = GetTiles(GroundTilemap, mainTilemap);
        List<Vector2> borderTiles = new List<Vector2>();
        int minX = (int)tiles.Min(x => x.x);
        int minY = (int)tiles.Min(x => x.y);
        int maxX = (int)tiles.Max(x => x.x);
        int maxY = (int)tiles.Max(x => x.y);

        for (int x = minX - 1; x < maxX + 1; x++)
            for (int y = minY - 1; y < maxY + 1; y++)
            {
                List<Vector2> tempTiles = tiles.Where(tile => tile.x == x && tile.y == y).ToList();
                if (!tempTiles.Any()) continue;
                Vector2 tile = tempTiles.First();
                List<Vector2> adjacentTiles = tiles.Where(adjacentTile =>
                {
                    return
                    adjacentTile.x == x + 1 && adjacentTile.y == y ||
                    adjacentTile.x == x - 1 && adjacentTile.y == y ||
                    adjacentTile.x == x && adjacentTile.y == y + 1 ||
                    adjacentTile.x == x && adjacentTile.y == y - 1;
                }).ToList();

                if (adjacentTiles.Count() != 4) borderTiles.Add(tile);
            }

        return borderTiles;
    }

    private List<Vector2> GetTiles(Tilemap tilemap, Tilemap mainTilemap)
    {
        return GroundTilemap.GetComponentsInChildren(typeof(Component)).
               Where(x => x.tag == "Ground" && x is Transform).
               Select(x =>
               {
                   Vector3Int position = mainTilemap.WorldToCell(x.transform.position);

                   return new Vector2(position.x, position.y);
               }).
               ToList();
    }


}
