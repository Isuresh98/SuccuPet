using System;

namespace SuccuPet.Core.Pets
{
    public sealed class PetState
    {
        public PetProfile Profile { get; }
        public PetNeeds Needs { get; }
        public PetStats Stats { get; }
        public DateTime LastNeedsUpdateUtc { get; private set; }

        public PetState(
            PetProfile profile,
            PetNeeds needs,
            PetStats stats,
            DateTime lastNeedsUpdateUtc)
        {
            Profile = profile ??
                throw new ArgumentNullException(nameof(profile));

            Needs = needs ??
                throw new ArgumentNullException(nameof(needs));

            Stats = stats ??
                throw new ArgumentNullException(nameof(stats));

            if (lastNeedsUpdateUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "Needs update time must use UTC.",
                    nameof(lastNeedsUpdateUtc));
            }

            LastNeedsUpdateUtc = lastNeedsUpdateUtc;
        }

        public static PetState CreateNew(
            string petId,
            string displayName,
            DateTime utcNow)
        {
            PetProfile profile = new PetProfile(
                petId,
                displayName,
                utcNow);

            return new PetState(
                profile,
                new PetNeeds(),
                new PetStats(),
                utcNow);
        }

        internal void MarkNeedsUpdated(DateTime utcNow)
        {
            LastNeedsUpdateUtc = utcNow;
        }
    }
}