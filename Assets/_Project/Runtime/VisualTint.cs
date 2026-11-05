using UnityEngine;

namespace CoffinEscape
{
    /// <summary>Applies a per-instance tint without editing a shared or third-party material.</summary>
    public sealed class VisualTint : MonoBehaviour
    {
        [SerializeField] private Color tint = Color.white;

        public void Configure(Color color)
        {
            tint = color;
            Apply();
        }

        private void OnEnable() => Apply();

        private void Apply()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", tint);
                properties.SetColor("_Color", tint);
                renderer.SetPropertyBlock(properties);
            }
        }
    }
}
