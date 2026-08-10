using System;

namespace SuccuPet.Core.Pets
{
    public sealed class PetStats
    {
        public int Level { get; private set; }
        public int CurrentExperience { get; private set; }
        public float Affection { get; private set; }
        public int Coins { get; private set; }

        public int ExperienceRequiredForNextLevel => Level * 100;

        public PetStats(
            int level = 1,
            int currentExperience = 0,
            float affection = 0f,
            int coins = 0)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            if (currentExperience < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentExperience));
            }

            if (coins < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(coins));
            }

            Level = level;
            CurrentExperience = currentExperience;
            Affection = ClampPercentage(affection);
            Coins = coins;

            ProcessLevelUps();
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            CurrentExperience += amount;
            ProcessLevelUps();
        }

        public void AddAffection(float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Affection = ClampPercentage(Affection + amount);
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Coins += amount;
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (Coins < amount)
            {
                return false;
            }

            Coins -= amount;
            return true;
        }

        private void ProcessLevelUps()
        {
            while (CurrentExperience >= ExperienceRequiredForNextLevel)
            {
                CurrentExperience -= ExperienceRequiredForNextLevel;
                Level++;
            }
        }

        private static float ClampPercentage(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 100f)
            {
                return 100f;
            }

            return value;
        }
    }
}