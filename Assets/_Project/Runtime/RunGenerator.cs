using System;
using System.Collections.Generic;
using System.Linq;

namespace CoffinEscape
{
    public enum SymbolId { Circle, Triangle, Square }
    public enum ClueLocation { PhotoBack, RightGraffiti, LeftGraffiti, TalismanBack, LidEngravingA, LidEngravingB }
    public enum PileContent { Photo, SpareOil }

    [Serializable]
    public sealed class CodeRunData
    {
        public SymbolId[] Symbols { get; }
        public int[] Digits { get; }
        public ClueLocation[] ClueLocations { get; }
        public CodeRunData(SymbolId[] symbols, int[] digits, ClueLocation[] locations)
        { Symbols = (SymbolId[])symbols.Clone(); Digits = (int[])digits.Clone(); ClueLocations = (ClueLocation[])locations.Clone(); }
        public int DigitFor(SymbolId symbol) { for (int i=0;i<3;i++) if (Symbols[i]==symbol) return Digits[i]; throw new ArgumentException("Symbol not in lock."); }
        public SymbolId SymbolAt(int slot) => Symbols[slot];
    }

    [Serializable]
    public sealed class RunData
    {
        public int Seed { get; }
        public CodeRunData TinLock { get; }
        public CodeRunData FinalLock { get; }
        public PileContent PileA { get; }
        public PileContent PileB { get; }
        internal RunData(int seed, CodeRunData tin, CodeRunData final, PileContent a, PileContent b)
        { Seed=seed; TinLock=tin; FinalLock=final; PileA=a; PileB=b; }
    }

    public static class RunGenerator
    {
        public static RunData Generate(int seed)
        {
            var random = new Random(seed);
            CodeRunData tin, final;
            do { tin = CreateLock(random, new[] { ClueLocation.PhotoBack, ClueLocation.RightGraffiti, ClueLocation.LeftGraffiti }); } while (IsZeroAnswer(tin));
            do { final = CreateLock(random, new[] { ClueLocation.TalismanBack, ClueLocation.LidEngravingA, ClueLocation.LidEngravingB }); } while (IsZeroAnswer(final));
            bool swap = random.Next(0,2)==1;
            var run = new RunData(seed,tin,final,swap?PileContent.SpareOil:PileContent.Photo,swap?PileContent.Photo:PileContent.SpareOil);
            AssertSolvable(run);
            return run;
        }

        private static CodeRunData CreateLock(Random random, ClueLocation[] clueLocations)
        {
            var symbols = new[] { SymbolId.Circle, SymbolId.Triangle, SymbolId.Square };
            Shuffle(random,symbols);
            var digits = new[] { random.Next(10), random.Next(10), random.Next(10) };
            var locations = (ClueLocation[])clueLocations.Clone(); Shuffle(random,locations);
            // Map each clue to the slot's symbol and digit; clue array order is independent of dial order.
            return new CodeRunData(symbols,digits,locations);
        }
        private static bool IsZeroAnswer(CodeRunData code) => code.Digits.All(d => d==0);
        private static void Shuffle<T>(Random random,T[] values) { for(int i=values.Length-1;i>0;i--){int j=random.Next(i+1); (values[i],values[j])=(values[j],values[i]);} }

        public static void AssertSolvable(RunData run)
        {
            if (run==null || run.TinLock==null || run.FinalLock==null) throw new InvalidOperationException("Run data is incomplete.");
            ValidateLock(run.TinLock,new[]{ClueLocation.PhotoBack,ClueLocation.RightGraffiti,ClueLocation.LeftGraffiti});
            ValidateLock(run.FinalLock,new[]{ClueLocation.TalismanBack,ClueLocation.LidEngravingA,ClueLocation.LidEngravingB});
            if (run.PileA==run.PileB || !new[]{run.PileA,run.PileB}.Contains(PileContent.Photo) || !new[]{run.PileA,run.PileB}.Contains(PileContent.SpareOil))
                throw new InvalidOperationException("Piles must contain one photo and one spare oil.");
            string[] chain={"photo","tin-can","tool","latch","final","exit"};
            var edges=new Dictionary<string,string>{{"photo","tin-can"},{"tin-can","tool"},{"tool","latch"},{"latch","final"},{"final","exit"}};
            var visited=new HashSet<string>(); string node=chain[0];
            while(edges.TryGetValue(node,out string next)){if(!visited.Add(node))throw new InvalidOperationException("Progression cycle detected.");node=next;}
            if(node!=chain[^1] || visited.Count!=chain.Length-1)throw new InvalidOperationException("Progression chain has missing dependencies.");
        }
        private static void ValidateLock(CodeRunData code,ClueLocation[] expected)
        {
            if(code.Symbols.Length!=3||code.Digits.Length!=3||code.ClueLocations.Length!=3||code.Symbols.Distinct().Count()!=3)throw new InvalidOperationException("Lock must have three unique symbols and three clues.");
            if(IsZeroAnswer(code)||code.Digits.Any(d=>d<0||d>9)||!expected.OrderBy(x=>x).SequenceEqual(code.ClueLocations.OrderBy(x=>x)))throw new InvalidOperationException("Lock clues or digits are invalid.");
            foreach(var loc in expected){int index=Array.IndexOf(code.ClueLocations,loc); if(index<0||code.DigitFor(code.Symbols[index])!=code.Digits[index])throw new InvalidOperationException("A clue does not derive the lock solution.");}
        }
    }
}
