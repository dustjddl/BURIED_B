using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace CoffinEscape
{
    public sealed class CodeLock : MonoBehaviour
    {
        [SerializeField] private int[] answer = new int[3];
        [SerializeField] private SymbolId[] symbols = new SymbolId[3];
        [SerializeField] private ResourceManager resources;
        [SerializeField] private Transform lid;
        [SerializeField] private GameObject[] rewards;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text[] dialValues = new TMP_Text[3];
        [SerializeField] private float openSeconds = 0.8f;
        [SerializeField] private float openAngle = 100f;
        [SerializeField] private float lockedContactCooldownSeconds = 0.5f;

        private CodeLockLogic logic;
        private bool opened;
        private float nextLockedContactTime;
        private int lastDialInteractionFrame = int.MinValue;
        private Quaternion initialLidRotation = Quaternion.identity;
        private bool hasInitialLidRotation;

        public event Action Opened;

        public bool IsOpened => opened;

        public void Configure(CodeRunData data, ResourceManager manager, Transform lidTransform, GameObject[] unlockables,
            float duration, float angle, TMP_Text label, TMP_Text[] digitLabels, float contactCooldown = 0.5f)
        {
            answer = (int[])data.Digits.Clone();
            symbols = (SymbolId[])data.Symbols.Clone();
            resources = manager;
            lid = lidTransform;
            rewards = unlockables;
            openSeconds = duration;
            openAngle = angle;
            lockedContactCooldownSeconds = Mathf.Max(0f, contactCooldown);
            status = label;
            dialValues = digitLabels;
            opened = false;
            nextLockedContactTime = 0f;
            logic = new CodeLockLogic(answer);

            if (lid != null)
            {
                if (!hasInitialLidRotation)
                {
                    initialLidRotation = lid.localRotation;
                    hasInitialLidRotation = true;
                }
                else
                {
                    lid.localRotation = initialLidRotation;
                }
            }

            if (status != null)
                status.text = "LOCKED";

            if (dialValues != null)
            {
                for (int i = 0; i < dialValues.Length; i++)
                {
                    if (dialValues[i] != null)
                        dialValues[i].text = "0";
                }
            }

            if (rewards != null)
            {
                for (int i = 0; i < rewards.Length; i++)
                {
                    if (rewards[i] != null)
                        rewards[i].SetActive(false);
                }
            }
        }

        private void Awake()
        {
            if (lid != null && !hasInitialLidRotation)
            {
                initialLidRotation = lid.localRotation;
                hasInitialLidRotation = true;
            }

            if (logic == null)
                logic = new CodeLockLogic(answer);
        }

        public CodeLockLogic.AttemptResult Confirm()
        {
            if (opened)
                return CodeLockLogic.AttemptResult.AlreadyTried;

            NotifyDialInteraction();
            CodeLockLogic.AttemptResult result = logic.Confirm();
            if (status != null)
            {
                status.text = result == CodeLockLogic.AttemptResult.Correct ? "OPEN"
                    : result == CodeLockLogic.AttemptResult.Wrong ? "WRONG"
                    : "TRIED";
            }

            if (result == CodeLockLogic.AttemptResult.Wrong)
                resources?.ApplyWrongInput();

            if (result == CodeLockLogic.AttemptResult.Correct)
            {
                opened = true;
                resources?.ApplyPuzzleProgress();
                if (rewards != null)
                {
                    foreach (GameObject reward in rewards)
                    {
                        if (reward != null)
                            reward.SetActive(true);
                    }
                }

                Opened?.Invoke();
                StartCoroutine(AnimateLid());
            }

            return result;
        }

        public void ChangeDial(int index, int delta)
        {
            if (opened)
                return;

            NotifyDialInteraction();
            logic.Change(index, delta);
            if (dialValues != null && index >= 0 && index < dialValues.Length && dialValues[index] != null)
                dialValues[index].text = logic.Dials[index].ToString();
        }

        public int GetDial(int index) => logic.Dials[index];

        public int[] GetDialsSnapshot() => new[] { logic.Dials[0], logic.Dials[1], logic.Dials[2] };

        public SymbolId GetSymbol(int slot) => symbols[slot];

        public void NotifyDialInteraction()
        {
            lastDialInteractionFrame = Time.frameCount;
        }

        public bool RegisterLockedContact(HandGrabToggle hand)
        {
            if (opened)
                return false;

            if (Time.frameCount - lastDialInteractionFrame <= 1)
                return false;

            if (Time.time < nextLockedContactTime)
                return false;

            nextLockedContactTime = Time.time + lockedContactCooldownSeconds;
            if (status != null)
                status.text = "LOCKED";
            resources?.ApplyLockedObjectClick();
            hand?.PulseContact(0.18f);
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (opened || !other.CompareTag("ControllerTip"))
                return;

            RegisterLockedContact(other.GetComponentInParent<HandGrabToggle>());
        }

        private IEnumerator AnimateLid()
        {
            if (lid == null)
                yield break;

            Quaternion start = lid.localRotation;
            Quaternion end = start * Quaternion.Euler(-openAngle, 0f, 0f);
            float time = 0f;
            while (time < openSeconds)
            {
                time += Time.deltaTime;
                lid.localRotation = Quaternion.Slerp(start, end, Mathf.Clamp01(time / openSeconds));
                yield return null;
            }

            lid.localRotation = end;
        }
    }
}
