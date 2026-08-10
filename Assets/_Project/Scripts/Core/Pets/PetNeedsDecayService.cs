using System;

namespace SuccuPet.Core.Pets
{
    public readonly struct PetDecayResult
    {
        public bool Applied { get; }
        public double AppliedHours { get; }
        public bool WasCapped { get; }

        public PetDecayResult(
            bool applied,
            double appliedHours,
            bool wasCapped)
        {
            Applied = applied;
            AppliedHours = appliedHours;
            WasCapped = wasCapped;
        }
    }

    public static class PetNeedsDecayService
    {
        public static PetDecayResult Apply(
            PetState petState,
            DateTime utcNow,
            PetDecayPolicy policy)
        {
            if (petState == null)
            {
                throw new ArgumentNullException(nameof(petState));
            }

            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            if (utcNow.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "Current time must use UTC.",
                    nameof(utcNow));
            }

            TimeSpan elapsed =
                utcNow - petState.LastNeedsUpdateUtc;

            if (elapsed <= TimeSpan.Zero)
            {
                return new PetDecayResult(
                    applied: false,
                    appliedHours: 0d,
                    wasCapped: false);
            }

            double actualHours = elapsed.TotalHours;

            double appliedHours = Math.Min(
                actualHours,
                policy.MaximumOfflineHours);

            petState.Needs.Reduce(
                PetNeedType.Fullness,
                (float)(policy.FullnessLossPerHour * appliedHours));

            petState.Needs.Reduce(
                PetNeedType.Energy,
                (float)(policy.EnergyLossPerHour * appliedHours));

            petState.Needs.Reduce(
                PetNeedType.Happiness,
                (float)(policy.HappinessLossPerHour * appliedHours));

            petState.Needs.Reduce(
                PetNeedType.Hygiene,
                (float)(policy.HygieneLossPerHour * appliedHours));

            petState.MarkNeedsUpdated(utcNow);

            return new PetDecayResult(
                applied: true,
                appliedHours: appliedHours,
                wasCapped:
                    actualHours > policy.MaximumOfflineHours);
        }
    }
}