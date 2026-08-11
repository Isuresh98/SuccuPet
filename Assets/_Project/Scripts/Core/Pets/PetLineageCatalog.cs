using System;
using System.Collections.Generic;

namespace SuccuPet.Core.Pets
{
    public static class PetLineageCatalog
    {
        public const string FreeSuccubusCrimsonId =
            "free-succubus-crimson";

        public const string FreeSuccubusVioletId =
            "free-succubus-violet";

        public const string FreeSuccubusMoonId =
            "free-succubus-moon";

        public const string FreeIncubusAzureId =
            "free-incubus-azure";

        public const string CafeSuccubusRoseId =
            "cafe-succubus-rose";

        public const string CafeSuccubusOnyxId =
            "cafe-succubus-onyx";

        public const string CafeSuccubusGoldId =
            "cafe-succubus-gold";

        public const string CafeIncubusSilverId =
            "cafe-incubus-silver";

        // Existing schema-v1-v3 pets receive this lineage during migration.
        public const string LegacyDefaultLineageId =
            FreeSuccubusCrimsonId;

        private static readonly PetLineageDefinition[] Lineages =
        {
            new PetLineageDefinition(
                FreeSuccubusCrimsonId,
                "Crimson Horn",
                PetAestheticType.SuccubusStyle,
                "curled-horns",
                isStarterEligible: true,
                isCafeExclusive: false),

            new PetLineageDefinition(
                FreeSuccubusVioletId,
                "Violet Wing",
                PetAestheticType.SuccubusStyle,
                "violet-wings",
                isStarterEligible: true,
                isCafeExclusive: false),

            new PetLineageDefinition(
                FreeSuccubusMoonId,
                "Moon Glow",
                PetAestheticType.SuccubusStyle,
                "moon-marking",
                isStarterEligible: true,
                isCafeExclusive: false),

            new PetLineageDefinition(
                FreeIncubusAzureId,
                "Azure Crest",
                PetAestheticType.IncubusStyle,
                "azure-crest",
                isStarterEligible: true,
                isCafeExclusive: false),

            new PetLineageDefinition(
                CafeSuccubusRoseId,
                "Cafe Rose",
                PetAestheticType.SuccubusStyle,
                "rose-tail",
                isStarterEligible: false,
                isCafeExclusive: true),

            new PetLineageDefinition(
                CafeSuccubusOnyxId,
                "Cafe Onyx",
                PetAestheticType.SuccubusStyle,
                "onyx-horns",
                isStarterEligible: false,
                isCafeExclusive: true),

            new PetLineageDefinition(
                CafeSuccubusGoldId,
                "Cafe Gold",
                PetAestheticType.SuccubusStyle,
                "gold-glow",
                isStarterEligible: false,
                isCafeExclusive: true),

            new PetLineageDefinition(
                CafeIncubusSilverId,
                "Cafe Silver",
                PetAestheticType.IncubusStyle,
                "silver-wings",
                isStarterEligible: false,
                isCafeExclusive: true)
        };

        public static IReadOnlyList<PetLineageDefinition> All =>
            Lineages;

        public static bool TryGet(
            string lineageId,
            out PetLineageDefinition definition)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(lineageId))
            {
                return false;
            }

            for (int index = 0;
                index < Lineages.Length;
                index++)
            {
                PetLineageDefinition candidate = Lineages[index];

                if (string.Equals(
                        candidate.Id,
                        lineageId.Trim(),
                        StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }

            return false;
        }

        public static PetLineageDefinition GetRequired(
            string lineageId)
        {
            if (TryGet(lineageId, out PetLineageDefinition definition))
            {
                return definition;
            }

            throw new ArgumentException(
                $"Unknown lineage ID: {lineageId}",
                nameof(lineageId));
        }
    }
}
