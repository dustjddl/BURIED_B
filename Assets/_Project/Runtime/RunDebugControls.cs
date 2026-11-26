using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CoffinEscape
{
    public sealed class RunDebugControls : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private RunController runController;
        [SerializeField] private CodeLock tinLock;
        [SerializeField] private TMP_Text readout;
        [SerializeField] private RunData data;

        public bool AnswersRevealed { get; private set; }

        public void Configure(GameConfig gameConfig, RunController controller, CodeLock lockComponent, RunData run, TMP_Text label)
        {
            config = gameConfig;
            runController = controller;
            tinLock = lockComponent;
            data = run;
            readout = label;
        }

        public void ToggleAnswers()
        {
            RunData activeRun = runController != null && runController.CurrentRun != null ? runController.CurrentRun : data;
            if (activeRun == null)
                return;

            AnswersRevealed = !AnswersRevealed;
            if (readout != null)
            {
                readout.text = AnswersRevealed
                    ? $"SEED {activeRun.Seed} | CAN: {string.Join("", activeRun.TinLock.Digits)} | FINAL: {string.Join("", activeRun.FinalLock.Digits)}"
                    : tinLock != null && tinLock.IsOpened ? "OPEN" : "LOCKED";
            }
        }

        public void RegenerateWithNextSeed()
        {
            RunData activeRun = runController != null && runController.CurrentRun != null ? runController.CurrentRun : data;
            int nextSeed = (activeRun != null ? activeRun.Seed : PlayerPrefs.GetInt(RunController.RunSeedPrefsKey, 1)) + 1;
            PlayerPrefs.SetInt(RunController.RunSeedPrefsKey, nextSeed);
            PlayerPrefs.Save();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Update()
        {
            if (Keyboard.current == null || config == null)
                return;

            if (Keyboard.current[config.debugRevealAnswersKey].wasPressedThisFrame)
                ToggleAnswers();

            if (Keyboard.current[config.debugRegenerateRunKey].wasPressedThisFrame)
                RegenerateWithNextSeed();
        }
    }
}
