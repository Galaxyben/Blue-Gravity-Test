using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 180f; // Adjust this to control rotation speed

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

        // Rotate the sprite towards movement direction
        RotateSprite();
    }

    private void RotateSprite()
    {
        if (movementInput != Vector2.zero)
        {
            float targetRotation = Mathf.Atan2(0, movementInput.x) * Mathf.Rad2Deg;
            Quaternion targetQuaternion = Quaternion.Euler(0f, targetRotation, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetQuaternion, rotationSpeed);
        }
    }
}
