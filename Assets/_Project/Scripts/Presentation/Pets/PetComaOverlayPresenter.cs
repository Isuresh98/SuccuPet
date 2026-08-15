using System;
using TMPro;
using UnityEngine;
using SuccuPet.Application.Pets;
using SuccuPet.Bootstrap;
using SuccuPet.Core.Pets;

namespace SuccuPet.Presentation.Pets
{
    public sealed class PetComaOverlayPresenter : MonoBehaviour
    {
        [Header("Coma UI")]
        [SerializeField]
        private GameObject comaOverlay;

        [SerializeField]
        private TMP_Text comaTitleText;

        [SerializeField]
        private TMP_Text comaMessageText;

        [SerializeField]
        private TMP_Text recoveryProgressText;

        private PetSession petSession;

        private void Start()
        {
            GameEntryPoint entryPoint = GameEntryPoint.Instance;

            if (entryPoint == null || !entryPoint.IsReady)
            {
                Debug.LogError(
                    "GameEntryPoint is not ready for the coma overlay.",
                    this);

                enabled = false;
                return;
            }

            petSession = entryPoint.PetSession;
            petSession.StateChanged += Refresh;

            Refresh(petSession.CurrentPetState);
        }

        private void Refresh(PetState petState)
        {
            if (petState == null)
            {
                return;
            }

            bool showComaOverlay = petState.IsInComa;

            if (comaOverlay != null &&
                comaOverlay.activeSelf != showComaOverlay)
            {
                comaOverlay.SetActive(showComaOverlay);
            }

            if (!showComaOverlay)
            {
                return;
            }

            PetHealthPolicy policy =
                PetHealthPolicy.Default;

            double recoveryProgress = Math.Min(
                petState.Health.ComaRecoveryProgressHours,
                policy.ComaRecoveryWindowHours);

            bool allNeedsAboveRecoveryThreshold =
                petState.Needs.Vitality >
                    policy.ComaRecoveryNeedThreshold &&
                petState.Needs.Rest >
                    policy.ComaRecoveryNeedThreshold &&
                petState.Needs.Mood >
                    policy.ComaRecoveryNeedThreshold &&
                petState.Needs.Allure >
                    policy.ComaRecoveryNeedThreshold;

            if (comaTitleText != null)
            {
                comaTitleText.text = "CARE COMA";
            }

            if (comaMessageText != null)
            {
                comaMessageText.text =
                    allNeedsAboveRecoveryThreshold
                        ? "All needs are stable.\n" +
                          "Recovery is in progress."
                        : "Your pet needs immediate care.\n" +
                          $"Keep all four needs above " +
                          $"{policy.ComaRecoveryNeedThreshold:0}.";
            }

            if (recoveryProgressText != null)
            {
                recoveryProgressText.text =
                    $"RECOVERY {recoveryProgress:0.0} / " +
                    $"{policy.ComaRecoveryWindowHours:0} HOURS";
            }
        }

        private void OnDestroy()
        {
            if (petSession != null)
            {
                petSession.StateChanged -= Refresh;
            }
        }
    }
}