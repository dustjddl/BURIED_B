using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

namespace CoffinEscape
{
    public sealed class PostureCalibrator : MonoBehaviour
    {
        [SerializeField] private XROrigin origin;
        [SerializeField] private Transform headAnchor;
        [SerializeField] private bool calibrateOnStart = true;
        [SerializeField] private Key debugCalibrateKey = Key.F5;

        private bool leftPrimaryWasPressed;

        private void Start()
        {
            if (calibrateOnStart)
                Calibrate();
        }

        private void Update()
        {
            bool primaryPressed = ReadLeftPrimaryButton();
            bool pressedThisFrame = primaryPressed && !leftPrimaryWasPressed;
            leftPrimaryWasPressed = primaryPressed;

            if (pressedThisFrame || (Keyboard.current != null && Keyboard.current[debugCalibrateKey].wasPressedThisFrame))
                Calibrate();
        }

        public void Calibrate()
        {
            if (origin == null || origin.Camera == null || headAnchor == null)
                return;

            origin.MatchOriginUpCameraForward(Vector3.up, headAnchor.forward);
            origin.MoveCameraToWorldLocation(headAnchor.position);
        }

        private static bool ReadLeftPrimaryButton()
        {
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (!device.isValid)
                return false;

            return device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool isPressed) && isPressed;
        }
    }
}
