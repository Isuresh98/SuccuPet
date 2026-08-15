using System;
using SuccuPet.Core.Pets;

namespace SuccuPet.Application.Pets
{
    public readonly struct PerformPetCareActionResult
    {
        public PetDecayResult DecayResult { get; }
        public PetCareActionResult CareResult { get; }
        public PetGrowthUpdateResult GrowthResult { get; }

        public bool IsSuccessful =>
            CareResult.IsSuccessful;

        public PerformPetCareActionResult(
            PetDecayResult decayResult,
            PetCareActionResult careResult,
            PetGrowthUpdateResult growthResult)
        {
            DecayResult = decayResult;
            CareResult = careResult;
            GrowthResult = growthResult;
        }
    }

    public sealed class PerformPetCareActionUseCase
    {
        private readonly PetDecayPolicy decayPolicy;
        private readonly PetGrowthPolicy growthPolicy;
        private readonly IPetStateRepository repository;

        public PerformPetCareActionUseCase(
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

        public PerformPetCareActionResult Execute(
            PetState petState,
            PetCareActionType actionType,
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

            PetDecayResult decayResult =
                PetNeedsDecayService.Apply(
                    petState,
                    utcNow,
                    decayPolicy);

            PetCareActionResult careResult =
                PetCareService.Perform(
                    petState,
                    actionType);

            PetGrowthUpdateResult growthResult =
                PetGrowthService.AddCareGrowth(
                    petState,
                    careResult.IsSuccessful
                        ? careResult.ExperienceEarned
                        : 0,
                    growthPolicy);

            // Persist both successful actions and any elapsed simulation.
            if (careResult.IsSuccessful || decayResult.Applied)
            {
                repository.Save(petState);
            }

            return new PerformPetCareActionResult(
                decayResult,
                careResult,
                growthResult);
        }
    }
}
