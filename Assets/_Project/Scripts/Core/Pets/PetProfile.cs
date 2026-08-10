using System;

namespace SuccuPet.Core.Pets
{
    public sealed class PetProfile
    {
        public string PetId { get; }
        public string DisplayName { get; }
        public DateTime CreatedAtUtc { get; }

        public PetProfile(
            string petId,
            string displayName,
            DateTime createdAtUtc)
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
                    "Display name cannot be empty.",
                    nameof(displayName));
            }

            if (createdAtUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "Created time must use UTC.",
                    nameof(createdAtUtc));
            }

            PetId = petId.Trim();
            DisplayName = displayName.Trim();
            CreatedAtUtc = createdAtUtc;
        }
    }
}