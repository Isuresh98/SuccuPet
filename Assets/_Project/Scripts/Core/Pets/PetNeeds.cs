using System;

namespace SuccuPet.Core.Pets
{
    public sealed class PetNeeds
    {
        public const float MinimumValue = 0f;
        public const float MaximumValue = 100f;

        public float Fullness { get; private set; }
        public float Energy { get; private set; }
        public float Happiness { get; private set; }
        public float Hygiene { get; private set; }

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
                case PetNeedType.Fullness:
                    return Fullness;

                case PetNeedType.Energy:
                    return Energy;

                case PetNeedType.Happiness:
                    return Happiness;

                case PetNeedType.Hygiene:
                    return Hygiene;

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
                case PetNeedType.Fullness:
                    Fullness = clampedValue;
                    break;

                case PetNeedType.Energy:
                    Energy = clampedValue;
                    break;

                case PetNeedType.Happiness:
                    Happiness = clampedValue;
                    break;

                case PetNeedType.Hygiene:
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