using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayerTarget : MonoBehaviour
{
    private PowerupThrow _pl;

    void Start()
    {
        _pl = gameObject.GetComponentInParent<PowerupThrow>();
    }

    void Update()
    {
        transform.position = new Vector3(_pl.TargetPos.x, 0, _pl.TargetPos.z);
    }
}