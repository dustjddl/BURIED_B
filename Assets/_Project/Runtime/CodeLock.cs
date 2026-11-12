using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CoffinEscape
{
    public sealed class CodeLock : MonoBehaviour
    {
        [SerializeField] private int[] answer=new int[3];
        [SerializeField] private SymbolId[] symbols=new SymbolId[3];
        [SerializeField] private ResourceManager resources;
        [SerializeField] private Transform lid;
        [SerializeField] private GameObject[] rewards;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text[] dialValues = new TMP_Text[3];
        [SerializeField] private float openSeconds=.8f,openAngle=100f;
        private CodeLockLogic logic;
        private bool opened;
        private readonly HashSet<int> contactFrames=new HashSet<int>();
        public void Configure(CodeRunData data,ResourceManager manager,Transform lidTransform,GameObject[] unlockables,float duration,float angle,TMP_Text label,TMP_Text[] digitLabels)
        { answer=(int[])data.Digits.Clone(); symbols=(SymbolId[])data.Symbols.Clone(); resources=manager; lid=lidTransform; rewards=unlockables; openSeconds=duration; openAngle=angle; status=label; dialValues=digitLabels; logic=new CodeLockLogic(answer); }
        private void Awake(){if(logic==null)logic=new CodeLockLogic(answer);}
        public CodeLockLogic.AttemptResult Confirm()
        {
            if(opened)return CodeLockLogic.AttemptResult.AlreadyTried;
            var result=logic.Confirm();
            if(status!=null)status.text=result==CodeLockLogic.AttemptResult.Correct?"OPEN":result==CodeLockLogic.AttemptResult.Wrong?"오답":"이미 시도함";
            if(result==CodeLockLogic.AttemptResult.Wrong)resources?.ApplyWrongInput();
            if(result==CodeLockLogic.AttemptResult.Correct){opened=true;resources?.ApplyPuzzleProgress();foreach(var r in rewards)if(r!=null)r.SetActive(true);StartCoroutine(AnimateLid());}
            return result;
        }
        public void ChangeDial(int index,int delta){if(!opened){logic.Change(index,delta);if(dialValues!=null&&index<dialValues.Length&&dialValues[index]!=null)dialValues[index].text=logic.Dials[index].ToString();}}
        public int GetDial(int index)=>logic.Dials[index];
        public SymbolId GetSymbol(int slot)=>symbols[slot];
        private IEnumerator AnimateLid()
        {
            if(lid==null)yield break;
            Quaternion start=lid.localRotation,end=start*Quaternion.Euler(-openAngle,0,0); float time=0;
            while(time<openSeconds){time+=Time.deltaTime;lid.localRotation=Quaternion.Slerp(start,end,Mathf.Clamp01(time/openSeconds));yield return null;}
            lid.localRotation=end;
        }
        public void RegisterLockedContact(HandGrabToggle hand)
        {
            if(opened)return;
            contactFrames.Clear();
            if(!contactFrames.Add(Time.frameCount))return;
            resources?.ApplyLockedObjectClick();hand?.PulseContact(.18f);
        }
    }
}
