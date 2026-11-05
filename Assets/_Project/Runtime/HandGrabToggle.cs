using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace CoffinEscape
{
    [RequireComponent(typeof(XRDirectInteractor))]
    public sealed class HandGrabToggle : MonoBehaviour
    {
        [SerializeField] private XRDirectInteractor interactor;
        [SerializeField] private XRNode hand = XRNode.RightHand;
        [SerializeField] private GameConfig config;
        [SerializeField] private InputActionReference gripAction;
        [SerializeField] private InputActionReference triggerAction;
        [SerializeField] private GrabbableItem startingItem;

        private readonly GrabToggleLogic<GrabbableItem> logic = new GrabToggleLogic<GrabbableItem>();
        private bool previousGripHeld;
        private bool previousTriggerHeld;

        public event Action<HandGrabToggle> TriggerPressed;
        public GrabbableItem HeldItem => logic.HeldItem;

        public bool TriggerHeld
        {
            get
            {
                bool actionHeld = triggerAction != null && triggerAction.action != null && triggerAction.action.IsPressed();
                UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
                bool deviceHeld = device.isValid &&
                    ((device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool triggerButton) && triggerButton) ||
                     (config != null && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float triggerValue) &&
                      triggerValue >= config.controllerButtonThreshold));
                bool debugHeld = Keyboard.current != null && config != null &&
                                 Keyboard.current[config.debugTriggerKey].isPressed;
                return actionHeld || deviceHeld || debugHeld;
            }
        }

        public void Configure(XRDirectInteractor targetInteractor, XRNode targetHand, GameConfig gameConfig,
            InputActionReference targetGripAction, InputActionReference targetTriggerAction, GrabbableItem initialItem)
        {
            interactor = targetInteractor;
            hand = targetHand;
            config = gameConfig;
            gripAction = targetGripAction;
            triggerAction = targetTriggerAction;
            startingItem = initialItem;
        }

        private IEnumerator Start()
        {
            if (startingItem == null)
                yield break;

            // Let XRI register the interactors and interactables before the initial grab.
            yield return null;
            ToggleGrip(startingItem, true);
        }

        private void Update()
        {
            if (interactor == null)
                return;

            bool gripHeld = ReadGripHeld();
            bool physicalPress = gripHeld && !previousGripHeld;
            previousGripHeld = gripHeld;

            bool triggerHeld = TriggerHeld;
            bool triggerPressed = triggerHeld && !previousTriggerHeld;
            previousTriggerHeld = triggerHeld;
            if (triggerPressed)
                TriggerPressed?.Invoke(this);

            Key debugKey = hand == XRNode.LeftHand ? config.leftHandDebugGripKey : config.rightHandDebugGripKey;
            bool debugPress = Keyboard.current != null && Keyboard.current[debugKey].wasPressedThisFrame;
            if (physicalPress || debugPress)
                ToggleGrip(FindClosestAvailableItem(), false);
        }

        private void OnDisable()
        {
            if (logic.HeldItem != null)
                ReleaseItem(logic.HeldItem);
            logic.Reset();
            previousGripHeld = false;
            previousTriggerHeld = false;
        }

        private bool ReadGripHeld()
        {
            bool actionHeld = gripAction != null && gripAction.action != null && gripAction.action.IsPressed();
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
            bool deviceHeld = device.isValid &&
                ((device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool gripButton) && gripButton) ||
                 (config != null && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float gripValue) &&
                  gripValue >= config.controllerButtonThreshold));
            return actionHeld || deviceHeld;
        }

        private GrabbableItem FindClosestAvailableItem()
        {
            GrabbableItem closest = null;
            float closestDistance = float.PositiveInfinity;
            Vector3 handPosition = interactor.attachTransform != null ? interactor.attachTransform.position : transform.position;

            // XRI hover is useful feedback, but a trigger-volume miss should not make
            // a physically close object impossible to grab. Measure against item colliders.
            GrabbableItem[] candidates = FindObjectsByType<GrabbableItem>(FindObjectsSortMode.None);
            float selectionRadius = config != null ? config.grabSelectionRadiusMeters : 0f;
            foreach (GrabbableItem item in candidates)
            {
                if (item == null || !item.CanBeGrabbed)
                    continue;

                Collider itemCollider = item.GetComponent<Collider>();
                Vector3 closestPoint = itemCollider != null ? itemCollider.ClosestPoint(handPosition) : item.transform.position;
                float distance = (closestPoint - handPosition).sqrMagnitude;
                if (distance > selectionRadius * selectionRadius)
                    continue;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = item;
                }
            }

            return closest;
        }

        private void ToggleGrip(GrabbableItem candidate, bool isStartingGrab)
        {
            GrabToggleLogic<GrabbableItem>.Result result = logic.Press(candidate, Time.frameCount, out GrabbableItem item);
            switch (result)
            {
                case GrabToggleLogic<GrabbableItem>.Result.Grab:
                    if (isStartingGrab && interactor.attachTransform != null)
                        item.transform.SetPositionAndRotation(interactor.attachTransform.position, interactor.attachTransform.rotation);
                    BeginGrab(item);
                    break;
                case GrabToggleLogic<GrabbableItem>.Result.Release:
                    ReleaseItem(item);
                    break;
                case GrabToggleLogic<GrabbableItem>.Result.None:
                    Pulse(config != null ? config.emptyGrabHapticAmplitude : 0f);
                    break;
            }
        }

        private void BeginGrab(GrabbableItem item)
        {
            if (item == null || !item.TrySetHolder(this))
            {
                logic.CancelGrab(item);
                Pulse(config != null ? config.emptyGrabHapticAmplitude : 0f);
                return;
            }

            interactor.StartManualInteraction((IXRSelectInteractable)item.Interactable);
            if (!interactor.isPerformingManualInteraction)
            {
                item.ClearHolder(this);
                item.ReturnHome();
                logic.CancelGrab(item);
                Pulse(config != null ? config.emptyGrabHapticAmplitude : 0f);
            }
        }

        private void ReleaseItem(GrabbableItem item)
        {
            bool itemSelectedByThisInteractor = interactor != null && item != null &&
                                                item.Interactable != null &&
                                                item.Interactable.interactorsSelecting.Contains(interactor);
            if (itemSelectedByThisInteractor && interactor.isPerformingManualInteraction)
                interactor.EndManualInteraction();

            if (item != null && item.HeldBy == this)
                item.ReturnHome();
        }

        private void Pulse(float amplitude)
        {
            if (interactor != null && amplitude > 0f && config != null)
                interactor.SendHapticImpulse(amplitude, config.contactHapticDurationSeconds);
        }
    }
}
