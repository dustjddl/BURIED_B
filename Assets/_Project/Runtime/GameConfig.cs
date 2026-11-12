using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace CoffinEscape
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "CoffinEscape/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Oxygen and fuel")]
        [Min(0.01f)] public float oxygenDurationSeconds = 840f;
        [Min(0f)] public float oxygenDrainRate = 1f;
        [Min(1f)] public float oxygenMultiplierWhileLighterLit = 1.25f;
        [Range(0f, 100f)] public float panicStressThreshold = 80f;
        [Min(1f)] public float panicOxygenMultiplier = 2f;
        [Min(0f)] public float lighterFuelDurationSeconds = 300f;

        [Header("Stress")]
        [Range(0f, 100f)] public float initialStress = 20f;
        [Min(0.01f)] public float maximumStress = 100f;
        [Min(0f)] public float darkStressDelaySeconds = 10f;
        [Min(0f)] public float darkStressPerSecond = 1f;
        [Min(0f)] public float wrongInputStress = 8f;
        [Min(0f)] public float lockedObjectStress = 3f;
        [Min(0f)] public float puzzleProgressStressReduction = 10f;
        [Min(0f)] public float lighterStressRecoveryPerSecond = 0.5f;

        [Header("Lighter")]
        [Min(0f)] public float lighterLightIntensity = 1.5f;

        [Header("Prototype visibility")]
        public Color coffinWallColor = new Color(0.24f, 0.27f, 0.32f, 1f);
        public Color coffinFloorColor = new Color(0.16f, 0.18f, 0.22f, 1f);
        public Color coffinLidColor = new Color(0.29f, 0.31f, 0.35f, 1f);
        public Color testCubeColor = new Color(0.42f, 0.66f, 0.84f, 1f);
        public Color testSphereColor = new Color(0.82f, 0.48f, 0.34f, 1f);
        public Color lighterBodyColor = new Color(0.91f, 0.66f, 0.28f, 1f);
        [Min(0.005f)] public float cameraBoundaryMarginMeters = 0.06f;
        [Min(0.005f)] public float controllerBoundaryMarginMeters = 0.04f;
        [Min(0f)] public float xrHardwareDetectionTimeoutSeconds = 2f;

        [Header("Prototype scene")]
        [Header("M3 puzzle tuning")]
        [Min(0.01f)] public float clueReadDistanceMeters = 0.95f;
        [Min(0f)] public float dialButtonCooldownSeconds = 0.3f;
        [Min(0f)] public float oilFuelBonusSeconds = 120f;
        [Min(0f)] public float lockLidOpenSeconds = 0.8f;
        [Range(0f, 180f)] public float lockLidOpenAngleDegrees = 100f;
        [Min(0.01f)] public float puzzleItemSizeMeters = 0.12f;
        [Min(0.01f)] public float tinCanSizeMeters = 0.25f;
        public Vector3 pileAPositionMeters = new Vector3(-0.17f, 0.08f, 1.72f);
        public Vector3 pileBPositionMeters = new Vector3(0.17f, 0.08f, 1.72f);
        public Vector3 tinCanPositionMeters = new Vector3(0.25f, 0.2f, 1.1f);
        public Key debugRegenerateRunKey = Key.F5;
        public Key debugRevealAnswersKey = Key.F6;

        [Min(0.1f)] public float coffinLengthMeters = 2f;
        [Min(0.1f)] public float coffinWidthMeters = 0.7f;
        [Min(0.1f)] public float coffinHeightMeters = 0.6f;
        [Min(0.005f)] public float coffinWallThicknessMeters = 0.04f;
        [Min(0f)] public float cameraAnchorHeightMeters = 0.25f;
        [Min(0f)] public float headAnchorForwardMeters = 0.18f;
        [Min(0.005f)] public float handInteractorRadiusMeters = 0.035f;
        [Min(0.01f)] public float grabSelectionRadiusMeters = 0.2f;
        [Min(0.001f)] public float controllerTipRadiusMeters = 0.012f;
        public Vector3 controllerTipLocalPositionMeters = new Vector3(0f, 0f, 0.08f);
        public Vector3 controllerHandLocalPositionMeters = new Vector3(0f, -0.025f, 0.055f);
        public Vector3 controllerHandPalmSizeMeters = new Vector3(0.075f, 0.035f, 0.085f);
        [Min(0.005f)] public float controllerHandFingerLengthMeters = 0.055f;
        [Min(0.002f)] public float controllerHandFingerThicknessMeters = 0.012f;
        [Range(0f, 120f)] public float controllerHandFingerCurlDegrees = 75f;
        public Color controllerHandColor = new Color(0.76f, 0.58f, 0.42f, 1f);
        [Range(0f, 1f)] public float controllerButtonThreshold = 0.5f;
        [Range(0f, 1f)] public float emptyGrabHapticAmplitude = 0.12f;
        [Min(0f)] public float contactHapticDurationSeconds = 0.05f;
        [Range(0f, 1f)] public float wallContactHapticAmplitude = 0.08f;
        [Min(0.01f)] public float grabTestObjectSizeMeters = 0.1f;
        public Vector3 cubeTestObjectPositionMeters = new Vector3(-0.16f, 0.22f, 0.66f);
        public Vector3 sphereTestObjectPositionMeters = new Vector3(0.16f, 0.22f, 0.66f);
        public Vector3 grabTestObjectPositionMeters = new Vector3(0.14f, 0.27f, 0.72f);
        public Vector3 lighterScaleMeters = new Vector3(0.045f, 0.12f, 0.045f);
        public Vector3 lighterFlameLocalPositionMeters = new Vector3(0f, 0.06f, 0f);
        public Vector3 lighterReturnAnchorLocalPositionMeters = new Vector3(0.05f, 0.3f, 0.5f);
        public XRNode lighterStartHand = XRNode.RightHand;
        public Key leftHandDebugGripKey = Key.Q;
        public Key rightHandDebugGripKey = Key.E;
        public Key debugTriggerKey = Key.T;
        public Key debugCalibrateKey = Key.R;
        [Min(0)] public int startupCalibrationDelayFrames = 2;
        [Min(0f)] public float lighterLightRangeMeters = 1.3f;
        [Min(0f)] public float ambientIntensity = 0.15f;
        public Color grayboxColor = new Color(0.42f, 0.42f, 0.42f, 1f);

        private void OnValidate()
        {
            oxygenDurationSeconds = Mathf.Max(0.01f, oxygenDurationSeconds);
            maximumStress = Mathf.Max(0.01f, maximumStress);
            initialStress = Mathf.Clamp(initialStress, 0f, maximumStress);
        }
    }
}
