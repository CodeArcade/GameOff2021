using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// </summary>
public class Pickup : MonoBehaviour
{
  private Inventory Inventory;

  // the int describes the drop chance in %
  // the higher the number, the higher the chance
  // order is irrelevant
  public List<Tuple<Item.Item, int>> DropTable;

  void Start()
  {
    Inventory = GameObject.FindGameObjectWithTag("Player").GetComponent<Inventory>();
  }

  void OnTriggerEnter(Collider other)
  {
    if (!other.CompareTag("Player"))
      return;

    if (gameObject.activeSelf)
    {
      Item.Item drop = GetItemDrop();
      if (drop != null)
      {
        Inventory.Items.Add(drop);
      }

      Destroy(gameObject);
    }
  }

  Item.Item GetItemDrop()
  {
    int random = UnityEngine.Random.Range(0, 101);

    Item.Item item = null;
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
