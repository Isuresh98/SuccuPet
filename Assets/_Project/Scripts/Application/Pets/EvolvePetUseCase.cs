using System;
using SuccuPet.Core.Pets;

namespace SuccuPet.Application.Pets
{
    public sealed class EvolvePetUseCase
    {
        private readonly PetDecayPolicy decayPolicy;
        private readonly PetGrowthPolicy growthPolicy;
        private readonly IPetStateRepository repository;

        public EvolvePetUseCase(
            PetDecayPolicy decayPolicy,
            PetGrowthPolicy growthPolicy,
            IPetStateRepository repository)
        {
            this.decayPolicy = decayPolicy ??
                throw new ArgumentNullException(nameof(decayPolicy));

            this.growthPolicy = growthPolicy ??
                throw new ArgumentNullException(nameof(growthPolicy));

            this.repository = repository ??
                throw new ArgumentNullException(nameof(repository));
        }

        public PetEvolutionResult Execute(
            PetState petState,
            DateTime utcNow)
        {
            if (petState == null)
            {
                throw new ArgumentNullException(nameof(petState));
            }

            PetNeedsDecayService.Apply(
                petState,
                utcNow,
                decayPolicy);

            PetGrowthService.ReevaluateOngoingVariant(
                petState,
                growthPolicy);

            PetEvolutionResult result =
                PetGrowthService.TryEvolve(
                    petState,
                    utcNow,
                    growthPolicy);

            repository.Save(petState);
            return result;
        }
    }
}
