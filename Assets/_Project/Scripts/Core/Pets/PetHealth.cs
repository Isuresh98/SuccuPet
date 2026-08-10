using System;

namespace SuccuPet.Core.Pets
{
    public enum PetHealthStatus
    {
        Healthy = 0,
        Fatigued = 1,
        Critical = 2,
        Comatose = 3
    }

    public sealed class PetHealth
    {
        public const int MinimumValue = 0;
        public const int MaximumValue = 100;

        public int Value { get; private set; }
        public double EvaluationProgressMinutes { get; private set; }
        public double ComaRecoveryProgressHours { get; private set; }

        public PetHealthStatus Status
        {
            get
            {
                if (Value <= MinimumValue)
                {
                    return PetHealthStatus.Comatose;
                }

                if (Value < 30)
                {
                    return PetHealthStatus.Critical;
                }

                if (Value < 60)
                {
                    return PetHealthStatus.Fatigued;
                }

                return PetHealthStatus.Healthy;
            }
        }

        public PetHealth(
            int value = MaximumValue,
            double evaluationProgressMinutes = 0d,
            double comaRecoveryProgressHours = 0d)
        {
            Value = ClampValue(value);
            EvaluationProgressMinutes = Math.Max(
                0d,
                evaluationProgressMinutes);

            ComaRecoveryProgressHours = Math.Max(
                0d,
                comaRecoveryProgressHours);
        }

        internal void Increase(int amount)
        {
            ValidateAmount(amount);
            Value = ClampValue(Value + amount);
        }

        internal void Reduce(int amount)
        {
            ValidateAmount(amount);
            Value = ClampValue(Value - amount);
        }

        internal void SetEvaluationProgressMinutes(double minutes)
        {
            EvaluationProgressMinutes = Math.Max(0d, minutes);
        }

        internal void SetComaRecoveryProgressHours(double hours)
        {
            ComaRecoveryProgressHours = Math.Max(0d, hours);
        }

        internal void RestoreAfterComa(int restoredHealth)
        {
            Value = ClampValue(restoredHealth);
            EvaluationProgressMinutes = 0d;
            ComaRecoveryProgressHours = 0d;
        }

        private static int ClampValue(int value)
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

        private static void ValidateAmount(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }
        }
    }
}
