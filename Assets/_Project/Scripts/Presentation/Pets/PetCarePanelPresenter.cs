using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using SuccuPet.Application.Pets;
using SuccuPet.Bootstrap;
using SuccuPet.Core.Pets;

namespace SuccuPet.Presentation.Pets
{
    public sealed class PetCarePanelPresenter : MonoBehaviour
    {
        [Serializable]
        private sealed class NeedView
        {
            [SerializeField]
            private TMP_Text labelText;

            [SerializeField]
            private Slider slider;

            [SerializeField]
            private Image fillImage;

            [SerializeField]
            private TMP_Text valueText;

            public void Refresh(
                string label,
                float value,
                Color healthyColor,
                Color warningColor,
                Color criticalColor)
            {
                float clampedValue = Mathf.Clamp(
                    value,
                    PetNeeds.MinimumValue,
                    PetNeeds.MaximumValue);

                if (labelText != null)
                {
                    labelText.text = label;
                }

                if (slider != null)
                {
                    slider.minValue = PetNeeds.MinimumValue;
                    slider.maxValue = PetNeeds.MaximumValue;
                    slider.SetValueWithoutNotify(clampedValue);
                }

                Image targetFillImage = ResolveFillImage();

                if (targetFillImage != null)
                {
                    targetFillImage.color = GetStatusColor(
                        clampedValue,
                        healthyColor,
                        warningColor,
                        criticalColor);
                }

                if (valueText != null)
                {
                    valueText.text = string.Empty;

                    if (valueText.gameObject.activeSelf)
                    {
                        valueText.gameObject.SetActive(false);
                    }
                }
            }

            private Image ResolveFillImage()
            {
                if (fillImage != null)
                {
                    return fillImage;
                }

                if (slider == null || slider.fillRect == null)
                {
                    return null;
                }

                fillImage =
                    slider.fillRect.GetComponent<Image>();

                return fillImage;
            }

            private static Color GetStatusColor(
                float value,
                Color healthyColor,
                Color warningColor,
                Color criticalColor)
            {
                if (value >= 60f)
                {
                    return healthyColor;
                }

                if (value >= 30f)
                {
                    return warningColor;
                }

                return criticalColor;
            }
        }

        [Header("Pet Information")]
        [SerializeField]
        private TMP_Text petNameText;

        [SerializeField]
        private TMP_Text levelText;

        [SerializeField]
        private TMP_Text experienceText;

        [SerializeField]
        private TMP_Text affectionText;

        [SerializeField]
        private TMP_Text coinsText;

        [Header("Pet Needs")]
        [FormerlySerializedAs("fullnessView")]
        [SerializeField]
        private NeedView vitalityView;

        [FormerlySerializedAs("energyView")]
        [SerializeField]
        private NeedView restView;

        [FormerlySerializedAs("happinessView")]
        [SerializeField]
        private NeedView moodView;

        [FormerlySerializedAs("hygieneView")]
        [SerializeField]
        private NeedView allureView;

        [Header("Need Bar Colors")]
        [SerializeField]
        private Color healthyColor =
            new Color(0.20f, 0.78f, 0.35f, 1f);

        [SerializeField]
        private Color warningColor =
            new Color(1f, 0.76f, 0.15f, 1f);

        [SerializeField]
        private Color criticalColor =
            new Color(0.92f, 0.22f, 0.22f, 1f);

        [Header("Care Buttons")]
        [SerializeField]
        private Button feedButton;

        [FormerlySerializedAs("restButton")]
        [SerializeField]
        private Button sleepButton;

        [SerializeField]
        private Button playButton;

        [FormerlySerializedAs("cleanButton")]
        [SerializeField]
        private Button batheButton;

        [SerializeField]
        private TMP_Text sleepButtonText;

        [Header("Sleep Presentation")]
        [SerializeField]
        private Animator petAnimator;

        [SerializeField]
        private string sleepTriggerName = "Sleep";

        [SerializeField]
        private string wakeTriggerName = "Wake";

        [Header("Action Feedback")]
        [SerializeField]
        private TMP_Text actionStatusText;

        [Header("Action Cooldown")]
        [Min(0f)]
        [SerializeField]
        private float actionCooldownSeconds = 1f;

        private PetSession petSession;
        private Coroutine cooldownCoroutine;
        private bool isBound;
        private bool isActionOnCooldown;

        private void Start()
        {
            GameEntryPoint entryPoint =
                GameEntryPoint.Instance;

            if (entryPoint == null)
            {
                Debug.LogError(
                    "GameEntryPoint could not be found.",
                    this);

                enabled = false;
                return;
            }

            if (!entryPoint.IsReady)
            {
                Debug.LogError(
                    "GameEntryPoint is not ready.",
                    this);

                enabled = false;
                return;
            }

            Bind(entryPoint.PetSession);
        }

        private void Bind(PetSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(
                    nameof(session));
            }

            petSession = session;
            petSession.StateChanged += Refresh;

            AddButtonListener(
                feedButton,
                HandleFeedClicked);

            AddButtonListener(
                sleepButton,
                HandleSleepClicked);

            AddButtonListener(
                playButton,
                HandlePlayClicked);

            AddButtonListener(
                batheButton,
                HandleBatheClicked);

            isBound = true;

            RefreshSleepingPresentation(
                petSession.CurrentPetState.IsSleeping,
                false);
            Refresh(petSession.CurrentPetState);

            RefreshConditionStatus(
                petSession.CurrentPetState);
        }

        private void HandleFeedClicked()
        {
            TryExecuteCareAction(
                PetCareActionType.Feed);
        }

        private void HandleSleepClicked()
        {
            if (!isBound || isActionOnCooldown)
            {
                return;
            }

            if (petSession.CurrentPetState.IsInComa)
            {
                SetActionStatus(
                    "Your pet cannot be woken manually during recovery.");
                return;
            }

            bool isSleeping =
                petSession.CurrentPetState.IsSleeping;

            if (isSleeping)
            {
                GameEntryPoint.Instance.SetPetSleeping(false);
                RefreshSleepingPresentation(false, true);
                SetActionStatus("Your pet is awake.");
                BeginActionCooldown();
                return;
            }

            GameEntryPoint.Instance.SetPetSleeping(true);
            RefreshSleepingPresentation(true, true);
            SetActionStatus("Your pet is sleeping.");

            BeginActionCooldown();
        }

        private void HandlePlayClicked()
        {
            TryExecuteCareAction(
                PetCareActionType.Play);
        }

        private void HandleBatheClicked()
        {
            TryExecuteCareAction(
                PetCareActionType.Bathe);
        }

        private void TryExecuteCareAction(
            PetCareActionType actionType)
        {
            if (!isBound ||
                petSession.CurrentPetState.IsSleeping ||
                isActionOnCooldown)
            {
                return;
            }

            ExecuteCareAction(actionType);
            BeginActionCooldown();
        }

        private PetCareActionResult ExecuteCareAction(
            PetCareActionType actionType)
        {
            PerformPetCareActionResult result =
                GameEntryPoint.Instance.PerformCareAction(
                    actionType);

            PetCareActionResult careResult =
                result.CareResult;

            if (!careResult.IsSuccessful)
            {
                SetActionStatus(careResult.Message);
                return careResult;
            }

            string actionName =
                PetCarePolicy.GetActionDisplayName(
                    careResult.ActionType);

            string levelUpMessage =
                careResult.DidLevelUp
                    ? $" Level Up! Lv.{careResult.CurrentLevel}"
                    : string.Empty;

            SetActionStatus(
                $"{actionName} completed.{levelUpMessage}");

            return careResult;
        }

        private void RefreshSleepingPresentation(
            bool shouldSleep,
            bool playAnimation)
        {
            bool isInComa =
                isBound &&
                petSession.CurrentPetState.IsInComa;

            if (sleepButtonText != null)
            {
                sleepButtonText.text =
                    isInComa
                        ? "Coma"
                        : shouldSleep
                        ? "Wake"
                        : "Sleep";
            }

            if (playAnimation && !isInComa)
            {
                TrySetAnimatorTrigger(
                    shouldSleep
                        ? sleepTriggerName
                        : wakeTriggerName);
            }

            RefreshButtonStates();
        }

        private void TrySetAnimatorTrigger(
            string triggerName)
        {
            if (petAnimator == null ||
                string.IsNullOrWhiteSpace(triggerName))
            {
                return;
            }

            AnimatorControllerParameter[] parameters =
                petAnimator.parameters;

            for (int index = 0;
                index < parameters.Length;
                index++)
            {
                AnimatorControllerParameter parameter =
                    parameters[index];

                if (parameter.type ==
                        AnimatorControllerParameterType.Trigger &&
                    parameter.name == triggerName)
                {
                    petAnimator.SetTrigger(triggerName);
                    return;
                }
            }
        }

        private void BeginActionCooldown()
        {
            if (cooldownCoroutine != null)
            {
                StopCoroutine(cooldownCoroutine);
            }

            cooldownCoroutine =
                StartCoroutine(ActionCooldownRoutine());
        }

        private IEnumerator ActionCooldownRoutine()
        {
            isActionOnCooldown = true;
            RefreshButtonStates();

            if (actionCooldownSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    actionCooldownSeconds);
            }

            isActionOnCooldown = false;
            cooldownCoroutine = null;
            RefreshButtonStates();
        }

        private void RefreshButtonStates()
        {
            bool isInComa =
                isBound &&
                petSession.CurrentPetState.IsInComa;

            bool canUseStandardCare =
                isBound &&
                !petSession.CurrentPetState.IsSleeping &&
                !isActionOnCooldown;

            SetButtonInteractable(
                feedButton,
                canUseStandardCare);

            SetButtonInteractable(
                playButton,
                canUseStandardCare);

            SetButtonInteractable(
                batheButton,
                canUseStandardCare);

            SetButtonInteractable(
                sleepButton,
                isBound &&
                !isInComa &&
                !isActionOnCooldown);
        }

        private void Refresh(PetState petState)
        {
            if (petState == null)
            {
                return;
            }

            RefreshSleepingPresentation(
                petState.IsSleeping,
                false);

            SetText(
                petNameText,
                petState.Profile.DisplayName);

            SetText(
                levelText,
                $"Level {petState.Stats.Level}");

            SetText(
                experienceText,
                $"XP: {petState.Stats.CurrentExperience}");

            SetText(
                affectionText,
                $"Affection: {petState.Stats.Affection:0}");

            SetText(
                coinsText,
                $"Coins: {petState.Stats.Coins}");

            RefreshNeedView(
                vitalityView,
                "Vitality",
                petState.Needs.Vitality);

            RefreshNeedView(
                restView,
                "Rest",
                petState.Needs.Rest);

            RefreshNeedView(
                moodView,
                "Mood",
                petState.Needs.Mood);

            RefreshNeedView(
                allureView,
                "Allure",
                petState.Needs.Allure);

            RefreshConditionStatus(petState);
        }

        private void RefreshConditionStatus(PetState petState)
        {
            if (petState.IsInComa)
            {
                bool recoveryCareReady =
                    petState.Needs.Vitality > 50f &&
                    petState.Needs.Rest > 50f &&
                    petState.Needs.Mood > 50f &&
                    petState.Needs.Allure > 50f;

                SetActionStatus(
                    recoveryCareReady
                        ? "Recovery care is working. Keep every need above halfway."
                        : "Your pet is in a care coma. Restore every need above halfway.");
                return;
            }

            switch (petState.Health.Status)
            {
                case PetHealthStatus.Critical:
                    SetActionStatus(
                        "Your pet is barely responding. Give consistent care now.");
                    break;

                case PetHealthStatus.Fatigued:
                    SetActionStatus(
                        "Your pet looks fatigued and needs steadier care.");
                    break;

                default:
                    SetActionStatus(
                        petState.IsSleeping
                            ? "Your pet is sleeping."
                            : "Your pet feels well cared for.");
                    break;
            }
        }

        private void RefreshNeedView(
            NeedView view,
            string label,
            float value)
        {
            if (view == null)
            {
                return;
            }

            view.Refresh(
                label,
                value,
                healthyColor,
                warningColor,
                criticalColor);
        }

        private void SetActionStatus(string message)
        {
            SetText(actionStatusText, message);
        }

        private static void AddButtonListener(
            Button button,
            UnityEngine.Events.UnityAction listener)
        {
            if (button != null)
            {
                button.onClick.AddListener(listener);
            }
        }

        private static void RemoveButtonListener(
            Button button,
            UnityEngine.Events.UnityAction listener)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(listener);
            }
        }

        private static void SetButtonInteractable(
            Button button,
            bool isInteractable)
        {
            if (button != null)
            {
                button.interactable = isInteractable;
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

        private void OnDestroy()
        {
            if (cooldownCoroutine != null)
            {
                StopCoroutine(cooldownCoroutine);
                cooldownCoroutine = null;
            }

            if (!isBound)
            {
                return;
            }

            petSession.StateChanged -= Refresh;

            RemoveButtonListener(
                feedButton,
                HandleFeedClicked);

            RemoveButtonListener(
                sleepButton,
                HandleSleepClicked);

            RemoveButtonListener(
                playButton,
                HandlePlayClicked);

            RemoveButtonListener(
                batheButton,
                HandleBatheClicked);

            isBound = false;
        }
    }
}
