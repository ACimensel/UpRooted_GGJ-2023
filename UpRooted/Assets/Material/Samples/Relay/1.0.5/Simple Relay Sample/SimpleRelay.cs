using System;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A simple sample showing how to use the Relay Allocation package. As the host, you can authenticate, request a relay allocation, get a join code and join the allocation.
/// </summary>
/// <remarks>
/// The sample is limited to calling the Relay Allocation Service and does not cover connecting the host game client to the relay using Unity Transport Protocol.
/// This will cause the allocation to be reclaimed about 10 seconds after creating it.
/// </remarks>
public class SimpleRelay : MonoBehaviour
{
    /// <summary>
    /// The textbox displaying the Player Id.
    /// </summary>
    public Text PlayerIdText;

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
    /// The textbox displaying the Allocation Id of the joined allocation.
    /// </summary>
    public Text PlayerAllocationIdText;

    Guid _hostAllocationId;
    Guid _playerAllocationId;
    string _allocationRegion = "";
    string _joinCode = "n/a";
    string _playerId = "Not signed in";
    string _autoSelectRegionName = "auto-select (QoS)";
    int _regionAutoSelectIndex = 0;
    List<Region> _regions = new List<Region>();
    List<string> _regionOptions = new List<string>();


    async void Start()
    {
        await UnityServices.InitializeAsync();

        UpdateUI();
    }

    void UpdateUI()
    {
        PlayerIdText.text = _playerId;
        RegionsDropdown.interactable = _regions.Count > 0;
        RegionsDropdown.options?.Clear();
        RegionsDropdown.AddOptions(new List<string> {_autoSelectRegionName});  // index 0 is always auto-select (use QoS)
        RegionsDropdown.AddOptions(_regionOptions);
        if (!String.IsNullOrEmpty(_allocationRegion))
        {
            if (_regionOptions.Count == 0)
            {
                RegionsDropdown.AddOptions(new List<String>(new[] { _allocationRegion }));
            }
            RegionsDropdown.value = RegionsDropdown.options.FindIndex(option => option.text == _allocationRegion);
        }
        HostAllocationIdText.text = _hostAllocationId.ToString();
        JoinCodeText.text = _joinCode;
        PlayerAllocationIdText.text = _playerAllocationId.ToString();
    }

    /// <summary>
    /// Event handler for when the Sign In button is clicked.
    /// </summary>
    public async void OnSignIn()
    {
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        _playerId = AuthenticationService.Instance.PlayerId;

        Debug.Log($"Signed in. Player ID: {_playerId}");
        UpdateUI();
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
        UpdateUI();
    }

    /// <summary>
    /// Event handler for when the Allocate button is clicked.
    /// </summary>
    public async void OnAllocate()
    {
        Debug.Log("Host - Creating an allocation.");

        // Determine region to use (user-selected or auto-select/QoS)
        string region = GetRegionOrQosDefault();
        Debug.Log($"The chosen region is: {region ?? _autoSelectRegionName}");

        // Important: Once the allocation is created, you have ten seconds to BIND
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4, region);
        _hostAllocationId = allocation.AllocationId;
        _allocationRegion = allocation.Region;

        Debug.Log($"Host Allocation ID: {_hostAllocationId}, region: {_allocationRegion}");

        UpdateUI();
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
    /// Event handler for when the Get Join Code button is clicked.
    /// </summary>
    public async void OnJoinCode()
    {
        Debug.Log("Host - Getting a join code for my allocation. I would share that join code with the other players so they can join my session.");

        try
        {
            _joinCode = await RelayService.Instance.GetJoinCodeAsync(_hostAllocationId);
            Debug.Log("Host - Got join code: " + _joinCode);
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError(ex.Message + "\n" + ex.StackTrace);
        }

        UpdateUI();
    }

    /// <summary>
    /// Event handler for when the Join button is clicked.
    /// </summary>
    public async void OnJoin()
    {
        Debug.Log("Player - Joining host allocation using join code.");

        try
        {
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(_joinCode);
            _playerAllocationId = joinAllocation.AllocationId;
            Debug.Log("Player Allocation ID: " + _playerAllocationId);
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError(ex.Message + "\n" + ex.StackTrace);
        }

        UpdateUI();
    }
}
