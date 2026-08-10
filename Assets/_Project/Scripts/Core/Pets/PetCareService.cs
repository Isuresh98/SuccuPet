using System;

namespace SuccuPet.Core.Pets
{
    public readonly struct PetCareActionResult
    {
        public PetCareActionType ActionType { get; }
        public PetNeedType TargetNeed { get; }

        public float PreviousNeedValue { get; }
        public float CurrentNeedValue { get; }

        public int ExperienceEarned { get; }
        public float AffectionEarned { get; }

        public int PreviousLevel { get; }
        public int CurrentLevel { get; }

        public bool IsSuccessful { get; }
        public string Message { get; }

        public bool DidLevelUp =>
            IsSuccessful &&
            CurrentLevel > PreviousLevel;

    
        public PetCareActionResult(
            PetCareActionType actionType,
            PetNeedType targetNeed,
            float previousNeedValue,
            float currentNeedValue,
            int experienceEarned,
            float affectionEarned,
            int previousLevel,
            int currentLevel)
            : this(
                actionType,
                targetNeed,
                previousNeedValue,
                currentNeedValue,
                experienceEarned,
                affectionEarned,
                previousLevel,
                currentLevel,
                true,
                string.Empty)
        {
        }

        public PetCareActionResult(
            PetCareActionType actionType,
            PetNeedType targetNeed,
            float previousNeedValue,
            float currentNeedValue,
            int experienceEarned,
            float affectionEarned,
            int previousLevel,
            int currentLevel,
            bool isSuccessful,
            string message)
        {
            ActionType = actionType;
            TargetNeed = targetNeed;

            PreviousNeedValue = previousNeedValue;
            CurrentNeedValue = currentNeedValue;

            ExperienceEarned = experienceEarned;
            AffectionEarned = affectionEarned;

            PreviousLevel = previousLevel;
            CurrentLevel = currentLevel;

            IsSuccessful = isSuccessful;
            Message = message ?? string.Empty;
        }
    }

    public static class PetCareService
    {
        private const float MaximumNeedValue = 100f;

        // Need value 95ට වඩා වැඩි නම් care action එක
        // අවශ්‍ය නැති action එකක් ලෙස reject කරනවා.
        // මෙයින් 99.99 වැනි invisible decay values භාවිත කර
        // XP farming කිරීමත් නවත්වනවා.
        private const float MinimumMissingValueForCare = 5f;

        public static PetCareActionResult Perform(
            PetState petState,
            PetCareActionType actionType)
        {
            if (petState == null)
            {
                throw new ArgumentNullException(
                    nameof(petState));
            }

            PetCareActionDefinition definition =
                PetCarePolicy.GetDefinition(actionType);

            float previousNeedValue =
                petState.Needs.GetValue(
                    definition.TargetNeed);

            int previousLevel =
                petState.Stats.Level;

            float missingNeedValue =
                MaximumNeedValue - previousNeedValue;

            // Full or almost full if action one reject.
            if (missingNeedValue <
                MinimumMissingValueForCare)
            {
                return new PetCareActionResult(
                    actionType,
                    definition.TargetNeed,
                    previousNeedValue,
                    previousNeedValue,
                    0,
                    0f,
                    previousLevel,
                    previousLevel,
                    false,
                    GetRejectedMessage(actionType));
            }

            petState.Needs.Restore(
                definition.TargetNeed,
                definition.RestoreAmount);

            petState.Stats.AddExperience(
                definition.ExperienceReward);

            petState.Stats.AddAffection(
                definition.AffectionReward);

            float currentNeedValue =
                petState.Needs.GetValue(
                    definition.TargetNeed);

            return new PetCareActionResult(
                actionType,
                definition.TargetNeed,
                previousNeedValue,
                currentNeedValue,
                definition.ExperienceReward,
                definition.AffectionReward,
                previousLevel,
                petState.Stats.Level,
                true,
                $"{actionType} completed");
        }

        private static string GetRejectedMessage(
            PetCareActionType actionType)
        {
            switch (actionType)
            {
                case PetCareActionType.Feed:
                    return "Your pet is already full.";

                case PetCareActionType.Rest:
                    return "Your pet is not tired yet.";

                case PetCareActionType.Play:
                    return "Your pet is already happy.";

                case PetCareActionType.Clean:
                    return "Your pet is already clean.";

                default:
                    return "Your pet does not need this care yet.";
            }
        }
    }
}