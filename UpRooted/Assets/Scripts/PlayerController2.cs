using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController2 : MonoBehaviour
{
    public Camera playerCamera;

    public int speed = 10;
    private Rigidbody rb;
    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        playerCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        Move();
        RotatePlayer();
        // Debug.Log(Input.GetAxis("Horizontal"));
        // Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
    }

    void Move()
    {
        Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        Debug.Log(movement);
        // transform.position += movement * speed * Time.deltaTime;
        
        // // rb.velocity = movement * speed * Time.deltaTime;
        // dirX = Input.GetAxisRaw("Horizontal") * moveSpeed;
        // // Move the character by finding the target velocity
        // Vector3 targetVelocity = new Vector2(dirX, rb.velocity.y);
        // // And then smoothing it out and applying it to the character
        // if (isGrounded)
        // rb.velocity = Vector3.SmoothDamp(rb.velocity, movement, ref velocity, 0.03f);
        rb.velocity = movement * speed * Time.deltaTime;
    }

    void RotatePlayer()
    {
        Ray mouseRay = playerCamera.ScreenPointToRay(Input.mousePosition);
        Plane p = new Plane(Vector3.up, transform.position);
        if (p.Raycast(mouseRay, out float hitDist))
        {
            Vector3 hitPoint = mouseRay.GetPoint(hitDist);
            transform.LookAt(hitPoint);
        }
    }
}