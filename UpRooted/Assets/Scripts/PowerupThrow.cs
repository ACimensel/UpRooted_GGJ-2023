using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PowerupThrow : MonoBehaviour
{
    public Transform projectile; // TODO instantiate
    public Renderer targetRend;
    public float firingAngle = 45.0f;
    public float gravity = 9.8f;
    [Range(1f, 8f)] public float minDist = 5f;

    [HideInInspector] public Vector3 targetPos;
    private float distToThrow;
    public int throwIncreaseSpeed = 12;
    private Coroutine coroutine;

    public GameObject heldItem;
    public GameObject touchingObject;


    void Start()
    {
        distToThrow = minDist;
    }

    void Update()
    {
        SetTargetPos();
        HandleMouseButton();
    }

    void SetTargetPos()
    {
        Vector3 v = transform.position + transform.forward * distToThrow;
        targetPos = new Vector3(v.x, projectile.localScale.y / 2, v.z);
    }

    void HandleMouseButton()
    {
        if (coroutine == null)
        {
            if(heldItem == null)
            {
                if(touchingObject.TryGetComponent<PlantGrowth>(out PlantGrowth plantGrowth))
                {
                    plantGrowth.Harvest();
                    heldItem = touchingObject;
                    print("touching plantgrowth");
                }

            }
            //Charge throw On Left Mouse Down
            else if (Input.GetMouseButton(0))
            {
                Debug.Log("Holding primary button.");
                targetRend.enabled = true;
                distToThrow += Time.deltaTime * throwIncreaseSpeed;
            }
            //Throw On Left Mouse Release
            else if (Input.GetMouseButtonUp(0))
            {
                Debug.Log("Primary button UP.");
                targetRend.enabled = false;

                distToThrow = minDist;

                if (heldItem != null) 
                {
                    heldItem.SendMessage("SetTargetPos", targetPos);

                    //NetworkObject networkObject = NetcodeObjectPool.Singleton.GetNetworkObject(projectile.gameObject, transform.position, Quaternion.identity);
                    //networkObject.gameObject.SendMessage("SetTargetPos", targetPos);

                    DropItem();
                }
            }
        }
        if (Input.GetMouseButtonDown(1) && heldItem != null) 
        {
            DropItem();
        }
        else if (Input.GetMouseButtonDown(1) && heldItem == null && touchingObject != null)
        {
            PickUpItem();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.GetComponent<ProjectileMotion>())
        {
            touchingObject = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        touchingObject = null;
    }

    private void PickUpItem()
    {
        //ObjectIwantToPickUp.GetComponent<Rigidbody>().isKinematic = true;
        heldItem = touchingObject;
        heldItem.transform.position = transform.position;
        heldItem.transform.parent = transform;
    }

    private void DropItem()
    {
        heldItem.transform.parent = null;
        heldItem = null;
        touchingObject = null;//???
    }
}