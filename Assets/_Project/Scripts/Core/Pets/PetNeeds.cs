using System;

namespace SuccuPet.Core.Pets
{
    public sealed class PetNeeds
    {
        public const float MinimumValue = 0f;
        public const float MaximumValue = 100f;

        // Legacy property names are retained for save-file compatibility.
        public float Fullness { get; private set; }
        public float Energy { get; private set; }
        public float Happiness { get; private set; }
        public float Hygiene { get; private set; }

        public float Vitality => Fullness;
        public float Rest => Energy;
        public float Mood => Happiness;
        public float Allure => Hygiene;

        public PetNeeds(
            float fullness = MaximumValue,
            float energy = MaximumValue,
            float happiness = MaximumValue,
            float hygiene = MaximumValue)
        {
            Fullness = Clamp(fullness);
            Energy = Clamp(energy);
            Happiness = Clamp(happiness);
            Hygiene = Clamp(hygiene);
        }

        public float GetValue(PetNeedType needType)
        {
            switch (needType)
            {
                case PetNeedType.Vitality:
                    return Vitality;

                case PetNeedType.Rest:
                    return Rest;

                case PetNeedType.Mood:
                    return Mood;

                case PetNeedType.Allure:
                    return Allure;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(needType),
                        needType,
                        "Unknown pet need type.");
            }
        }

        public void Restore(PetNeedType needType, float amount)
        {
            ValidateAmount(amount);
            SetValue(needType, GetValue(needType) + amount);
        }

        public void Reduce(PetNeedType needType, float amount)
        {
            ValidateAmount(amount);
            SetValue(needType, GetValue(needType) - amount);
        }

        private void SetValue(PetNeedType needType, float value)
        {
            float clampedValue = Clamp(value);

            switch (needType)
            {
                case PetNeedType.Vitality:
                    Fullness = clampedValue;
                    break;

                case PetNeedType.Rest:
                    Energy = clampedValue;
                    break;

                case PetNeedType.Mood:
                    Happiness = clampedValue;
                    break;

                case PetNeedType.Allure:
                    Hygiene = clampedValue;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(needType),
                        needType,
                        "Unknown pet need type.");
            }
        }

        private static void ValidateAmount(float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Amount cannot be negative.");
            }
        }

        private static float Clamp(float value)
        {
            if (value < MinimumValue)
            {
                return MinimumValue;
            }

            if (value > MaximumValue)
            {
                return MaximumValue;
            }

            return value;
        }
    }
}