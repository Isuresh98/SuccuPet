using System;

namespace SuccuPet.Core.Pets
{
    public readonly struct PetGrowthUpdateResult
    {
        public int PreviousGrowthPoints { get; }
        public int CurrentGrowthPoints { get; }
        public int GrowthPointsAdded { get; }
        public PetEvolutionVariant PreviousVariant { get; }
        public PetEvolutionVariant CurrentVariant { get; }

        public bool VariantChanged =>
            PreviousVariant != CurrentVariant;

        public PetGrowthUpdateResult(
            int previousGrowthPoints,
            int currentGrowthPoints,
            int growthPointsAdded,
            PetEvolutionVariant previousVariant,
            PetEvolutionVariant currentVariant)
        {
            PreviousGrowthPoints = previousGrowthPoints;
            CurrentGrowthPoints = currentGrowthPoints;
            GrowthPointsAdded = growthPointsAdded;
            PreviousVariant = previousVariant;
            CurrentVariant = currentVariant;
        }
    }

    public readonly struct PetEvolutionResult
    {
        public bool IsSuccessful { get; }
        public PetEvolutionGate Gate { get; }
        public PetGrowthStage PreviousStage { get; }
        public PetGrowthStage CurrentStage { get; }
        public PetEvolutionVariant PreviousVariant { get; }
        public PetEvolutionVariant CurrentVariant { get; }
        public string Message { get; }

        public bool DidEvolve =>
            IsSuccessful && PreviousStage != CurrentStage;

        public PetEvolutionResult(
            bool isSuccessful,
            PetEvolutionGate gate,
            PetGrowthStage previousStage,
            PetGrowthStage currentStage,
            PetEvolutionVariant previousVariant,
            PetEvolutionVariant currentVariant,
            string message)
        {
            IsSuccessful = isSuccessful;
            Gate = gate;
            PreviousStage = previousStage;
            CurrentStage = currentStage;
            PreviousVariant = previousVariant;
            CurrentVariant = currentVariant;
            Message = message ?? string.Empty;
        }
    }

    public readonly struct PetTrainingResult
    {
        public bool IsSuccessful { get; }
        public int PreviousSessions { get; }
        public int CurrentSessions { get; }
        public int GrowthPointsEarned { get; }
        public string Message { get; }

        public PetTrainingResult(
            bool isSuccessful,
            int previousSessions,
            int currentSessions,
            int growthPointsEarned,
            string message)
        {
            IsSuccessful = isSuccessful;
            PreviousSessions = previousSessions;
            CurrentSessions = currentSessions;
            GrowthPointsEarned = growthPointsEarned;
            Message = message ?? string.Empty;
        }
    }

    public static class PetGrowthService
    {
        public static PetGrowthUpdateResult AddCareGrowth(
            PetState petState,
            int experienceReward,
            PetGrowthPolicy policy)
        {
            Validate(petState, policy);

            int previousPoints = petState.Growth.GrowthPoints;
            PetEvolutionVariant previousVariant =
                petState.Growth.Variant;

            if (experienceReward > 0)
            {
                petState.Growth.AddGrowthPoints(experienceReward);
            }

            ReevaluateOngoingVariant(petState, policy);

            return new PetGrowthUpdateResult(
                previousPoints,
                petState.Growth.GrowthPoints,
                petState.Growth.GrowthPoints - previousPoints,
                previousVariant,
                petState.Growth.Variant);
        }

        public static PetEvolutionResult CompleteHatching(
            PetState petState,
            DateTime utcNow)
        {
            if (petState == null)
            {
                throw new ArgumentNullException(nameof(petState));
            }

            ValidateUtc(utcNow);

            if (!petState.Origin.HasSelectedLineage)
            {
                return Rejected(
                    petState,
                    PetEvolutionGate.Hatching,
                    "Choose a starter egg before completing hatching.");
            }

            if (petState.Growth.Stage != PetGrowthStage.Egg)
            {
                return Rejected(
                    petState,
                    PetEvolutionGate.Hatching,
                    "This pet has already hatched.");
            }

            PetGrowthStage previousStage = petState.Growth.Stage;

            petState.Growth.AdvanceTo(
                PetGrowthStage.Bat,
                PetEvolutionVariant.None,
                utcNow);

            petState.MarkSimulationUpdated(utcNow);

            return Successful(
                PetEvolutionGate.Hatching,
                previousStage,
                petState.Growth.Stage,
                PetEvolutionVariant.None,
                petState.Growth.Variant,
                "The egg hatched into the Bat (Baby) stage.");
        }

        public static PetEvolutionResult TryEvolve(
            PetState petState,
            DateTime utcNow,
            PetGrowthPolicy policy)
        {
            Validate(petState, policy);
            ValidateUtc(utcNow);

            if (petState.IsInComa)
            {
                return Rejected(
                    petState,
                    PetEvolutionGate.None,
                    "The pet cannot evolve while in a care coma.");
            }

            if (petState.Growth.Stage == PetGrowthStage.Egg)
            {
                return Rejected(
                    petState,
                    PetEvolutionGate.Hatching,
                    "Complete the hatching sequence first.");
            }

            if (petState.Growth.Stage == PetGrowthStage.Adult)
            {
                return Rejected(
                    petState,
                    PetEvolutionGate.None,
                    "The pet is already fully evolved.");
            }

            int requiredPoints = policy.GetRequiredGrowthPoints(
                petState.Growth.Stage);

            if (petState.Growth.GrowthPoints < requiredPoints)
            {
                return Rejected(
                    petState,
                    GetGate(petState.Growth.Stage),
                    $"Growth is not ready: {petState.Growth.GrowthPoints}/{requiredPoints}.");
            }

            PetGrowthStage previousStage = petState.Growth.Stage;
            PetEvolutionVariant previousVariant =
                petState.Growth.Variant;

            if (previousStage == PetGrowthStage.Bat)
            {
                PetEvolutionVariant teenVariant =
                    petState.Health.Value >= policy.SpecialHealthThreshold
                        ? PetEvolutionVariant.Special
                        : PetEvolutionVariant.Default;

                petState.Growth.AdvanceTo(
                    PetGrowthStage.Teen,
                    teenVariant,
                    utcNow);

                return Successful(
                    PetEvolutionGate.GateOne,
                    previousStage,
                    petState.Growth.Stage,
                    previousVariant,
                    petState.Growth.Variant,
                    teenVariant == PetEvolutionVariant.Special
                        ? "Gate 1 complete: Special Teen unlocked."
                        : "Gate 1 complete: Default Teen unlocked.");
            }

            ReevaluateOngoingVariant(petState, policy);

            bool qualifiesForSpecialAdult =
                petState.Growth.Variant == PetEvolutionVariant.Special &&
                petState.Health.Value >= policy.SpecialHealthThreshold &&
                petState.Growth.TeenTrainingSessions >=
                    policy.SpecialAdultTrainingSessionsRequired;

            PetEvolutionVariant adultVariant =
                qualifiesForSpecialAdult
                    ? PetEvolutionVariant.Special
                    : PetEvolutionVariant.Default;

            petState.Growth.AdvanceTo(
                PetGrowthStage.Adult,
                adultVariant,
                utcNow);

            return Successful(
                PetEvolutionGate.GateTwo,
                previousStage,
                petState.Growth.Stage,
                previousVariant,
                petState.Growth.Variant,
                adultVariant == PetEvolutionVariant.Special
                    ? "Gate 2 complete: Special Adult unlocked."
                    : "Gate 2 complete: Default Adult unlocked.");
        }

        public static PetTrainingResult RegisterTeenTraining(
            PetState petState,
            PetGrowthPolicy policy)
        {
            Validate(petState, policy);

            int previousSessions =
                petState.Growth.TeenTrainingSessions;

            if (petState.Growth.Stage != PetGrowthStage.Teen)
            {
                return new PetTrainingResult(
                    false,
                    previousSessions,
                    previousSessions,
                    0,
                    "School/Gym training is available only during the Teen stage.");
            }

            int requiredSessions =
    policy.SpecialAdultTrainingSessionsRequired;

if (previousSessions >= requiredSessions)
{
    return new PetTrainingResult(
        false,
        previousSessions,
        previousSessions,
        0,
        "Required Teen training is already complete.");
}


            if (petState.IsSleeping || petState.IsInComa)
            {
                return new PetTrainingResult(
                    false,
                    previousSessions,
                    previousSessions,
                    0,
                    "The pet must be awake and healthy enough to train.");
            }

            petState.Growth.RegisterTeenTrainingSession(
                policy.TeenTrainingGrowthReward);

            ReevaluateOngoingVariant(petState, policy);

            return new PetTrainingResult(
                true,
                previousSessions,
                petState.Growth.TeenTrainingSessions,
                policy.TeenTrainingGrowthReward,
                "Teen training session completed.");
        }

        public static bool ReevaluateOngoingVariant(
            PetState petState,
            PetGrowthPolicy policy)
        {
            Validate(petState, policy);

            bool isHighCare =
                petState.Health.Value >= policy.SpecialHealthThreshold;

            if (petState.Growth.Stage == PetGrowthStage.Teen)
            {
                return petState.Growth.SetVariant(
                    isHighCare
                        ? PetEvolutionVariant.Special
                        : PetEvolutionVariant.Default);
            }

            if (petState.Growth.Stage == PetGrowthStage.Adult &&
                petState.Growth.Variant == PetEvolutionVariant.Special &&
                !isHighCare)
            {
                return petState.Growth.SetVariant(
                    PetEvolutionVariant.Default);
            }

            return false;
        }

        public static bool IsEvolutionReady(
            PetState petState,
            PetGrowthPolicy policy)
        {
            Validate(petState, policy);

            int required = policy.GetRequiredGrowthPoints(
                petState.Growth.Stage);

            return required > 0 &&
                petState.Growth.GrowthPoints >= required &&
                !petState.IsInComa;
        }

        private static PetEvolutionGate GetGate(PetGrowthStage stage)
        {
            return stage == PetGrowthStage.Bat
                ? PetEvolutionGate.GateOne
                : stage == PetGrowthStage.Teen
                    ? PetEvolutionGate.GateTwo
                    : PetEvolutionGate.None;
        }

        private static PetEvolutionResult Successful(
            PetEvolutionGate gate,
            PetGrowthStage previousStage,
            PetGrowthStage currentStage,
            PetEvolutionVariant previousVariant,
            PetEvolutionVariant currentVariant,
            string message)
        {
            return new PetEvolutionResult(
                true,
                gate,
                previousStage,
                currentStage,
                previousVariant,
                currentVariant,
                message);
        }

        private static PetEvolutionResult Rejected(
            PetState petState,
            PetEvolutionGate gate,
            string message)
        {
            return new PetEvolutionResult(
                false,
                gate,
                petState.Growth.Stage,
                petState.Growth.Stage,
                petState.Growth.Variant,
                petState.Growth.Variant,
                message);
        }

        private static void Validate(
            PetState petState,
            PetGrowthPolicy policy)
        {
            if (petState == null)
            {
                throw new ArgumentNullException(nameof(petState));
            }

            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }
        }

        private static void ValidateUtc(DateTime utcNow)
        {
            if (utcNow.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "Evolution time must use UTC.",
                    nameof(utcNow));
            }
        }
    }
}
