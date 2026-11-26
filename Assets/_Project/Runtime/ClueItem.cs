using System;
using TMPro;
using UnityEngine;

namespace CoffinEscape
{
    public sealed class ClueItem : MonoBehaviour
    {
        [SerializeField] private SymbolId symbol;
        [SerializeField, Range(0, 9)] private int digit;
        [SerializeField] private Light requiredLight;
        [SerializeField] private Transform reader;
        [SerializeField] private float readDistance = 0.45f;
        [SerializeField] private TextMeshPro worldText;
        [SerializeField] private SymbolView symbolView;

        public event Action<ClueItem> BecameReadable;

        public bool IsReadable { get; private set; }
        public bool HasBeenRead { get; private set; }
        public SymbolId Symbol => symbol;
        public int Digit => digit;

        public void Configure(SymbolId id, int value, Light light, Transform readingFrom, float maxDistance,
            TextMeshPro label, SymbolView view = null)
        {
            requiredLight = light;
            reader = readingFrom;
            readDistance = maxDistance;
            worldText = label;
            symbolView = view != null ? view : GetComponentInChildren<SymbolView>(true);
            SetClue(id, value);
        }

        public void SetClue(SymbolId id, int value)
        {
            symbol = id;
            digit = Mathf.Clamp(value, 0, 9);
            IsReadable = false;
            HasBeenRead = false;
            if (symbolView != null)
                symbolView.Set(symbol, symbolView.Diameter);
            if (worldText != null)
                worldText.text = digit.ToString();
        }

        private void Update()
        {
            bool readable = requiredLight != null && requiredLight.enabled && requiredLight.intensity > 0f &&
                            reader != null && (reader.position - transform.position).sqrMagnitude <= readDistance * readDistance;
            if (readable && !IsReadable)
            {
                HasBeenRead = true;
                BecameReadable?.Invoke(this);
            }

            IsReadable = readable;
            if (worldText != null)
                worldText.gameObject.SetActive(readable);
        }
    }
}
