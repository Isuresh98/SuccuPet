using System;

namespace SuccuPet.Core.Pets
{
    public sealed class PetGrowthState
    {
        public PetGrowthStage Stage { get; private set; }
        public PetEvolutionVariant Variant { get; private set; }
        public int GrowthPoints { get; private set; }
        public int TeenTrainingSessions { get; private set; }
        public DateTime StageStartedAtUtc { get; private set; }

        public bool IsFullyEvolved =>
            Stage == PetGrowthStage.Adult;

        public PetGrowthState(
            PetGrowthStage stage,
            PetEvolutionVariant variant,
            int growthPoints,
            int teenTrainingSessions,
            DateTime stageStartedAtUtc)
        {
            ValidateStageAndVariant(stage, variant);

            if (growthPoints < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(growthPoints));
            }

            if (teenTrainingSessions < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(teenTrainingSessions));
            }

            ValidateUtc(stageStartedAtUtc, nameof(stageStartedAtUtc));

            Stage = stage;
            Variant = variant;
            GrowthPoints = growthPoints;
            TeenTrainingSessions = teenTrainingSessions;
            StageStartedAtUtc = stageStartedAtUtc;
        }

        public static PetGrowthState CreateNew(DateTime utcNow)
        {
            return new PetGrowthState(
                PetGrowthStage.Egg,
                PetEvolutionVariant.None,
                0,
                0,
                utcNow);
        }

        internal void AddGrowthPoints(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (Stage != PetGrowthStage.Bat &&
                Stage != PetGrowthStage.Teen)
            {
                return;
            }

            GrowthPoints = checked(GrowthPoints + amount);
        }

        internal void RegisterTeenTrainingSession(int growthReward)
        {
            if (Stage != PetGrowthStage.Teen)
            {
                throw new InvalidOperationException(
                    "Training sessions can only be registered during the Teen stage.");
            }

            if (growthReward <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(growthReward));
            }

            TeenTrainingSessions = checked(TeenTrainingSessions + 1);
            GrowthPoints = checked(GrowthPoints + growthReward);
        }

        internal void AdvanceTo(
            PetGrowthStage stage,
            PetEvolutionVariant variant,
            DateTime utcNow)
        {
            ValidateUtc(utcNow, nameof(utcNow));
            ValidateStageAndVariant(stage, variant);

            if ((int)stage != (int)Stage + 1)
            {
                throw new InvalidOperationException(
                    "Pet growth stages must advance one stage at a time.");
            }

            Stage = stage;
            Variant = variant;
            GrowthPoints = 0;
            StageStartedAtUtc = utcNow;
        }

        internal bool SetVariant(PetEvolutionVariant variant)
        {
            ValidateStageAndVariant(Stage, variant);

            if (Variant == variant)
            {
                return false;
            }

            Variant = variant;
            return true;
        }

        private static void ValidateStageAndVariant(
            PetGrowthStage stage,
            PetEvolutionVariant variant)
        {
            if (!Enum.IsDefined(typeof(PetGrowthStage), stage))
            {
                throw new ArgumentOutOfRangeException(nameof(stage));
            }

            if (!Enum.IsDefined(typeof(PetEvolutionVariant), variant))
            {
                throw new ArgumentOutOfRangeException(nameof(variant));
            }

            bool isPreGateStage =
                stage == PetGrowthStage.Egg ||
                stage == PetGrowthStage.Bat;

            if (isPreGateStage && variant != PetEvolutionVariant.None)
            {
                throw new ArgumentException(
                    "Egg and Bat stages cannot have an evolution variant.",
                    nameof(variant));
            }

            if (!isPreGateStage && variant == PetEvolutionVariant.None)
            {
                throw new ArgumentException(
                    "Teen and Adult stages require Default or Special variant.",
                    nameof(variant));
            }
        }

        private static void ValidateUtc(
            DateTime value,
            string parameterName)
        {
            if (value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "Growth time must use UTC.",
                    parameterName);
            }
        }
    }
}
