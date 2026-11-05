using UnityEngine;

namespace CoffinEscape
{
    public sealed class LighterController : MonoBehaviour
    {
        [SerializeField] private Light lighterLight;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private GameConfig config;
        [SerializeField] private GrabbableItem grabbableItem;

        private bool debugIgnitionHeld;
        private bool ignitionToggledOn;
        private HandGrabToggle ignitionHand;

        public bool IsLit { get; private set; }

        public void Configure(Light targetLight, ResourceManager targetResources, GameConfig gameConfig)
        {
            lighterLight = targetLight;
            resources = targetResources;
            config = gameConfig;
            grabbableItem = GetComponent<GrabbableItem>();
        }

        /// <summary>Allows editor tests to drive the same hold behavior as the controller trigger.</summary>
        public void SetIgnitionHeld(bool held) => debugIgnitionHeld = held;

        private void Update()
        {
            HandGrabToggle currentHand = grabbableItem != null ? grabbableItem.HeldBy : null;
            if (currentHand != ignitionHand)
            {
                if (ignitionHand != null)
                    ignitionHand.TriggerPressed -= OnTriggerPressed;

                ignitionHand = currentHand;
                ignitionToggledOn = false;
                if (ignitionHand != null)
                    ignitionHand.TriggerPressed += OnTriggerPressed;
            }

            bool wantsLight = ignitionToggledOn || debugIgnitionHeld;
            bool isBurning = resources != null && resources.AdvanceTime(Time.deltaTime, wantsLight);
            SetLit(isBurning && wantsLight);

            if (ignitionToggledOn && resources != null && !resources.HasFuel)
                ignitionToggledOn = false;
        }

        private void OnDisable()
        {
            debugIgnitionHeld = false;
            ignitionToggledOn = false;
            if (ignitionHand != null)
                ignitionHand.TriggerPressed -= OnTriggerPressed;
            ignitionHand = null;
            SetLit(false);
        }

        private void OnTriggerPressed(HandGrabToggle hand)
        {
            if (grabbableItem != null && grabbableItem.HeldBy == hand)
                ignitionToggledOn = !ignitionToggledOn;
        }

        private void SetLit(bool lit)
        {
            IsLit = lit;
            if (lighterLight != null && config != null)
                lighterLight.intensity = lit ? config.lighterLightIntensity : 0f;
        }
    }
}
