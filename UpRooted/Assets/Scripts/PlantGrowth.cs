using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantGrowth : MonoBehaviour
{

    [Header("Current scale")]
    public Vector3 currentScale;

    [Header("Max scale")]
    public Vector3 maxScale;

    [Header("Scaling Speed")]
    public Vector3 growthSpeed;

    //[SerializeField] float growthSpeed = 5f;
    //[SerializeField] int finalGrowthStage = 100;
    //[SerializeField] int currentGrowthStage = 0;

    public bool fullyGrown;
    //[SerializeField] AudioSource audioSource;
    //[SerializeField] AudioClip audioClip;

    void OnEnable()
    {
        currentScale = Vector3.zero;
    }

    private void Update()
    {
        if(currentScale.magnitude <= maxScale.magnitude)
        {
            UpdateGrowth();
        }
        else
        {
            fullyGrown = true;
        }
    }

    protected virtual void UpdateGrowth()
    {
            transform.localScale = transform.localScale + growthSpeed * Time.deltaTime;


    }

    protected virtual void Initialize()
    {

    }

    protected virtual void UpdatePosition()
    {

    }

    public void Harvest()
    {

    }
}