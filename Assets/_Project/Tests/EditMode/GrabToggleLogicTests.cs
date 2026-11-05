using NUnit.Framework;

namespace CoffinEscape.Tests
{
    public sealed class GrabToggleLogicTests
    {
        private GrabToggleLogic<object> logic;

        [SetUp]
        public void SetUp() => logic = new GrabToggleLogic<object>();

        [Test]
        public void EmptyHandWithCandidateGrabsIt()
        {
            object candidate = new object();

            var result = logic.Press(candidate, 10, out object affected);

            Assert.That(result, Is.EqualTo(GrabToggleLogic<object>.Result.Grab));
            Assert.That(affected, Is.SameAs(candidate));
            Assert.That(logic.HeldItem, Is.SameAs(candidate));
        }

        [Test]
        public void PressWhileHoldingAlwaysReleasesCurrentItem()
        {
            object held = new object();
            object otherCandidate = new object();
            logic.Press(held, 10, out _);

            var result = logic.Press(otherCandidate, 11, out object affected);

            Assert.That(result, Is.EqualTo(GrabToggleLogic<object>.Result.Release));
            Assert.That(affected, Is.SameAs(held));
            Assert.That(logic.HeldItem, Is.Null);
        }

        [Test]
        public void NoCandidateLeavesEmptyHandUnchanged()
        {
            var result = logic.Press(null, 10, out object affected);

            Assert.That(result, Is.EqualTo(GrabToggleLogic<object>.Result.None));
            Assert.That(affected, Is.Null);
            Assert.That(logic.IsHolding, Is.False);
        }

        [Test]
        public void OneHandCannotHoldASecondItemBeforeReleasingFirst()
        {
            object first = new object();
            object second = new object();
            logic.Press(first, 10, out _);

            var result = logic.Press(second, 11, out object affected);

            Assert.That(result, Is.EqualTo(GrabToggleLogic<object>.Result.Release));
            Assert.That(affected, Is.SameAs(first));
            Assert.That(logic.HeldItem, Is.Null);
        }

        [Test]
        public void DuplicatePressInSameFrameIsIgnored()
        {
            object candidate = new object();
            logic.Press(candidate, 10, out _);

            var result = logic.Press(null, 10, out object affected);

            Assert.That(result, Is.EqualTo(GrabToggleLogic<object>.Result.IgnoredDuplicateFrame));
            Assert.That(affected, Is.Null);
            Assert.That(logic.HeldItem, Is.SameAs(candidate));
        }
    }
}
