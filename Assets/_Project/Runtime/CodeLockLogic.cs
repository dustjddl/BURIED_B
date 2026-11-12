using System;
using System.Collections.Generic;

namespace CoffinEscape
{
    public sealed class CodeLockLogic
    {
        public enum AttemptResult { Correct, Wrong, AlreadyTried }
        private readonly int[] answer;
        private readonly int[] dials = new int[3];
        private readonly HashSet<int> tried = new HashSet<int>();
        public IReadOnlyList<int> Dials => dials;
        public CodeLockLogic(int[] correctDigits)
        { if(correctDigits==null||correctDigits.Length!=3)throw new ArgumentException("Three digits required."); answer=(int[])correctDigits.Clone(); }
        public void Change(int slot,int delta) { if(slot<0||slot>=3)throw new ArgumentOutOfRangeException(nameof(slot)); dials[slot]=(dials[slot]+delta+10)%10; }
        public AttemptResult Confirm()
        {
            int key=dials[0]*100+dials[1]*10+dials[2];
            if(!tried.Add(key)) return AttemptResult.AlreadyTried;
            return dials[0]==answer[0]&&dials[1]==answer[1]&&dials[2]==answer[2]?AttemptResult.Correct:AttemptResult.Wrong;
        }
    }
}
