using UnityEngine;

namespace BrokenVector.LowPolyFencePack
{
    /// <summary>
    /// This class toggles the door animation.
    /// The gameobject of this script has to have the DoorController script which needs an Animator component
    /// and some kind of Collider which detects your mouse click applied.
    /// </summary>
    [RequireComponent(typeof(DoorController))]
	public class DoorToggle : MonoBehaviour
    {

        private DoorController _doorController;

        void Awake()
        {
            _doorController = GetComponent<DoorController>();
        }

	    void OnMouseDown()
	    {
	        _doorController.ToggleDoor();
	    }

	}
}