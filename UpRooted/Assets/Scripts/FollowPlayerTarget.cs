using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayerTarget : MonoBehaviour
{
    private PowerupThrow pl;

    void Start()
    {
        pl = gameObject.GetComponentInParent<PowerupThrow>();
    }

    void Update()
    {
        transform.position = new Vector3(pl.targetPos.x, 0, pl.targetPos.z);
    }
}