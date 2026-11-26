using UnityEngine;

namespace CoffinEscape
{
    public sealed class PileInteractable : MonoBehaviour
    {
        [SerializeField] private PileContent content;
        [SerializeField] private GrabbableItem item;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private float fuelBonusSeconds = 120f;
        [SerializeField] private float hapticAmplitude = 0.35f;

        private bool awarded;

        public PileContent Content => content;
        public bool IsAwarded => awarded;

        public void Configure(PileContent value, GrabbableItem grabbable, ResourceManager manager, float bonus)
        {
            content = value;
            item = grabbable;
            resources = manager;
            fuelBonusSeconds = bonus;
            awarded = false;
        }

        private void Update()
        {
            if (!awarded && item != null && item.HeldBy != null)
            {
                awarded = true;
                if (content == PileContent.SpareOil)
                {
                    if (resources != null)
                        resources.AddFuel(fuelBonusSeconds);
                    VisualTint tint = item.GetComponentInChildren<VisualTint>(true);
                    if (tint != null)
                        tint.Configure(new Color(0.28f, 0.25f, 0.2f, 1f));
                }

                Debug.Log($"Pile contents acquired: {content}");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("ControllerTip"))
                return;

            HandGrabToggle hand = other.GetComponentInParent<HandGrabToggle>();
            if (hand != null)
            {
                float amp = content == PileContent.Photo ? hapticAmplitude : hapticAmplitude * 0.65f;
                hand.PulseContact(amp);
            }

            Debug.Log($"Controller touched pile: {content}");
        }
    }
}
