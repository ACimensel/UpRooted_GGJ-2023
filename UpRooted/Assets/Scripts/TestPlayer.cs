using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TestPlayer : MonoBehaviour
{
    float horizontal, vertical;
    [SerializeField] float speed = 5f;

    [SerializeField] Rigidbody rb;
    [SerializeField] GameObject itemPrefab;
    [SerializeField] Transform itemStartTransform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        if (Input.GetButtonDown("Fire1"))
        {
            print("Fire");
            Fire();
        }
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector3(horizontal * speed, vertical * speed);
    }

    void Fire()
    {
        NetworkObject networkObject = NetcodeObjectPool.Singleton.GetNetworkObject(itemPrefab, itemStartTransform.position, Quaternion.identity);
       
    }
}
