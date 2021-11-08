using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{

  public CharacterController controller;
  public Camera mainCamera;

  public bool canMove = true;
  public float speed = 6f;
  public float dashCooldown = 1f;
  public float dashDuration = 0.2f;
  public float dashSpeedMultiplier = 3f;

  bool isDodging = false;
  public float dashTimer { get; private set; } = 0f;
  // Update is called once per frame
  void Update()
  {
    lookTowardsMouse();
    if (!canMove) return;
    
    move();
    dash();
  }

  void move()
  {
    Vector3 direction = getMovementDirection();

    if (!isDodging && direction.magnitude >= 0.1f)
    {
      controller.Move(direction * speed * Time.deltaTime);
    }
  }

  Vector3 getMovementDirection()
  {
    float horizontal = Input.GetAxisRaw("Horizontal");
    float vertical = Input.GetAxisRaw("Vertical");

    Vector3 movement = new Vector3(horizontal, 0f, vertical).normalized;

    Vector3 forward = mainCamera.transform.forward;
    Vector3 right = mainCamera.transform.right;

    forward.y = 0f;
    right.y = 0f;
    forward.Normalize();
    right.Normalize();

    Vector3 direction = forward * movement.z + right * movement.x;
    return direction;
  }

  void lookTowardsMouse()
  {
    Ray cameraRay = mainCamera.ScreenPointToRay(Input.mousePosition);
    Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
    float rayLength;

    if (groundPlane.Raycast(cameraRay, out rayLength))
    {
      Vector3 pointToLook = cameraRay.GetPoint(rayLength);
      Debug.DrawLine(cameraRay.origin, pointToLook, Color.cyan);

      transform.LookAt(new Vector3(pointToLook.x, transform.position.y, pointToLook.z));
    }
  }

  void dash()
  {
    dashTimer += Time.deltaTime;

    if (isDodging && dashTimer < dashDuration)
    {
      controller.Move(transform.forward * speed * dashSpeedMultiplier * Time.deltaTime);
    }

    if (dashTimer >= dashDuration)
    {
      isDodging = false;
    }

    if (dashTimer < dashCooldown) return;

    if (Input.GetKeyDown(KeyCode.Space))
    {
      isDodging = true;
      dashTimer = 0f;
      controller.Move(transform.forward * speed * dashSpeedMultiplier * Time.deltaTime);
    }
  }
}