using System;

namespace SuccuPet.Core.Pets
{
    public sealed class PetGrowthPolicy
    {
        public int BatGrowthPointsRequired { get; }
        public int TeenGrowthPointsRequired { get; }
        public int SpecialHealthThreshold { get; }
        public int SpecialAdultTrainingSessionsRequired { get; }
        public int TeenTrainingGrowthReward { get; }

        public static PetGrowthPolicy Default { get; } =
            new PetGrowthPolicy(
                batGrowthPointsRequired: 100,
                teenGrowthPointsRequired: 250,
                specialHealthThreshold: 60,
                specialAdultTrainingSessionsRequired: 5,
                teenTrainingGrowthReward: 20);

        public PetGrowthPolicy(
            int batGrowthPointsRequired,
            int teenGrowthPointsRequired,
            int specialHealthThreshold,
            int specialAdultTrainingSessionsRequired,
            int teenTrainingGrowthReward)
        {
            if (batGrowthPointsRequired <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(batGrowthPointsRequired));
            }

            if (teenGrowthPointsRequired <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(teenGrowthPointsRequired));
            }

            if (specialHealthThreshold < PetHealth.MinimumValue ||
                specialHealthThreshold > PetHealth.MaximumValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(specialHealthThreshold));
            }

            if (specialAdultTrainingSessionsRequired <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(specialAdultTrainingSessionsRequired));
            }

            if (teenTrainingGrowthReward <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(teenTrainingGrowthReward));
            }

            BatGrowthPointsRequired = batGrowthPointsRequired;
            TeenGrowthPointsRequired = teenGrowthPointsRequired;
            SpecialHealthThreshold = specialHealthThreshold;
            SpecialAdultTrainingSessionsRequired =
                specialAdultTrainingSessionsRequired;
            TeenTrainingGrowthReward = teenTrainingGrowthReward;
        }

        public int GetRequiredGrowthPoints(PetGrowthStage stage)
        {
            switch (stage)
            {
                case PetGrowthStage.Bat:
                    return BatGrowthPointsRequired;

                case PetGrowthStage.Teen:
                    return TeenGrowthPointsRequired;

                default:
                    return 0;
            }
        }
    }
}
