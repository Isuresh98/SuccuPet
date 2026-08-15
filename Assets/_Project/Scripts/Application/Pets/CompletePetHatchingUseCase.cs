using System;
using SuccuPet.Core.Pets;

namespace SuccuPet.Application.Pets
{
    public sealed class CompletePetHatchingUseCase
    {
        private readonly IPetStateRepository repository;

        public CompletePetHatchingUseCase(
            IPetStateRepository repository)
        {
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

            PetEvolutionResult result =
                PetGrowthService.CompleteHatching(
                    petState,
                    utcNow);

            if (result.IsSuccessful)
            {
                repository.Save(petState);
            }

            return result;
        }
    }
}
