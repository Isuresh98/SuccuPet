using System;
using SuccuPet.Core.Pets;

namespace SuccuPet.Application.Pets
{
    public readonly struct SetPetSleepingResult
    {
        public PetDecayResult DecayResult { get; }
        public bool StateChanged { get; }
        public bool IsSleeping { get; }

        public SetPetSleepingResult(
            PetDecayResult decayResult,
            bool stateChanged,
            bool isSleeping)
        {
            DecayResult = decayResult;
            StateChanged = stateChanged;
            IsSleeping = isSleeping;
        }
    }

    public sealed class UpdatePetStateUseCase
    {
        private readonly PetDecayPolicy decayPolicy;
        private readonly PetGrowthPolicy growthPolicy;
        private readonly IPetStateRepository repository;

        public UpdatePetStateUseCase(
            PetDecayPolicy decayPolicy,
            PetGrowthPolicy growthPolicy,
            IPetStateRepository repository)
        {
            this.decayPolicy = decayPolicy ??
                throw new ArgumentNullException(
                    nameof(decayPolicy));

            this.growthPolicy = growthPolicy ??
                throw new ArgumentNullException(
                    nameof(growthPolicy));

            this.repository = repository ??
                throw new ArgumentNullException(
                    nameof(repository));
        }

        public PetDecayResult SimulateAndSave(
            PetState petState,
            DateTime utcNow)
        {
            Validate(petState, utcNow);

            PetDecayResult decayResult =
                PetNeedsDecayService.Apply(
                    petState,
                    utcNow,
                    decayPolicy);

            PetGrowthService.ReevaluateOngoingVariant(
                petState,
                growthPolicy);

            repository.Save(petState);
            return decayResult;
        }

        public SetPetSleepingResult SetSleeping(
            PetState petState,
            bool shouldSleep,
            DateTime utcNow)
        {
            Validate(petState, utcNow);

            // Process the old state up to this exact moment before toggling.
            PetDecayResult decayResult =
                PetNeedsDecayService.Apply(
                    petState,
                    utcNow,
                    decayPolicy);

            PetGrowthService.ReevaluateOngoingVariant(
                petState,
                growthPolicy);

            bool stateChanged = shouldSleep
                ? petState.StartSleeping(utcNow)
                : petState.Wake(utcNow);

            repository.Save(petState);

            return new SetPetSleepingResult(
                decayResult,
                stateChanged,
                petState.IsSleeping);
        }

        private static void Validate(
            PetState petState,
            DateTime utcNow)
        {
            if (petState == null)
            {
                throw new ArgumentNullException(
                    nameof(petState));
            }

            if (utcNow.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "Current time must use UTC.",
                    nameof(utcNow));
            }
        }
    }
}
