using Unity.Netcode;
using UnityEngine;

public class NetcodePlayer : NetworkBehaviour
{
    [SerializeField] private float XBounds = 30;
    [SerializeField] private float YSpawnHeight;
    
    public int Speed = 10;

    private Camera _playerCamera;
    private Animator _anim;
    private Rigidbody _rb;
    private Vector3 _unitTargetVelocity;
    private float _strafeUpper = 0.4f;

    private bool _hasSpawned = false;
    private static readonly int Forward = Animator.StringToHash("Forward");
    private static readonly int Backward = Animator.StringToHash("Backward");
    private static readonly int Left = Animator.StringToHash("Left");
    private static readonly int Right = Animator.StringToHash("Right");
    private static readonly int Idle = Animator.StringToHash("Idle");

    public override void OnNetworkSpawn()
    {
        SetInitialPosition();
        _playerCamera = Camera.main;
        _rb = GetComponent<Rigidbody>();
        _anim = GetComponent<Animator>();
        _hasSpawned = true;
    }

    private void SetInitialPosition()
    {
        if (!IsOwner) return;
        Vector3 centeredPosition;
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer )
        {
            centeredPosition = CenterOfGarden(-1.0f);
        }
        else
        {
            centeredPosition = CenterOfGarden(1.0f);
        }
        transform.position = centeredPosition;
        transform.LookAt(Vector3.zero);
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
        PlayAnimation();
    }

    void Move()
    {
        _unitTargetVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        Vector3.Normalize(_unitTargetVelocity);
        // rb.velocity = Vector3.SmoothDamp(rb.velocity, unitTargetVelocity * speed, ref velocity, 0.03f);
        _rb.velocity = _unitTargetVelocity * Speed;
    }

    void RotatePlayer()
    {
        Ray mouseRay = _playerCamera.ScreenPointToRay(Input.mousePosition);
        Plane p = new Plane(Vector3.up, transform.position);
        if (p.Raycast(mouseRay, out float hitDist))
        {
            Vector3 hitPoint = mouseRay.GetPoint(hitDist);
            transform.LookAt(hitPoint);
        }
    }
    
    void PlayAnimation()
    {
        float dot = Vector3.Dot(transform.forward, _unitTargetVelocity);
        bool isFacingRight = (transform.forward.x > 0f);

        if(_rb.velocity.magnitude > 0.01f){
            if(dot > _strafeUpper){
                _anim.SetTrigger(Forward);
            }
            else if(dot < -_strafeUpper){
                _anim.SetTrigger(Backward);
            }
            else{
                if(isFacingRight && _unitTargetVelocity.z > 0.1f) _anim.SetTrigger(Left);
                else if(isFacingRight && _unitTargetVelocity.z < -0.1f) _anim.SetTrigger(Right);
                else if(!isFacingRight && _unitTargetVelocity.z > 0.1f) _anim.SetTrigger(Right);
                else if(!isFacingRight && _unitTargetVelocity.z < -0.1f) _anim.SetTrigger(Left);
            }
        }
        else{
            _anim.SetTrigger(Idle);
        }
    }
}