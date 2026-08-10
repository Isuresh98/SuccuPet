using System;

namespace SuccuPet.Infrastructure.Persistence.Pets
{
    [Serializable]
    public sealed class PetSaveData
    {
        public const int CurrentSchemaVersion = 2;

        public int schemaVersion = CurrentSchemaVersion;

        public string petId;
        public string displayName;
        public string createdAtUtc;

        // Kept so schema-v1 saves can be migrated without data loss.
        public string lastNeedsUpdateUtc;

        public bool isSleeping;
        public long sleepStartedUtcTicks;
        public long lastSimulationUtcTicks;

        public float fullness;
        public float energy;
        public float happiness;
        public float hygiene;

        public int level;
        public int currentExperience;
        public float affection;
        public int coins;
    }
}
