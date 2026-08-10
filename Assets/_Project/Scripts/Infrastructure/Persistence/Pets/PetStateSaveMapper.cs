using System;
using System.Globalization;
using System.IO;
using SuccuPet.Core.Pets;

namespace SuccuPet.Infrastructure.Persistence.Pets
{
    public static class PetStateSaveMapper
    {
        public static PetSaveData ToSaveData(PetState petState)
        {
            if (petState == null)
            {
                throw new ArgumentNullException(nameof(petState));
            }

            return new PetSaveData
            {
                schemaVersion = PetSaveData.CurrentSchemaVersion,

                petId = petState.Profile.PetId,
                displayName = petState.Profile.DisplayName,

                createdAtUtc = petState.Profile.CreatedAtUtc.ToString(
                    "O",
                    CultureInfo.InvariantCulture),

                lastNeedsUpdateUtc =
                    petState.LastNeedsUpdateUtc.ToString(
                        "O",
                        CultureInfo.InvariantCulture),

                fullness = petState.Needs.Fullness,
                energy = petState.Needs.Energy,
                happiness = petState.Needs.Happiness,
                hygiene = petState.Needs.Hygiene,

                level = petState.Stats.Level,
                currentExperience =
                    petState.Stats.CurrentExperience,

                affection = petState.Stats.Affection,
                coins = petState.Stats.Coins
            };
        }

        public static PetState ToDomain(PetSaveData saveData)
        {
            if (saveData == null)
            {
                throw new InvalidDataException(
                    "Pet save data is missing.");
            }

            if (saveData.schemaVersion !=
                PetSaveData.CurrentSchemaVersion)
            {
                throw new InvalidDataException(
                    $"Unsupported save version: " +
                    $"{saveData.schemaVersion}.");
            }

            DateTime createdAtUtc = ParseUtcDateTime(
                saveData.createdAtUtc,
                nameof(saveData.createdAtUtc));

            DateTime lastNeedsUpdateUtc = ParseUtcDateTime(
                saveData.lastNeedsUpdateUtc,
                nameof(saveData.lastNeedsUpdateUtc));

            PetProfile profile = new PetProfile(
                saveData.petId,
                saveData.displayName,
                createdAtUtc);

            PetNeeds needs = new PetNeeds(
                saveData.fullness,
                saveData.energy,
                saveData.happiness,
                saveData.hygiene);

            PetStats stats = new PetStats(
                saveData.level,
                saveData.currentExperience,
                saveData.affection,
                saveData.coins);

            return new PetState(
                profile,
                needs,
                stats,
                lastNeedsUpdateUtc);
        }

        private static DateTime ParseUtcDateTime(
            string value,
            string fieldName)
        {
            bool parsedSuccessfully =
                DateTime.TryParseExact(
                    value,
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime parsedValue);

            if (!parsedSuccessfully ||
                parsedValue.Kind != DateTimeKind.Utc)
            {
                throw new InvalidDataException(
                    $"Save field '{fieldName}' " +
                    $"does not contain a valid UTC time.");
            }

            return parsedValue;
        }
    }
}