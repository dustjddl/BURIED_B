using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace CoffinEscape
{
    public sealed class HandSelectInput : MonoBehaviour
    {
        [SerializeField] private XRDirectInteractor interactor;
        [SerializeField] private XRNode hand = XRNode.LeftHand;
        [SerializeField] private GameConfig config;
        [SerializeField] private Key debugGripKey = Key.Q;

        private void Awake()
        {
            if (interactor == null)
                interactor = GetComponent<XRDirectInteractor>();

            if (interactor != null)
            {
                XRInputButtonReader reader = interactor.selectInput;
                reader.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
                interactor.selectInput = reader;
            }
        }

        public void Configure(XRDirectInteractor targetInteractor, XRNode targetHand, GameConfig gameConfig, Key keyboardKey)
        {
            interactor = targetInteractor;
            hand = targetHand;
            config = gameConfig;
            debugGripKey = keyboardKey;
        }

        private void Update()
        {
            if (interactor == null)
                return;

            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
            bool controllerGrip = device.isValid &&
                (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool gripButton) && gripButton ||
                 (config != null && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float gripValue) &&
                  gripValue >= config.controllerButtonThreshold));
            bool keyboardGrip = Keyboard.current != null && Keyboard.current[debugGripKey].isPressed;
            interactor.selectInput.manualPerformed = controllerGrip || keyboardGrip;
        }
    }
}
