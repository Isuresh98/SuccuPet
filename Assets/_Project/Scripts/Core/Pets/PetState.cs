using System;

namespace SuccuPet.Core.Pets
{
    public sealed class PetState
    {
        public PetProfile Profile { get; }
        public PetNeeds Needs { get; }
        public PetStats Stats { get; }
        public PetHealth Health { get; }
        public PetOrigin Origin { get; private set; }
        public PetGrowthState Growth { get; }

        public DateTime LastSimulationUtc { get; private set; }

        public bool IsSleeping { get; private set; }
        public DateTime? SleepStartedUtc { get; private set; }

        public bool IsInComa { get; private set; }
        public DateTime? ComaStartedUtc { get; private set; }

        public bool IsDead { get; private set; }
        public DateTime? DiedAtUtc { get; private set; }

        public bool IsAlive => !IsDead;

        public DateTime LastNeedsUpdateUtc =>
            LastSimulationUtc;

        public PetState(
            PetProfile profile,
            PetNeeds needs,
            PetStats stats,
            DateTime lastSimulationUtc,
            bool isSleeping = false,
            DateTime? sleepStartedUtc = null,
            PetHealth health = null,
            bool isInComa = false,
            DateTime? comaStartedUtc = null,
            PetOrigin origin = null,
            PetGrowthState growth = null,
            bool isDead = false,
            DateTime? diedAtUtc = null)
        {
            Profile = profile ??
                throw new ArgumentNullException(nameof(profile));

            Needs = needs ??
                throw new ArgumentNullException(nameof(needs));

            Stats = stats ??
                throw new ArgumentNullException(nameof(stats));

            Health = health ?? new PetHealth();
            Origin = origin ?? PetOrigin.Unselected;

            Growth = growth ?? PetGrowthState.CreateNew(
                profile.CreatedAtUtc);

            ValidateUtc(
                lastSimulationUtc,
                nameof(lastSimulationUtc));

            ValidateOptionalUtc(
                sleepStartedUtc,
                nameof(sleepStartedUtc));

            ValidateOptionalUtc(
                comaStartedUtc,
                nameof(comaStartedUtc));

            ValidateOptionalUtc(
                diedAtUtc,
                nameof(diedAtUtc));

            LastSimulationUtc = lastSimulationUtc;

            IsDead = isDead;

            DiedAtUtc = IsDead
                ? diedAtUtc ??
                    comaStartedUtc ??
                    lastSimulationUtc
                : null;

            IsInComa =
                !IsDead &&
                (isInComa ||
                    Health.Value <= PetHealth.MinimumValue);

            ComaStartedUtc = IsInComa
                ? comaStartedUtc ?? lastSimulationUtc
                : null;

            IsSleeping =
                isSleeping &&
                !IsInComa &&
                !IsDead;

            SleepStartedUtc = IsSleeping
                ? sleepStartedUtc ?? lastSimulationUtc
                : null;
        }

        public static PetState CreateNew(
            string petId,
            string displayName,
            DateTime utcNow)
        {
            PetProfile profile = new PetProfile(
                petId,
                displayName,
                utcNow);

            return new PetState(
                profile,
                new PetNeeds(),
                new PetStats(),
                utcNow,
                origin: PetOrigin.Unselected,
                growth: PetGrowthState.CreateNew(utcNow));
        }

        public bool StartSleeping(DateTime utcNow)
        {
            ValidateUtc(utcNow, nameof(utcNow));

            if (IsDead ||
                IsSleeping ||
                IsInComa ||
                !Origin.HasSelectedLineage ||
                Growth.Stage == PetGrowthStage.Egg)
            {
                return false;
            }

            IsSleeping = true;
            SleepStartedUtc = utcNow;
            return true;
        }

        public bool Wake(DateTime utcNow)
        {
            ValidateUtc(utcNow, nameof(utcNow));

            if (IsDead || !IsSleeping)
            {
                return false;
            }

            IsSleeping = false;
            SleepStartedUtc = null;
            return true;
        }

        internal bool EnterComa(DateTime utcNow)
        {
            ValidateUtc(utcNow, nameof(utcNow));

            if (IsDead || IsInComa)
            {
                return false;
            }

            IsInComa = true;
            ComaStartedUtc = utcNow;

            IsSleeping = false;
            SleepStartedUtc = null;

            Health.SetEvaluationProgressMinutes(0d);
            Health.SetComaRecoveryProgressHours(0d);

            return true;
        }

        internal bool RecoverFromComa(
            DateTime utcNow,
            int restoredHealth)
        {
            ValidateUtc(utcNow, nameof(utcNow));

            if (IsDead || !IsInComa)
            {
                return false;
            }

            Health.RestoreAfterComa(restoredHealth);

            IsInComa = false;
            ComaStartedUtc = null;

            IsSleeping = false;
            SleepStartedUtc = null;

            return true;
        }

        internal bool Die(DateTime utcNow)
        {
            ValidateUtc(utcNow, nameof(utcNow));

            if (IsDead || !IsInComa)
            {
                return false;
            }

            IsDead = true;
            DiedAtUtc = utcNow;

            IsInComa = false;
            ComaStartedUtc = null;

            IsSleeping = false;
            SleepStartedUtc = null;

            Health.SetEvaluationProgressMinutes(0d);
            Health.SetComaRecoveryProgressHours(0d);

            return true;
        }

        internal void AssignOrigin(
            PetOrigin origin,
            DateTime utcNow)
        {
            if (origin == null)
            {
                throw new ArgumentNullException(nameof(origin));
            }

            ValidateUtc(utcNow, nameof(utcNow));

            if (IsDead)
            {
                throw new InvalidOperationException(
                    "A dead pet cannot receive an origin.");
            }

            if (Origin.HasSelectedLineage)
            {
                throw new InvalidOperationException(
                    "Pet origin has already been assigned.");
            }

            if (!origin.HasSelectedLineage)
            {
                throw new ArgumentException(
                    "Assigned origin must contain a lineage.",
                    nameof(origin));
            }

            Origin = origin;
            LastSimulationUtc = utcNow;
        }

        internal void MarkSimulationUpdated(DateTime utcNow)
        {
            ValidateUtc(utcNow, nameof(utcNow));
            LastSimulationUtc = utcNow;
        }

        internal void MarkNeedsUpdated(DateTime utcNow)
        {
            MarkSimulationUpdated(utcNow);
        }

        private static void ValidateOptionalUtc(
            DateTime? value,
            string parameterName)
        {
            if (value.HasValue)
            {
                ValidateUtc(value.Value, parameterName);
            }
        }

        private static void ValidateUtc(
            DateTime value,
            string parameterName)
        {
            if (value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "Time must use UTC.",
                    parameterName);
            }
        }
    }
}