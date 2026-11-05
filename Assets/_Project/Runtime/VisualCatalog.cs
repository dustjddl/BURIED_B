using UnityEngine;

namespace CoffinEscape
{
    [CreateAssetMenu(fileName = "VisualCatalog", menuName = "CoffinEscape/Visual Catalog")]
    public sealed class VisualCatalog : ScriptableObject
    {
        [Header("Optional visual prefabs. Empty references use primitive graybox visuals.")]
        public GameObject coffinPanelPrefab;
        public GameObject cubePickupPrefab;
        public GameObject spherePickupPrefab;
        public GameObject lighterPrefab;
        public GameObject controllerHandPrefab;
    }
}
