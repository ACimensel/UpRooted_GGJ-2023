using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
    [SerializeField] ParticleSystem ExplosionParticles;





    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<PlantGrowth>())
        {
            ExplosionParticles.Play();

        }
    }
}
