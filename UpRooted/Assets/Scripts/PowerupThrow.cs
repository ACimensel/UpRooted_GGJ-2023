using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PowerupThrow : NetworkBehaviour
{
    public Transform Projectile;
    public Renderer TargetRend;
    public float FiringAngle = 45.0f;
    public float Gravity = 9.8f;
    [Range(1f, 8f)] public float MinDist = 5f;

    [HideInInspector] public Vector3 TargetPos;
    private float _distToThrow;
    public int ThrowIncreaseSpeed = 12;
    private Coroutine _coroutine;

    public GameObject HeldItem;
    public GameObject TouchingObject;


    void Start()
    {
        _distToThrow = MinDist;
    }

    void Update()
    {
        HandleMouseButton();
    }

    void SetTargetPos()
    {
        Vector3 v = transform.position + transform.forward * _distToThrow;
        TargetPos = new Vector3(Mathf.Clamp(v.x, -14.9f, 14.9f), Projectile.localScale.y / 2, Mathf.Clamp(v.z, -4.95f, 4.95f));
    }

    void HandleMouseButton()
    {
        if (_coroutine == null)
        {
            if(HeldItem == null)
            {
                

            }
            //Charge throw On Left Mouse Down
            else if (Input.GetMouseButton(0))
            {
                Debug.Log("Holding primary button.");
                TargetRend.enabled = true;
                _distToThrow += Time.deltaTime * ThrowIncreaseSpeed;
                SetTargetPos();
            }
            //Throw On Left Mouse Release
            else if (Input.GetMouseButtonUp(0))
            {
                Debug.Log("Primary button UP.");
                TargetRend.enabled = false;

                _distToThrow = MinDist;

                if (HeldItem != null) 
                {
                    HeldItem.SendMessage("SetTargetPos", TargetPos);

                    //NetworkObject networkObject = NetcodeObjectPool.Singleton.GetNetworkObject(projectile.gameObject, transform.position, Quaternion.identity);
                    //networkObject.gameObject.SendMessage("SetTargetPos", targetPos);

                    DropItem();
                }
            }
        }
        if (Input.GetMouseButtonDown(1) && HeldItem != null) 
        {
            DropItem();
        }
        else if (Input.GetMouseButtonDown(1) && HeldItem == null && TouchingObject != null)
        {
            PickUpItem();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.GetComponent<ProjectileMotion>())
        {
            TouchingObject = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        TouchingObject = null;
    }

    private void PickUpItem()
    {
        if (TouchingObject.TryGetComponent<PlantGrowth>(out PlantGrowth plantGrowth))
        {
            if (plantGrowth.FullyGrown == false)
                return;
      
            //plantGrowth.Harvest();
            HeldItem = TouchingObject;
            HeldItem.transform.position = transform.position;
            HeldItem.transform.parent = transform;
        }

        //ObjectIwantToPickUp.GetComponent<Rigidbody>().isKinematic = true;
    }

    private void DropItem()
    {
        HeldItem.transform.parent = null;
        HeldItem = null;
        TouchingObject = null;//???
    }
}