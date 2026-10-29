using System;
using UnityEngine;

namespace CoffinEscape
{
    public sealed class ResourceManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        public event Action ValuesChanged;
        public event Action OxygenDepleted;

        public float OxygenRemaining { get; private set; }
        public float FuelRemaining { get; private set; }
        public float Stress { get; private set; }
        public float DarkSeconds { get; private set; }
        public bool IsOxygenDepleted { get; private set; }
        public bool HasFuel => FuelRemaining > 0f;
        public GameConfig Config => config;

        private void Awake()
        {
            if (config != null)
                ResetResources();
        }

        public void Initialize(GameConfig gameConfig)
        {
            config = gameConfig;
            ResetResources();
        }

        public void ResetResources()
        {
            if (config == null)
                throw new InvalidOperationException("ResourceManager requires a GameConfig.");

            OxygenRemaining = config.oxygenDurationSeconds;
            FuelRemaining = config.lighterFuelDurationSeconds;
            Stress = Mathf.Clamp(config.initialStress, 0f, config.maximumStress);
            DarkSeconds = 0f;
            IsOxygenDepleted = false;
            ValuesChanged?.Invoke();
        }

        /// <summary>Advances game resources by an injected time step; returns whether the lighter actually burned.</summary>
        public bool AdvanceTime(float deltaSeconds, bool lighterHeld)
        {
            if (config == null)
                throw new InvalidOperationException("ResourceManager requires a GameConfig.");
            if (deltaSeconds <= 0f || IsOxygenDepleted)
                return false;

            float litSeconds = lighterHeld ? Mathf.Min(deltaSeconds, FuelRemaining) : 0f;
            float darkSeconds = deltaSeconds - litSeconds;
            FuelRemaining = Mathf.Max(0f, FuelRemaining - litSeconds);

            if (litSeconds > 0f)
            {
                DarkSeconds = 0f;
                Stress = Mathf.Max(0f, Stress - config.lighterStressRecoveryPerSecond * litSeconds);
            }

            if (darkSeconds > 0f)
            {
                float stressTimeBefore = Mathf.Max(0f, DarkSeconds - config.darkStressDelaySeconds);
                DarkSeconds += darkSeconds;
                float stressTimeAfter = Mathf.Max(0f, DarkSeconds - config.darkStressDelaySeconds);
                Stress = Mathf.Min(config.maximumStress,
                    Stress + (stressTimeAfter - stressTimeBefore) * config.darkStressPerSecond);
            }

            float panicMultiplier = Stress >= config.panicStressThreshold ? config.panicOxygenMultiplier : 1f;
            float oxygenSeconds = darkSeconds + litSeconds * config.oxygenMultiplierWhileLighterLit;
            OxygenRemaining = Mathf.Max(0f, OxygenRemaining - oxygenSeconds * config.oxygenDrainRate * panicMultiplier);

            if (OxygenRemaining <= 0f && !IsOxygenDepleted)
            {
                IsOxygenDepleted = true;
                OxygenDepleted?.Invoke();
            }

            ValuesChanged?.Invoke();
            return litSeconds > 0f;
        }

        public void AddFuel(float seconds)
        {
            if (config == null)
                throw new InvalidOperationException("ResourceManager requires a GameConfig.");
            if (seconds <= 0f)
                return;

            FuelRemaining += seconds;
            ValuesChanged?.Invoke();
        }

        public void ApplyWrongInput() => ChangeStress(config.wrongInputStress);
        public void ApplyLockedObjectClick() => ChangeStress(config.lockedObjectStress);
        public void ApplyPuzzleProgress() => ChangeStress(-config.puzzleProgressStressReduction);

        private void ChangeStress(float amount)
        {
            if (config == null)
                throw new InvalidOperationException("ResourceManager requires a GameConfig.");

            Stress = Mathf.Clamp(Stress + amount, 0f, config.maximumStress);
            ValuesChanged?.Invoke();
        }
    }
}
