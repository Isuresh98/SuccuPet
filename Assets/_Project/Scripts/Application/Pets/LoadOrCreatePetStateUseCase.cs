using System;
using SuccuPet.Core.Pets;

namespace SuccuPet.Application.Pets
{
    public readonly struct LoadPetStateResult
    {
        public PetState PetState { get; }
        public bool WasCreated { get; }
        public PetDecayResult DecayResult { get; }

        public LoadPetStateResult(
            PetState petState,
            bool wasCreated,
            PetDecayResult decayResult)
        {
            PetState = petState;
            WasCreated = wasCreated;
            DecayResult = decayResult;
        }
    }

    public sealed class LoadOrCreatePetStateUseCase
    {
        private readonly IPetStateRepository repository;
        private readonly PetDecayPolicy decayPolicy;
        private readonly PetGrowthPolicy growthPolicy;

        public LoadOrCreatePetStateUseCase(
            IPetStateRepository repository,
            PetDecayPolicy decayPolicy,
            PetGrowthPolicy growthPolicy)
        {
            this.repository = repository ??
                throw new ArgumentNullException(nameof(repository));

            this.decayPolicy = decayPolicy ??
                throw new ArgumentNullException(nameof(decayPolicy));

            this.growthPolicy = growthPolicy ??
                throw new ArgumentNullException(nameof(growthPolicy));
        }

        public LoadPetStateResult Execute(
            string petId,
            string displayName,
            DateTime utcNow)
        {
            if (utcNow.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "Current time must use UTC.",
                    nameof(utcNow));
            }

            if (repository.TryLoad(out PetState petState))
            {
                PetDecayResult decayResult =
                    PetNeedsDecayService.Apply(
                        petState,
                        utcNow,
                        decayPolicy);

                PetGrowthService.ReevaluateOngoingVariant(
                    petState,
                    growthPolicy);

                repository.Save(petState);

                return new LoadPetStateResult(
                    petState,
                    wasCreated: false,
                    decayResult);
            }

            petState = PetState.CreateNew(
                petId,
                displayName,
                utcNow);

            repository.Save(petState);

            return new LoadPetStateResult(
                petState,
                wasCreated: true,
                new PetDecayResult(
                    applied: false,
                    appliedHours: 0d,
                    wasCapped: false));
        }
    }
}
