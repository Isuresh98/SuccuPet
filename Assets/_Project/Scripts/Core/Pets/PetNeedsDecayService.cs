using System;

namespace SuccuPet.Core.Pets
{
    public readonly struct PetDecayResult
    {
        public bool Applied { get; }
        public double AppliedHours { get; }
        public bool WasCapped { get; }
        public int PreviousHealth { get; }
        public int CurrentHealth { get; }
        public int HealthEvaluations { get; }
        public bool EnteredComa { get; }
        public bool RecoveredFromComa { get; }

        public bool HealthChanged =>
            PreviousHealth != CurrentHealth;

        public PetDecayResult(
            bool applied,
            double appliedHours,
            bool wasCapped)
            : this(
                applied,
                appliedHours,
                wasCapped,
                PetHealth.MaximumValue,
                PetHealth.MaximumValue,
                0,
                false,
                false)
        {
        }

        public PetDecayResult(
            bool applied,
            double appliedHours,
            bool wasCapped,
            int previousHealth,
            int currentHealth,
            int healthEvaluations,
            bool enteredComa,
            bool recoveredFromComa)
        {
            Applied = applied;
            AppliedHours = appliedHours;
            WasCapped = wasCapped;
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            HealthEvaluations = healthEvaluations;
            EnteredComa = enteredComa;
            RecoveredFromComa = recoveredFromComa;
        }
    }

    public static class PetNeedsDecayService
    {
        private const double MaximumSimulationStepHours = 0.5d;
        private const double Epsilon = 0.0000001d;

        public static PetDecayResult Apply(
            PetState petState,
            DateTime utcNow,
            PetDecayPolicy policy)
        {
            return Apply(
                petState,
                utcNow,
                policy,
                PetHealthPolicy.Default);
        }

        public static PetDecayResult Apply(
            PetState petState,
            DateTime utcNow,
            PetDecayPolicy decayPolicy,
            PetHealthPolicy healthPolicy)
        {
            Validate(
                petState,
                utcNow,
                decayPolicy,
                healthPolicy);

            DateTime simulationStartUtc =
                petState.LastSimulationUtc;

            TimeSpan elapsed = utcNow - simulationStartUtc;

            if (elapsed <= TimeSpan.Zero)
            {
                return CreateNoChangeResult(petState);
            }

            double actualHours = elapsed.TotalHours;
            double appliedHours = Math.Min(
                actualHours,
                decayPolicy.MaximumOfflineHours);

            int previousHealth = petState.Health.Value;
            int healthEvaluations = 0;
            bool enteredComa = false;
            bool recoveredFromComa = false;
            double processedHours = 0d;

            NormalizeHealthProgress(petState, healthPolicy);

            while (processedHours + Epsilon < appliedHours)
            {
                double remainingHours =
                    appliedHours - processedHours;

                double stepHours = GetNextStepHours(
                    petState,
                    healthPolicy,
                    remainingHours);

                double effectiveDecayHours =
                    CalculateEffectiveDecayHours(
                        decayPolicy,
                        processedHours,
                        stepHours);

                ApplyNeedsProgress(
                    petState,
                    decayPolicy,
                    effectiveDecayHours);

                DateTime stepEndUtc = simulationStartUtc.AddHours(
                    processedHours + stepHours);

                if (petState.IsInComa)
                {
                    bool recovered = ApplyComaRecoveryProgress(
                        petState,
                        healthPolicy,
                        stepHours,
                        stepEndUtc);

                    recoveredFromComa |= recovered;
                }
                else
                {
                    bool entered = ApplyHealthEvaluationProgress(
                        petState,
                        healthPolicy,
                        stepHours,
                        stepEndUtc,
                        ref healthEvaluations);

                    enteredComa |= entered;
                }

                processedHours += stepHours;
            }

            // Consume the complete elapsed window even when progression was
            // capped, preventing the same offline time from running twice.
            petState.MarkSimulationUpdated(utcNow);

            return new PetDecayResult(
                applied: true,
                appliedHours: appliedHours,
                wasCapped:
                    actualHours > decayPolicy.MaximumOfflineHours,
                previousHealth: previousHealth,
                currentHealth: petState.Health.Value,
                healthEvaluations: healthEvaluations,
                enteredComa: enteredComa,
                recoveredFromComa: recoveredFromComa);
        }

        private static double GetNextStepHours(
            PetState petState,
            PetHealthPolicy healthPolicy,
            double remainingHours)
        {
            double stepHours = Math.Min(
                remainingHours,
                MaximumSimulationStepHours);

            if (petState.IsInComa)
            {
                double recoveryHoursRemaining = Math.Max(
                    Epsilon,
                    healthPolicy.ComaRecoveryWindowHours -
                    petState.Health.ComaRecoveryProgressHours);

                return Math.Min(stepHours, recoveryHoursRemaining);
            }

            double evaluationMinutesRemaining = Math.Max(
                Epsilon,
                healthPolicy.EvaluationIntervalMinutes -
                petState.Health.EvaluationProgressMinutes);

            return Math.Min(
                stepHours,
                evaluationMinutesRemaining / 60d);
        }

        private static double CalculateEffectiveDecayHours(
            PetDecayPolicy policy,
            double processedActualHours,
            double stepActualHours)
        {
            double fullSpeedHoursRemaining = Math.Max(
                0d,
                policy.FullSpeedOfflineHours -
                processedActualHours);

            double fullSpeedHours = Math.Min(
                stepActualHours,
                fullSpeedHoursRemaining);

            double extendedHours = Math.Max(
                0d,
                stepActualHours - fullSpeedHours);

            return fullSpeedHours +
                (extendedHours *
                    policy.ExtendedOfflineMultiplier);
        }

        private static void ApplyNeedsProgress(
            PetState petState,
            PetDecayPolicy policy,
            double effectiveHours)
        {
            if (petState.IsSleeping || petState.IsInComa)
            {
                ApplySleepingProgress(
                    petState,
                    policy,
                    effectiveHours);
                return;
            }

            ApplyAwakeProgress(
                petState,
                policy,
                effectiveHours);
        }

        private static bool ApplyHealthEvaluationProgress(
            PetState petState,
            PetHealthPolicy policy,
            double stepHours,
            DateTime stepEndUtc,
            ref int healthEvaluations)
        {
            double progressMinutes =
                petState.Health.EvaluationProgressMinutes +
                (stepHours * 60d);

            if (progressMinutes + Epsilon <
                policy.EvaluationIntervalMinutes)
            {
                petState.Health.SetEvaluationProgressMinutes(
                    progressMinutes);
                return false;
            }

            petState.Health.SetEvaluationProgressMinutes(0d);
            healthEvaluations++;

            float averageNeeds = GetAverageNeeds(petState.Needs);

            if (averageNeeds > policy.HealthyAverageThreshold)
            {
                petState.Health.Increase(policy.HealthyHealthGain);
            }
            else if (averageNeeds <
                policy.NeglectedAverageThreshold)
            {
                petState.Health.Reduce(policy.NeglectHealthLoss);
            }

            if (petState.Health.Value > PetHealth.MinimumValue)
            {
                return false;
            }

            return petState.EnterComa(stepEndUtc);
        }

        private static bool ApplyComaRecoveryProgress(
            PetState petState,
            PetHealthPolicy policy,
            double stepHours,
            DateTime stepEndUtc)
        {
            if (!AreAllNeedsAbove(
                    petState.Needs,
                    policy.ComaRecoveryNeedThreshold))
            {
                petState.Health.SetComaRecoveryProgressHours(0d);
                return false;
            }

            double recoveryHours =
                petState.Health.ComaRecoveryProgressHours +
                stepHours;

            petState.Health.SetComaRecoveryProgressHours(
                recoveryHours);

            if (recoveryHours + Epsilon <
                policy.ComaRecoveryWindowHours)
            {
                return false;
            }

            return petState.RecoverFromComa(
                stepEndUtc,
                policy.RecoveredHealthValue);
        }

        private static void NormalizeHealthProgress(
            PetState petState,
            PetHealthPolicy policy)
        {
            if (petState.IsInComa)
            {
                petState.Health.SetEvaluationProgressMinutes(0d);
                return;
            }

            double progress =
                petState.Health.EvaluationProgressMinutes;

            if (progress >= policy.EvaluationIntervalMinutes)
            {
                petState.Health.SetEvaluationProgressMinutes(
                    progress % policy.EvaluationIntervalMinutes);
            }
        }

        private static float GetAverageNeeds(PetNeeds needs)
        {
            return (
                needs.Vitality +
                needs.Rest +
                needs.Mood +
                needs.Allure) / 4f;
        }

        private static bool AreAllNeedsAbove(
            PetNeeds needs,
            float threshold)
        {
            return needs.Vitality > threshold &&
                needs.Rest > threshold &&
                needs.Mood > threshold &&
                needs.Allure > threshold;
        }

        private static PetDecayResult CreateNoChangeResult(
            PetState petState)
        {
            return new PetDecayResult(
                applied: false,
                appliedHours: 0d,
                wasCapped: false,
                previousHealth: petState.Health.Value,
                currentHealth: petState.Health.Value,
                healthEvaluations: 0,
                enteredComa: false,
                recoveredFromComa: false);
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

        private static void Validate(
            PetState petState,
            DateTime utcNow,
            PetDecayPolicy decayPolicy,
            PetHealthPolicy healthPolicy)
        {
            if (petState == null)
            {
                throw new ArgumentNullException(nameof(petState));
            }

            if (decayPolicy == null)
            {
                throw new ArgumentNullException(nameof(decayPolicy));
            }

            if (healthPolicy == null)
            {
                throw new ArgumentNullException(nameof(healthPolicy));
            }

            if (utcNow.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "Current time must use UTC.",
                    nameof(utcNow));
            }
        }
    }
}
