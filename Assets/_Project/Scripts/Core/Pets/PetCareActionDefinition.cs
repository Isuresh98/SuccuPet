using System;

namespace SuccuPet.Core.Pets
{
    public sealed class PetCareActionDefinition
    {
        public PetCareActionType ActionType { get; }
        public PetNeedType TargetNeed { get; }
        public float RestoreAmount { get; }
        public int ExperienceReward { get; }
        public float AffectionReward { get; }

        public PetCareActionDefinition(
            PetCareActionType actionType,
            PetNeedType targetNeed,
            float restoreAmount,
            int experienceReward,
            float affectionReward)
        {
            if (restoreAmount <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(restoreAmount));
            }

            if (experienceReward <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(experienceReward));
            }

            if (affectionReward < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(affectionReward));
            }

            ActionType = actionType;
            TargetNeed = targetNeed;
            RestoreAmount = restoreAmount;
            ExperienceReward = experienceReward;
            AffectionReward = affectionReward;
        }
    }
}