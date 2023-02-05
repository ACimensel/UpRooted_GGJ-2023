using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreCounterManager : MonoBehaviour
{
    private static ScoreCounterManager _instance;

    public static ScoreCounterManager Singleton => _instance;

    [SerializeField] OverlapBox LeftPointCounter;
    [SerializeField] OverlapBox RightPointCounter;




    public void Count()
    {
        LeftPointCounter.MyCollisions();
        RightPointCounter.MyCollisions();
    }



    public void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }
}
