using System;
using TMPro;
using UnityEngine;
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
            private Slider slider;

            [SerializeField]
            private TMP_Text valueText;

            public void Refresh(float value)
            {
                float clampedValue = Mathf.Clamp(
                    value,
                    0f,
                    100f);

                if (slider != null)
                {
                    slider.minValue = 0f;
                    slider.maxValue = 100f;

                    slider.SetValueWithoutNotify(
                        clampedValue);
                }

                if (valueText != null)
                {
                    valueText.text =
                        $"{clampedValue:0}/100";
                }
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
        [SerializeField]
        private NeedView fullnessView;

        [SerializeField]
        private NeedView energyView;

        [SerializeField]
        private NeedView happinessView;

        [SerializeField]
        private NeedView hygieneView;

        [Header("Care Buttons")]
        [SerializeField]
        private Button feedButton;

        [SerializeField]
        private Button restButton;

        [SerializeField]
        private Button playButton;

        [SerializeField]
        private Button cleanButton;

        [Header("Action Feedback")]
        [SerializeField]
        private TMP_Text actionStatusText;

        private PetSession petSession;
        private bool isBound;

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

            feedButton.onClick.AddListener(
                HandleFeedClicked);

            restButton.onClick.AddListener(
                HandleRestClicked);

            playButton.onClick.AddListener(
                HandlePlayClicked);

            cleanButton.onClick.AddListener(
                HandleCleanClicked);

            isBound = true;

            Refresh(petSession.CurrentPetState);

            if (actionStatusText != null)
            {
                actionStatusText.text =
                    "Choose a care action";
            }
        }

        private void HandleFeedClicked()
        {
            ExecuteCareAction(
                PetCareActionType.Feed);
        }

        private void HandleRestClicked()
        {
            ExecuteCareAction(
                PetCareActionType.Rest);
        }

        private void HandlePlayClicked()
        {
            ExecuteCareAction(
                PetCareActionType.Play);
        }

        private void HandleCleanClicked()
        {
            ExecuteCareAction(
                PetCareActionType.Clean);
        }

        private void ExecuteCareAction(
            PetCareActionType actionType)
        {
            if (!isBound)
            {
                return;
            }

            PerformPetCareActionResult result =
                GameEntryPoint.Instance.PerformCareAction(
                    actionType);

            PetCareActionResult careResult =
                result.CareResult;

            if (actionStatusText == null)
            {
                return;
            }

            string levelUpMessage =
                careResult.DidLevelUp
                    ? $" • Level Up! Lv.{careResult.CurrentLevel}"
                    : string.Empty;

            actionStatusText.text =
                $"{careResult.ActionType} completed • " +
                $"{careResult.PreviousNeedValue:0} → " +
                $"{careResult.CurrentNeedValue:0} • " +
                $"+{careResult.ExperienceEarned} XP" +
                levelUpMessage;
        }

        private void Refresh(PetState petState)
        {
            if (petState == null)
            {
                return;
            }

            petNameText.text =
                petState.Profile.DisplayName;

            levelText.text =
                $"Level {petState.Stats.Level}";

            experienceText.text =
                $"XP: {petState.Stats.CurrentExperience}";

            affectionText.text =
                $"Affection: {petState.Stats.Affection:0}";

            coinsText.text =
                $"Coins: {petState.Stats.Coins}";

            fullnessView.Refresh(
                petState.Needs.Fullness);

            energyView.Refresh(
                petState.Needs.Energy);

            happinessView.Refresh(
                petState.Needs.Happiness);

            hygieneView.Refresh(
                petState.Needs.Hygiene);
        }

        private void OnDestroy()
        {
            if (!isBound)
            {
                return;
            }

            petSession.StateChanged -= Refresh;

            feedButton.onClick.RemoveListener(
                HandleFeedClicked);

            restButton.onClick.RemoveListener(
                HandleRestClicked);

            playButton.onClick.RemoveListener(
                HandlePlayClicked);

            cleanButton.onClick.RemoveListener(
                HandleCleanClicked);

            isBound = false;
        }
    }
}