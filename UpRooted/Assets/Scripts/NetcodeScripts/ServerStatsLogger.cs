using Unity.Netcode;
using UnityEngine;

public class ServerStatsLogger : NetworkBehaviour
{
    private readonly NetworkVariable<float> _serverUptimeNetworkVariable = new();
    private float _lastSecond;
    private const float TimeBetweenLogs = 5f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _serverUptimeNetworkVariable.Value = 0.0f;
            _lastSecond = 0.0f;
            Debug.Log($"Server's uptime var initialized to: {_serverUptimeNetworkVariable.Value}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {
            _serverUptimeNetworkVariable.Value += Time.deltaTime;
            if (_serverUptimeNetworkVariable.Value > _lastSecond + TimeBetweenLogs)
            {
                _lastSecond += TimeBetweenLogs;
                Debug.Log($"Server uptime is now: {_serverUptimeNetworkVariable.Value}");
                Debug.Log($"There are {NetworkManager.Singleton.ConnectedClients.Count} Clients connected");
            }
        }
    }
}