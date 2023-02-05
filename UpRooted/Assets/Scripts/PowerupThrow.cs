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
            if (Input.GetMouseButton(0))
            {
                Debug.Log("Holding primary button.");
                targetRend.enabled = true;
                distToThrow += Time.deltaTime * throwIncreaseSpeed;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                NetworkObject networkObject = NetcodeObjectPool.Singleton.GetNetworkObject(projectile.gameObject, transform.position, Quaternion.identity);

                networkObject.gameObject.SendMessage("SetTargetPos", targetPos);

                Debug.Log("Primary button UP.");
                targetRend.enabled = false;

                //projectile.position = transform.position;
                //projectile.gameObject.SendMessage("SetTargetPos", targetPos);

                distToThrow = minDist;
            }
        }
    }
}