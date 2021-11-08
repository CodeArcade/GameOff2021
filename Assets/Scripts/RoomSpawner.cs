using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomSpawner : MonoBehaviour
{
    public List<GameObject> Enemies;
    public List<int> EnemySpawnWeight;
    public List<int> HordeSpawnChance;

    private System.Random Random { get; set; } = new System.Random();

    public void Spawn(Vector2 bossRoomPosition, Tilemap groundTilemap, GameObject player)
    {
        Vector3Int position = groundTilemap.WorldToCell(transform.position);
        Vector2 positionVector2 = new Vector2(position.x, position.y);
        int maxDistance = (int)Mathf.Sqrt((new Vector2(0, 0) - bossRoomPosition).sqrMagnitude);
        int distanceToBoss = (int)Mathf.Sqrt((positionVector2 - bossRoomPosition).sqrMagnitude);
        int powerLevel = (maxDistance - distanceToBoss) / 10;

        int totalChance = EnemySpawnWeight.Sum();
        int spawnChance = Random.Next(0, totalChance);

        for (int i = 0; i < Enemies.Count; i++)
        {
            int minSpawnWeight = 0;
            int maxSpawnWeight = 0;
            for (int j = 0; j < i; j++)
                minSpawnWeight += EnemySpawnWeight[j];

            for (int j = 0; j <= i; j++)
                maxSpawnWeight += EnemySpawnWeight[j];

            if (spawnChance >= minSpawnWeight && spawnChance < maxSpawnWeight)
            {
                Vector3 pos = groundTilemap.CellToWorld(new Vector3Int(
                    (int)positionVector2.x,
                    (int)positionVector2.y,
                    (int)groundTilemap.transform.position.z));
                pos += new Vector3(0, 1.5f, 0);

                Instantiate(Enemies[i]).GetComponent<Enemy>().Spawn(pos, powerLevel, player);

                if (Random.Next(1, 101) <= HordeSpawnChance[i])
                {
                    int count = Random.Next(1, 5);
                    for (int k = 0; k < count; k++)
                    {
                        switch (i)
                        {
                            case 0:
                                pos = groundTilemap.CellToWorld(new Vector3Int(
                                    (int)positionVector2.x + 1,
                                    (int)positionVector2.y,
                                    (int)groundTilemap.transform.position.z));
                                break;
                            case 1:
                                pos = groundTilemap.CellToWorld(new Vector3Int(
                                    (int)positionVector2.x - 1,
                                    (int)positionVector2.y,
                                    (int)groundTilemap.transform.position.z));
                                break;
                            case 2:
                                pos = groundTilemap.CellToWorld(new Vector3Int(
                                    (int)positionVector2.x,
                                    (int)positionVector2.y + 1,
                                    (int)groundTilemap.transform.position.z));
                                break;
                            case 3:
                                pos = groundTilemap.CellToWorld(new Vector3Int(
                                    (int)positionVector2.x,
                                    (int)positionVector2.y - 1,
                                    (int)groundTilemap.transform.position.z));
                                break;
                        }

                        pos += new Vector3(0, 1.5f, 0);
                        Instantiate(Enemies[i]).GetComponent<Enemy>().Spawn(pos, powerLevel, player);
                    }
                }
            }
        }

    }

}
