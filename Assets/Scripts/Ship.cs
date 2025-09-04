using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Ship : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private BoxCollider2D bc;
    [SerializeField] private PlayerInput pI;
    [SerializeField] private InputAction move;
    [SerializeField] private Vector2 moveInput;
    [SerializeField] private float currentSpeed;
    [SerializeField] private int health;
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        bc = gameObject.GetComponent<BoxCollider2D>();
        pI = gameObject.GetComponent<PlayerInput>();
        currentSpeed = 5;
        health = 3;
    }

    private void OnEnable()
    {
        move = pI.actions["ShipMovement"];
    }

    // Update is called once per frame
    void Update()
    {
        MoveShip();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void MoveShip()
    {
        try
        {
            moveInput = move.ReadValue<Vector2>();
            if (moveInput.magnitude > 1f)
            {
                moveInput = moveInput.normalized;
            }

        }
        catch (Exception ex)
        {
            print("no ship in scene");
        }

    }

    void HandleMovement()
    {
        try
        {
            rb.linearVelocity = moveInput * currentSpeed;
        }
        catch (Exception ex)
        {
            print("no ship in scene");
        }
    }

    public void GetDamage(int value)
    {
       health -= value;

        if (GetHealth() <= 0)
        {
            this.gameObject.SetActive(false);
            print("has perdido");
        }
    }

    public int GetHealth()
    {
        return health;
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<RockBehaviour>() != null)
        {
            print(collision.gameObject.name);
            GetDamage(3);
        }
    }
}
