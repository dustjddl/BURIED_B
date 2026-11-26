using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class HangingTinCanController : MonoBehaviour
{
    [Header("Part References")]
    [SerializeField] private Transform bottomLid;
    [SerializeField] private Transform latchHasp;
    [SerializeField] private Transform combinationPadlock;

    [Header("Open Settings")]
    [Tooltip("Local X rotation in degrees to swing Bottom_Lid open downward.")]
    [SerializeField] private float openAngle = 105f;
    [SerializeField] private float haspReleaseAngle = -25f;
    [SerializeField] private float openDuration = 0.45f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Padlock Behavior")]
    [SerializeField] private bool dropPadlockOnUnlock = true;

    [Header("Events")]
    public UnityEvent onUnlockedAndOpened;

    public bool IsOpen { get; private set; }

    private Quaternion _lidClosedLocalRot;
    private Quaternion _haspClosedLocalRot;
    private Vector3 _padlockClosedLocalPos;
    private Quaternion _padlockClosedLocalRot;
    private Transform _padlockOriginalParent;
    private Coroutine _animCoroutine;
    private bool _cachedRestPose;

    private void Awake()
    {
        AutoFindPartsIfNeeded();
        CacheRestPose();
    }

    private void Reset()
    {
        AutoFindPartsIfNeeded();
    }

    private void AutoFindPartsIfNeeded()
    {
        if (bottomLid == null) bottomLid = FindDeepChild(transform, "Bottom_Lid");
        if (latchHasp == null) latchHasp = FindDeepChild(transform, "Latch_Hasp");
        if (combinationPadlock == null) combinationPadlock = FindDeepChild(transform, "Combination_Padlock");
    }

    private void CacheRestPose()
    {
        if (_cachedRestPose) return;
        if (bottomLid != null) _lidClosedLocalRot = bottomLid.localRotation;
        if (latchHasp != null) _haspClosedLocalRot = latchHasp.localRotation;
        if (combinationPadlock != null)
        {
            _padlockOriginalParent = combinationPadlock.parent;
            _padlockClosedLocalPos = combinationPadlock.localPosition;
            _padlockClosedLocalRot = combinationPadlock.localRotation;
        }
        _cachedRestPose = true;
    }

    [ContextMenu("Unlock And Open")]
    public void UnlockAndOpen()
    {
        if (IsOpen) return;
        AutoFindPartsIfNeeded();
        CacheRestPose();
        IsOpen = true;

        if (combinationPadlock != null)
        {
            if (dropPadlockOnUnlock && Application.isPlaying)
            {
                combinationPadlock.SetParent(null, true);
                var rb = combinationPadlock.GetComponent<Rigidbody>();
                if (rb == null) rb = combinationPadlock.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = false;
            }
            else
            {
                combinationPadlock.gameObject.SetActive(false);
            }
        }

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        if (Application.isPlaying)
        {
            _animCoroutine = StartCoroutine(AnimateOpenRoutine());
        }
        else
        {
            ApplyOpenPose(1f);
        }
    }

    [ContextMenu("Close And Lock")]
    public void CloseAndLock()
    {
        AutoFindPartsIfNeeded();
        CacheRestPose();
        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }

        IsOpen = false;
        if (bottomLid != null) bottomLid.localRotation = _lidClosedLocalRot;
        if (latchHasp != null) latchHasp.localRotation = _haspClosedLocalRot;

        if (combinationPadlock != null)
        {
            var rb = combinationPadlock.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            if (_padlockOriginalParent != null) combinationPadlock.SetParent(_padlockOriginalParent, false);
            combinationPadlock.localPosition = _padlockClosedLocalPos;
            combinationPadlock.localRotation = _padlockClosedLocalRot;
            combinationPadlock.gameObject.SetActive(true);
        }
    }

    private IEnumerator AnimateOpenRoutine()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, openDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyOpenPose(openCurve.Evaluate(t));
            yield return null;
        }
        ApplyOpenPose(1f);
        _animCoroutine = null;
        onUnlockedAndOpened?.Invoke();
    }

    private void ApplyOpenPose(float t)
    {
        if (latchHasp != null)
        {
            float hT = Mathf.Clamp01(t * 2.5f);
            latchHasp.localRotation = _haspClosedLocalRot * Quaternion.Euler(haspReleaseAngle * hT, 0f, 0f);
        }
        if (bottomLid != null)
        {
            bottomLid.localRotation = _lidClosedLocalRot * Quaternion.Euler(openAngle * t, 0f, 0f);
        }
    }

    private static Transform FindDeepChild(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName) return child;
            var found = FindDeepChild(child, targetName);
            if (found != null) return found;
        }
        return null;
    }
}
