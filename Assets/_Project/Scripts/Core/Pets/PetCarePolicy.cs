using System;

namespace SuccuPet.Core.Pets
{
    public static class PetCarePolicy
    {
        private static readonly PetCareActionDefinition FeedDefinition =
            new PetCareActionDefinition(
                PetCareActionType.Feed,
                PetNeedType.Fullness,
                restoreAmount: 25f,
                experienceReward: 10,
                affectionReward: 3f);

        private static readonly PetCareActionDefinition RestDefinition =
            new PetCareActionDefinition(
                PetCareActionType.Rest,
                PetNeedType.Energy,
                restoreAmount: 35f,
                experienceReward: 8,
                affectionReward: 2f);

        private static readonly PetCareActionDefinition PlayDefinition =
            new PetCareActionDefinition(
                PetCareActionType.Play,
                PetNeedType.Happiness,
                restoreAmount: 30f,
                experienceReward: 12,
                affectionReward: 5f);

        private static readonly PetCareActionDefinition CleanDefinition =
            new PetCareActionDefinition(
                PetCareActionType.Clean,
                PetNeedType.Hygiene,
                restoreAmount: 40f,
                experienceReward: 10,
                affectionReward: 3f);

        public static PetCareActionDefinition GetDefinition(
            PetCareActionType actionType)
        {
            switch (actionType)
            {
                case PetCareActionType.Feed:
                    return FeedDefinition;

                case PetCareActionType.Rest:
                    return RestDefinition;

                case PetCareActionType.Play:
                    return PlayDefinition;

                case PetCareActionType.Clean:
                    return CleanDefinition;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(actionType),
                        actionType,
                        "Unknown pet care action.");
            }
        }
    }
}