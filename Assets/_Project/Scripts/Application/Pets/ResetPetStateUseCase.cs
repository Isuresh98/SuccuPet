using System;
using SuccuPet.Core.Pets;

namespace SuccuPet.Application.Pets
{
    public sealed class ResetPetStateUseCase
    {
        private readonly IPetStateRepository repository;

        public ResetPetStateUseCase(
            IPetStateRepository repository)
        {
            this.repository = repository ??
                throw new ArgumentNullException(
                    nameof(repository));
        }

        public PetState Execute(
            string petId,
            string displayName,
            DateTime utcNow)
        {
            if (string.IsNullOrWhiteSpace(petId))
            {
                throw new ArgumentException(
                    "Pet ID cannot be empty.",
                    nameof(petId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "Pet display name cannot be empty.",
                    nameof(displayName));
            }

            if (utcNow.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "Current time must use UTC.",
                    nameof(utcNow));
            }

            PetState newPetState =
                PetState.CreateNew(
                    petId,
                    displayName,
                    utcNow);

            repository.Save(newPetState);

            return newPetState;
        }
    }
}