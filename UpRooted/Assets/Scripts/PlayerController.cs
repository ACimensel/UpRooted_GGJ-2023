using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Camera PlayerCamera;

    private readonly int _speed = 10;
    private Rigidbody _rb;

    private void Awake()
    {
        PlayerCamera = Camera.main;
        _rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        Move();
        RotatePlayer();
    }

    void Move()
    {
        Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        transform.position += movement * _speed * Time.deltaTime;
        
        // rb.velocity = movement * speed * Time.deltaTime;
    }

    void RotatePlayer()
    {
        Ray mouseRay = PlayerCamera.ScreenPointToRay(Input.mousePosition);
        Plane p = new Plane(Vector3.up, transform.position);
        if (p.Raycast(mouseRay, out float hitDist))
        {
            Vector3 hitPoint = mouseRay.GetPoint(hitDist);
            transform.LookAt(hitPoint);
        }
    }
}