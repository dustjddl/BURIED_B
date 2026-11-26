using UnityEngine;
using UnityEngine.InputSystem;

namespace CoffinEscape
{
    public sealed class PrototypeDebugHud : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private LighterController lighter;
        [SerializeField] private RunController runController;
        [SerializeField] private RunDebugControls debugControls;
        [SerializeField] private CodeLock tinLock;
        [SerializeField] private ClueItem[] clues;
        [SerializeField] private bool visible = true;

        public void Configure(
            GameConfig gameConfig,
            ResourceManager resourceManager,
            LighterController lighterController,
            RunController controller,
            RunDebugControls controls,
            CodeLock tinCodeLock,
            ClueItem[] clueItems)
        {
            config = gameConfig;
            resources = resourceManager;
            lighter = lighterController;
            runController = controller;
            debugControls = controls;
            tinLock = tinCodeLock;
            clues = clueItems;
        }

        private void Update()
        {
            if (Keyboard.current != null && config != null && Keyboard.current[config.debugToggleHudKey].wasPressedThisFrame)
                visible = !visible;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!visible || resources == null)
                return;

            float mult = resources.GetOxygenDrainMultiplier(lighter != null && lighter.IsLit);
            string oxy = FormatSeconds(resources.OxygenRemaining);
            string fuel = FormatSeconds(resources.FuelRemaining);
            string litText = lighter != null && lighter.IsLit ? "LIT" : "OFF";
            string panicText = resources.IsPanic ? " [PANIC]" : string.Empty;

            RunData run = runController != null ? runController.CurrentRun : null;
            string runLine = run != null
                ? $"Seed: {run.Seed} | PileA: {run.PileA} | PileB: {run.PileB}"
                : "Seed: -";

            string lockLine = tinLock != null
                ? $"TinLock: {(tinLock.IsOpened ? "OPEN" : "LOCKED")} | Dials: {tinLock.GetDial(0)}{tinLock.GetDial(1)}{tinLock.GetDial(2)}"
                : "TinLock: -";

            int readCount = 0;
            int totalClues = clues != null ? clues.Length : 0;
            if (clues != null)
            {
                for (int i = 0; i < clues.Length; i++)
                {
                    if (clues[i] != null && clues[i].HasBeenRead)
                        readCount++;
                }
            }

            string answerLine = debugControls != null && debugControls.AnswersRevealed && run != null
                ? $"\nAnswers -> Tin: {string.Join("", run.TinLock.Digits)} | Final: {string.Join("", run.FinalLock.Digits)}"
                : string.Empty;

            string text =
                $"Oxygen: {oxy} (x{mult:0.00}){(resources.IsOxygenDepleted ? " [DEPLETED]" : string.Empty)}\n" +
                $"Fuel: {fuel} | Lighter: {litText}\n" +
                $"Stress: {resources.Stress:0.0}/{resources.Config.maximumStress:0}{panicText}\n" +
                $"{runLine}\n" +
                $"{lockLine} | Clues Read: {readCount}/{totalClues}" +
                answerLine +
                "\n[Q/E] Grip  [T] Lighter  [R] Calibrate  [F5] New Seed  [F6] Answers  [H] HUD";

            GUI.Box(new Rect(12f, 12f, 470f, answerLine.Length > 0 ? 148f : 130f), text);
        }

        private static string FormatSeconds(float totalSeconds)
        {
            int clamped = Mathf.Max(0, Mathf.CeilToInt(totalSeconds));
            return $"{clamped / 60:00}:{clamped % 60:00}";
        }
#endif
    }
}
