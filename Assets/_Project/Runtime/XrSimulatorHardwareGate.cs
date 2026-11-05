using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace CoffinEscape
{
    /// <summary>Keeps the keyboard/mouse simulator available until a complete physical XR rig is tracked.</summary>
    public sealed class XrSimulatorHardwareGate : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        private XRInteractionSimulator simulator;
        private float nextCheckTime;
        private float fallbackLogTime;
        private bool? usingPhysicalRig;

        public void Configure(GameConfig gameConfig) => config = gameConfig;

        private void Awake()
        {
            simulator = GetComponent<XRInteractionSimulator>();
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup < nextCheckTime)
                return;

            nextCheckTime = Time.realtimeSinceStartup + 0.25f;
            InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool completePhysicalRig = IsPhysical(head) && IsPhysical(left) && IsPhysical(right);

            if (usingPhysicalRig == completePhysicalRig)
                return;

            usingPhysicalRig = completePhysicalRig;
            if (simulator != null)
                simulator.enabled = !completePhysicalRig;

            if (completePhysicalRig)
            {
                Debug.Log("CoffinEscape: HMD and both physical controllers detected; using hardware input.");
                return;
            }

            float warningDelay = config != null ? config.xrHardwareDetectionTimeoutSeconds : 0f;
            if (Time.realtimeSinceStartup >= fallbackLogTime + warningDelay)
            {
                fallbackLogTime = Time.realtimeSinceStartup;
                Debug.LogWarning($"CoffinEscape: physical XR input incomplete (HMD={IsPhysical(head)}, left={IsPhysical(left)}, right={IsPhysical(right)}); keyboard/mouse simulator remains enabled.");
            }
        }

        private static bool IsPhysical(InputDevice device) =>
            device.isValid && device.name.IndexOf("Simulated", StringComparison.OrdinalIgnoreCase) < 0;
    }
}
