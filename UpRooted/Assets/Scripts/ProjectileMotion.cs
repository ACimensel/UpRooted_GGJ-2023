using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileMotion : MonoBehaviour
{
    public float firingAngle = 45.0f;
    public float gravity = 9.8f;

    private Vector3 targetPos;
    private Coroutine coroutine;

    public void SetTargetPos(Vector3 pos)
    {
        targetPos = pos;
        if (coroutine != null) StopCoroutine(coroutine);
        StartCoroutine(Simulateprojectile(targetPos));
    }

    IEnumerator Simulateprojectile(Vector3 targPos)
    {
        // Calculate distance to target
        float targetDist = Vector3.Distance(transform.position, targPos);

        // Calculate the velocity needed to throw the object to the target at specified angle
        float projectileVel = targetDist / (Mathf.Sin(2 * firingAngle * Mathf.Deg2Rad) / gravity);

        // Extract the X  Y componenent of the velocity
        float Vx = Mathf.Sqrt(projectileVel) * Mathf.Cos(firingAngle * Mathf.Deg2Rad);
        float Vy = Mathf.Sqrt(projectileVel) * Mathf.Sin(firingAngle * Mathf.Deg2Rad);

        // Calculate flight time
        float flightDuration = targetDist / Vx;

        // Rotate projectile to face the target
        transform.rotation = Quaternion.LookRotation(targPos - transform.position);

        float elapse_time = 0;
        while (elapse_time < flightDuration)
        {
            transform.Translate(0, (Vy - (gravity * elapse_time)) * Time.deltaTime, Vx * Time.deltaTime);

            elapse_time += Time.deltaTime;

            yield return null;
        }
    }
}