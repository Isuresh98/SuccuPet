using System;

namespace SuccuPet.Core.Pets
{
    public static class PetCarePolicy
    {
        private static readonly PetCareActionDefinition FeedDefinition =
            new PetCareActionDefinition(
                PetCareActionType.Feed,
                PetNeedType.Vitality,
                restoreAmount: 25f,
                experienceReward: 10,
                affectionReward: 3f);

        // Sleep is now handled by UpdatePetStateUseCase. The legacy values are
        // retained here because PetCareActionDefinition requires positive
        // amounts, but PetCareService rejects direct Sleep care requests.
        private static readonly PetCareActionDefinition SleepDefinition =
            new PetCareActionDefinition(
                PetCareActionType.Sleep,
                PetNeedType.Rest,
                restoreAmount: 35f,
                experienceReward: 8,
                affectionReward: 2f);

        private static readonly PetCareActionDefinition PlayDefinition =
            new PetCareActionDefinition(
                PetCareActionType.Play,
                PetNeedType.Mood,
                restoreAmount: 30f,
                experienceReward: 12,
                affectionReward: 5f);

        private static readonly PetCareActionDefinition BatheDefinition =
            new PetCareActionDefinition(
                PetCareActionType.Bathe,
                PetNeedType.Allure,
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

                case PetCareActionType.Sleep:
                    return SleepDefinition;

                case PetCareActionType.Play:
                    return PlayDefinition;

                case PetCareActionType.Bathe:
                    return BatheDefinition;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(actionType),
                        actionType,
                        "Unknown pet care action.");
            }
        }

        public static string GetActionDisplayName(
            PetCareActionType actionType)
        {
            switch (actionType)
            {
                case PetCareActionType.Feed:
                    return "Feed";

                case PetCareActionType.Sleep:
                    return "Sleep";

                case PetCareActionType.Play:
                    return "Play";

                case PetCareActionType.Bathe:
                    return "Bathe";

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(actionType),
                        actionType,
                        "Unknown pet care action.");
            }
        }

        public static string GetNeedDisplayName(
            PetNeedType needType)
        {
            switch (needType)
            {
                case PetNeedType.Vitality:
                    return "Vitality";

                case PetNeedType.Rest:
                    return "Rest";

                case PetNeedType.Mood:
                    return "Mood";

                case PetNeedType.Allure:
                    return "Allure";

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(needType),
                        needType,
                        "Unknown pet need type.");
            }
        }
    }
}
