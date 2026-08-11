using System;

namespace SuccuPet.Core.Pets
{
    public sealed class PetLineageDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public PetAestheticType AestheticType { get; }
        public string TellKey { get; }
        public bool IsStarterEligible { get; }
        public bool IsCafeExclusive { get; }

        public PetLineageDefinition(
            string id,
            string displayName,
            PetAestheticType aestheticType,
            string tellKey,
            bool isStarterEligible,
            bool isCafeExclusive)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    "Lineage ID cannot be empty.",
                    nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "Lineage display name cannot be empty.",
                    nameof(displayName));
            }

            if (string.IsNullOrWhiteSpace(tellKey))
            {
                throw new ArgumentException(
                    "Lineage tell key cannot be empty.",
                    nameof(tellKey));
            }

            if (isCafeExclusive && isStarterEligible)
            {
                throw new ArgumentException(
                    "Cafe-exclusive lineages cannot be starter eggs.");
            }

            Id = id.Trim();
            DisplayName = displayName.Trim();
            AestheticType = aestheticType;
            TellKey = tellKey.Trim();
            IsStarterEligible = isStarterEligible;
            IsCafeExclusive = isCafeExclusive;
        }
    }
}
