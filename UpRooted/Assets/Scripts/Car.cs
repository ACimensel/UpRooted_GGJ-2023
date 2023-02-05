using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
    [SerializeField] ParticleSystem explosionParticles;





    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<PlantGrowth>())
        {
            explosionParticles.Play();

        }
    }
}
