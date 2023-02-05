using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestLoopingSounds : MonoBehaviour
{
    public AudioClip FarmMusic;
    void Start() {
        SoundManager.Instance.PlayLoop(FarmMusic);
    }
}
