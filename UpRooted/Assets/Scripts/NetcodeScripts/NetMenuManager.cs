using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class NetMenuManager : MonoBehaviour
{
    public enum ConnectionState
    {
        MenuEntry = 0,

        LocalPlay,

        NetworkPlay,
        HostOrJoin,

        HostSignin,
        Region,
        Allocate,
        BindAsHost,
        GetJoinCode,

        JoinSignin,
        JoinCodeEntry,
        JoinWithCode,
        BindToHost,
        ConnectToHost,

        InGame = 999
    }

    [SerializeField] private List<NetMenu> Menus = new();

    [SerializeField] private NetMenu ActiveMenu;
    
    /// <summary>
    /// The dropdown displaying the region.
    /// </summary>
    [SerializeField] private Dropdown RegionsDropdown;
    
    /// <summary>
    /// The input field for the Join Code that the Player inputs to join the Host's Relay server.
    /// </summary>
    [SerializeField] private InputField JoinCodeInput;

    private bool _interactable = true;
    
    // GUI vars
    private string _joinCode = "n/a";
    private string _playerId = "Not signed in";
    private string _autoSelectRegionName = "auto-select (QoS)";
    private int _regionAutoSelectIndex = 0;
    private List<Region> _regions = new();
    private List<string> _regionOptions = new();
    private string _hostLatestMessageReceived;
    private string _playerLatestMessageReceived;

    // Allocation response objects
    private Allocation _hostAllocation;
    private JoinAllocation _playerAllocation;

    // Control vars
    private bool _isHost;
    private bool _isPlayer;

    // UTP vars
    private NetworkDriver _hostDriver;
    private NetworkDriver _playerDriver;
    private NativeList<NetworkConnection> _serverConnections;
    private NetworkConnection _clientConnection;

    // Start is called before the first frame update
    private void Start()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) 
            HideAll();
        else 
            UnityServices.InitializeAsync();
    }
    
    public void ButtonChangeToMenuState(int newState)
    {
        if (_interactable)
            HandleMenuState((ConnectionState)newState);
    }

    private void HandleMenuState(ConnectionState newState)
    {
        _interactable = false;
        switch (newState)
        {
            case ConnectionState.MenuEntry:
                UpdateActiveMenu(newState); // to LocalOrNetworkMenu
                break;
            case ConnectionState.LocalPlay:
                UpdateActiveMenu(newState);
                // Then buttons set state as InGame
                break;

            case ConnectionState.NetworkPlay:
                HandleMenuState(ConnectionState.HostOrJoin);
                break;
            case ConnectionState.HostOrJoin:
                UpdateActiveMenu(newState); // to NetworkMenu
                break;

            case ConnectionState.HostSignin:
                DoSignIn(ConnectionState.Region);
                break;
            case ConnectionState.Region:
                OnRegion(); // async, then to region menu
                break;
            case ConnectionState.Allocate:
                OnAllocate();
                break;
            case ConnectionState.BindAsHost:
                OnBindHost();
                break;
            case ConnectionState.GetJoinCode:
                OnJoinCode();
                break;

            case ConnectionState.JoinSignin:
                DoSignIn(ConnectionState.JoinCodeEntry);
                break;
            case ConnectionState.JoinCodeEntry:
                UpdateActiveMenu(newState); // to JoinCodeEntry
                break;
            case ConnectionState.JoinWithCode:
                OnJoin();
                break;
            case ConnectionState.BindToHost:
                OnBindPlayer();
                break;
            case ConnectionState.ConnectToHost:
                OnConnectPlayer();
                break;
            case ConnectionState.InGame:
                HideAll();
                break;
        }
    }

    private void UpdateActiveMenu(ConnectionState newState)
    {
        if (ActiveMenu != null) ActiveMenu.gameObject.SetActive(false);
        ActiveMenu = Menus.First((menu) => menu.ConnectionState == newState);
        ActiveMenu.gameObject.SetActive(true);
        _interactable = true;
    }

    private async void DoSignIn(ConnectionState nextState)
    {
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        _playerId = AuthenticationService.Instance.PlayerId;

        Debug.Log($"Signed in. Player ID: {_playerId}");
        
        HandleMenuState(nextState);
    }
    #region Host_UTP
    /// <summary>
    /// Event handler for when the Get Regions button is clicked.
    /// </summary>
    private async void OnRegion()
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
        UpdateActiveMenu(ConnectionState.Region); //  To Region Menu
    }
    
    /// <summary>
    /// Event handler for when the Allocate button is clicked.
    /// </summary>
    private async void OnAllocate()
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
        
        HandleMenuState(ConnectionState.BindAsHost);
    }
    
    private string GetRegionOrQosDefault()
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
    /// Event handler for when the Bind Host to Relay (UTP) button is clicked.
    /// </summary>
    private void OnBindHost()
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

        HandleMenuState(ConnectionState.GetJoinCode);
    }
    
    /// <summary>
    /// Event handler for when the Get Join Code button is clicked.
    /// </summary>
    private async void OnJoinCode()
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
        
        HandleMenuState(ConnectionState.InGame);
    }
    #endregion
    
#region Client_UTP
    
        
    /// <summary>
    /// Event handler for when the Join button is clicked.
    /// </summary>
    private async void OnJoin()
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
        
        HandleMenuState(ConnectionState.BindToHost);
    }
    
    
    /// <summary>
    /// Event handler for when the Bind Player to Relay (UTP) button is clicked.
    /// </summary>
    private void OnBindPlayer()
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

        HandleMenuState(ConnectionState.ConnectToHost);
    }
    
    /// <summary>
    /// Event handler for when the Connect Player to Relay (UTP) button is clicked.
    /// </summary>
    private void OnConnectPlayer()
    {
        Debug.Log("Player - Connecting to Host's client.");

        // Sends a connection request to the Host Player.
        _clientConnection = _playerDriver.Connect();
        
        HandleMenuState(ConnectionState.InGame);
    }

#endregion
    

    private void HideAll()
    {
        _interactable = true;
        gameObject.SetActive(false);
    }
    
    #region LocalMenu

    public void ButtonHost()
    {
        NetworkManager.Singleton.StartHost();
        HideAll();
    }

    public void ButtonJoin()
    {
        NetworkManager.Singleton.StartClient();
        HideAll();
    }

    public void ButtonServer()
    {
        NetworkManager.Singleton.StartServer();
        HideAll();
    }

    // Closes the application
    // Does nothing in editor
    public void ButtonQuit()
    {
        if (Application.isEditor) Debug.LogWarning("Hey, it's expected that this doesn't work in editor.");
        Application.Quit();
    }
    #endregion
}