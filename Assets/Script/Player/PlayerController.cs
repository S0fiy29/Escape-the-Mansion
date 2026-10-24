using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerAnimation playerAnimation;
    private Vector2 input;
    [SerializeField] private float moveSpeed = 4.0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Smooth rendering between physics steps and no tunnelling through thin walls or doors.
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        playerAnimation = GetComponentInChildren<PlayerAnimation>();
        if (playerAnimation == null)
        {
            playerAnimation = FindAnyObjectByType<PlayerAnimation>();
        }
    }

    // Dialogues and panels disable this component to freeze the player, so stop the body and the walk animation here.
    private void OnDisable()
    {
        input = Vector2.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        if (playerAnimation != null)
        {
            playerAnimation.SetDirection(Vector2.zero);
        }
    }

    // Read input every frame so quick key presses are not missed between physics steps.
    private void Update()
    {
        input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        // Keeps diagonal movement from being faster than straight movement.
        input = Vector2.ClampMagnitude(input, 1f);
    }

    private void FixedUpdate()
    {
        Vector2 velocity = input * moveSpeed;
        rb.linearVelocity = velocity;

        if (playerAnimation != null)
        {
            playerAnimation.SetDirection(velocity);
        }
    }
}
