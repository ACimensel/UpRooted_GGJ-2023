using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class NetworkCommandLine : MonoBehaviour
{
    [SerializeField] private NetworkManager NetManager;
   
    // Start is called before the first frame update
    void Start()
    {
        if (Application.isEditor) return;

        var args = GetCommandLineArgs();
        
        if (args.TryGetValue("-mode", out string mode))
        {
            switch (mode)
            {
                case "server":
                    NetManager.StartServer();
                    break;
                case "host":
                    NetManager.StartHost();
                    break;
                case "client":
                    NetManager.StartClient();
                    break;
            }
        }
    }

    private Dictionary<string, string> GetCommandLineArgs()
    {
        Dictionary<string, string> argDictionary = new Dictionary<string, string>();

        var args = System.Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; ++i)
        {
            var arg = args[i].ToLower();
            if (arg.StartsWith("-"))
            {
                var value = i < args.Length - 1 ? args[i + 1].ToLower() : null;
                value = (value?.StartsWith("-") ?? false) ? null : value;
                
                argDictionary.Add(arg,value);
            }
        }

        return argDictionary;
    }
}
