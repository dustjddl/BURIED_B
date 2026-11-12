using System.Linq;
using NUnit.Framework;

namespace CoffinEscape.Tests
{
    public sealed class RunGeneratorTests
    {
        [Test] public void ThousandSeedsAreSolvableAndHaveNonzeroAnswers()
        {for(int i=0;i<1000;i++){var run=RunGenerator.Generate(i);RunGenerator.AssertSolvable(run);Assert.That(run.TinLock.Digits.All(d=>d==0),Is.False);Assert.That(run.FinalLock.Digits.All(d=>d==0),Is.False);Assert.That(run.TinLock.Symbols.Distinct().Count(),Is.EqualTo(3));Assert.That(run.FinalLock.Symbols.Distinct().Count(),Is.EqualTo(3));}}
        [Test] public void SameSeedProducesSameRun()
        {var a=RunGenerator.Generate(4701);var b=RunGenerator.Generate(4701);CollectionAssert.AreEqual(a.TinLock.Symbols,b.TinLock.Symbols);CollectionAssert.AreEqual(a.TinLock.Digits,b.TinLock.Digits);CollectionAssert.AreEqual(a.TinLock.ClueLocations,b.TinLock.ClueLocations);CollectionAssert.AreEqual(a.FinalLock.Digits,b.FinalLock.Digits);Assert.AreEqual(a.PileA,b.PileA);}
        [Test] public void CodeLockAcceptsRejectsAndSuppressesDuplicateAttempts()
        {var logic=new CodeLockLogic(new[]{1,2,3});Assert.AreEqual(CodeLockLogic.AttemptResult.Wrong,logic.Confirm());Assert.AreEqual(CodeLockLogic.AttemptResult.AlreadyTried,logic.Confirm());logic.Change(0,1);logic.Change(1,2);logic.Change(2,3);Assert.AreEqual(CodeLockLogic.AttemptResult.Correct,logic.Confirm());}
    }
}
