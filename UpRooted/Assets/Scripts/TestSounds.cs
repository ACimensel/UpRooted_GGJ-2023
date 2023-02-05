using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSounds : MonoBehaviour
{
    public AudioClip FarmMusic;
    void Start() {
        SoundManager.Instance.PlayMusic(FarmMusic);
    }
}
