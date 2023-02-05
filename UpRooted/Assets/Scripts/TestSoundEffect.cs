using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSoundEffect : MonoBehaviour
{
    public AudioClip LettuceCut;
    void Start() {
        SoundManager.Instance.Play(LettuceCut);
    }
}
