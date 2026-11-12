using System;
using TMPro;
using UnityEngine;

namespace CoffinEscape
{
    public sealed class ClueItem : MonoBehaviour
    {
        [SerializeField] private SymbolId symbol;
        [SerializeField,Range(0,9)] private int digit;
        [SerializeField] private Light requiredLight;
        [SerializeField] private Transform reader;
        [SerializeField] private float readDistance=.45f;
        [SerializeField] private TextMeshPro worldText;
        public event Action<ClueItem> BecameReadable;
        public bool IsReadable { get; private set; }
        public SymbolId Symbol=>symbol; public int Digit=>digit;
        public void Configure(SymbolId id,int value,Light light,Transform readingFrom,float maxDistance,TextMeshPro label)
        { symbol=id; digit=Mathf.Clamp(value,0,9); requiredLight=light; reader=readingFrom; readDistance=maxDistance; worldText=label; if(worldText!=null)worldText.text=digit.ToString(); }
        private void Update()
        {
            bool readable=requiredLight!=null&&requiredLight.enabled&&requiredLight.intensity>0&&reader!=null&&(reader.position-transform.position).sqrMagnitude<=readDistance*readDistance;
            if(readable&&!IsReadable) BecameReadable?.Invoke(this);
            IsReadable=readable;
            if(worldText!=null)worldText.gameObject.SetActive(readable);
        }
    }
}
