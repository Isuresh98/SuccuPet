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
                utcNow - petState.LastSimulationUtc;

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

            double fullSpeedHours = Math.Min(
                appliedHours,
                policy.FullSpeedOfflineHours);

            double extendedHours = Math.Max(
                0d,
                appliedHours - fullSpeedHours);

            double effectiveHours =
                fullSpeedHours +
                (extendedHours *
                    policy.ExtendedOfflineMultiplier);

            if (petState.IsSleeping)
            {
                ApplySleepingProgress(
                    petState,
                    policy,
                    effectiveHours);
            }
            else
            {
                ApplyAwakeProgress(
                    petState,
                    policy,
                    effectiveHours);
            }

            // Even when offline progress is capped, consume the full elapsed
            // window so the same time is never processed twice.
            petState.MarkSimulationUpdated(utcNow);

            return new PetDecayResult(
                applied: true,
                appliedHours: appliedHours,
                wasCapped:
                    actualHours > policy.MaximumOfflineHours);
        }

        private static void ApplyAwakeProgress(
            PetState petState,
            PetDecayPolicy policy,
            double effectiveHours)
        {
            petState.Needs.Reduce(
                PetNeedType.Vitality,
                (float)(policy.VitalityLossPerHour *
                    effectiveHours));

            petState.Needs.Reduce(
                PetNeedType.Rest,
                (float)(policy.RestLossPerHour *
                    effectiveHours));

            petState.Needs.Reduce(
                PetNeedType.Mood,
                (float)(policy.MoodLossPerHour *
                    effectiveHours));

            petState.Needs.Reduce(
                PetNeedType.Allure,
                (float)(policy.AllureLossPerHour *
                    effectiveHours));
        }

        private static void ApplySleepingProgress(
            PetState petState,
            PetDecayPolicy policy,
            double effectiveHours)
        {
            double slowedHours =
                effectiveHours *
                policy.SleepingNeedsLossMultiplier;

            petState.Needs.Reduce(
                PetNeedType.Vitality,
                (float)(policy.VitalityLossPerHour *
                    slowedHours));

            petState.Needs.Reduce(
                PetNeedType.Mood,
                (float)(policy.MoodLossPerHour *
                    slowedHours));

            petState.Needs.Reduce(
                PetNeedType.Allure,
                (float)(policy.AllureLossPerHour *
                    slowedHours));

            petState.Needs.Restore(
                PetNeedType.Rest,
                (float)(policy.SleepRestRecoveryPerHour *
                    effectiveHours));
        }
    }
}
