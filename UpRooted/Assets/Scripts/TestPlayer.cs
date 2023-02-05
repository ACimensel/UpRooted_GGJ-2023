using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TestPlayer : MonoBehaviour
{
    float _horizontal, _vertical;
    [SerializeField] float Speed = 5f;

    [SerializeField] Rigidbody Rb;
    [SerializeField] GameObject ItemPrefab;
    [SerializeField] Transform ItemStartTransform;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        _horizontal = Input.GetAxisRaw("Horizontal");
        _vertical = Input.GetAxisRaw("Vertical");

        if (Input.GetButtonDown("Fire1"))
        {
            print("Fire");
            Fire();
        }
    }

    private void FixedUpdate()
    {
        Rb.velocity = new Vector3(_horizontal * Speed, _vertical * Speed);
    }

    void Fire()
    {
        NetworkObject networkObject = NetcodeObjectPool.Singleton.GetNetworkObject(ItemPrefab, ItemStartTransform.position, Quaternion.identity);
       
    }
}
