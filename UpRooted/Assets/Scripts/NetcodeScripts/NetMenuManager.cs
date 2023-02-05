using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

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
        BindToHost,
        ConnectToHost,

        InGame = 999
    }

    [SerializeField] private List<NetMenu> Menus = new();

    private NetMenu _activeMenu;

    public void ButtonChangeToMenuState(int newState)
    {
        HandleMenuState((ConnectionState)newState);
    }

    private void HandleMenuState(ConnectionState newState)
    {
        var updateMenu = false;
        switch (newState)
        {
            case ConnectionState.MenuEntry:
                updateMenu = true; // to LocalOrNetworkMenu
                break;
            case ConnectionState.LocalPlay:
                updateMenu = true;
                // Then buttons set state as InGame
                break;

            case ConnectionState.NetworkPlay:
                HandleMenuState(ConnectionState.HostOrJoin);
                break;
            case ConnectionState.HostOrJoin:
                updateMenu = true; // to NetworkMenu
                break;

            case ConnectionState.HostSignin:
                HandleMenuState(ConnectionState.Region);
                break;
            case ConnectionState.Region:
                updateMenu = true; //  To Region Menu
                break;
            case ConnectionState.Allocate:
                HandleMenuState(ConnectionState.BindAsHost);
                break;
            case ConnectionState.BindAsHost:
                HandleMenuState(ConnectionState.GetJoinCode);
                break;
            case ConnectionState.GetJoinCode:
                HandleMenuState(ConnectionState.InGame);
                break;

            case ConnectionState.JoinSignin:
                HandleMenuState(ConnectionState.JoinCodeEntry);
                break;
            case ConnectionState.JoinCodeEntry:
                updateMenu = true; // to JoinCodeEntry
                break;
            case ConnectionState.BindToHost:
                HandleMenuState(ConnectionState.ConnectToHost);
                break;
            case ConnectionState.ConnectToHost:
                HandleMenuState(ConnectionState.InGame);
                break;
            case ConnectionState.InGame:
                HideAll();
                break;
        }

        if (updateMenu)
        {
            _activeMenu.gameObject.SetActive(false);
            _activeMenu = Menus.First((menu) => menu.ConnectionState == newState);
            _activeMenu.gameObject.SetActive(true);
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            HideAll();
    }

    private void HideAll()
    {
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