using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace CoffinEscape
{
    [RequireComponent(typeof(XRDirectInteractor))]
    public sealed class ControllerWallHaptics : MonoBehaviour
    {
        [SerializeField] private XRDirectInteractor interactor;
        [SerializeField] private GameConfig config;

        public void Configure(XRDirectInteractor targetInteractor, GameConfig gameConfig)
        {
            interactor = targetInteractor;
            config = gameConfig;
        }

        private void Awake()
        {
            if (interactor == null)
                interactor = GetComponent<XRDirectInteractor>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || !other.CompareTag("CoffinWall") || interactor == null || config == null)
                return;

            interactor.SendHapticImpulse(config.wallContactHapticAmplitude, config.contactHapticDurationSeconds);
        }
    }
}
