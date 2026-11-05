using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CoffinEscape
{
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
    public sealed class ReturnToAnchor : MonoBehaviour
    {
        [SerializeField] private Transform returnAnchor;
        [SerializeField] private BoxCollider movementBounds;

        private Rigidbody body;
        private XRGrabInteractable grabInteractable;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool cachedInitialPose;

        private void Awake() => CacheComponentsAndInitialPose();

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

        public void Configure(Transform anchor, BoxCollider bounds)
        {
            returnAnchor = anchor;
            movementBounds = bounds;
            CacheComponentsAndInitialPose();
        }

        public void SetReturnAnchor(Transform anchor) => returnAnchor = anchor;
        public void SetMovementBounds(BoxCollider bounds) => movementBounds = bounds;

        public void ReturnHome()
        {
            CacheComponentsAndInitialPose();
            Vector3 position = returnAnchor != null ? returnAnchor.position : initialPosition;
            Quaternion rotation = returnAnchor != null ? returnAnchor.rotation : initialRotation;

            transform.SetPositionAndRotation(position, rotation);
            if (body != null)
            {
                body.position = position;
                body.rotation = rotation;
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
            }
        }

        private void CacheComponentsAndInitialPose()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (grabInteractable == null)
                grabInteractable = GetComponent<XRGrabInteractable>();
            if (!cachedInitialPose)
            {
                initialPosition = transform.position;
                initialRotation = transform.rotation;
                cachedInitialPose = true;
            }
        }

        private static float ClampInside(float value, float minimum, float maximum, float center)
        {
            return minimum <= maximum ? Mathf.Clamp(value, minimum, maximum) : center;
        }
    }
}
