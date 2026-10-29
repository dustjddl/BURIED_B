using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CoffinEscape
{
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
    public sealed class GrabbableItem : MonoBehaviour
    {
        [SerializeField] private Transform returnAnchor;
        [SerializeField] private BoxCollider movementBounds;

        private Rigidbody body;
        private XRGrabInteractable grabInteractable;
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            grabInteractable = GetComponent<XRGrabInteractable>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            body.isKinematic = true;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            grabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        private void OnDestroy()
        {
            if (grabInteractable != null)
                grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        public void SetReturnAnchor(Transform anchor) => returnAnchor = anchor;
        public void SetMovementBounds(BoxCollider bounds) => movementBounds = bounds;

        private void LateUpdate()
        {
            if (movementBounds == null || grabInteractable == null || !grabInteractable.isSelected)
                return;

            Collider itemCollider = GetComponent<Collider>();
            if (itemCollider == null)
                return;

            Bounds bounds = movementBounds.bounds;
            Vector3 extents = itemCollider.bounds.extents;
            Vector3 position = transform.position;
            position.x = ClampInside(position.x, bounds.min.x + extents.x, bounds.max.x - extents.x, bounds.center.x);
            position.y = ClampInside(position.y, bounds.min.y + extents.y, bounds.max.y - extents.y, bounds.center.y);
            position.z = ClampInside(position.z, bounds.min.z + extents.z, bounds.max.z - extents.z, bounds.center.z);
            transform.position = position;
        }

        public void ReturnHome()
        {
            Vector3 position = returnAnchor != null ? returnAnchor.position : initialPosition;
            Quaternion rotation = returnAnchor != null ? returnAnchor.rotation : initialRotation;
            body.position = position;
            body.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        private void OnReleased(SelectExitEventArgs _)
        {
            // XRI 3.3 sends the exit event after the final hand releases the object.
            if (grabInteractable.interactorsSelecting.Count == 0)
                ReturnHome();
        }

        private static float ClampInside(float value, float minimum, float maximum, float center)
        {
            return minimum <= maximum ? Mathf.Clamp(value, minimum, maximum) : center;
        }
    }
}
