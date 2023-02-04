using Unity.Netcode;
using UnityEngine;

public class NetcodeMenu : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            HideAll();
    }

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
        if (Application.isEditor)
        {
            Debug.LogWarning("Hey, it's expected that this doesn't work in editor.");
        }
        Application.Quit();
    }

    private void HideAll()
    {
        gameObject.SetActive(false);
    }
}
