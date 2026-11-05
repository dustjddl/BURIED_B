using UnityEngine;
using Unity.XR.CoreUtils;

namespace CoffinEscape
{
    /// <summary>Keeps tracked head and controller poses inside the coffin's interior volume.</summary>
    [RequireComponent(typeof(XROrigin))]
    public sealed class CoffinBoundaryConstraint : MonoBehaviour
    {
        [SerializeField] private XROrigin origin;
        [SerializeField] private BoxCollider interiorBounds;
        [SerializeField] private Transform[] controllerTransforms;
        [SerializeField] private GameConfig config;

        public void Configure(XROrigin targetOrigin, BoxCollider bounds, Transform[] controllers, GameConfig gameConfig)
        {
            origin = targetOrigin;
            interiorBounds = bounds;
            controllerTransforms = controllers;
            config = gameConfig;
        }

        private void LateUpdate()
        {
            if (origin == null || origin.Camera == null || interiorBounds == null || config == null)
                return;

            Bounds bounds = interiorBounds.bounds;
            Vector3 constrainedHead = ClampInside(origin.Camera.transform.position, bounds,
                config.cameraBoundaryMarginMeters);
            origin.transform.position += constrainedHead - origin.Camera.transform.position;

            if (controllerTransforms == null)
                return;

            for (int i = 0; i < controllerTransforms.Length; i++)
            {
                Transform controller = controllerTransforms[i];
                if (controller != null)
                    controller.position = ClampInside(controller.position, bounds, config.controllerBoundaryMarginMeters);
            }
        }

        private static Vector3 ClampInside(Vector3 position, Bounds bounds, float margin)
        {
            Vector3 minimum = bounds.min + Vector3.one * margin;
            Vector3 maximum = bounds.max - Vector3.one * margin;
            return new Vector3(
                ClampToRange(position.x, minimum.x, maximum.x, bounds.center.x),
                ClampToRange(position.y, minimum.y, maximum.y, bounds.center.y),
                ClampToRange(position.z, minimum.z, maximum.z, bounds.center.z));
        }

        private static float ClampToRange(float value, float minimum, float maximum, float center)
        {
            return minimum <= maximum ? Mathf.Clamp(value, minimum, maximum) : center;
        }
    }
}
