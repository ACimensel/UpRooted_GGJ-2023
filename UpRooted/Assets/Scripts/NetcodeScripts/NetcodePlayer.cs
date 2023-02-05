using Unity.Netcode;
using UnityEngine;

public class NetcodePlayer : NetworkBehaviour
{
    // Network managed Positions.
    // Host's can modify this directly.
    // Client's must ask permission and receive the updated value before applying it.
    //[NonSerialized] public readonly NetworkVariable<Vector3> Position = new();
    //[NonSerialized] public readonly NetworkVariable<Quaternion> Rotation = new();

    [SerializeField] private float XBounds = 30;
    [SerializeField] private float YSpawnHeight = .5f;
    
    private readonly int _speed = 10;
    private Camera _playerCamera;

    private bool _hasSpawned = false;

    public override void OnNetworkSpawn()
    {
        SetInitialPosition();
        _playerCamera = Camera.current;
        _hasSpawned = true;
    }

    private void SetInitialPosition()
    {
        if (!IsOwner) return;
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer )
        {
            var centeredPosition = CenterOfGarden(-1.0f);
            transform.position = centeredPosition;
        }
        else
        {
            var centeredPosition = CenterOfGarden(1.0f);
            transform.position = centeredPosition;
        }
    }
    
    // Returns a point centered on that side of the the play area
    // Rect is centered at 0,0
    private Vector3 CenterOfGarden(float multi)
    {
        return new Vector3(
            multi * XBounds/4,
            YSpawnHeight, // fixed height
            0.0f
        );
    }
    
    // Called every frame by Unity
    private void Update()
    {
        if (!IsOwner || !_hasSpawned) return;
        
        HostMove();
    }

    void HostMove()
    {
        Move();
        RotatePlayer();
    }

    void Move()
    {
        Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        transform.position += movement * _speed * Time.deltaTime;
    }

    void RotatePlayer()
    {
        if (_playerCamera == null) _playerCamera = Camera.current;
        Ray mouseRay = _playerCamera.ScreenPointToRay(Input.mousePosition);
        Plane p = new Plane(Vector3.up, transform.position);
        if (p.Raycast(mouseRay, out float hitDist))
        {
            Vector3 hitPoint = mouseRay.GetPoint(hitDist);
            transform.LookAt(hitPoint);
        }
    }
}