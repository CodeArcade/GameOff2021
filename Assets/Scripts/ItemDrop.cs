using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// </summary>
public class ItemDrop : MonoBehaviour
{
  // the int describes the drop chance in %
  // the higher the number, the higher the chance
  // order is irrelevant
  // the game objects described in this list will be prefabs of the items
  public List<Tuple<GameObject, int>> DropTable;

  void OnDestroy()
  {
    if (gameObject.activeSelf)
    {
      GameObject drop = GetItemDrop();
      if (drop != null)
      {
        Instantiate(drop).transform.position = gameObject.transform.position;
      }

      Destroy(gameObject);
    }
  }

  GameObject GetItemDrop()
  {
    int random = UnityEngine.Random.Range(0, 101);

    GameObject item = null;
    int dropChance = 100;

    // loop over the drop table
    // if an item is found that fits the drop rate roll and has a lower drop chance than the current item, replace it
    for (int i = 0; i < DropTable.Count; i++)
    {
      if (random <= DropTable[i].Item2 && DropTable[i].Item2 < dropChance)
      {
        item = DropTable[i].Item1;
        dropChance = DropTable[i].Item2;
      }
    }

    return item;
  }
}
