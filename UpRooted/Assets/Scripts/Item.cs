using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] float speed = 24f;
    [SerializeField] Rigidbody rb;
    //[SerializeField] AudioSource audioSource;
    //[SerializeField] AudioClip audioClip;


    private void FixedUpdate()
    {
        UpdatePosition();
    }

    protected virtual void UpdatePosition()
    {
        rb.velocity = Vector3.right * speed;
    }

    void OnEnable()
    {
        Initialize();
    }

    protected virtual void Initialize()
    {

    }

    private void OnCollisionEnter(Collision collision)
    {
        //if (collision.gameObject.GetComponent<Wall>())
        //{
        //    //audioSource.PlayOneShot(audioClip);
        //    //print(collision.gameObject);
        //}

        //gameObject.SetActive(false);
    }
}