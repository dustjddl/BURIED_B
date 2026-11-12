using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CoffinEscape
{
    public sealed class RunDebugControls : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private CodeLock tinLock;
        [SerializeField] private TMP_Text readout;
        [SerializeField] private RunData data;
        public void Configure(GameConfig gameConfig,CodeLock lockComponent,RunData run,TMP_Text label)
        {config=gameConfig;tinLock=lockComponent;data=run;readout=label;}
        private void Update()
        {
            if(Keyboard.current==null||config==null)return;
            if(Keyboard.current[config.debugRevealAnswersKey].wasPressedThisFrame&&readout!=null)
                readout.text=$"SEED {data.Seed} | CAN: {string.Join("",data.TinLock.Digits)} | FINAL: {string.Join("",data.FinalLock.Digits)}";
            if(Keyboard.current[config.debugRegenerateRunKey].wasPressedThisFrame)
            {PlayerPrefs.SetInt("CoffinEscape.RunSeed",data.Seed+1);SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
        }
    }
}
