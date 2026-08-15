using System;

namespace SuccuPet.Infrastructure.Persistence.Pets
{
    [Serializable]
    public sealed class PetSaveData
    {
        public const int CurrentSchemaVersion = 6;

        public int schemaVersion = CurrentSchemaVersion;

        public string petId;
        public string displayName;
        public string createdAtUtc;

        public string lastNeedsUpdateUtc;

        public bool isSleeping;
        public long sleepStartedUtcTicks;
        public long lastSimulationUtcTicks;

        public int health;
        public double healthEvaluationProgressMinutes;

        public bool isInComa;
        public long comaStartedUtcTicks;
        public double comaRecoveryProgressHours;

        public bool isDead;
        public long diedAtUtcTicks;

        public bool hasSelectedStarterEgg;
        public string lineageId;
        public int acquisitionType;
        public int colorSeed;
        public int colorRarity;
        public long acquiredAtUtcTicks;

        public int growthStage;
        public int evolutionVariant;
        public int growthPoints;
        public int teenTrainingSessions;
        public long stageStartedAtUtcTicks;

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