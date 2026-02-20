using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class player : MonoBehaviour
{
    //The player's movement, jump, and rotation speed
    public float movementSpeed, jumpSpeed, rotationSpeed;

    //The player's raycast distance determines how long the raycast that detects if the player is touching the ground is
    public float raycastDistance;

    //Amount that is added onto the movement speed when the player sprints
    public float sprintAddAmount;

    //The player's walking/starting speed
    float originalSpeed;

    //The player's Rigidbody
    public Rigidbody playerRigidbody;

    //Bool that determines if the player can jump or not
    bool canJump;

    //The LayerMask is used to make sure our raycast doesn't detect layers you don't want it to detect
    public LayerMask layerMask;

    //The Start() void is used for actions that happen at the start of the scene
    void Start()
    {
        //original speed will equal to the movement speed at the start of the scene
        originalSpeed = movementSpeed;
    }

    //The FixedUpdate() void is used for physics calculations. It keeps physics-related stuff consistent at any frame rate.
    void FixedUpdate()
    {
        //Gets the player's vertical input (forwards and backwards)
        float verticalInput = Input.GetAxis("Vertical");

        //Determines the player's movement direction based on the vertical input
        Vector3 movement = (transform.forward * verticalInput) * movementSpeed;

        //The Y axis of the movement Vector3 variable will equal to the velocity of the player's Rigidbody's movement velocity when moving up/down
        movement.y = playerRigidbody.linearVelocity.y;

        //The player's Rigidbody's velcoity will equal to the movement Vector3 variable so the player can move
        playerRigidbody.linearVelocity = movement;

        //The touchingGround bool will equal true or false depending on whether the raycast is hitting the ground or not
        bool touchingGround = Physics.Raycast(transform.position, Vector3.down, raycastDistance, layerMask);

        //canJump will equal to whatever touchingGround equals to
        canJump = touchingGround;
    }

    //The Update() void is where actions will happen every frame
    void Update()
    {
        //If the player can jump
        if (canJump)
        {
            //If the player presses down the Space key
            if (Input.GetKeyDown(KeyCode.Space))
            {
                //The player's Rigidbody will have force added so it moves up multiplied by the jump speed. ForceMode.Impulse indicates we want the Rigidbody to have the force applied quickly
                playerRigidbody.AddForce(Vector3.up * jumpSpeed, ForceMode.Impulse);
            }
        }
        //If the player holds down the left shift key
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            //The movement speed will have the sprint add amount added onto it
            movementSpeed = movementSpeed + sprintAddAmount;
        }
        //If the player lets go of the left shift key
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            //The movement speed will equal to the original speed
            movementSpeed = originalSpeed;
        }

        //Gets the player's horizontal input (left and right)
        float rotationInput = Input.GetAxis("Horizontal");

        //Player will rotate left/right based on the rotation input
        transform.Rotate(0, rotationInput * rotationSpeed * Time.deltaTime, 0);
    }
}