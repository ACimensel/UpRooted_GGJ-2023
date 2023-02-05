using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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
    [SerializeField] private TMP_Dropdown RegionsDropdown;
    
    /// <summary>
    /// The input field for the Join Code that the Player inputs to join the Host's Relay server.
    /// </summary>
    [SerializeField] private TMP_InputField JoinCodeInput;

    [SerializeField] private TMP_Text JoinCodeOutput;

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
                OnAllocate(); // then into game
                break;

            case ConnectionState.JoinSignin:
                DoSignIn(ConnectionState.JoinCodeEntry);
                break;
            case ConnectionState.JoinCodeEntry:
                UpdateActiveMenu(newState); // to JoinCodeEntry
                break;
            case ConnectionState.JoinWithCode:
                OnJoin(); // then into game
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
        if (_playerId == "Not signed in")
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            _playerId = AuthenticationService.Instance.PlayerId;

            Debug.Log($"Signed in. Player ID: {_playerId}");
        }

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
        int maxConnections = 2;

        await AllocateRelayServerAndGetJoinCode(maxConnections, region);

        StartCoroutine(ConfigureTransportAndStartNgoAsHost(maxConnections));
    }
    
    private async Task<RelayServerData> AllocateRelayServerAndGetJoinCode(int maxConnections, string region = null)
    {
        Allocation allocation;
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections, region);
        }
        catch (Exception e)
        {
            Debug.LogError($"Relay create allocation request failed {e.Message}");
            throw;
        }

        Debug.Log($"server: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"server: {allocation.AllocationId}");

        try
        {
            _joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        }
        catch
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        return new RelayServerData(allocation, "dtls");
    }
    
    IEnumerator ConfigureTransportAndStartNgoAsHost(int maxConnections)
    {
        Task<RelayServerData> serverRelayUtilityTask = AllocateRelayServerAndGetJoinCode(maxConnections);
        while (!serverRelayUtilityTask.IsCompleted)
        {
            yield return null;
        }
        if (serverRelayUtilityTask.IsFaulted)
        {
            Debug.LogError("Exception thrown when attempting to start Relay Server. Server not started. Exception: " + serverRelayUtilityTask.Exception.Message);
            yield break;
        }

        RelayServerData relayServerData = serverRelayUtilityTask.Result;

        // TODO Display the joinCode to the user.
        Debug.Log("***** " + _joinCode + " *****");
        JoinCodeOutput.text = _joinCode;

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        NetworkManager.Singleton.StartHost();
        HandleMenuState(ConnectionState.InGame);
        yield return null;
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

        await JoinRelayServerFromJoinCode(JoinCodeInput.text);
        StartCoroutine(ConfigureTransportAndStartNgoAsConnectingPlayer(JoinCodeInput.text));
    }
    
    private static async Task<RelayServerData> JoinRelayServerFromJoinCode(string joinCode)
    {
        JoinAllocation allocation;
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        Debug.Log($"client: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"host: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");
        Debug.Log($"client: {allocation.AllocationId}");

        return new RelayServerData(allocation, "dtls");
    }
    
    IEnumerator ConfigureTransportAndStartNgoAsConnectingPlayer(string userEnteredJoinCode)
    {
        // Populate RelayJoinCode beforehand through the UI
        var clientRelayUtilityTask = JoinRelayServerFromJoinCode(userEnteredJoinCode);

        while (!clientRelayUtilityTask.IsCompleted)
        {
            yield return null;
        }

        if (clientRelayUtilityTask.IsFaulted)
        {
            Debug.LogError("Exception thrown when attempting to connect to Relay Server. Exception: " + clientRelayUtilityTask.Exception.Message);
            yield break;
        }

        var relayServerData = clientRelayUtilityTask.Result;

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartClient();
        yield return null;
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
        HostLocal();
    }

    private async void HostLocal()
    {
        Task<RelayServerData> serverRelayUtilityTask = AllocateRelayServerAndGetJoinCode(2);
        RelayServerData relayServerData = serverRelayUtilityTask.Result;
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
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