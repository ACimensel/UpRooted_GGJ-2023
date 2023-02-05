using UnityEngine;

public class PlayerControllerV : MonoBehaviour
{
    public int Speed = 10;

    private Camera _playerCamera;
    private Animator _anim;
    private Rigidbody _rb;
    private Vector3 _velocity = Vector3.zero;
    private Vector3 _unitTargetVelocity;
    private float _strafeUpper = 0.4f;

    private void Awake()
    {
        _playerCamera = Camera.main;
        _rb = GetComponent<Rigidbody>();
        _anim = GetComponent<Animator>();
    }

    void Update()
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
        bool isMovingUp = (_unitTargetVelocity.z > 0f);

        if(_rb.velocity.magnitude > 0.01f){
            if(dot > _strafeUpper){
                _anim.SetTrigger("Forward");
            }
            else if(dot < -_strafeUpper){
                _anim.SetTrigger("Backward");
            }
            else{
                if(isFacingRight && _unitTargetVelocity.z > 0.1f) _anim.SetTrigger("Left");
                else if(isFacingRight && _unitTargetVelocity.z < -0.1f) _anim.SetTrigger("Right");
                else if(!isFacingRight && _unitTargetVelocity.z > 0.1f) _anim.SetTrigger("Right");
                else if(!isFacingRight && _unitTargetVelocity.z < -0.1f) _anim.SetTrigger("Left");
            }
        }
        else{
            _anim.SetTrigger("Idle");
        }
    }
}