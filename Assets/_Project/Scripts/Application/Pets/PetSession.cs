using System;
using SuccuPet.Core.Pets;

namespace SuccuPet.Application.Pets
{
    public sealed class PetSession
    {
        private readonly LoadOrCreatePetStateUseCase loadUseCase;
        private readonly PerformPetCareActionUseCase careUseCase;
        private readonly UpdatePetStateUseCase updateUseCase;

        public PetState CurrentPetState { get; private set; }

        public bool IsInitialized => CurrentPetState != null;

        public event Action<PetState> StateChanged;

        public event Action<PerformPetCareActionResult>
            CareActionPerformed;

        public PetSession(
            LoadOrCreatePetStateUseCase loadUseCase,
            PerformPetCareActionUseCase careUseCase,
            UpdatePetStateUseCase updateUseCase)
        {
            this.loadUseCase = loadUseCase ??
                throw new ArgumentNullException(
                    nameof(loadUseCase));

            this.careUseCase = careUseCase ??
                throw new ArgumentNullException(
                    nameof(careUseCase));

            this.updateUseCase = updateUseCase ??
                throw new ArgumentNullException(
                    nameof(updateUseCase));
        }

        public LoadPetStateResult Initialize(
            string petId,
            string displayName,
            DateTime utcNow)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException(
                    "Pet session has already been initialized.");
            }

            LoadPetStateResult result =
                loadUseCase.Execute(
                    petId,
                    displayName,
                    utcNow);

            CurrentPetState = result.PetState;

            StateChanged?.Invoke(CurrentPetState);

            return result;
        }

        public PerformPetCareActionResult PerformCareAction(
            PetCareActionType actionType,
            DateTime utcNow)
        {
            EnsureInitialized();

            PerformPetCareActionResult result =
                careUseCase.Execute(
                    CurrentPetState,
                    actionType,
                    utcNow);

            StateChanged?.Invoke(CurrentPetState);
            CareActionPerformed?.Invoke(result);

            return result;
        }

        public SetPetSleepingResult SetSleeping(
            bool shouldSleep,
            DateTime utcNow)
        {
            EnsureInitialized();

            SetPetSleepingResult result =
                updateUseCase.SetSleeping(
                    CurrentPetState,
                    shouldSleep,
                    utcNow);

            StateChanged?.Invoke(CurrentPetState);
            return result;
        }

        public PetDecayResult SimulateAndSave(
            DateTime utcNow)
        {
            EnsureInitialized();

            PetDecayResult result =
                updateUseCase.SimulateAndSave(
                    CurrentPetState,
                    utcNow);

            StateChanged?.Invoke(CurrentPetState);
            return result;
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(
                    "Pet session must be initialized first.");
            }
        }
    }
}
