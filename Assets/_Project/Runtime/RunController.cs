using TMPro;
using UnityEngine;

namespace CoffinEscape
{
    public sealed class RunController : MonoBehaviour
    {
        public const string RunSeedPrefsKey = "CoffinEscape.RunSeed";

        [SerializeField] private GameConfig config;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private PileInteractable pileA;
        [SerializeField] private PileInteractable pileB;
        [SerializeField] private GrabbableItem photoItem;
        [SerializeField] private GrabbableItem spareOilItem;
        [SerializeField] private Transform photoAnchor;
        [SerializeField] private Transform spareOilAnchor;
        [SerializeField] private ClueItem photoBackClue;
        [SerializeField] private ClueItem rightGraffitiClue;
        [SerializeField] private ClueItem leftGraffitiClue;
        [SerializeField] private ClueItem talismanBackClue;
        [SerializeField] private CodeLock tinLock;
        [SerializeField] private SymbolView[] tinDialSymbols = new SymbolView[3];
        [SerializeField] private Transform tinLidHinge;
        [SerializeField] private GameObject[] tinRewards = new GameObject[2];
        [SerializeField] private TMP_Text tinStatusText;
        [SerializeField] private TMP_Text[] tinDialTexts = new TMP_Text[3];

        public RunData CurrentRun { get; private set; }

        public void Configure(
            GameConfig gameConfig,
            ResourceManager resourceManager,
            PileInteractable firstPile,
            PileInteractable secondPile,
            GrabbableItem photo,
            GrabbableItem spareOil,
            Transform photoReturnAnchor,
            Transform spareOilReturnAnchor,
            ClueItem photoClue,
            ClueItem rightGraffiti,
            ClueItem leftGraffiti,
            ClueItem talismanClue,
            CodeLock tinCodeLock,
            SymbolView[] dialSymbols,
            Transform lidHinge,
            GameObject[] rewards,
            TMP_Text statusText,
            TMP_Text[] dialTexts)
        {
            config = gameConfig;
            resources = resourceManager;
            pileA = firstPile;
            pileB = secondPile;
            photoItem = photo;
            spareOilItem = spareOil;
            photoAnchor = photoReturnAnchor;
            spareOilAnchor = spareOilReturnAnchor;
            photoBackClue = photoClue;
            rightGraffitiClue = rightGraffiti;
            leftGraffitiClue = leftGraffiti;
            talismanBackClue = talismanClue;
            tinLock = tinCodeLock;
            tinDialSymbols = dialSymbols;
            tinLidHinge = lidHinge;
            tinRewards = rewards;
            tinStatusText = statusText;
            tinDialTexts = dialTexts;
        }

        private void Awake()
        {
            int seed = PlayerPrefs.GetInt(RunSeedPrefsKey, 1);
            ApplySeed(seed);
        }

        public void ApplySeed(int seed)
        {
            CurrentRun = RunGenerator.Generate(seed);
            ApplyRun(CurrentRun);
        }

        public void ApplyRun(RunData run)
        {
            if (run == null || config == null)
                return;

            CurrentRun = run;

            Vector3 pileAPickupPos = config.pileAPositionMeters + Vector3.up * config.puzzleItemSizeMeters;
            Vector3 pileBPickupPos = config.pileBPositionMeters + Vector3.up * config.puzzleItemSizeMeters;
            bool photoInPileA = run.PileA == PileContent.Photo;

            if (photoAnchor != null)
                photoAnchor.localPosition = photoInPileA ? pileAPickupPos : pileBPickupPos;
            if (spareOilAnchor != null)
                spareOilAnchor.localPosition = photoInPileA ? pileBPickupPos : pileAPickupPos;

            photoItem?.ReturnHome();
            spareOilItem?.ReturnHome();
            if (spareOilItem != null)
            {
                VisualTint tint = spareOilItem.GetComponentInChildren<VisualTint>(true);
                if (tint != null)
                    tint.Configure(config.lighterBodyColor);
            }

            pileA?.Configure(run.PileA, photoInPileA ? photoItem : spareOilItem, resources, config.oilFuelBonusSeconds);
            pileB?.Configure(run.PileB, photoInPileA ? spareOilItem : photoItem, resources, config.oilFuelBonusSeconds);

            ApplyClue(photoBackClue, run.TinLock, ClueLocation.PhotoBack);
            ApplyClue(rightGraffitiClue, run.TinLock, ClueLocation.RightGraffiti);
            ApplyClue(leftGraffitiClue, run.TinLock, ClueLocation.LeftGraffiti);
            ApplyClue(talismanBackClue, run.FinalLock, ClueLocation.TalismanBack);

            if (tinDialSymbols != null)
            {
                for (int i = 0; i < Mathf.Min(3, tinDialSymbols.Length); i++)
                {
                    if (tinDialSymbols[i] != null)
                        tinDialSymbols[i].Set(run.TinLock.SymbolAt(i), tinDialSymbols[i].Diameter);
                }
            }

            if (tinLock != null)
            {
                tinLock.Configure(
                    run.TinLock,
                    resources,
                    tinLidHinge,
                    tinRewards,
                    config.lockLidOpenSeconds,
                    config.lockLidOpenAngleDegrees,
                    tinStatusText,
                    tinDialTexts,
                    config.lockedContactCooldownSeconds);
            }
        }

        private static void ApplyClue(ClueItem clueItem, CodeRunData codeData, ClueLocation location)
        {
            if (clueItem == null || codeData == null)
                return;

            int slot = System.Array.IndexOf(codeData.ClueLocations, location);
            if (slot >= 0)
                clueItem.SetClue(codeData.SymbolAt(slot), codeData.Digits[slot]);
        }
    }
}
