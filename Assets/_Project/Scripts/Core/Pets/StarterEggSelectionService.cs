using System;

namespace SuccuPet.Core.Pets
{
    public readonly struct StarterEggSelectionResult
    {
        public bool IsSuccessful { get; }
        public string Message { get; }
        public PetLineageDefinition Lineage { get; }
        public PetColorRarity ColorRarity { get; }

        public StarterEggSelectionResult(
            bool isSuccessful,
            string message,
            PetLineageDefinition lineage,
            PetColorRarity colorRarity)
        {
            IsSuccessful = isSuccessful;
            Message = message ?? string.Empty;
            Lineage = lineage;
            ColorRarity = colorRarity;
        }
    }

    public static class StarterEggSelectionService
    {
        public static StarterEggSelectionResult Select(
            PetState petState,
            string lineageId,
            DateTime utcNow)
        {
            if (petState == null)
            {
                throw new ArgumentNullException(nameof(petState));
            }

            if (utcNow.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "Current time must use UTC.",
                    nameof(utcNow));
            }

            if (petState.Origin.HasSelectedLineage)
            {
                return Rejected(
                    "A starter egg has already been selected.");
            }

            if (!PetLineageCatalog.TryGet(
                    lineageId,
                    out PetLineageDefinition lineage))
            {
                return Rejected("The selected egg does not exist.");
            }

            if (lineage.IsCafeExclusive)
            {
                return Rejected(
                    "This egg is unlocked only by a verified cafe visit.",
                    lineage);
            }

            if (!lineage.IsStarterEligible)
            {
                return Rejected(
                    "This egg is not available as a starter.",
                    lineage);
            }

            int colorSeed = CreateColorSeed(
                petState.Profile.PetId,
                lineage.Id,
                utcNow.Ticks);

            PetColorRarity rarity = GetRarity(colorSeed);

            PetOrigin origin = new PetOrigin(
                lineage.Id,
                PetAcquisitionType.StarterEgg,
                colorSeed,
                rarity,
                utcNow);

            petState.AssignOrigin(origin, utcNow);

            PrepareNeedsForFirstCare(petState);

            return new StarterEggSelectionResult(
                true,
                $"{lineage.DisplayName} selected.",
                lineage,
                rarity);
        }

        private static void PrepareNeedsForFirstCare(
            PetState petState)
        {
            // A new PetNeeds instance starts at 100. These three reductions
            // guarantee that the guided Feed, Bathe and Play actions are
            // genuine successful care actions instead of rejected full-stat
            // button presses. Policy values are reused so future balancing
            // changes stay synchronized with the tutorial setup.
            ReduceByActionAmount(
                petState,
                PetCareActionType.Feed);

            ReduceByActionAmount(
                petState,
                PetCareActionType.Bathe);

            ReduceByActionAmount(
                petState,
                PetCareActionType.Play);
        }

        private static void ReduceByActionAmount(
            PetState petState,
            PetCareActionType actionType)
        {
            PetCareActionDefinition definition =
                PetCarePolicy.GetDefinition(actionType);

            petState.Needs.Reduce(
                definition.TargetNeed,
                definition.RestoreAmount);
        }

        private static StarterEggSelectionResult Rejected(
            string message,
            PetLineageDefinition lineage = null)
        {
            return new StarterEggSelectionResult(
                false,
                message,
                lineage,
                PetColorRarity.Common);
        }

        private static int CreateColorSeed(
            string petId,
            string lineageId,
            long utcTicks)
        {
            unchecked
            {
                uint hash = 2166136261u;
                AddStringToHash(ref hash, petId);
                AddStringToHash(ref hash, lineageId);
                hash = (hash ^ (uint)utcTicks) * 16777619u;
                hash = (hash ^ (uint)(utcTicks >> 32)) * 16777619u;
                return (int)(hash & 0x7FFFFFFF);
            }
        }

        private static void AddStringToHash(
            ref uint hash,
            string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            for (int index = 0;
                index < value.Length;
                index++)
            {
                hash = (hash ^ value[index]) * 16777619u;
            }
        }

        private static PetColorRarity GetRarity(int colorSeed)
        {
            int roll = colorSeed % 100;

            if (roll < 80)
            {
                return PetColorRarity.Common;
            }

            if (roll < 95)
            {
                return PetColorRarity.Uncommon;
            }

            return PetColorRarity.Rare;
        }
    }
}