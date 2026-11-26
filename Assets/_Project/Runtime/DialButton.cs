using UnityEngine;

namespace CoffinEscape
{
    public sealed class DialButton : MonoBehaviour
    {
        [SerializeField] private CodeLock codeLock;
        [SerializeField] private int slot;
        [SerializeField] private int delta;
        [SerializeField] private bool confirm;
        [SerializeField] private float cooldown = 0.3f;

        private float nextAllowed;

        public void Configure(CodeLock target, int dialSlot, int direction, bool isConfirm, float cooldownSeconds)
        {
            codeLock = target;
            slot = dialSlot;
            delta = direction;
            confirm = isConfirm;
            cooldown = cooldownSeconds;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (codeLock == null || codeLock.IsOpened || !other.CompareTag("ControllerTip") || Time.time < nextAllowed)
                return;

            nextAllowed = Time.time + cooldown;
            codeLock.NotifyDialInteraction();
            if (confirm)
                codeLock.Confirm();
            else
                codeLock.ChangeDial(slot, delta);

            other.GetComponentInParent<HandGrabToggle>()?.PulseContact(0.2f);
        }
    }
}
