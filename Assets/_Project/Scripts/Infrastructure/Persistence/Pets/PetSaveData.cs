using System;

namespace SuccuPet.Infrastructure.Persistence.Pets
{
    [Serializable]
    public sealed class PetSaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;

        public string petId;
        public string displayName;
        public string createdAtUtc;
        public string lastNeedsUpdateUtc;

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