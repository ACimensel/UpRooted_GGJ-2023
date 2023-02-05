using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CountDownUI : NetworkBehaviour
{
    [SerializeField] private int CountDown = 4;
    [SerializeField] private TMP_Text NumberDisplay;
    [SerializeField] private AnimationCurve NumberAnimation;

    private float _timer;
    private int _displayNumber;
    private float _animationTimer;
    private bool _countdown;

    public void OnEnable()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NumberDisplay.text = "Waiting for other player.";
            _displayNumber = CountDown;
            _timer = 0.0f;
            _animationTimer = 0.0f;
            NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }
        else
        {
            NumberDisplay.text = "Connecting...";
            StartCoroutine(WaitForConnected());
        }
    }

    IEnumerator WaitForConnected()
    {
        while (!NetworkManager.Singleton.IsConnectedClient)
            yield return null;
        
        _countdown = true;
    }

    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        _countdown = true;
        NetworkManager.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
    }

    private void Update()
    {
        if (!_countdown) return;

        _timer += Time.deltaTime;

        if (_timer > CountDown)
        {
            OnCountdownFinished();
            return;
        }
        _animationTimer += Time.deltaTime;
        
        _displayNumber = Mathf.FloorToInt(CountDown - _timer);
        string displayString = _displayNumber.ToString();
        if (displayString != NumberDisplay.text)
        {
            NumberDisplay.text = displayString;
            _animationTimer = 0;
            NumberAnimation.Evaluate(_animationTimer);
        }
        else
        {
            NumberAnimation.Evaluate(_animationTimer);
        }
    }

    private void OnCountdownFinished()
    {
        if (NetworkManager.Singleton.IsServer)
            NetcodePickupSpawner.Singleton.RoundBegin();
        
        gameObject.SetActive(false);
        _countdown = false;
    }
}
