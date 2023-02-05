using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControllerV : MonoBehaviour
{
    public int speed = 10;

    private Camera playerCamera;
    private Animator anim;
    private Rigidbody rb;
    private Vector3 velocity = Vector3.zero;
    private Vector3 unitTargetVelocity;
    private float strafeUpper = 0.4f;

    private void Awake()
    {
        playerCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        Move();
        RotatePlayer();
        PlayAnimation();
    }

    void Move()
    {
        unitTargetVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        Vector3.Normalize(unitTargetVelocity);
        // rb.velocity = Vector3.SmoothDamp(rb.velocity, unitTargetVelocity * speed, ref velocity, 0.03f);
        rb.velocity = unitTargetVelocity * speed;
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

    void PlayAnimation()
    {
        float dot = Vector3.Dot(transform.forward, unitTargetVelocity);
        bool isFacingRight = (transform.forward.x > 0f);
        bool isMovingUp = (unitTargetVelocity.z > 0f);

        if(rb.velocity.magnitude > 0.01f){
            if(dot > strafeUpper){
                anim.SetTrigger("Forward");
            }
            else if(dot < -strafeUpper){
                anim.SetTrigger("Backward");
            }
            else{
                if(isFacingRight && unitTargetVelocity.z > 0.1f) anim.SetTrigger("Left");
                else if(isFacingRight && unitTargetVelocity.z < -0.1f) anim.SetTrigger("Right");
                else if(!isFacingRight && unitTargetVelocity.z > 0.1f) anim.SetTrigger("Right");
                else if(!isFacingRight && unitTargetVelocity.z < -0.1f) anim.SetTrigger("Left");
            }
        }
        else{
            anim.SetTrigger("Idle");
        }
    }
}