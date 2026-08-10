using System;

namespace SuccuPet.Core.Pets
{
    public sealed class PetHealthPolicy
    {
        public double EvaluationIntervalMinutes { get; }
        public float HealthyAverageThreshold { get; }
        public float NeglectedAverageThreshold { get; }
        public int HealthyHealthGain { get; }
        public int NeglectHealthLoss { get; }
        public float ComaRecoveryNeedThreshold { get; }
        public double ComaRecoveryWindowHours { get; }
        public int RecoveredHealthValue { get; }

        public static PetHealthPolicy Default { get; } =
            new PetHealthPolicy(
                evaluationIntervalMinutes: 30d,
                healthyAverageThreshold: 50f,
                neglectedAverageThreshold: 20f,
                healthyHealthGain: 1,
                neglectHealthLoss: 1,
                comaRecoveryNeedThreshold: 50f,
                comaRecoveryWindowHours: 24d,
                recoveredHealthValue: 30);

        public PetHealthPolicy(
            double evaluationIntervalMinutes,
            float healthyAverageThreshold,
            float neglectedAverageThreshold,
            int healthyHealthGain,
            int neglectHealthLoss,
            float comaRecoveryNeedThreshold,
            double comaRecoveryWindowHours,
            int recoveredHealthValue)
        {
            if (evaluationIntervalMinutes <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(evaluationIntervalMinutes));
            }

            if (neglectedAverageThreshold < PetNeeds.MinimumValue ||
                healthyAverageThreshold > PetNeeds.MaximumValue ||
                neglectedAverageThreshold >= healthyAverageThreshold)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(healthyAverageThreshold));
            }

            if (healthyHealthGain < 0 || neglectHealthLoss < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(healthyHealthGain));
            }

            if (comaRecoveryNeedThreshold < PetNeeds.MinimumValue ||
                comaRecoveryNeedThreshold >= PetNeeds.MaximumValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(comaRecoveryNeedThreshold));
            }

            if (comaRecoveryWindowHours <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(comaRecoveryWindowHours));
            }

            if (recoveredHealthValue <= PetHealth.MinimumValue ||
                recoveredHealthValue > PetHealth.MaximumValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(recoveredHealthValue));
            }

            EvaluationIntervalMinutes = evaluationIntervalMinutes;
            HealthyAverageThreshold = healthyAverageThreshold;
            NeglectedAverageThreshold = neglectedAverageThreshold;
            HealthyHealthGain = healthyHealthGain;
            NeglectHealthLoss = neglectHealthLoss;
            ComaRecoveryNeedThreshold = comaRecoveryNeedThreshold;
            ComaRecoveryWindowHours = comaRecoveryWindowHours;
            RecoveredHealthValue = recoveredHealthValue;
        }
    }
}
