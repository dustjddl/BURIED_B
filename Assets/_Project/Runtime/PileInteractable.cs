using UnityEngine;

namespace CoffinEscape
{
    public sealed class PileInteractable : MonoBehaviour
    {
        [SerializeField] private PileContent content;
        [SerializeField] private GrabbableItem item;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private float fuelBonusSeconds=120f;
        [SerializeField] private float hapticAmplitude=.35f;
        private bool awarded;
        public void Configure(PileContent value,GrabbableItem grabbable,ResourceManager manager,float bonus)
        { content=value; item=grabbable; resources=manager; fuelBonusSeconds=bonus; }
        private void Update()
        {
            if(!awarded&&item!=null&&item.HeldBy!=null)
            { awarded=true; if(content==PileContent.SpareOil&&resources!=null)resources.AddFuel(fuelBonusSeconds); Debug.Log($"Pile contents acquired: {content}"); }
        }
        private void OnTriggerEnter(Collider other)
        {
            if(!other.CompareTag("ControllerTip"))return;
            HandGrabToggle hand=other.GetComponentInParent<HandGrabToggle>();
            if(hand!=null) { float amp=content==PileContent.Photo?hapticAmplitude:hapticAmplitude*.65f; hand.PulseContact(amp); }
            Debug.Log($"Controller touched pile: {content}");
        }
    }
}
