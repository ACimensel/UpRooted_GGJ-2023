using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantGrowth : MonoBehaviour
{

    public Vector3 currentScale;
    private Vector3 startingScale;

    public Vector3 maxScale;

    public Vector3 growthSpeed;

    //[SerializeField] float growthSpeed = 5f;
    //[SerializeField] int finalGrowthStage = 100;
    //[SerializeField] int currentGrowthStage = 0;

    [SerializeField] ParticleSystem readyParticles;

    public bool fullyGrown;
    //[SerializeField] AudioSource audioSource;
    //[SerializeField] AudioClip audioClip;

    void Awake()
    {
        startingScale = transform.localScale; // Store the starting scale
        transform.localScale = Vector3.zero;
    }

    //void OnEnable()
    //{
    //    currentScale = Vector3.zero;
    //}

    private void Update()
    {
        currentScale = transform.localScale;

        if(!fullyGrown && currentScale.magnitude <= maxScale.magnitude)
        {
            UpdateGrowth();
        }
        else if(!fullyGrown)
        {
            FullyGrown();
        }
    }

    protected virtual void UpdateGrowth()
    {
        transform.localScale = transform.localScale + growthSpeed * Time.deltaTime;
    }

    protected virtual void FullyGrown()
    {
        readyParticles.Play();
        fullyGrown = true;
    }


    protected virtual void Initialize()
    {

    }

    protected virtual void UpdatePosition()
    {

    }

    public void Harvest()
    {
        if (fullyGrown)
        {
            print("Harvest");
        }
        else
        {
            print("HARVESTED TOO EARLY");
        }

        
    }
}