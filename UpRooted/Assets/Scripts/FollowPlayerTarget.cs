using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayerTarget : MonoBehaviour
{
    public float y_offset = 0.3f;

    private PowerupThrow _pl;

    void Start()
    {
        _pl = gameObject.GetComponentInParent<PowerupThrow>();
    }

    void Update()
    {
        transform.position = new Vector3(_pl.TargetPos.x, y_offset, _pl.TargetPos.z);
    }
}