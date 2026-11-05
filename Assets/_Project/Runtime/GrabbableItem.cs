using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CoffinEscape
{
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable), typeof(ReturnToAnchor))]
    public sealed class GrabbableItem : MonoBehaviour
    {
        private Rigidbody body;
        private XRGrabInteractable grabInteractable;
        private ReturnToAnchor returnToAnchor;

        public HandGrabToggle HeldBy { get; private set; }
        public XRGrabInteractable Interactable => grabInteractable != null ? grabInteractable : GetComponent<XRGrabInteractable>();
        public ReturnToAnchor ReturnComponent => returnToAnchor != null ? returnToAnchor : GetComponent<ReturnToAnchor>();

        private void Awake()
        {
            CacheComponents();
            body.isKinematic = true;
            body.useGravity = false;
            grabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;
            grabInteractable.selectMode = InteractableSelectMode.Single;
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        private void OnDestroy()
        {
            if (grabInteractable != null)
                grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        public bool CanBeGrabbed =>
            isActiveAndEnabled && Interactable != null && Interactable.isActiveAndEnabled &&
            HeldBy == null && Interactable.interactorsSelecting.Count == 0;

        public bool TrySetHolder(HandGrabToggle holder)
        {
            if (holder == null || !CanBeGrabbed)
                return false;

            HeldBy = holder;
            return true;
        }

        public void ClearHolder(HandGrabToggle holder)
        {
            if (HeldBy == holder)
                HeldBy = null;
        }

        public void ReturnHome()
        {
            ClearHolder(HeldBy);
            ReturnComponent?.ReturnHome();
        }

        private void OnReleased(SelectExitEventArgs _)
        {
            if (Interactable.interactorsSelecting.Count == 0)
                ReturnHome();
        }

        private void CacheComponents()
        {
            body = GetComponent<Rigidbody>();
            grabInteractable = GetComponent<XRGrabInteractable>();
            returnToAnchor = GetComponent<ReturnToAnchor>();
        }
    }
}
