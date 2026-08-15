using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SuccuPet.Core.Pets;

namespace SuccuPet.Presentation.Pets
{
    public sealed class FirstCareTutorialPresenter : MonoBehaviour
    {
        private enum TutorialStep
        {
            Feed = 0,
            Bathe = 1,
            Play = 2,
            Sleep = 3,
            Wake = 4,
            Complete = 5
        }

        private const string PendingKey =
            "FIRST_CARE_TUTORIAL_PENDING";

        private const string CompletedKey =
            "FIRST_CARE_TUTORIAL_COMPLETED";

        private const int TotalSteps = 5;

        [Header("Tutorial Root")]
        [SerializeField]
        private GameObject tutorialOverlay;

        [SerializeField]
        private Canvas tutorialCanvas;

        [Header("Tutorial Text")]
        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text messageText;

        [SerializeField]
        private TMP_Text stepText;

        [Header("Highlight")]
        [SerializeField]
        private RectTransform highlightFrame;

        [Min(0f)]
        [SerializeField]
        private float highlightPadding = 18f;

        [Min(0f)]
        [SerializeField]
        private float pulseAmount = 0.05f;

        [Min(0.1f)]
        [SerializeField]
        private float pulseSpeed = 2.5f;

        [Header("Care Button Targets")]
        [SerializeField]
        private RectTransform feedButtonTarget;

        [SerializeField]
        private RectTransform batheButtonTarget;

        [SerializeField]
        private RectTransform playButtonTarget;

        [SerializeField]
        private RectTransform sleepButtonTarget;

        [Header("Controls")]
        [SerializeField]
        private Button skipButton;

        [Header("Completion")]
        [Min(0f)]
        [SerializeField]
        private float completionDisplaySeconds = 1.1f;

        private TutorialStep currentStep;
        private Coroutine completionCoroutine;
        private Coroutine highlightCoroutine;
        private Vector3 highlightBaseScale = Vector3.one;
        private bool isInitialized;
        private bool isTutorialActive;

        public bool IsTutorialActive => isTutorialActive;

        public event Action StateChanged;

        private void Awake()
        {
            if (tutorialCanvas == null)
            {
                tutorialCanvas = GetComponentInParent<Canvas>();
            }

            if (highlightFrame != null)
            {
                highlightBaseScale = highlightFrame.localScale;
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(
                    HandleSkipClicked);
            }

            SetOverlayVisible(false);
        }

        private void OnEnable()
        {
            // PetCarePanel is disabled during egg selection and hatching.
            // Re-check the persisted pending flag every time the panel is
            // opened so the tutorial starts immediately after hatching.
            Initialize();
        }

        private void Update()
        {
            if (!isTutorialActive ||
                currentStep == TutorialStep.Complete ||
                highlightFrame == null)
            {
                return;
            }

            float pulse =
                1f +
                Mathf.Sin(Time.unscaledTime * pulseSpeed) *
                pulseAmount;

            highlightFrame.localScale =
                highlightBaseScale * pulse;
        }

        public static void MarkPendingForNewPet()
        {
            PlayerPrefs.SetInt(PendingKey, 1);
            PlayerPrefs.SetInt(CompletedKey, 0);
            PlayerPrefs.Save();
        }

        public void Initialize()
        {
            if (isTutorialActive)
            {
                return;
            }

            isInitialized = true;

            bool isPending =
                PlayerPrefs.GetInt(PendingKey, 0) == 1;

            bool isCompleted =
                PlayerPrefs.GetInt(CompletedKey, 0) == 1;

            if (!isPending || isCompleted)
            {
                SetOverlayVisible(false);
                return;
            }

            BeginTutorial();
        }

        public bool AllowsCareAction(
            PetCareActionType actionType)
        {
            if (!isTutorialActive)
            {
                return true;
            }

            switch (currentStep)
            {
                case TutorialStep.Feed:
                    return actionType == PetCareActionType.Feed;

                case TutorialStep.Bathe:
                    return actionType == PetCareActionType.Bathe;

                case TutorialStep.Play:
                    return actionType == PetCareActionType.Play;

                default:
                    return false;
            }
        }

        public bool AllowsSleepToggle(bool isCurrentlySleeping)
        {
            if (!isTutorialActive)
            {
                return true;
            }

            if (currentStep == TutorialStep.Sleep)
            {
                return !isCurrentlySleeping;
            }

            if (currentStep == TutorialStep.Wake)
            {
                return isCurrentlySleeping;
            }

            return false;
        }

        public void NotifyCareActionSucceeded(
            PetCareActionType actionType)
        {
            if (!isTutorialActive ||
                !AllowsCareAction(actionType))
            {
                return;
            }

            switch (currentStep)
            {
                case TutorialStep.Feed:
                    ShowStep(TutorialStep.Bathe);
                    break;

                case TutorialStep.Bathe:
                    ShowStep(TutorialStep.Play);
                    break;

                case TutorialStep.Play:
                    ShowStep(TutorialStep.Sleep);
                    break;
            }
        }

        public void NotifySleepStateChanged(bool isSleeping)
        {
            if (!isTutorialActive)
            {
                return;
            }

            if (currentStep == TutorialStep.Sleep && isSleeping)
            {
                ShowStep(TutorialStep.Wake);
                return;
            }

            if (currentStep == TutorialStep.Wake && !isSleeping)
            {
                CompleteTutorial();
            }
        }

        public bool IsButtonAllowed(
            PetCareActionType actionType,
            bool isCurrentlySleeping)
        {
            if (actionType == PetCareActionType.Sleep)
            {
                return AllowsSleepToggle(isCurrentlySleeping);
            }

            return AllowsCareAction(actionType);
        }

        private void BeginTutorial()
        {
            isTutorialActive = true;
            SetOverlayVisible(true);

            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(true);
                skipButton.interactable = true;
            }

            SetText(titleText, "FIRST CARE");
            ShowStep(TutorialStep.Feed);
        }

        private void ShowStep(TutorialStep step)
        {
            currentStep = step;

            RectTransform target = null;
            string message = string.Empty;
            int visibleStep = 1;

            switch (step)
            {
                case TutorialStep.Feed:
                    target = feedButtonTarget;
                    visibleStep = 1;
                    message =
                        "Let's care for your new companion.\n" +
                        "Tap FEED to restore Vitality.";
                    break;

                case TutorialStep.Bathe:
                    target = batheButtonTarget;
                    visibleStep = 2;
                    message =
                        "Great! Now tap BATHE to restore Allure.";
                    break;

                case TutorialStep.Play:
                    target = playButtonTarget;
                    visibleStep = 3;
                    message =
                        "Perfect. Tap PLAY to improve Mood.";
                    break;

                case TutorialStep.Sleep:
                    target = sleepButtonTarget;
                    visibleStep = 4;
                    message =
                        "Your companion needs rest too.\n" +
                        "Tap SLEEP.";
                    break;

                case TutorialStep.Wake:
                    target = sleepButtonTarget;
                    visibleStep = 5;
                    message =
                        "Your companion is sleeping.\n" +
                        "Tap WAKE to finish.";
                    break;
            }

            SetText(messageText, message);
            SetText(
                stepText,
                $"STEP {visibleStep} OF {TotalSteps}");

            QueueHighlightMove(target);
            StateChanged?.Invoke();
        }

        private void CompleteTutorial()
        {
            CancelPendingHighlightMove();

            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.SetInt(PendingKey, 0);
            PlayerPrefs.Save();

            currentStep = TutorialStep.Complete;

            SetText(titleText, "TUTORIAL COMPLETE");
            SetText(
                messageText,
                "Excellent! You now know how to care " +
                "for your companion.");
            SetText(stepText, "READY TO CARE");

            SetHighlightVisible(false);

            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(false);
            }

            StateChanged?.Invoke();

            if (completionCoroutine != null)
            {
                StopCoroutine(completionCoroutine);
            }

            completionCoroutine =
                StartCoroutine(CompleteAfterDelay());
        }

        private IEnumerator CompleteAfterDelay()
        {
            if (completionDisplaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    completionDisplaySeconds);
            }

            completionCoroutine = null;
            EndTutorialPresentation();
        }

        private void HandleSkipClicked()
        {
            if (!isTutorialActive)
            {
                return;
            }

            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.SetInt(PendingKey, 0);
            PlayerPrefs.Save();

            EndTutorialPresentation();
        }

        private void EndTutorialPresentation()
        {
            CancelPendingHighlightMove();
            isTutorialActive = false;
            SetHighlightVisible(false);
            SetOverlayVisible(false);
            StateChanged?.Invoke();
        }

        private void QueueHighlightMove(RectTransform target)
        {
            CancelPendingHighlightMove();

            if (target == null)
            {
                SetHighlightVisible(false);
                return;
            }

            SetHighlightVisible(false);
            highlightCoroutine =
                StartCoroutine(MoveHighlightAfterLayout(target));
        }

        private IEnumerator MoveHighlightAfterLayout(
            RectTransform target)
        {
            // PetCarePanel becomes active on the same frame that the
            // tutorial begins. Wait until Unity has calculated the
            // action-button layout before reading target.rect.
            yield return null;

            Canvas.ForceUpdateCanvases();

            RectTransform targetParent =
                target.parent as RectTransform;

            if (targetParent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    targetParent);
            }

            RectTransform highlightParent =
                highlightFrame != null
                    ? highlightFrame.parent as RectTransform
                    : null;

            if (highlightParent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    highlightParent);
            }

            Canvas.ForceUpdateCanvases();

            highlightCoroutine = null;

            if (!isTutorialActive ||
                currentStep == TutorialStep.Complete ||
                target == null ||
                !target.gameObject.activeInHierarchy)
            {
                SetHighlightVisible(false);
                yield break;
            }

            MoveHighlightTo(target);
        }

        private void CancelPendingHighlightMove()
        {
            if (highlightCoroutine == null)
            {
                return;
            }

            StopCoroutine(highlightCoroutine);
            highlightCoroutine = null;
        }

        private void MoveHighlightTo(RectTransform target)
        {
            if (highlightFrame == null || target == null)
            {
                SetHighlightVisible(false);
                return;
            }

            RectTransform highlightParent =
                highlightFrame.parent as RectTransform;

            if (highlightParent == null)
            {
                SetHighlightVisible(false);
                return;
            }

            // Calculate the target directly in the HighlightFrame parent's
            // local coordinate space. This avoids screen/camera conversion
            // and works correctly with CanvasScaler, anchors and pivots.
            Bounds targetBounds =
                RectTransformUtility.CalculateRelativeRectTransformBounds(
                    highlightParent,
                    target);

            highlightFrame.anchorMin = new Vector2(0.5f, 0.5f);
            highlightFrame.anchorMax = new Vector2(0.5f, 0.5f);
            highlightFrame.pivot = new Vector2(0.5f, 0.5f);

            highlightFrame.sizeDelta = new Vector2(
                targetBounds.size.x + highlightPadding * 2f,
                targetBounds.size.y + highlightPadding * 2f);

            Vector3 frameLocalPosition =
                highlightFrame.localPosition;

            frameLocalPosition.x = targetBounds.center.x;
            frameLocalPosition.y = targetBounds.center.y;
            highlightFrame.localPosition = frameLocalPosition;

            highlightFrame.localScale = highlightBaseScale;
            SetHighlightVisible(true);
        }

        private void SetOverlayVisible(bool isVisible)
        {
            if (tutorialOverlay != null)
            {
                tutorialOverlay.SetActive(isVisible);
            }
        }

        private void SetHighlightVisible(bool isVisible)
        {
            if (highlightFrame != null)
            {
                highlightFrame.gameObject.SetActive(isVisible);

                if (!isVisible)
                {
                    highlightFrame.localScale =
                        highlightBaseScale;
                }
            }
        }

        private static void SetText(
            TMP_Text target,
            string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        [ContextMenu("Reset First-Care Tutorial Progress")]
        private void ResetTutorialProgressForTesting()
        {
            PlayerPrefs.DeleteKey(PendingKey);
            PlayerPrefs.DeleteKey(CompletedKey);
            PlayerPrefs.Save();

            isInitialized = false;
            isTutorialActive = false;
            SetOverlayVisible(false);
        }

        private void OnDestroy()
        {
            CancelPendingHighlightMove();

            if (completionCoroutine != null)
            {
                StopCoroutine(completionCoroutine);
                completionCoroutine = null;
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(
                    HandleSkipClicked);
            }
        }
    }
}