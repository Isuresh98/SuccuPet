using System;

namespace SuccuPet.Core.Pets
{
    public sealed class PetDecayPolicy
    {
        public float FullnessLossPerHour { get; }
        public float EnergyLossPerHour { get; }
        public float HappinessLossPerHour { get; }
        public float HygieneLossPerHour { get; }
        public double MaximumOfflineHours { get; }

        public static PetDecayPolicy Default { get; } =
            new PetDecayPolicy(
                fullnessLossPerHour: 6f,
                energyLossPerHour: 4f,
                happinessLossPerHour: 2f,
                hygieneLossPerHour: 1.5f,
                maximumOfflineHours: 24d);

        public PetDecayPolicy(
            float fullnessLossPerHour,
            float energyLossPerHour,
            float happinessLossPerHour,
            float hygieneLossPerHour,
            double maximumOfflineHours)
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

            FullnessLossPerHour = fullnessLossPerHour;
            EnergyLossPerHour = energyLossPerHour;
            HappinessLossPerHour = happinessLossPerHour;
            HygieneLossPerHour = hygieneLossPerHour;
            MaximumOfflineHours = maximumOfflineHours;
        }

        private static void ValidateRate(float value, string parameterName)
        {
            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}