using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace CoffinEscape.Tests
{
    public sealed class CodeLockTests
    {
        private GameConfig config;
        private GameObject owner;
        private ResourceManager resources;
        private CodeLock codeLock;
        private GameObject rewardA;
        private GameObject rewardB;
        private GameObject statusObject;
        private TextMeshPro statusText;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<GameConfig>();
            owner = new GameObject("CodeLockTests");
            resources = owner.AddComponent<ResourceManager>();
            resources.Initialize(config);

            codeLock = owner.AddComponent<CodeLock>();
            rewardA = new GameObject("RewardA");
            rewardB = new GameObject("RewardB");
            statusObject = new GameObject("Status");
            statusText = statusObject.AddComponent<TextMeshPro>();

            var run = new CodeRunData(
                new[] { SymbolId.Circle, SymbolId.Triangle, SymbolId.Square },
                new[] { 9, 0, 1 },
                new[] { ClueLocation.PhotoBack, ClueLocation.RightGraffiti, ClueLocation.LeftGraffiti });

            codeLock.Configure(run, resources, null, new[] { rewardA, rewardB }, 0.1f, 90f, statusText, null, 1f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(statusObject);
            Object.DestroyImmediate(rewardA);
            Object.DestroyImmediate(rewardB);
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void DialWrapsInBothDirections()
        {
            codeLock.ChangeDial(0, -1);
            Assert.That(codeLock.GetDial(0), Is.EqualTo(9));

            codeLock.ChangeDial(0, 1);
            Assert.That(codeLock.GetDial(0), Is.EqualTo(0));
        }

        [Test]
        public void WrongAndDuplicateConfirmApplyStressOnlyOncePerCombination()
        {
            float initialStress = resources.Stress;

            Assert.That(codeLock.Confirm(), Is.EqualTo(CodeLockLogic.AttemptResult.Wrong));
            Assert.That(resources.Stress, Is.EqualTo(initialStress + config.wrongInputStress).Within(0.001f));

            Assert.That(codeLock.Confirm(), Is.EqualTo(CodeLockLogic.AttemptResult.AlreadyTried));
            Assert.That(resources.Stress, Is.EqualTo(initialStress + config.wrongInputStress).Within(0.001f));
        }

        [Test]
        public void CorrectConfirmOpensLockReducesStressAndActivatesRewards()
        {
            Assert.That(rewardA.activeSelf, Is.False);
            Assert.That(rewardB.activeSelf, Is.False);

            codeLock.ChangeDial(0, -1); // 9
            codeLock.ChangeDial(2, 1);  // 1

            Assert.That(codeLock.Confirm(), Is.EqualTo(CodeLockLogic.AttemptResult.Correct));
            Assert.That(codeLock.IsOpened, Is.True);
            Assert.That(rewardA.activeSelf, Is.True);
            Assert.That(rewardB.activeSelf, Is.True);
            Assert.That(resources.Stress, Is.EqualTo(config.initialStress - config.puzzleProgressStressReduction).Within(0.001f));
            Assert.That(statusText.text, Is.EqualTo("OPEN"));
        }

        [Test]
        public void LockedContactRespectsDialInteractionGuardAndCooldown()
        {
            float initialStress = resources.Stress;

            codeLock.ChangeDial(1, 1);
            Assert.That(codeLock.RegisterLockedContact(null), Is.False);
            Assert.That(resources.Stress, Is.EqualTo(initialStress).Within(0.001f));
        }
    }
}
