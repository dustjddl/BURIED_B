using UnityEngine;

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

        [Header("Prototype scene")]
        [Min(0.1f)] public float coffinLengthMeters = 2f;
        [Min(0.1f)] public float coffinWidthMeters = 0.7f;
        [Min(0.1f)] public float coffinHeightMeters = 0.6f;
        [Min(0.005f)] public float coffinWallThicknessMeters = 0.04f;
        [Min(0f)] public float cameraAnchorHeightMeters = 0.25f;
        [Min(0f)] public float headAnchorForwardMeters = 0.18f;
        [Min(0.005f)] public float handInteractorRadiusMeters = 0.035f;
        [Range(0f, 1f)] public float controllerButtonThreshold = 0.5f;
        [Min(0.01f)] public float grabTestObjectSizeMeters = 0.1f;
        public Vector3 grabTestObjectPositionMeters = new Vector3(0.14f, 0.27f, 0.72f);
        public Vector3 lighterScaleMeters = new Vector3(0.045f, 0.12f, 0.045f);
        public Vector3 lighterFlameLocalPositionMeters = new Vector3(0f, 0.06f, 0f);
        [Min(0f)] public float lighterLightRangeMeters = 1.3f;
        [Min(0f)] public float ambientIntensity = 0.035f;
        public Color grayboxColor = new Color(0.42f, 0.42f, 0.42f, 1f);

        private void OnValidate()
        {
            oxygenDurationSeconds = Mathf.Max(0.01f, oxygenDurationSeconds);
            maximumStress = Mathf.Max(0.01f, maximumStress);
            initialStress = Mathf.Clamp(initialStress, 0f, maximumStress);
        }
    }
}
