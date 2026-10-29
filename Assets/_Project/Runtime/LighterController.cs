using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace CoffinEscape
{
    public sealed class LighterController : MonoBehaviour
    {
        [SerializeField] private Light lighterLight;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private GameConfig config;
        [SerializeField] private XRNode ignitionHand = XRNode.RightHand;
        [SerializeField] private Key debugIgnitionKey = Key.F;
        [Tooltip("Hold this action to keep the lighter lit. The action must be enabled by the InputActionManager or this component.")]
        [SerializeField] private InputActionReference toggleAction;

        private bool ignitionHeld;
        private bool enabledActionHere;

        public bool IsLit { get; private set; }

        public void Configure(Light targetLight, ResourceManager targetResources, GameConfig gameConfig)
        {
            lighterLight = targetLight;
            resources = targetResources;
            config = gameConfig;
        }

        private void OnEnable()
        {
            if (toggleAction == null || toggleAction.action == null)
                return;

            toggleAction.action.started += OnIgnitionStarted;
            toggleAction.action.canceled += OnIgnitionCanceled;
            if (!toggleAction.action.enabled)
            {
                toggleAction.action.Enable();
                enabledActionHere = true;
            }
        }

        private void OnDisable()
        {
            if (toggleAction != null && toggleAction.action != null)
            {
                toggleAction.action.started -= OnIgnitionStarted;
                toggleAction.action.canceled -= OnIgnitionCanceled;
                if (enabledActionHere)
                    toggleAction.action.Disable();
            }

            enabledActionHere = false;
            ignitionHeld = false;
            SetLit(false);
        }

        private void Update()
        {
            bool held = ignitionHeld || ReadFallbackHold();
            bool lit = resources != null && resources.AdvanceTime(Time.deltaTime, held);
            SetLit(lit);
        }

        /// <summary>Allows keyboard/mouse debug controls to drive the same hold behavior.</summary>
        public void SetIgnitionHeld(bool held) => ignitionHeld = held;

        private void OnIgnitionStarted(InputAction.CallbackContext _) => ignitionHeld = true;
        private void OnIgnitionCanceled(InputAction.CallbackContext _) => ignitionHeld = false;

        private bool ReadFallbackHold()
        {
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(ignitionHand);
            bool hardwareHeld = device.isValid &&
                (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool triggerButton) && triggerButton ||
                 (config != null && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float triggerValue) &&
                  triggerValue >= config.controllerButtonThreshold));
            return hardwareHeld || (Keyboard.current != null && Keyboard.current[debugIgnitionKey].isPressed);
        }

        private void SetLit(bool lit)
        {
            IsLit = lit;
            if (lighterLight != null && resources != null && resources.Config != null)
                lighterLight.intensity = lit ? resources.Config.lighterLightIntensity : 0f;
        }
    }
}
