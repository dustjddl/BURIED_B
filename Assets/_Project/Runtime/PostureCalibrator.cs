using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using System.Collections;

namespace CoffinEscape
{
    public sealed class PostureCalibrator : MonoBehaviour
    {
        [SerializeField] private XROrigin origin;
        [SerializeField] private Transform headAnchor;
        [SerializeField] private GameConfig config;
        [SerializeField] private bool calibrateOnStart = true;

        private bool leftPrimaryWasPressed;

        public void Configure(XROrigin targetOrigin, Transform targetHeadAnchor, GameConfig gameConfig)
        {
            origin = targetOrigin;
            headAnchor = targetHeadAnchor;
            config = gameConfig;
        }

        private IEnumerator Start()
        {
            if (!calibrateOnStart)
                yield break;

            int delayFrames = config != null ? Mathf.Max(0, config.startupCalibrationDelayFrames) : 0;
            for (int frame = 0; frame < delayFrames; frame++)
                yield return null;

            // XR runtimes can apply the first tracked HMD pose after MonoBehaviour.Start.
            // Recenter after those initial pose updates so the player starts inside the coffin.
            yield return new WaitForEndOfFrame();
            Calibrate();
        }

        private void Update()
        {
            bool primaryPressed = ReadLeftPrimaryButton();
            bool pressedThisFrame = primaryPressed && !leftPrimaryWasPressed;
            leftPrimaryWasPressed = primaryPressed;

            Key debugKey = config != null ? config.debugCalibrateKey : Key.R;
            if (pressedThisFrame || (Keyboard.current != null && Keyboard.current[debugKey].wasPressedThisFrame))
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
