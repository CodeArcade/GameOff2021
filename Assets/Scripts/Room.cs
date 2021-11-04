using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Room : MonoBehaviour
{
    public Tilemap GroundTilemap;

    public Bounds Bounds(Tilemap tilemap, Tilemap mainTilemap)
    {
        return Bounds(GetTiles(GroundTilemap, mainTilemap));
    }

    public Bounds Bounds(List<Vector2> tiles)
    {
        int x = (int)tiles.Min(x => x.x);
        int y = (int)tiles.Min(x => x.y);
        int width = (int)Mathf.Abs(tiles.Max(x => x.x) - x);
        int height = (int)Mathf.Abs(tiles.Max(x => x.y) - x);

        return new Bounds(new Vector3(x, y, 0), new Vector3(width, height, 0));
    }


    public List<Vector2> GetFloorTilePositions(Tilemap mainTilemap)
    {
        return GetTiles(GroundTilemap, mainTilemap);
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
