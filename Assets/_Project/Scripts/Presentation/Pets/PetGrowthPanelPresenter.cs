using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SuccuPet.Application.Pets;
using SuccuPet.Bootstrap;
using SuccuPet.Core.Pets;

namespace SuccuPet.Presentation.Pets
{
    public sealed class PetGrowthPanelPresenter : MonoBehaviour
    {
        [Header("Growth Text")]
        [SerializeField]
        private TMP_Text stageText;

        [SerializeField]
        private TMP_Text variantText;

        [SerializeField]
        private TMP_Text growthText;

        [SerializeField]
        private TMP_Text trainingText;

        [SerializeField]
        private TMP_Text statusText;

        [Header("Growth Progress")]
        [SerializeField]
        private Slider growthSlider;

        [SerializeField]
        private Button evolveButton;

        [Tooltip("Temporary test hook. Replace this with the School/Gym activity result later.")]
        [SerializeField]
        private Button testTrainingButton;

        [Header("Evolution Animation Hooks")]
        [SerializeField]
        private Animator evolutionAnimator;

        [SerializeField]
        private string gateOneTrigger = "GateOneEvolution";

        [SerializeField]
        private string gateTwoDefaultTrigger = "GateTwoDefault";

        [SerializeField]
        private string gateTwoSpecialTrigger = "GateTwoSpecial";

        private readonly PetGrowthPolicy growthPolicy =
            PetGrowthPolicy.Default;

        private PetSession petSession;
        private bool isBound;

        private void Start()
        {
            GameEntryPoint entryPoint = GameEntryPoint.Instance;

            if (entryPoint == null || !entryPoint.IsReady)
            {
                Debug.LogError(
                    "GameEntryPoint is not ready for the growth panel.",
                    this);
                enabled = false;
                return;
            }

            petSession = entryPoint.PetSession;
            petSession.StateChanged += Refresh;

            if (evolveButton != null)
            {
                evolveButton.onClick.AddListener(
                    HandleEvolveClicked);
            }

            if (testTrainingButton != null)
            {
                testTrainingButton.onClick.AddListener(
                    HandleTestTrainingClicked);
            }

            isBound = true;
            Refresh(petSession.CurrentPetState);
        }

        private void HandleEvolveClicked()
        {
            if (!isBound)
            {
                return;
            }

            PetEvolutionResult result =
                GameEntryPoint.Instance.TryEvolvePet();

            SetText(statusText, result.Message);

            if (result.IsSuccessful)
            {
                PlayEvolutionAnimation(result);
            }
        }

        private void HandleTestTrainingClicked()
        {
            if (!isBound)
            {
                return;
            }

            PetTrainingResult result =
                GameEntryPoint.Instance.RegisterTeenTrainingSession();

            SetText(statusText, result.Message);
        }

        private void Refresh(PetState petState)
        {
            if (petState == null)
            {
                return;
            }

            PetGrowthState growth = petState.Growth;
            int requiredPoints = growthPolicy.GetRequiredGrowthPoints(
                growth.Stage);

            SetText(
                stageText,
                $"STAGE: {GetStageDisplayName(growth.Stage)}");

            SetText(
                variantText,
                GetVariantDisplayName(
                    growth.Stage,
                    growth.Variant));

            if (requiredPoints > 0)
            {
                int visiblePoints = Math.Min(
                    growth.GrowthPoints,
                    requiredPoints);

                SetText(
                    growthText,
                    $"GROWTH {visiblePoints}/{requiredPoints}");

                if (growthSlider != null)
                {
                    growthSlider.minValue = 0f;
                    growthSlider.maxValue = requiredPoints;
                    growthSlider.value = visiblePoints;
                }
            }
            else
            {
                SetText(
                    growthText,
                    growth.Stage == PetGrowthStage.Adult
                        ? "GROWTH COMPLETE"
                        : "HATCHING PENDING");

                if (growthSlider != null)
                {
                    growthSlider.minValue = 0f;
                    growthSlider.maxValue = 1f;
                    growthSlider.value =
                        growth.Stage == PetGrowthStage.Adult
                            ? 1f
                            : 0f;
                }
            }

            bool isTeen = growth.Stage == PetGrowthStage.Teen;

            int requiredTrainingSessions =
    growthPolicy.SpecialAdultTrainingSessionsRequired;

int visibleTrainingSessions = Math.Min(
    growth.TeenTrainingSessions,
    requiredTrainingSessions);


            if (trainingText != null)
            {
                trainingText.gameObject.SetActive(isTeen);
               trainingText.text =
    $"TRAINING {visibleTrainingSessions}/" +
    $"{requiredTrainingSessions}";
            }

            if (testTrainingButton != null)
            {
                testTrainingButton.gameObject.SetActive(isTeen);
                testTrainingButton.interactable =
    isTeen &&
    growth.TeenTrainingSessions <
        requiredTrainingSessions &&
    !petState.IsSleeping &&
    !petState.IsInComa;
            }

            if (evolveButton != null)
            {
                bool isReady = PetGrowthService.IsEvolutionReady(
                    petState,
                    growthPolicy);

                evolveButton.gameObject.SetActive(
                    growth.Stage == PetGrowthStage.Bat ||
                    growth.Stage == PetGrowthStage.Teen);

                evolveButton.interactable = isReady;
            }
        }

        private void PlayEvolutionAnimation(
            PetEvolutionResult result)
        {
            if (evolutionAnimator == null)
            {
                return;
            }

            string triggerName = string.Empty;

            if (result.Gate == PetEvolutionGate.GateOne)
            {
                triggerName = gateOneTrigger;
            }
            else if (result.Gate == PetEvolutionGate.GateTwo)
            {
                triggerName =
                    result.CurrentVariant ==
                        PetEvolutionVariant.Special
                        ? gateTwoSpecialTrigger
                        : gateTwoDefaultTrigger;
            }

            if (!string.IsNullOrWhiteSpace(triggerName))
            {
                evolutionAnimator.SetTrigger(triggerName);
            }
        }

        private static string GetStageDisplayName(
            PetGrowthStage stage)
        {
            switch (stage)
            {
                case PetGrowthStage.Egg:
                    return "EGG";

                case PetGrowthStage.Bat:
                    return "BAT (BABY)";

                case PetGrowthStage.Teen:
                    return "TEEN";

                case PetGrowthStage.Adult:
                    return "ADULT";

                default:
                    return stage.ToString().ToUpperInvariant();
            }
        }

        private static string GetVariantDisplayName(
            PetGrowthStage stage,
            PetEvolutionVariant variant)
        {
            if (stage == PetGrowthStage.Egg ||
                stage == PetGrowthStage.Bat)
            {
                return "FORM: GROWING";
            }

            return variant == PetEvolutionVariant.Special
                ? "FORM: SPECIAL"
                : "FORM: DEFAULT";
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
            if (isBound && petSession != null)
            {
                petSession.StateChanged -= Refresh;
            }

            if (evolveButton != null)
            {
                evolveButton.onClick.RemoveListener(
                    HandleEvolveClicked);
            }

            if (testTrainingButton != null)
            {
                testTrainingButton.onClick.RemoveListener(
                    HandleTestTrainingClicked);
            }
        }
    }
}
