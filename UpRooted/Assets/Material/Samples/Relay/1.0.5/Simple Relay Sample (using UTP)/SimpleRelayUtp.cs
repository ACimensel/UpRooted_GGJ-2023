using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A simple sample showing how to use the Relay Allocation package with the Unity Transport Protocol (UTP).
/// It demonstrates how UTP can be used as either Hosts or Joining Players, covering the entire connection flow.
/// As a bonus, a simple demonstration of Relaying messages from Host to Players, and vice versa, is included.
/// </summary>
public class SimpleRelayUtp : MonoBehaviour
{
    // GUI GameObjects

    /// <summary>
    /// The textbox displaying the Host's Player Id.
    /// </summary>
    public Text HostPlayerIdText;

    /// <summary>
    /// The textbox displaying the Player's Player Id.
    /// </summary>
    public Text PlayerPlayerIdText;

    /// <summary>
    /// The dropdown displaying the region.
    /// </summary>
    public Dropdown RegionsDropdown;

    /// <summary>
    /// The textbox displaying the Allocation Id.
    /// </summary>
    public Text HostAllocationIdText;

    /// <summary>
    /// The textbox displaying the Join Code.
    /// </summary>
    public Text JoinCodeText;

    /// <summary>
    /// The input field for the Join Code that the Player inputs to join the Host's Relay server.
    /// </summary>
    public InputField JoinCodeInput;

    /// <summary>
    /// The textbox displaying the Allocation Id of the joined allocation.
    /// </summary>
    public Text PlayerAllocationIdText;

    /// <summary>
    /// The textbox displaying the Allocation Id of the joined allocation.
    /// </summary>
    public Text PlayerRegionText;

    /// <summary>
    /// The textbox displaying whether or not the Host is bound.
    /// </summary>
    public Text HostBoundText;

    /// <summary>
    /// The textbox displaying whether or not the Player is bound.
    /// </summary>
    public Text PlayerBoundText;

    /// <summary>
    /// The textbox displaying whether or not the Player is connected to the Host.
    /// </summary>
    public Text PlayerConnectedText;

    /// <summary>
    /// The input field for the message to send from the Host to the Player.
    /// </summary>
    public InputField HostMessageInput;

    /// <summary>
    /// The input field for the message to send from the Player to the Host.
    /// </summary>
    public InputField PlayerMessageInput;

    /// <summary>
    /// Reference to the MainMenuPanel game object.
    /// </summary>
    public GameObject MainMenuPanel;

    /// <summary>
    /// Reference to the HostPanel game object.
    /// </summary>
    public GameObject HostPanel;

    /// <summary>
    /// Reference to the PlayerPanel game object.
    /// </summary>
    public GameObject PlayerPanel;

    /// <summary>
    /// The textbox displaying the latest message received by the Host.
    /// </summary>
    public Text HostMessageReceivedText;

    /// <summary>
    /// The textbox displaying the number of the Host's connected players.
    /// </summary>
    public Text HostConnectedPlayersText;

    /// <summary>
    /// The textbox displaying the latest message received by the Player.
    /// </summary>
    public Text PlayerMessageReceivedText;

    // GUI vars
    string _joinCode = "n/a";
    string _playerId = "Not signed in";
    string _autoSelectRegionName = "auto-select (QoS)";
    int _regionAutoSelectIndex = 0;
    List<Region> _regions = new List<Region>();
    List<string> _regionOptions = new List<string>();
    string _hostLatestMessageReceived;
    string _playerLatestMessageReceived;

    // Allocation response objects
    Allocation _hostAllocation;
    JoinAllocation _playerAllocation;

    // Control vars
    bool _isHost;
    bool _isPlayer;

    // UTP vars
    NetworkDriver _hostDriver;
    NetworkDriver _playerDriver;
    NativeList<NetworkConnection> _serverConnections;
    NetworkConnection _clientConnection;

    async void Start()
    {
        // Set GUI to Main Menu
        MainMenuPanel.SetActive(true);
        HostPanel.SetActive(false);
        PlayerPanel.SetActive(false);

        // Allow Enter key to be used for input fields
        AddEnterKeyListenerToInputField(HostMessageInput, OnHostSendMessage);
        AddEnterKeyListenerToInputField(PlayerMessageInput, OnPlayerSendMessage);
        AddEnterKeyListenerToInputField(JoinCodeInput, OnJoin);

        // Initialize Unity Services
        await UnityServices.InitializeAsync();
    }

    void AddEnterKeyListenerToInputField(InputField inputField, Action clickButton)
    {
        inputField.onEndEdit.AddListener(_ =>
        {
            // Submit == Enter key
            if (Input.GetButton("Submit"))
            {
                clickButton();
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);
            }
        });
    }

    void Update()
    {
        if (_isHost)
        {
            UpdateHost();
            UpdateHostUI();
        }
        else if (_isPlayer)
        {
            UpdatePlayer();
            UpdatePlayerUI();
        }
    }

    void OnDestroy()
    {
        // Cleanup objects upon exit
        if (_isHost)
        {
            _hostDriver.Dispose();
            _serverConnections.Dispose();
        }
        else if (_isPlayer)
        {
            _playerDriver.Dispose();
        }
    }

    void UpdateHostUI()
    {
        HostPlayerIdText.text = _playerId;
        RegionsDropdown.interactable = _regions.Count > 0;
        RegionsDropdown.options?.Clear();
        RegionsDropdown.AddOptions(new List<string> {_autoSelectRegionName});  // index 0 is always auto-select (use QoS)
        RegionsDropdown.AddOptions(_regionOptions);
        if (!String.IsNullOrEmpty(_hostAllocation?.Region))
        {
            if (_regionOptions.Count == 0)
            {
                RegionsDropdown.AddOptions(new List<String>(new[] { _hostAllocation.Region }));
            }
            RegionsDropdown.value = RegionsDropdown.options.FindIndex(option => option.text == _hostAllocation.Region);
        }
        HostAllocationIdText.text = _hostAllocation?.AllocationId.ToString();
        JoinCodeText.text = _joinCode;
        HostBoundText.text = _hostDriver.IsCreated ? _hostDriver.Bound.ToString() : false.ToString();
        HostConnectedPlayersText.text = _serverConnections.IsCreated ? _serverConnections.Length.ToString() : 0.ToString();
        HostMessageReceivedText.text = _hostLatestMessageReceived;
    }

    void UpdatePlayerUI()
    {
        PlayerPlayerIdText.text = _playerId;
        PlayerAllocationIdText.text = _playerAllocation?.AllocationId.ToString();
        PlayerRegionText.text = _playerAllocation?.Region;
        PlayerBoundText.text = _playerDriver.IsCreated ? _playerDriver.Bound.ToString() : false.ToString();
        PlayerConnectedText.text = _clientConnection.IsCreated.ToString();
        PlayerMessageReceivedText.text = _playerLatestMessageReceived;
    }

    /// <summary>
    /// Event handler for when the Start game as Host client button is clicked.
    /// </summary>
    public void OnStartClientAsHost()
    {
        MainMenuPanel.SetActive(false);
        HostPanel.SetActive(true);
        _isHost = true;
    }

    /// <summary>
    /// Event handler for when the Start game as Player client button is clicked.
    /// </summary>
    public void OnStartClientAsPlayer()
    {
        MainMenuPanel.SetActive(false);
        PlayerPanel.SetActive(true);
        _isPlayer = true;
    }

    /// <summary>
    /// Event handler for when the Sign In button is clicked.
    /// </summary>
    public async void OnSignIn()
    {
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        _playerId = AuthenticationService.Instance.PlayerId;

        Debug.Log($"Signed in. Player ID: {_playerId}");
    }

    /// <summary>
    /// Event handler for when the Get Regions button is clicked.
    /// </summary>
    public async void OnRegion()
    {
        Debug.Log("Host - Getting regions.");
        var allRegions = await RelayService.Instance.ListRegionsAsync();
        _regions.Clear();
        _regionOptions.Clear();
        foreach (var region in allRegions)
        {
            Debug.Log(region.Id + ": " + region.Description);
            _regionOptions.Add(region.Id);
            _regions.Add(region);
        }
    }

    string GetRegionOrQosDefault()
    {
        // Return null (indicating to auto-select the region/QoS) if regions list is empty OR auto-select/QoS is chosen
        if (!_regions.Any() || RegionsDropdown.value == _regionAutoSelectIndex)
        {
            return null;
        }
        // else use chosen region (offset -1 in dropdown due to first option being auto-select/QoS)
        return _regions[RegionsDropdown.value - 1].Id;
    }

    /// <summary>
    /// Event handler for when the Allocate button is clicked.
    /// </summary>
    public async void OnAllocate()
    {
        Debug.Log("Host - Creating an allocation. Upon success, I have 10 seconds to BIND to the Relay server that I've allocated.");

        // Determine region to use (user-selected or auto-select/QoS)
        string region = GetRegionOrQosDefault();
        Debug.Log($"The chosen region is: {region ?? _autoSelectRegionName}");

        // Set max connections. Can be up to 100, but note the more players connected, the higher the bandwidth/latency impact.
        int maxConnections = 4;

        // Important: Once the allocation is created, you have ten seconds to BIND, else the allocation times out.
        _hostAllocation = await RelayService.Instance.CreateAllocationAsync(maxConnections, region);
        Debug.Log($"Host Allocation ID: {_hostAllocation.AllocationId}, region: {_hostAllocation.Region}");

        // Initialize NetworkConnection list for the server (Host).
        // This list object manages the NetworkConnections which represent connected players.
        _serverConnections = new NativeList<NetworkConnection>(maxConnections, Allocator.Persistent);
    }

    /// <summary>
    /// Event handler for when the Bind Host to Relay (UTP) button is clicked.
    /// </summary>
    public void OnBindHost()
    {
        Debug.Log("Host - Binding to the Relay server using UTP.");

        // Extract the Relay server data from the Allocation response.
        var relayServerData = new RelayServerData(_hostAllocation, "udp");

        // Create NetworkSettings using the Relay server data.
        var settings = new NetworkSettings();
        settings.WithRelayParameters(ref relayServerData);

        // Create the Host's NetworkDriver from the NetworkSettings.
        _hostDriver = NetworkDriver.Create(settings);

        // Bind to the Relay server.
        if (_hostDriver.Bind(NetworkEndPoint.AnyIpv4) != 0)
        {
            Debug.LogError("Host client failed to bind");
        }
        else
        {
            if (_hostDriver.Listen() != 0)
            {
                Debug.LogError("Host client failed to listen");
            }
            else
            {
                Debug.Log("Host client bound to Relay server");
            }
        }
    }

    /// <summary>
    /// Event handler for when the Get Join Code button is clicked.
    /// </summary>
    public async void OnJoinCode()
    {
        Debug.Log("Host - Getting a join code for my allocation. I would share that join code with the other players so they can join my session.");

        try
        {
            _joinCode = await RelayService.Instance.GetJoinCodeAsync(_hostAllocation.AllocationId);
            Debug.Log("Host - Got join code: " + _joinCode);
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError(ex.Message + "\n" + ex.StackTrace);
        }
    }

    /// <summary>
    /// Event handler for when the Join button is clicked.
    /// </summary>
    public async void OnJoin()
    {
        // Input join code in the respective input field first.
        if (String.IsNullOrEmpty(JoinCodeInput.text))
        {
            Debug.LogError("Please input a join code.");
            return;
        }

        Debug.Log("Player - Joining host allocation using join code. Upon success, I have 10 seconds to BIND to the Relay server that I've allocated.");

        try
        {
            _playerAllocation = await RelayService.Instance.JoinAllocationAsync(JoinCodeInput.text);
            Debug.Log("Player Allocation ID: " + _playerAllocation.AllocationId);
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError(ex.Message + "\n" + ex.StackTrace);
        }
    }

    /// <summary>
    /// Event handler for when the Bind Player to Relay (UTP) button is clicked.
    /// </summary>
    public void OnBindPlayer()
    {
        Debug.Log("Player - Binding to the Relay server using UTP.");

        // Extract the Relay server data from the Join Allocation response.
        var relayServerData = new RelayServerData(_playerAllocation, "udp");

        // Create NetworkSettings using the Relay server data.
        var settings = new NetworkSettings();
        settings.WithRelayParameters(ref relayServerData);

        // Create the Player's NetworkDriver from the NetworkSettings object.
        _playerDriver = NetworkDriver.Create(settings);

        // Bind to the Relay server.
        if (_playerDriver.Bind(NetworkEndPoint.AnyIpv4) != 0)
        {
            Debug.LogError("Player client failed to bind");
        }
        else
        {
            Debug.Log("Player client bound to Relay server");
        }
    }

    /// <summary>
    /// Event handler for when the Connect Player to Relay (UTP) button is clicked.
    /// </summary>
    public void OnConnectPlayer()
    {
        Debug.Log("Player - Connecting to Host's client.");

        // Sends a connection request to the Host Player.
        _clientConnection = _playerDriver.Connect();
    }

    /// <summary>
    /// Event handler for when the Send message from Host to Relay (UTP) button is clicked.
    /// </summary>
    public void OnHostSendMessage()
    {
        if (_serverConnections.Length == 0)
        {
            Debug.LogError("No players connected to send messages to.");
            return;
        }

        // Get message from the input field, or default to the placeholder text.
        var msg = !String.IsNullOrEmpty(HostMessageInput.text) ? HostMessageInput.text : HostMessageInput.placeholder.GetComponent<Text>().text;

        // In this sample, we will simply broadcast a message to all connected clients.
        for (int i = 0; i < _serverConnections.Length; i++)
        {
            if (_hostDriver.BeginSend(_serverConnections[i], out var writer) == 0)
            {
                // Send the message. Aside from FixedString32, many different types can be used.
                writer.WriteFixedString32(msg);
                _hostDriver.EndSend(writer);
            }
        }
    }

    /// <summary>
    /// Event handler for when the Send message from Player to Host (UTP) button is clicked.
    /// </summary>
    public void OnPlayerSendMessage()
    {
        if (!_clientConnection.IsCreated)
        {
            Debug.LogError("Player is not connected. No Host client to send message to.");
            return;
        }

        // Get message from the input field, or default to the placeholder text.
        var msg = !String.IsNullOrEmpty(PlayerMessageInput.text) ? PlayerMessageInput.text : PlayerMessageInput.placeholder.GetComponent<Text>().text;
        if (_playerDriver.BeginSend(_clientConnection, out var writer) == 0)
        {
            // Send the message. Aside from FixedString32, many different types can be used.
            writer.WriteFixedString32(msg);
            _playerDriver.EndSend(writer);
        }
    }

    /// <summary>
    /// Event handler for when the DisconnectPlayers (UTP) button is clicked.
    /// </summary>
    public void OnDisconnectPlayers()
    {
        if (_serverConnections.Length == 0)
        {
            Debug.LogError("No players connected to disconnect.");
            return;
        }

        // In this sample, we will simply disconnect all connected clients.
        for (int i = 0; i < _serverConnections.Length; i++)
        {
            // This sends a disconnect event to the destination client,
            // letting them know they are disconnected from the Host.
            _hostDriver.Disconnect(_serverConnections[i]);

            // Here, we set the destination client's NetworkConnection to the default value.
            // It will be recognized in the Host's Update loop as a stale connection, and be removed.
            _serverConnections[i] = default(NetworkConnection);
        }
    }

    /// <summary>
    /// Event handler for when the Disconnect (UTP) button is clicked.
    /// </summary>
    public void OnDisconnect()
    {
        // This sends a disconnect event to the Host client,
        // letting them know they are disconnecting.
        _playerDriver.Disconnect(_clientConnection);

        // We remove the reference to the current connection by overriding it.
        _clientConnection = default(NetworkConnection);
    }

    void UpdateHost()
    {
        // Skip update logic if the Host is not yet bound.
        if (!_hostDriver.IsCreated || !_hostDriver.Bound)
        {
            return;
        }

        // This keeps the binding to the Relay server alive,
        // preventing it from timing out due to inactivity.
        _hostDriver.ScheduleUpdate().Complete();

        // Clean up stale connections.
        for (int i = 0; i < _serverConnections.Length; i++)
        {
            if (!_serverConnections[i].IsCreated)
            {
                Debug.Log("Stale connection removed");
                _serverConnections.RemoveAt(i);
                --i;
            }
        }

        // Accept incoming client connections.
        NetworkConnection incomingConnection;
        while ((incomingConnection = _hostDriver.Accept()) != default(NetworkConnection))
        {
            // Adds the requesting Player to the serverConnections list.
            // This also sends a Connect event back the requesting Player,
            // as a means of acknowledging acceptance.
            Debug.Log("Accepted an incoming connection.");
            _serverConnections.Add(incomingConnection);
        }

        // Process events from all connections.
        for (int i = 0; i < _serverConnections.Length; i++)
        {
            Assert.IsTrue(_serverConnections[i].IsCreated);

            // Resolve event queue.
            NetworkEvent.Type eventType;
            while ((eventType = _hostDriver.PopEventForConnection(_serverConnections[i], out var stream)) != NetworkEvent.Type.Empty)
            {
                switch (eventType)
                {
                    // Handle Relay events.
                    case NetworkEvent.Type.Data:
                        FixedString32Bytes msg = stream.ReadFixedString32();
                        Debug.Log($"Server received msg: {msg}");
                        _hostLatestMessageReceived = msg.ToString();
                        break;

                    // Handle Disconnect events.
                    case NetworkEvent.Type.Disconnect:
                        Debug.Log("Server received disconnect from client");
                        _serverConnections[i] = default(NetworkConnection);
                        break;
                }
            }
        }
    }

    void UpdatePlayer()
    {
        // Skip update logic if the Player is not yet bound.
        if (!_playerDriver.IsCreated || !_playerDriver.Bound)
        {
            return;
        }

        // This keeps the binding to the Relay server alive,
        // preventing it from timing out due to inactivity.
        _playerDriver.ScheduleUpdate().Complete();

        // Resolve event queue.
        NetworkEvent.Type eventType;
        while ((eventType = _clientConnection.PopEvent(_playerDriver, out var stream)) != NetworkEvent.Type.Empty)
        {
            switch (eventType)
            {
                // Handle Relay events.
                case NetworkEvent.Type.Data:
                    FixedString32Bytes msg = stream.ReadFixedString32();
                    Debug.Log($"Player received msg: {msg}");
                    _playerLatestMessageReceived = msg.ToString();
                    break;

                // Handle Connect events.
                case NetworkEvent.Type.Connect:
                    Debug.Log("Player connected to the Host");
                    break;

                // Handle Disconnect events.
                case NetworkEvent.Type.Disconnect:
                    Debug.Log("Player got disconnected from the Host");
                    _clientConnection = default(NetworkConnection);
                    break;
            }
        }
    }
}
