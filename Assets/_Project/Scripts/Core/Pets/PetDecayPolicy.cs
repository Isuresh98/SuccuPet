using System;

namespace SuccuPet.Core.Pets
{
    public sealed class PetDecayPolicy
    {
        // Legacy property names are retained for existing decay-service code.
        public float FullnessLossPerHour { get; }
        public float EnergyLossPerHour { get; }
        public float HappinessLossPerHour { get; }
        public float HygieneLossPerHour { get; }

        public float VitalityLossPerHour =>
            FullnessLossPerHour;

        public float RestLossPerHour =>
            EnergyLossPerHour;

        public float MoodLossPerHour =>
            HappinessLossPerHour;

        public float AllureLossPerHour =>
            HygieneLossPerHour;

        public double MaximumOfflineHours { get; }
        public double FullSpeedOfflineHours { get; }
        public float ExtendedOfflineMultiplier { get; }
        public float SleepingNeedsLossMultiplier { get; }
        public float SleepRestRecoveryPerHour { get; }

        public static PetDecayPolicy Default { get; } =
            new PetDecayPolicy(
                fullnessLossPerHour: 6f,
                energyLossPerHour: 4f,
                happinessLossPerHour: 2f,
                hygieneLossPerHour: 1.5f,
                maximumOfflineHours: 24d,
                fullSpeedOfflineHours: 4d,
                extendedOfflineMultiplier: 0.5f,
                sleepingNeedsLossMultiplier: 0.25f,
                sleepRestRecoveryPerHour: 12.5f);

        public PetDecayPolicy(
            float fullnessLossPerHour,
            float energyLossPerHour,
            float happinessLossPerHour,
            float hygieneLossPerHour,
            double maximumOfflineHours,
            double fullSpeedOfflineHours = 4d,
            float extendedOfflineMultiplier = 0.5f,
            float sleepingNeedsLossMultiplier = 0.25f,
            float sleepRestRecoveryPerHour = 12.5f)
        {
            ValidateRate(
                fullnessLossPerHour,
                nameof(fullnessLossPerHour));

            ValidateRate(
                energyLossPerHour,
                nameof(energyLossPerHour));

            ValidateRate(
                happinessLossPerHour,
                nameof(happinessLossPerHour));

            ValidateRate(
                hygieneLossPerHour,
                nameof(hygieneLossPerHour));

            if (maximumOfflineHours <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumOfflineHours));
            }

            if (fullSpeedOfflineHours < 0d ||
                fullSpeedOfflineHours > maximumOfflineHours)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fullSpeedOfflineHours));
            }

            ValidateMultiplier(
                extendedOfflineMultiplier,
                nameof(extendedOfflineMultiplier));

            ValidateMultiplier(
                sleepingNeedsLossMultiplier,
                nameof(sleepingNeedsLossMultiplier));

            ValidateRate(
                sleepRestRecoveryPerHour,
                nameof(sleepRestRecoveryPerHour));

            FullnessLossPerHour = fullnessLossPerHour;
            EnergyLossPerHour = energyLossPerHour;
            HappinessLossPerHour = happinessLossPerHour;
            HygieneLossPerHour = hygieneLossPerHour;
            MaximumOfflineHours = maximumOfflineHours;
            FullSpeedOfflineHours = fullSpeedOfflineHours;
            ExtendedOfflineMultiplier =
                extendedOfflineMultiplier;
            SleepingNeedsLossMultiplier =
                sleepingNeedsLossMultiplier;
            SleepRestRecoveryPerHour =
                sleepRestRecoveryPerHour;
        }

        private static void ValidateRate(
            float value,
            string parameterName)
        {
            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName);
            }
        }

        private static void ValidateMultiplier(
            float value,
            string parameterName)
        {
            if (value < 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Multiplier must be between 0 and 1.");
            }
        }
    }
}
