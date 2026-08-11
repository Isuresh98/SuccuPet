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
                    petState.LastSimulationUtc.ToString(
                        "O",
                        CultureInfo.InvariantCulture),

                isSleeping = petState.IsSleeping,
                sleepStartedUtcTicks =
                    petState.SleepStartedUtc.HasValue
                        ? petState.SleepStartedUtc.Value.Ticks
                        : 0L,

                lastSimulationUtcTicks =
                    petState.LastSimulationUtc.Ticks,

                health = petState.Health.Value,
                healthEvaluationProgressMinutes =
                    petState.Health.EvaluationProgressMinutes,

                isInComa = petState.IsInComa,
                comaStartedUtcTicks =
                    petState.ComaStartedUtc.HasValue
                        ? petState.ComaStartedUtc.Value.Ticks
                        : 0L,

                comaRecoveryProgressHours =
                    petState.Health.ComaRecoveryProgressHours,

                hasSelectedStarterEgg =
                    petState.Origin.HasSelectedLineage,

                lineageId = petState.Origin.LineageId,
                acquisitionType =
                    (int)petState.Origin.AcquisitionType,
                colorSeed = petState.Origin.ColorSeed,
                colorRarity =
                    (int)petState.Origin.ColorRarity,

                acquiredAtUtcTicks =
                    petState.Origin.AcquiredAtUtc.HasValue
                        ? petState.Origin.AcquiredAtUtc.Value.Ticks
                        : 0L,

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

            if (saveData.schemaVersion < 1 ||
                saveData.schemaVersion >
                    PetSaveData.CurrentSchemaVersion)
            {
                throw new InvalidDataException(
                    $"Unsupported save version: " +
                    $"{saveData.schemaVersion}.");
            }

            DateTime createdAtUtc = ParseUtcDateTime(
                saveData.createdAtUtc,
                nameof(saveData.createdAtUtc));

            DateTime lastSimulationUtc;
            bool isSleeping;
            DateTime? sleepStartedUtc;
            PetHealth health;
            bool isInComa;
            DateTime? comaStartedUtc;
            PetOrigin origin;

            if (saveData.schemaVersion == 1)
            {
                lastSimulationUtc = ParseUtcDateTime(
                    saveData.lastNeedsUpdateUtc,
                    nameof(saveData.lastNeedsUpdateUtc));

                isSleeping = false;
                sleepStartedUtc = null;

                health = new PetHealth();
                isInComa = false;
                comaStartedUtc = null;
                origin = CreateLegacyOrigin(createdAtUtc);
            }
            else
            {
                lastSimulationUtc = ParseUtcTicks(
                    saveData.lastSimulationUtcTicks,
                    nameof(saveData.lastSimulationUtcTicks));

                isSleeping = saveData.isSleeping;
                sleepStartedUtc = isSleeping
                    ? ParseUtcTicks(
                        saveData.sleepStartedUtcTicks,
                        nameof(saveData.sleepStartedUtcTicks))
                    : (DateTime?)null;

                if (saveData.schemaVersion == 2)
                {
                    health = new PetHealth();
                    isInComa = false;
                    comaStartedUtc = null;
                    origin = CreateLegacyOrigin(createdAtUtc);
                }
                else
                {
                    health = new PetHealth(
                        saveData.health,
                        saveData.healthEvaluationProgressMinutes,
                        saveData.comaRecoveryProgressHours);

                    isInComa = saveData.isInComa ||
                        health.Value <= PetHealth.MinimumValue;

                    comaStartedUtc = saveData.isInComa
                        ? ParseUtcTicks(
                            saveData.comaStartedUtcTicks,
                            nameof(saveData.comaStartedUtcTicks))
                        : isInComa
                            ? lastSimulationUtc
                            : (DateTime?)null;

                    origin = saveData.schemaVersion == 3
                        ? CreateLegacyOrigin(createdAtUtc)
                        : CreateSchemaFourOrigin(saveData);
                }
            }

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
                lastSimulationUtc,
                isSleeping,
                sleepStartedUtc,
                health,
                isInComa,
                comaStartedUtc,
                origin);
        }

        private static PetOrigin CreateLegacyOrigin(
            DateTime createdAtUtc)
        {
            return new PetOrigin(
                PetLineageCatalog.LegacyDefaultLineageId,
                PetAcquisitionType.LegacyMigration,
                0,
                PetColorRarity.Common,
                createdAtUtc);
        }

        private static PetOrigin CreateSchemaFourOrigin(
            PetSaveData saveData)
        {
            if (!saveData.hasSelectedStarterEgg)
            {
                return PetOrigin.Unselected;
            }

            if (!Enum.IsDefined(
                    typeof(PetAcquisitionType),
                    saveData.acquisitionType))
            {
                throw new InvalidDataException(
                    "Save acquisition type is invalid.");
            }

            if (!Enum.IsDefined(
                    typeof(PetColorRarity),
                    saveData.colorRarity))
            {
                throw new InvalidDataException(
                    "Save color rarity is invalid.");
            }

            DateTime acquiredAtUtc = ParseUtcTicks(
                saveData.acquiredAtUtcTicks,
                nameof(saveData.acquiredAtUtcTicks));

            return new PetOrigin(
                saveData.lineageId,
                (PetAcquisitionType)saveData.acquisitionType,
                saveData.colorSeed,
                (PetColorRarity)saveData.colorRarity,
                acquiredAtUtc);
        }

        private static DateTime ParseUtcTicks(
            long ticks,
            string fieldName)
        {
            if (ticks <= DateTime.MinValue.Ticks ||
                ticks > DateTime.MaxValue.Ticks)
            {
                throw new InvalidDataException(
                    $"Save field '{fieldName}' " +
                    "does not contain valid UTC ticks.");
            }

            return new DateTime(ticks, DateTimeKind.Utc);
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
