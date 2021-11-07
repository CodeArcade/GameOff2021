using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{

  public GameObject bullet;
  public Transform bulletSpawn;

  public float fireRate = 2f;
  private float nextFire = 0.0f;

  // needs to be replaced with actual ammo system
  public int maxAmmo = 8;
  public int currentAmmo;

  public float reloadTime = 2f;
  private float reloadCooldown = 0f;
  private bool autoReload = false;

  public bool hasBeenShot = false;

  void Start()
  {
    currentAmmo = maxAmmo;
  }

  // Update is called once per frame
  void Update()
  {
    autoReload = currentAmmo == 0;
    nextFire += Time.deltaTime;
    reloadCooldown -= Time.deltaTime;
    Shoot();
    Reload();
  }

  void Shoot()
  {
    if (!hasBeenShot
        && Input.GetMouseButtonDown(0)
        && nextFire > fireRate
        && currentAmmo > 0
        && reloadCooldown <= 0)
    {
      nextFire = 0f;
      currentAmmo--;
      Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation);
      hasBeenShot = true;
    }
    if (hasBeenShot && Input.GetMouseButtonUp(0))
    {
      hasBeenShot = false;
    }
  }

  public void Reload()
  {
    if ((Input.GetKey(KeyCode.R) && reloadCooldown <= 0) || autoReload)
    {
      reloadCooldown = reloadTime;
      currentAmmo = maxAmmo;
    }
  }
}
