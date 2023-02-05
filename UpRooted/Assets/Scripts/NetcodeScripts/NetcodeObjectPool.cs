using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class NetcodeObjectPool : NetworkBehaviour
{
    private static NetcodeObjectPool _instance;

    public static NetcodeObjectPool Singleton => _instance;

    [SerializeField] private List<PoolConfigObject> PooledPrefabsList;

    /// <summary>
    /// Not sure if the pooled objects will be multiple different prefabs or just one multi-use type
    /// Assuming we might use polymorphism and multiple prefabs
    /// </summary>
    private HashSet<GameObject> _prefabs = new HashSet<GameObject>();

    private Dictionary<GameObject, Queue<NetworkObject>> _pooledObjects = new();

    private bool _hasInitialized;

    public void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public override void OnNetworkSpawn()
    {
        InitializePool();
    }

    public override void OnNetworkDespawn()
    {
        ClearPool();
    }

    public void OnValidate()
    {
        for (var i = 0; i < PooledPrefabsList.Count; i++)
        {
            var prefab = PooledPrefabsList[i].Prefab;
            if (prefab != null)
            {
                Assert.IsNotNull(prefab.GetComponent<NetworkObject>(), $"{nameof(NetcodeObjectPool)}: Pooled prefab \"{prefab.name}\" at index {i.ToString()} has no {nameof(NetworkObject)} component.");
            }
        }
    }

    /// <summary>
    /// Returns an instance of the given prefab from the object pool
    /// Resets the pos and rot in case the caller wants to set that itself
    /// </summary>
    /// <param name="prefab"></param>
    /// <returns></returns>
    public NetworkObject GetNetworkObject(GameObject prefab)
    {
        // calling a private method with extra params
        return GetNetworkObjectInternal(prefab, Vector3.zero, Quaternion.identity);
    }
    
    /// <summary>
    /// Override when we know the new pos and rot
    /// TODO Call from SpawnItem / SpawnPickable / SpawnThrowable or whatever it will be
    /// </summary>
    public NetworkObject GetNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        // calling a private method with extra params
        return GetNetworkObjectInternal(prefab, position, rotation);
    }

    /// <summary>
    /// Resets and returns an object to it's pool
    /// </summary>
    /// <param name="networkObject"></param>
    /// <param name="prefab"></param>
    public void ReturnNetworkObject(NetworkObject networkObject, GameObject prefab)
    {
        var baseObject = networkObject.gameObject;
        baseObject.SetActive(false); // turn it off, but don't destroy it
        _pooledObjects[prefab].Enqueue(networkObject);
    }

    /// <summary>
    /// For external systems to register a prefab to the pool
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="prewarmCount"></param>
    public void AddPrefab(GameObject prefab, int prewarmCount = 0)
    {
        var networkObject = prefab.GetComponent<NetworkObject>();
        
        Assert.IsNotNull(networkObject, $"{nameof(prefab)} must have {nameof(networkObject)} component.");
        Assert.IsFalse(_prefabs.Contains(prefab), $"Prefab {prefab.name} is already registered in the pool.");

        RegisterPrefabInternal(prefab, prewarmCount);
    }

    /// <summary>
    /// Create the cache for this prefab type
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="prewarmCount"></param>
    private void RegisterPrefabInternal(GameObject prefab, int prewarmCount)
    {
        _prefabs.Add(prefab);
        var prefabQueue = new Queue<NetworkObject>();
        _pooledObjects[prefab] = prefabQueue;
        for (int i = 0; i < prewarmCount; ++i)
        {
            var newInstance = CreateInstance(prefab);
            ReturnNetworkObject(newInstance.GetComponent<NetworkObject>(), prefab);
        }

        //NetworkManager.Singleton.PrefabHandler.AddHandler(prefab, new PooledPrefabInstanceHandler(prefab, this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GameObject CreateInstance(GameObject prefab)
    {
        return Instantiate(prefab);
    }

    private NetworkObject GetNetworkObjectInternal(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var queue = _pooledObjects[prefab];

        NetworkObject networkObject;
        if (queue.Count > 0)
        {
            networkObject = queue.Dequeue();
        }
        else
        {
            networkObject = CreateInstance(prefab).GetComponent<NetworkObject>();
        }

        // Reverse the changes from ReturnNetworkObject
        var baseObject = networkObject.gameObject;
        baseObject.SetActive(true); // turn it on, we are about to use it

        baseObject.transform.position = position;
        baseObject.transform.rotation = rotation;

        return networkObject;
    }

    private void InitializePool()
    {
        if (_hasInitialized) return;
        foreach (var configObject in PooledPrefabsList)
        {
            RegisterPrefabInternal(configObject.Prefab, configObject.PrewarmCount);
        }

        _hasInitialized = true;
    }
    
    private void ClearPool()
    {
        foreach (var prefab in _prefabs)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(prefab);
        }

        _pooledObjects.Clear();
    }

}

[Serializable]
struct PoolConfigObject
{
    public GameObject Prefab;
    public int PrewarmCount;
}


class PooledPrefabInstanceHandler : INetworkPrefabInstanceHandler
{
    private GameObject _prefab;
    private NetcodeObjectPool _pool;

    public PooledPrefabInstanceHandler(GameObject prefab, NetcodeObjectPool pool)
    {
        _prefab = prefab;
        _pool = pool;
    }

    NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        var netObject = _pool.GetNetworkObject(_prefab, position, rotation);
        return netObject;
    }

    void INetworkPrefabInstanceHandler.Destroy(NetworkObject networkObject)
    {
        _pool.ReturnNetworkObject(networkObject, _prefab);
    }
}