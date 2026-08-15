using System;
using SuccuPet.Core.Pets;

namespace SuccuPet.Application.Pets
{
    public sealed class PetSession
    {
        private readonly LoadOrCreatePetStateUseCase loadUseCase;
        private readonly PerformPetCareActionUseCase careUseCase;
        private readonly UpdatePetStateUseCase updateUseCase;
        private readonly SelectStarterEggUseCase starterEggUseCase;
        private readonly CompletePetHatchingUseCase hatchingUseCase;
        private readonly EvolvePetUseCase evolveUseCase;
        private readonly RegisterPetTrainingUseCase trainingUseCase;

        public PetState CurrentPetState { get; private set; }

        public bool IsInitialized => CurrentPetState != null;

        public event Action<PetState> StateChanged;
        

        public event Action<PerformPetCareActionResult>
            CareActionPerformed;

        public event Action<PetEvolutionResult>
            PetEvolved;

        public event Action<PetTrainingResult>
            TrainingPerformed;

            public event Action<PetState> PetDied;


        public PetSession(
            LoadOrCreatePetStateUseCase loadUseCase,
            PerformPetCareActionUseCase careUseCase,
            UpdatePetStateUseCase updateUseCase,
            SelectStarterEggUseCase starterEggUseCase,
            CompletePetHatchingUseCase hatchingUseCase,
            EvolvePetUseCase evolveUseCase,
            RegisterPetTrainingUseCase trainingUseCase)
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

            this.starterEggUseCase = starterEggUseCase ??
                throw new ArgumentNullException(
                    nameof(starterEggUseCase));

            this.hatchingUseCase = hatchingUseCase ??
                throw new ArgumentNullException(
                    nameof(hatchingUseCase));

            this.evolveUseCase = evolveUseCase ??
                throw new ArgumentNullException(
                    nameof(evolveUseCase));

            this.trainingUseCase = trainingUseCase ??
                throw new ArgumentNullException(
                    nameof(trainingUseCase));
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

        public StarterEggSelectionResult SelectStarterEgg(
            string lineageId,
            DateTime utcNow)
        {
            EnsureInitialized();

            StarterEggSelectionResult result =
                starterEggUseCase.Execute(
                    CurrentPetState,
                    lineageId,
                    utcNow);

            if (result.IsSuccessful)
            {
                StateChanged?.Invoke(CurrentPetState);
            }

            return result;
        }

        public PetEvolutionResult CompleteHatching(DateTime utcNow)
        {
            EnsureInitialized();

            PetEvolutionResult result =
                hatchingUseCase.Execute(
                    CurrentPetState,
                    utcNow);

            if (result.IsSuccessful)
            {
                StateChanged?.Invoke(CurrentPetState);
                PetEvolved?.Invoke(result);
            }

            return result;
        }

        public PetEvolutionResult TryEvolve(DateTime utcNow)
        {
            EnsureInitialized();

            PetEvolutionResult result =
                evolveUseCase.Execute(
                    CurrentPetState,
                    utcNow);

            StateChanged?.Invoke(CurrentPetState);

            if (result.IsSuccessful)
            {
                PetEvolved?.Invoke(result);
            }

            return result;
        }

        public PetTrainingResult RegisterTeenTraining(
            DateTime utcNow)
        {
            EnsureInitialized();

            PetTrainingResult result =
                trainingUseCase.Execute(
                    CurrentPetState,
                    utcNow);

            StateChanged?.Invoke(CurrentPetState);

            if (result.IsSuccessful)
            {
                TrainingPerformed?.Invoke(result);
            }

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

    if (result.Died)
    {
        PetDied?.Invoke(CurrentPetState);
    }

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
