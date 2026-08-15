using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SuccuPet.Application.Pets;
using SuccuPet.Bootstrap;
using SuccuPet.Core.Pets;

namespace SuccuPet.Presentation.Pets
{
    public sealed class StarterEggSelectionPresenter : MonoBehaviour
    {
        [Serializable]
        private sealed class EggOptionView
        {
            [SerializeField]
            private string lineageId;

            [SerializeField]
            private TMP_Text nameText;

            [SerializeField]
            private TMP_Text availabilityText;

            [SerializeField]
            private Button selectButton;

            [SerializeField]
            private GameObject lockedVisual;

            private Action<string> onSelected;

            public string LineageId => lineageId;

            public void Bind(
                PetLineageDefinition lineage,
                bool starterAlreadySelected,
                Action<string> selectionHandler)
            {
                onSelected = selectionHandler;

                if (nameText != null)
                {
                    nameText.text = lineage.DisplayName;
                }

                if (availabilityText != null)
                {
                    availabilityText.text =
                        lineage.IsCafeExclusive
                            ? "Unlock at cafe"
                            : lineage.IsStarterEligible
                                ? "Free starter"
                                : "Locked";
                }

                if (lockedVisual != null)
                {
                    lockedVisual.SetActive(
                        lineage.IsCafeExclusive);
                }

                if (selectButton != null)
                {
                    selectButton.onClick.RemoveListener(
                        HandleClicked);

                    selectButton.onClick.AddListener(
                        HandleClicked);

                    selectButton.interactable =
                        !starterAlreadySelected &&
                        lineage.IsStarterEligible &&
                        !lineage.IsCafeExclusive;
                }
            }

            public void Unbind()
            {
                if (selectButton != null)
                {
                    selectButton.onClick.RemoveListener(
                        HandleClicked);
                }

                onSelected = null;
            }

            private void HandleClicked()
            {
                onSelected?.Invoke(lineageId);
            }
        }

        [Header("Main Panels")]
        [SerializeField]
        private GameObject selectionPanel;

        [SerializeField]
        private GameObject confirmationPanel;

        [SerializeField]
        private GameObject petCarePanel;

        [SerializeField]
        private StarterEggHatchingPresenter hatchingPresenter;

        [Header("Eight Launch Eggs")]
        [SerializeField]
        private EggOptionView[] eggOptions;

        [Header("Confirmation Popup")]
        [SerializeField]
        private TMP_Text confirmationTitleText;

        [SerializeField]
        private TMP_Text confirmationBodyText;

        [SerializeField]
        private Button backButton;

        [SerializeField]
        private Button confirmButton;

        [Header("Selection Status")]
        [SerializeField]
        private TMP_Text statusText;

        private PetSession petSession;
        private string pendingLineageId;
        private bool isBound;
        private bool isCompletingSelection;

        private void Awake()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(
                    HandleBackClicked);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(
                    HandleConfirmClicked);
            }
        }

        private void OnEnable()
        {
            TryBindAndRefresh(logNotReady: false);
        }

        private void Start()
        {
            TryBindAndRefresh(logNotReady: true);
        }

        private void TryBindAndRefresh(bool logNotReady)
        {
            GameEntryPoint entryPoint =
                GameEntryPoint.Instance;

            if (entryPoint == null ||
                !entryPoint.IsReady)
            {
                if (logNotReady)
                {
                    Debug.LogError(
                        "GameEntryPoint is not ready for starter selection.",
                        this);
                }

                return;
            }

            if (petSession != entryPoint.PetSession)
            {
                UnbindStateChanged();
                petSession = entryPoint.PetSession;
            }

            if (!isBound)
            {
                petSession.StateChanged += Refresh;
                isBound = true;
            }

            Refresh(petSession.CurrentPetState);
        }

        private void Refresh(PetState petState)
        {
            if (petState == null)
            {
                return;
            }

            bool hasSelectedEgg =
                petState.Origin.HasSelectedLineage;

            if (hasSelectedEgg)
            {
                // SelectStarterEgg can raise StateChanged before it returns.
                // During a new selection the hatching screen owns the flow.
                if (isCompletingSelection)
                {
                    return;
                }

                // If the app closed after egg confirmation but before the
                // hatching sequence completed, resume hatching instead of
                // opening Pet Care with an Egg-stage pet.
                if (petState.Growth.Stage == PetGrowthStage.Egg)
                {
                    ShowHatchingState(
                        petState.Origin.LineageId);
                    return;
                }

                // A previously saved pet should open Pet Care directly.
                ShowPetCareState();
                return;
            }

            BindEggOptions(false);

            if (string.IsNullOrWhiteSpace(
                    pendingLineageId))
            {
                ShowEggSelectionState();
            }
        }

        private void ShowEggSelectionState()
        {
            pendingLineageId = null;
            isCompletingSelection = false;

            if (petCarePanel != null)
            {
                petCarePanel.SetActive(false);
            }

            if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
            }

            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(false);
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
            }

            SetText(
                statusText,
                "Choose one free starter egg");
        }

        private void ShowConfirmationState(
            PetLineageDefinition lineage)
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
            }

            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(true);
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }

            SetText(
                confirmationTitleText,
                $"CHOOSE {lineage.DisplayName.ToUpperInvariant()}?");

            SetText(
                confirmationBodyText,
                $"{lineage.DisplayName} will become your " +
                "starter pet.\n\nThis choice cannot be " +
                "changed for this pet.");

            if (confirmButton != null)
            {
                confirmButton.interactable = true;
            }
        }

        private void ShowHatchingState(
            string selectedLineageId)
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }

            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(false);
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }

            bool startedHatching =
                hatchingPresenter != null &&
                hatchingPresenter.Play(selectedLineageId);

            if (!startedHatching)
            {
                Debug.LogError(
                    "The hatching screen could not start. " +
                    "Opening Pet Care as a safe fallback.",
                    this);

                ShowPetCareState();
                return;
            }

            UnbindStateChanged();
            gameObject.SetActive(false);
        }

        private void ShowPetCareState()
        {
            pendingLineageId = null;
            isCompletingSelection = false;

            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }

            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(false);
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }

            if (petCarePanel != null)
            {
                petCarePanel.SetActive(true);
            }

            UnbindStateChanged();
            gameObject.SetActive(false);
        }

        private void BindEggOptions(
            bool starterAlreadySelected)
        {
            if (eggOptions == null)
            {
                return;
            }

            for (int index = 0;
                index < eggOptions.Length;
                index++)
            {
                EggOptionView option =
                    eggOptions[index];

                if (option == null)
                {
                    continue;
                }

                if (!PetLineageCatalog.TryGet(
                        option.LineageId,
                        out PetLineageDefinition lineage))
                {
                    Debug.LogWarning(
                        $"Unknown starter egg lineage ID: " +
                        $"{option.LineageId}",
                        this);

                    continue;
                }

                option.Bind(
                    lineage,
                    starterAlreadySelected,
                    HandleEggSelected);
            }
        }

        private void HandleEggSelected(
            string lineageId)
        {
            if (isCompletingSelection)
            {
                return;
            }

            if (!PetLineageCatalog.TryGet(
                    lineageId,
                    out PetLineageDefinition lineage))
            {
                ShowSelectionError(
                    "Unable to find the selected egg.");
                return;
            }

            if (!lineage.IsStarterEligible ||
                lineage.IsCafeExclusive)
            {
                ShowSelectionError(
                    "This egg cannot be selected as a free starter.");
                return;
            }

            pendingLineageId = lineageId;
            ShowConfirmationState(lineage);
        }

        private void HandleBackClicked()
        {
            if (isCompletingSelection)
            {
                return;
            }

            pendingLineageId = null;

            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(false);
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
            }

            SetText(
                statusText,
                "Choose one free starter egg");
        }

        private void HandleConfirmClicked()
        {
            if (isCompletingSelection ||
                string.IsNullOrWhiteSpace(
                    pendingLineageId))
            {
                return;
            }

            GameEntryPoint entryPoint =
                GameEntryPoint.Instance;

            if (entryPoint == null ||
                !entryPoint.IsReady)
            {
                ShowSelectionError(
                    "The game is not ready. Please try again.");
                return;
            }

            isCompletingSelection = true;

            if (confirmButton != null)
            {
                confirmButton.interactable = false;
            }

            string selectedLineageId =
                pendingLineageId;

            StarterEggSelectionResult result =
                entryPoint.SelectStarterEgg(
                    selectedLineageId);

            if (!result.IsSuccessful)
            {
                isCompletingSelection = false;
                ShowSelectionError(result.Message);
                return;
            }

            // The pending marker makes the tutorial survive an app close
            // between egg confirmation and the Pet Care screen.
            FirstCareTutorialPresenter.MarkPendingForNewPet();

            pendingLineageId = null;
            ShowHatchingState(selectedLineageId);
        }

        private void ShowSelectionError(
            string message)
        {
            pendingLineageId = null;
            isCompletingSelection = false;

            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(false);
            }

            if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
            }

            if (confirmButton != null)
            {
                confirmButton.interactable = true;
            }

            SetText(statusText, message);
        }

        private void UnbindStateChanged()
        {
            if (!isBound || petSession == null)
            {
                return;
            }

            petSession.StateChanged -= Refresh;
            isBound = false;
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
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(
                    HandleBackClicked);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(
                    HandleConfirmClicked);
            }

            if (eggOptions != null)
            {
                for (int index = 0;
                    index < eggOptions.Length;
                    index++)
                {
                    eggOptions[index]?.Unbind();
                }
            }

            UnbindStateChanged();
        }
    }
}