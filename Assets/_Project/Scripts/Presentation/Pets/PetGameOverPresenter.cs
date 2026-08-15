using System;
using TMPro;
using UnityEngine;
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

        private PetSession petSession;

        private void Start()
        {
            GameEntryPoint entryPoint =
                GameEntryPoint.Instance;

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

            petSession = entryPoint.PetSession;

            petSession.StateChanged +=
                HandleStateChanged;

            Refresh(petSession.CurrentPetState);
        }

        private void HandleStateChanged(
            PetState petState)
        {
            Refresh(petState);
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
        }
    }
}