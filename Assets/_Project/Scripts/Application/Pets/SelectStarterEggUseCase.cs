using System;
using SuccuPet.Core.Pets;

namespace SuccuPet.Application.Pets
{
    public sealed class SelectStarterEggUseCase
    {
        private readonly IPetStateRepository repository;

        public SelectStarterEggUseCase(
            IPetStateRepository repository)
        {
            this.repository = repository ??
                throw new ArgumentNullException(
                    nameof(repository));
        }

        public StarterEggSelectionResult Execute(
            PetState petState,
            string lineageId,
            DateTime utcNow)
        {
            StarterEggSelectionResult result =
                StarterEggSelectionService.Select(
                    petState,
                    lineageId,
                    utcNow);

            if (result.IsSuccessful)
            {
                repository.Save(petState);
            }

            return result;
        }
    }
}
