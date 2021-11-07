using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
  public float speed = 10f;
  public float TTL = 20f;

  void Update()
  {
    TTL -= Time.deltaTime;
    transform.Translate(Vector3.forward * speed * Time.deltaTime);
    if (TTL <= 0)
    {
      Destroy(gameObject);
    }
  }

  void OnTriggerEnter(Collider collision)
  {
    if (collision.isTrigger) return;
    if (collision.gameObject.tag == "Enemy")
    {
      // deal damage and so on
    }
    if (collision.gameObject.tag == "Enemy" || collision.gameObject.tag == "Wall")
    {
      Destroy(gameObject);
    }
  }
}
