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
        public bool Died { get; }

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
            bool recoveredFromComa,
            bool died = false)
        {
            Applied = applied;
            AppliedHours = appliedHours;
            WasCapped = wasCapped;
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            HealthEvaluations = healthEvaluations;
            EnteredComa = enteredComa;
            RecoveredFromComa = recoveredFromComa;
            Died = died;
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

            if (petState.IsDead)
            {
                petState.MarkSimulationUpdated(utcNow);
                return CreateNoChangeResult(petState);
            }

            if (!petState.Origin.HasSelectedLineage ||
                petState.Growth.Stage == PetGrowthStage.Egg)
            {
                petState.MarkSimulationUpdated(utcNow);
                return CreateNoChangeResult(petState);
            }

            DateTime simulationStartUtc =
                petState.LastSimulationUtc;

            TimeSpan elapsed =
                utcNow - simulationStartUtc;

            if (elapsed <= TimeSpan.Zero)
            {
                return CreateNoChangeResult(petState);
            }

            double actualHours = elapsed.TotalHours;

            double appliedHours = Math.Min(
                actualHours,
                decayPolicy.MaximumOfflineHours);

            int previousHealth =
                petState.Health.Value;

            int healthEvaluations = 0;
            bool enteredComa = false;
            bool recoveredFromComa = false;
            bool died = false;
            double processedHours = 0d;

            NormalizeHealthProgress(
                petState,
                healthPolicy);

            while (processedHours + Epsilon <
                appliedHours)
            {
                DateTime stepStartUtc =
                    simulationStartUtc.AddHours(
                        processedHours);

                if (petState.IsInComa &&
                    HasReachedComaDeathDeadline(
                        petState,
                        healthPolicy,
                        stepStartUtc))
                {
                    died |= petState.Die(stepStartUtc);
                    break;
                }

                double remainingHours =
                    appliedHours - processedHours;

                double stepHours = GetNextStepHours(
                    petState,
                    healthPolicy,
                    stepStartUtc,
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

                DateTime stepEndUtc =
                    stepStartUtc.AddHours(stepHours);

                if (petState.IsInComa)
                {
                    if (HasReachedComaDeathDeadline(
                            petState,
                            healthPolicy,
                            stepEndUtc))
                    {
                        died |= petState.Die(stepEndUtc);
                    }
                    else
                    {
                        bool recovered =
                            ApplyComaRecoveryProgress(
                                petState,
                                healthPolicy,
                                stepHours,
                                stepEndUtc);

                        recoveredFromComa |= recovered;
                    }
                }
                else
                {
                    bool entered =
                        ApplyHealthEvaluationProgress(
                            petState,
                            healthPolicy,
                            stepHours,
                            stepEndUtc,
                            ref healthEvaluations);

                    enteredComa |= entered;
                }

                processedHours += stepHours;

                if (petState.IsDead)
                {
                    break;
                }
            }

            petState.MarkSimulationUpdated(utcNow);

            return new PetDecayResult(
                applied: true,
                appliedHours: appliedHours,
                wasCapped:
                    actualHours >
                    decayPolicy.MaximumOfflineHours,
                previousHealth: previousHealth,
                currentHealth: petState.Health.Value,
                healthEvaluations: healthEvaluations,
                enteredComa: enteredComa,
                recoveredFromComa: recoveredFromComa,
                died: died);
        }

        private static double GetNextStepHours(
            PetState petState,
            PetHealthPolicy healthPolicy,
            DateTime stepStartUtc,
            double remainingHours)
        {
            double stepHours = Math.Min(
                remainingHours,
                MaximumSimulationStepHours);

            if (petState.IsInComa)
            {
                double recoveryHoursRemaining =
                    Math.Max(
                        Epsilon,
                        healthPolicy.ComaRecoveryWindowHours -
                        petState.Health
                            .ComaRecoveryProgressHours);

                double deathHoursRemaining =
                    GetComaDeathHoursRemaining(
                        petState,
                        healthPolicy,
                        stepStartUtc);

                stepHours = Math.Min(
                    stepHours,
                    recoveryHoursRemaining);

                stepHours = Math.Min(
                    stepHours,
                    Math.Max(
                        Epsilon,
                        deathHoursRemaining));

                return stepHours;
            }

            double evaluationMinutesRemaining =
                Math.Max(
                    Epsilon,
                    healthPolicy.EvaluationIntervalMinutes -
                    petState.Health
                        .EvaluationProgressMinutes);

            return Math.Min(
                stepHours,
                evaluationMinutesRemaining / 60d);
        }

        private static double GetComaDeathHoursRemaining(
            PetState petState,
            PetHealthPolicy healthPolicy,
            DateTime currentUtc)
        {
            if (!petState.ComaStartedUtc.HasValue)
            {
                return healthPolicy.ComaDeathWindowHours;
            }

            DateTime deathDeadlineUtc =
                petState.ComaStartedUtc.Value.AddHours(
                    healthPolicy.ComaDeathWindowHours);

            return (
                deathDeadlineUtc -
                currentUtc).TotalHours;
        }

        private static bool HasReachedComaDeathDeadline(
            PetState petState,
            PetHealthPolicy healthPolicy,
            DateTime currentUtc)
        {
            if (!petState.IsInComa ||
                !petState.ComaStartedUtc.HasValue)
            {
                return false;
            }

            DateTime deathDeadlineUtc =
                petState.ComaStartedUtc.Value.AddHours(
                    healthPolicy.ComaDeathWindowHours);

            return currentUtc >= deathDeadlineUtc;
        }

        private static double CalculateEffectiveDecayHours(
            PetDecayPolicy policy,
            double processedActualHours,
            double stepActualHours)
        {
            double fullSpeedHoursRemaining =
                Math.Max(
                    0d,
                    policy.FullSpeedOfflineHours -
                    processedActualHours);

            double fullSpeedHours =
                Math.Min(
                    stepActualHours,
                    fullSpeedHoursRemaining);

            double extendedHours =
                Math.Max(
                    0d,
                    stepActualHours -
                    fullSpeedHours);

            return fullSpeedHours +
                (extendedHours *
                    policy.ExtendedOfflineMultiplier);
        }

        private static void ApplyNeedsProgress(
            PetState petState,
            PetDecayPolicy policy,
            double effectiveHours)
        {
            if (petState.IsDead)
            {
                return;
            }

            if (petState.IsSleeping ||
                petState.IsInComa)
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

            float averageNeeds =
                GetAverageNeeds(petState.Needs);

            if (averageNeeds >
                policy.HealthyAverageThreshold)
            {
                petState.Health.Increase(
                    policy.HealthyHealthGain);
            }
            else if (averageNeeds <
                policy.NeglectedAverageThreshold)
            {
                petState.Health.Reduce(
                    policy.NeglectHealthLoss);
            }

            if (petState.Health.Value >
                PetHealth.MinimumValue)
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
            if (petState.IsDead)
            {
                return false;
            }

            if (!AreAllNeedsAbove(
                    petState.Needs,
                    policy.ComaRecoveryNeedThreshold))
            {
                petState.Health
                    .SetComaRecoveryProgressHours(0d);

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
            if (petState.IsDead)
            {
                petState.Health
                    .SetEvaluationProgressMinutes(0d);

                petState.Health
                    .SetComaRecoveryProgressHours(0d);

                return;
            }

            if (petState.IsInComa)
            {
                petState.Health
                    .SetEvaluationProgressMinutes(0d);

                return;
            }

            double progress =
                petState.Health
                    .EvaluationProgressMinutes;

            if (progress >=
                policy.EvaluationIntervalMinutes)
            {
                petState.Health
                    .SetEvaluationProgressMinutes(
                        progress %
                        policy.EvaluationIntervalMinutes);
            }
        }

        private static float GetAverageNeeds(
            PetNeeds needs)
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
                recoveredFromComa: false,
                died: false);
        }

        private static void ApplyAwakeProgress(
            PetState petState,
            PetDecayPolicy policy,
            double effectiveHours)
        {
            petState.Needs.Reduce(
                PetNeedType.Vitality,
                (float)(
                    policy.VitalityLossPerHour *
                    effectiveHours));

            petState.Needs.Reduce(
                PetNeedType.Rest,
                (float)(
                    policy.RestLossPerHour *
                    effectiveHours));

            petState.Needs.Reduce(
                PetNeedType.Mood,
                (float)(
                    policy.MoodLossPerHour *
                    effectiveHours));

            petState.Needs.Reduce(
                PetNeedType.Allure,
                (float)(
                    policy.AllureLossPerHour *
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
                (float)(
                    policy.VitalityLossPerHour *
                    slowedHours));

            petState.Needs.Reduce(
                PetNeedType.Mood,
                (float)(
                    policy.MoodLossPerHour *
                    slowedHours));

            petState.Needs.Reduce(
                PetNeedType.Allure,
                (float)(
                    policy.AllureLossPerHour *
                    slowedHours));

            petState.Needs.Restore(
                PetNeedType.Rest,
                (float)(
                    policy.SleepRestRecoveryPerHour *
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
                throw new ArgumentNullException(
                    nameof(petState));
            }

            if (decayPolicy == null)
            {
                throw new ArgumentNullException(
                    nameof(decayPolicy));
            }

            if (healthPolicy == null)
            {
                throw new ArgumentNullException(
                    nameof(healthPolicy));
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