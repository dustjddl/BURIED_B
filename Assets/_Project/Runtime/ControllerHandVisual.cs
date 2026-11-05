using UnityEngine;
using UnityEngine.XR;

namespace CoffinEscape
{
    /// <summary>Primitive fallback hand parented to the tracked controller, closing while it holds an item.</summary>
    public sealed class ControllerHandVisual : MonoBehaviour
    {
        [SerializeField] private HandGrabToggle grabToggle;
        [SerializeField] private XRNode hand = XRNode.RightHand;
        [SerializeField] private GameConfig config;
        [SerializeField] private Material fallbackMaterial;
        [SerializeField] private GameObject handPrefab;
        [SerializeField] private Transform controllerVisual;

        private Transform[] fingers;
        private Transform thumb;
        private bool initialized;

        public void Configure(HandGrabToggle toggle, XRNode handNode, GameConfig gameConfig,
            Material material, GameObject optionalHandPrefab, Transform existingControllerVisual)
        {
            grabToggle = toggle;
            hand = handNode;
            config = gameConfig;
            fallbackMaterial = material;
            handPrefab = optionalHandPrefab;
            controllerVisual = existingControllerVisual;
        }

        private void Awake() => BuildVisual();

        private void LateUpdate()
        {
            if (!initialized || fingers == null || config == null)
                return;

            float curl = grabToggle != null && grabToggle.HeldItem != null
                ? config.controllerHandFingerCurlDegrees
                : 0f;
            for (int index = 0; index < fingers.Length; index++)
            {
                if (fingers[index] != null)
                    fingers[index].localRotation = Quaternion.Euler(curl, 0f, 0f);
            }

            if (thumb != null)
            {
                float side = hand == XRNode.LeftHand ? -1f : 1f;
                thumb.localRotation = Quaternion.Euler(curl * 0.65f, 0f, side * (curl * 0.35f));
            }
        }

        private void BuildVisual()
        {
            if (initialized || config == null)
                return;

            if (controllerVisual != null)
                controllerVisual.gameObject.SetActive(false);

            Transform root = new GameObject("Tracked Hand Mesh").transform;
            root.SetParent(transform, false);
            root.localPosition = config.controllerHandLocalPositionMeters;
            root.localRotation = Quaternion.identity;

            if (handPrefab != null)
            {
                GameObject instance = Instantiate(handPrefab, root, false);
                instance.name = hand == XRNode.LeftHand ? "Left Tracked Hand" : "Right Tracked Hand";
                ApplyTint(instance, config.controllerHandColor);
                initialized = true;
                return;
            }

            Vector3 palmSize = config.controllerHandPalmSizeMeters;
            CreatePart("Palm", PrimitiveType.Sphere, root, Vector3.zero, Quaternion.identity,
                palmSize, config.controllerHandColor);

            fingers = new Transform[4];
            float spacing = palmSize.x * 0.19f;
            for (int index = 0; index < fingers.Length; index++)
            {
                float centeredIndex = index - (fingers.Length - 1f) * 0.5f;
                Transform finger = new GameObject($"Finger {index + 1}").transform;
                finger.SetParent(root, false);
                finger.localPosition = new Vector3(centeredIndex * spacing, 0f, palmSize.z * 0.36f);
                fingers[index] = finger;

                CreatePart("Finger Segment", PrimitiveType.Capsule, finger,
                    new Vector3(0f, 0f, config.controllerHandFingerLengthMeters * 0.5f),
                    Quaternion.Euler(90f, 0f, 0f),
                    new Vector3(config.controllerHandFingerThicknessMeters,
                        config.controllerHandFingerLengthMeters * 0.5f,
                        config.controllerHandFingerThicknessMeters), config.controllerHandColor);
            }

            float handSide = hand == XRNode.LeftHand ? -1f : 1f;
            thumb = new GameObject("Thumb").transform;
            thumb.SetParent(root, false);
            thumb.localPosition = new Vector3(-handSide * palmSize.x * 0.45f, 0f, palmSize.z * 0.04f);
            CreatePart("Thumb Segment", PrimitiveType.Capsule, thumb,
                new Vector3(handSide * config.controllerHandFingerLengthMeters * 0.23f, 0f,
                    config.controllerHandFingerLengthMeters * 0.27f),
                Quaternion.Euler(90f, 0f, -handSide * 38f),
                new Vector3(config.controllerHandFingerThicknessMeters * 1.15f,
                    config.controllerHandFingerLengthMeters * 0.38f,
                    config.controllerHandFingerThicknessMeters * 1.15f), config.controllerHandColor);

            initialized = true;
        }

        private GameObject CreatePart(string name, PrimitiveType primitive, Transform parent,
            Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;

            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider != null)
                Destroy(partCollider);

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null && fallbackMaterial != null)
                renderer.sharedMaterial = fallbackMaterial;
            ApplyTint(part, color);
            return part;
        }

        private static void ApplyTint(GameObject root, Color color)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", color);
                properties.SetColor("_Color", color);
                renderer.SetPropertyBlock(properties);
            }
        }
    }
}
