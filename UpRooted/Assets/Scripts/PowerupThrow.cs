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
        if (_coroutine == null & IsOwner)
        {
            //Charge throw On Left Mouse Down
            if (Input.GetMouseButton(0))
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

                if ( TouchingObject != null)
                {
                    if (NetworkManager.Singleton.IsServer)
                        TouchingObject.SendMessage("SetTargetPos", TargetPos);
                    else ThrowServerRpc(TouchingObject.GetComponent<NetworkObject>().NetworkObjectId, TargetPos);
                    DropItem();
                }
            }
        }
    }

    [ServerRpc]
    private void ThrowServerRpc(ulong objId, Vector3 targetPos)
    {
        NetworkManager.SpawnManager.SpawnedObjects[objId].SendMessage("SetTargetPos", targetPos);
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


    private void DropItem()
    {
        TouchingObject = null;//???
    }
}