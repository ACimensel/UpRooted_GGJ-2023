using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class NetcodePickupSpawner : NetworkBehaviour
{
    private static NetcodePickupSpawner _instance;

    public static NetcodePickupSpawner Singleton => _instance;

    public bool CanSpawn = false;

    // 30 x 10, at 00 (15x each dir, 5z each dir)
    [SerializeField] private float XBounds = 30;
    [SerializeField] private float YSpawnHeight = 0;
    [SerializeField] private float ZBounds = 10;
    [SerializeField] private int StartingItems = 5;
    [SerializeField] private float SpawnInterval = 4;

    [SerializeField] private GameObject[] PickupPrefabs;

    private float _spawnTimer = 0.0f;
    
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

    public void RoundBegin()
    {
        CanSpawn = true;
        for (int i = 0; i < StartingItems; i++)
        {
            SpawnNewItem(true);
        }
    }
    
    private void SpawnNewItem(bool startsGrown)
    {
        if (!CanSpawn) return;
        // Left side & right side spawn
        var spawnPlusPos = RandomPointInsideRect(1);
        var spawnNegPos = RandomPointInsideRect(-1);

        GameObject prefabToSpawn = PickupPrefabs[Random.Range(0, PickupPrefabs.Length)];
        
        NetworkObject plusNetworkObject = NetcodeObjectPool.Singleton.GetNetworkObject(prefabToSpawn, spawnPlusPos, Quaternion.identity);
        NetworkObject negNetworkObject = NetcodeObjectPool.Singleton.GetNetworkObject(prefabToSpawn, spawnNegPos, Quaternion.identity);
        if (startsGrown)
        {
            //plusNetworkObject.isPickable;
            //plusNetworkObject.isPickable
        }
    }
    
    // Returns a point within the play area
    // Rect is centered at 0,0
    private Vector3 RandomPointInsideRect(float multi)
    {
        return new Vector3(
            (Random.value * multi) * XBounds/2,
            YSpawnHeight, // fixed height
            (Random.value - 0.5f) * ZBounds
            );
    }

    // Called every frame, tracks the passe time until next spawn
    private void Update()
    {
        if (!CanSpawn) return;
        
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer > SpawnInterval)
        {
            _spawnTimer = 0.0f;
            SpawnNewItem(false);
        }
    }
}
