using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SuccuPet.Application.Pets;
using SuccuPet.Bootstrap;
using SuccuPet.Core.Pets;

namespace SuccuPet.Presentation.Pets
{
    public sealed class PetGameOverPresenter :
        MonoBehaviour
    {
        [Header("Game Over UI")]
        [SerializeField]
        private GameObject gameOverOverlay;

        [SerializeField]
        private TMP_Text gameOverTitleText;

        [SerializeField]
        private TMP_Text gameOverMessageText;

        [SerializeField]
        private TMP_Text survivalTimeText;

        [SerializeField]
        private Button restartButton;

        private GameEntryPoint entryPoint;
        private PetSession petSession;

        private void Start()
        {
            entryPoint = GameEntryPoint.Instance;

            if (entryPoint == null ||
                !entryPoint.IsReady)
            {
                Debug.LogError(
                    "GameEntryPoint is not ready for the game over presenter.",
                    this);

                enabled = false;
                return;
            }

            if (gameOverOverlay == null)
            {
                Debug.LogError(
                    "Game Over Overlay is not assigned.",
                    this);

                enabled = false;
                return;
            }

            if (restartButton == null)
            {
                Debug.LogError(
                    "Restart Button is not assigned.",
                    this);

                enabled = false;
                return;
            }

            petSession = entryPoint.PetSession;

            petSession.StateChanged +=
                HandleStateChanged;

            restartButton.onClick.AddListener(
                HandleRestartClicked);

            Refresh(petSession.CurrentPetState);
        }

        private void HandleStateChanged(
            PetState petState)
        {
            Refresh(petState);
        }

        private void HandleRestartClicked()
        {
            restartButton.interactable = false;

            try
            {
                entryPoint.StartNewPet();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Could not start a new pet: " +
                    $"{exception.Message}",
                    this);

                restartButton.interactable = true;
            }
        }

        private void Refresh(PetState petState)
        {
            bool shouldShow =
                petState != null &&
                petState.IsDead;

            gameOverOverlay.SetActive(shouldShow);

            if (!shouldShow)
            {
                return;
            }

            gameOverOverlay.transform.SetAsLastSibling();
            restartButton.interactable = true;

            if (gameOverTitleText != null)
            {
                gameOverTitleText.text =
                    "GAME OVER";
            }

            if (gameOverMessageText != null)
            {
                gameOverMessageText.text =
                    $"{petState.Profile.DisplayName} " +
                    "could not recover in time.";
            }

            if (survivalTimeText != null)
            {
                survivalTimeText.text =
                    FormatSurvivalTime(petState);
            }
        }

        private static string FormatSurvivalTime(
            PetState petState)
        {
            DateTime endUtc =
                petState.DiedAtUtc ??
                petState.LastSimulationUtc;

            TimeSpan survivalDuration =
                endUtc -
                petState.Profile.CreatedAtUtc;

            if (survivalDuration < TimeSpan.Zero)
            {
                survivalDuration = TimeSpan.Zero;
            }

            int totalDays =
                (int)Math.Floor(
                    survivalDuration.TotalDays);

            return
                $"SURVIVED: {totalDays}D " +
                $"{survivalDuration.Hours:00}H " +
                $"{survivalDuration.Minutes:00}M";
        }

        private void OnDestroy()
        {
            if (petSession != null)
            {
                petSession.StateChanged -=
                    HandleStateChanged;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(
                    HandleRestartClicked);
            }
        }
    }
}