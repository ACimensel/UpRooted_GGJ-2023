using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreCounterManager : MonoBehaviour
{
    [SerializeField] OverlapBox LeftOverlapBox;
    [SerializeField] OverlapBox RightOverlapBox;

    private static ScoreCounterManager _instance;

    public static ScoreCounterManager Singleton => _instance;

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

    public void Count()
    {
        LeftOverlapBox.MyCollisions();
        RightOverlapBox.MyCollisions();
    }

}
