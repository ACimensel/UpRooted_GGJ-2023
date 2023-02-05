using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileMotion : MonoBehaviour
{
    public float FiringAngle = 45.0f;
    public float Gravity = 9.8f;

    private Vector3 _targetPos;
    private Coroutine _coroutine;

    public void SetTargetPos(Vector3 pos)
    {
        _targetPos = pos;
        if (_coroutine != null) StopCoroutine(_coroutine);
        StartCoroutine(Simulateprojectile(_targetPos));
    }

    IEnumerator Simulateprojectile(Vector3 targPos)
    {
        // Calculate distance to target
        float targetDist = Vector3.Distance(transform.position, targPos);

        // Calculate the velocity needed to throw the object to the target at specified angle
        float projectileVel = targetDist / (Mathf.Sin(2 * FiringAngle * Mathf.Deg2Rad) / Gravity);

        // Extract the X  Y componenent of the velocity
        float vx = Mathf.Sqrt(projectileVel) * Mathf.Cos(FiringAngle * Mathf.Deg2Rad);
        float vy = Mathf.Sqrt(projectileVel) * Mathf.Sin(FiringAngle * Mathf.Deg2Rad);

        // Calculate flight time
        float flightDuration = targetDist / vx;

        // Rotate projectile to face the target
        transform.rotation = Quaternion.LookRotation(targPos - transform.position);

        float elapseTime = 0;
        while (elapseTime < flightDuration)
        {
            transform.Translate(0, (vy - (Gravity * elapseTime)) * Time.deltaTime, vx * Time.deltaTime);

            elapseTime += Time.deltaTime;

            yield return null;
        }
    }
}