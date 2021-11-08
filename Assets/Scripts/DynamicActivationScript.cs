using System;
using System.Collections.Concurrent;
using UnityEngine;

public class DynamicActivationScript : MonoBehaviour
{
    public static ConcurrentDictionary<Guid, GameObject> GameObjects { get; set; } = new ConcurrentDictionary<Guid, GameObject>();

    private void FixedUpdate()
    {
        foreach (var gameObject in GameObjects.Values) ChangeState(gameObject);
    }

    private void ChangeState(GameObject gameObject)
    {
        if (gameObject is null) return;

        if (DistanceTo(gameObject.transform.position) < 50)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }
        else
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private int DistanceTo(Vector3 targetNode)
    {
        return (int)Math.Sqrt((new Vector2(targetNode.x, targetNode.y) - new Vector2(transform.position.x, transform.position.y)).sqrMagnitude);
    }
}
