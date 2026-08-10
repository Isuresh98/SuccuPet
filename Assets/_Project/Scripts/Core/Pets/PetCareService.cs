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

        public bool DidLevelUp => CurrentLevel > PreviousLevel;

        public PetCareActionResult(
            PetCareActionType actionType,
            PetNeedType targetNeed,
            float previousNeedValue,
            float currentNeedValue,
            int experienceEarned,
            float affectionEarned,
            int previousLevel,
            int currentLevel)
        {
            ActionType = actionType;
            TargetNeed = targetNeed;
            PreviousNeedValue = previousNeedValue;
            CurrentNeedValue = currentNeedValue;
            ExperienceEarned = experienceEarned;
            AffectionEarned = affectionEarned;
            PreviousLevel = previousLevel;
            CurrentLevel = currentLevel;
        }
    }

    public static class PetCareService
    {
        public static PetCareActionResult Perform(
            PetState petState,
            PetCareActionType actionType)
        {
            if (petState == null)
            {
                throw new ArgumentNullException(nameof(petState));
            }

            PetCareActionDefinition definition =
                PetCarePolicy.GetDefinition(actionType);

            float previousNeedValue =
                petState.Needs.GetValue(definition.TargetNeed);

            int previousLevel = petState.Stats.Level;

            petState.Needs.Restore(
                definition.TargetNeed,
                definition.RestoreAmount);

            petState.Stats.AddExperience(
                definition.ExperienceReward);

            petState.Stats.AddAffection(
                definition.AffectionReward);

            return new PetCareActionResult(
                actionType,
                definition.TargetNeed,
                previousNeedValue,
                petState.Needs.GetValue(definition.TargetNeed),
                definition.ExperienceReward,
                definition.AffectionReward,
                previousLevel,
                petState.Stats.Level);
        }
    }
}