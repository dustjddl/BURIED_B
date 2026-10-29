using NUnit.Framework;
using UnityEngine;

namespace CoffinEscape.Tests
{
    public sealed class ResourceManagerTests
    {
        private GameConfig config;
        private GameObject owner;
        private ResourceManager resources;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<GameConfig>();
            owner = new GameObject("ResourceManagerTests");
            resources = owner.AddComponent<ResourceManager>();
            resources.Initialize(config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void ResetUsesConfiguredStartingResources()
        {
            Assert.That(resources.OxygenRemaining, Is.EqualTo(config.oxygenDurationSeconds));
            Assert.That(resources.FuelRemaining, Is.EqualTo(config.lighterFuelDurationSeconds));
            Assert.That(resources.Stress, Is.EqualTo(config.initialStress));
        }

        [Test]
        public void LighterConsumesFuelAndMultipliesOxygenDrain()
        {
            resources.AdvanceTime(10f, true);

            Assert.That(resources.FuelRemaining, Is.EqualTo(config.lighterFuelDurationSeconds - 10f).Within(0.001f));
            Assert.That(resources.OxygenRemaining, Is.EqualTo(config.oxygenDurationSeconds - 10f * config.oxygenMultiplierWhileLighterLit).Within(0.001f));
        }

        [Test]
        public void PanicAndLighterMultipliersStack()
        {
            config.initialStress = config.panicStressThreshold;
            config.lighterStressRecoveryPerSecond = 0f;
            resources.ResetResources();
            resources.AdvanceTime(2f, true);

            float expectedDrain = 2f * config.oxygenMultiplierWhileLighterLit * config.panicOxygenMultiplier;
            Assert.That(resources.OxygenRemaining, Is.EqualTo(config.oxygenDurationSeconds - expectedDrain).Within(0.001f));
        }

        [Test]
        public void DarknessStressStartsAfterConfiguredGracePeriod()
        {
            resources.AdvanceTime(config.darkStressDelaySeconds, false);
            Assert.That(resources.Stress, Is.EqualTo(config.initialStress).Within(0.001f));

            resources.AdvanceTime(3f, false);
            Assert.That(resources.Stress, Is.EqualTo(config.initialStress + 3f * config.darkStressPerSecond).Within(0.001f));
        }

        [Test]
        public void OxygenDepletionIsClampedAndRaisedOnce()
        {
            int depletionEvents = 0;
            resources.OxygenDepleted += () => depletionEvents++;

            resources.AdvanceTime(config.oxygenDurationSeconds + 1f, false);
            resources.AdvanceTime(1f, false);

            Assert.That(resources.OxygenRemaining, Is.Zero);
            Assert.That(depletionEvents, Is.EqualTo(1));
        }

        [Test]
        public void FuelCannotBeConsumedAfterItRunsOut()
        {
            resources.AdvanceTime(config.lighterFuelDurationSeconds + 5f, true);

            Assert.That(resources.FuelRemaining, Is.Zero);
            Assert.That(resources.OxygenRemaining,
                Is.EqualTo(config.oxygenDurationSeconds - config.lighterFuelDurationSeconds * config.oxygenMultiplierWhileLighterLit - 5f).Within(0.001f));
        }
    }
}
