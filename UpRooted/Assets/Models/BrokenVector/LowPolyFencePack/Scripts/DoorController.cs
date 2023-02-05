using UnityEngine;

namespace BrokenVector.LowPolyFencePack
{
    /// <summary>
    /// This class manages the door animations.
    /// It needs the legacy animation component.
    /// </summary>
    [RequireComponent(typeof(Animation))]
    public class DoorController : MonoBehaviour
    {

        /// <summary>
        /// door state: Open or Closed
        /// </summary>
        public enum DoorState
        {
            Open,
            Closed
        }

        /// <summary></summary>
        /// <returns>
        /// returns and sets the current door state
        /// </returns>
        public DoorState CurrentState {
            get
            {
                return _currentState;
            }
            set
            {
                _currentState = value;
                Animate();
            }
        }
        /// <returns>
        /// returns wether the door is currently open or closed
        /// </returns>
        public bool IsDoorOpen { get { return CurrentState == DoorState.Open; } }
        /// <returns>
        /// returns wether the door is currently open or closed
        /// </returns>
        public bool IsDoorClosed { get { return CurrentState == DoorState.Closed; } }

        public DoorState InitialState = DoorState.Closed;
        public float AnimationSpeed = 1;

        [SerializeField]
        private AnimationClip OpenAnimation;
        [SerializeField]
        private AnimationClip CloseAnimation;

        private Animation _animator;
        private DoorState _currentState;

        void Awake()
        {
            _animator = GetComponent<Animation>();
            if (_animator == null)
            {
                Debug.LogError("Every DoorController needs an Animator.");
                return;
            }
            
            // animator settings
            _animator.playAutomatically = false;

            // prepare animation clips
            OpenAnimation.legacy = true;
            CloseAnimation.legacy = true;
            _animator.AddClip(OpenAnimation, DoorState.Open.ToString());
            _animator.AddClip(CloseAnimation, DoorState.Closed.ToString());
        }

        void Start()
        {            
            // a little hack, to set the initial state
            _currentState = InitialState;
            var clip = GetCurrentAnimation();
            _animator[clip].speed = 9999;
            _animator.Play(clip);
        }

        /// <summary>
        /// Closes the door.
        /// </summary>
        public void CloseDoor()
        {
            if (IsDoorClosed)
                return;

            CurrentState = DoorState.Closed;
        }

        /// <summary>
        /// Opens the door.
        /// </summary>
        public void OpenDoor()
        {
            if (IsDoorOpen)
                return;

            CurrentState = DoorState.Open;
        }

        /// <summary>
        /// Changes the current door state.
        /// </summary>
        public void ToggleDoor()
        {
            if (IsDoorOpen)
                CloseDoor();
            else
                OpenDoor();
        }

        private void Animate()
        {
            var clip = GetCurrentAnimation();
            _animator[clip].speed = AnimationSpeed;
            _animator.Play(clip);
        }

        private string GetCurrentAnimation()
        {
            return CurrentState.ToString();
        }
    }
}