using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movementInput;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Handle input in Update
        HandleInput();
    }

    private void FixedUpdate()
    {
        // Apply physics calculations in FixedUpdate
        Move();
    }

    private void HandleInput()
    {
        // Keyboard input
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        movementInput = new Vector2(horizontalInput, verticalInput).normalized;
    }

    private void Move()
    {
        // Calculate movement direction and velocity
        Vector2 movementVelocity = movementInput * moveSpeed;

        // Apply the calculated velocity to the Rigidbody
        rb.velocity = movementVelocity;
    }
}
