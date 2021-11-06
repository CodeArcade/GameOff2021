using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    public GameObject Player;
    public bool DetectedPlayer;
    public bool IsAttacking;

    public float Speed = 6;
    public float Range = 2;
    public float DetectionRadius = 10;
    public int Health = 1;
    public int Damage = 1;
    public float AttackSpeedInSeconds = 1;
    private float AttackSpeedTimer;

    public List<Item.Item> Drops { get; set; }

    private CharacterController CharacterController;
    private SphereCollider DetectionArea;

    protected float DistanceToPlayer => Vector3.Distance(transform.position, Player.transform.position);
    protected Vector3 DirectionToPlayer => new Vector3(Player.transform.position.x, transform.position.y, Player.transform.position.z);

    void Start()
    {
        DetectedPlayer = false;

        DetectionArea = gameObject.GetComponent<SphereCollider>();
        DetectionArea.radius = DetectionRadius;
        DetectionArea.isTrigger = true;

        CharacterController = gameObject.GetComponent<CharacterController>();
    }

    void FixedUpdate()
    {
        AttackSpeedInSeconds += Time.deltaTime;

        if (!DetectedPlayer) return;

        Rotate();
        Move();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == Player && !DetectedPlayer)
        {
            Debug.Log("Detected player");

            DetectedPlayer = true;
        }
    }

    protected virtual void Move()
    {
        if (DistanceToPlayer > Range && !IsAttacking)
            CharacterController.
            Move((Player.transform.position - transform.position).normalized * Speed * Time.deltaTime);
    }

    protected virtual void Rotate()
    {
        transform.LookAt(DirectionToPlayer);
    }

    protected virtual bool CanAttack()
    {
        return DistanceToPlayer <= Range && !IsAttacking && AttackSpeedTimer >= AttackSpeedInSeconds;
    }

    protected virtual void Attack()
    {
        IsAttacking = true;

        // Triggering Attack is done by animation event
    }

    protected virtual void EndAttack()
    {
        AttackSpeedTimer = 0;
        IsAttacking = false;
    }

}
