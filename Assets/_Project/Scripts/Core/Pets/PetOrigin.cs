using System;

namespace SuccuPet.Core.Pets
{
    public sealed class PetOrigin
    {
        public string LineageId { get; }
        public PetAcquisitionType AcquisitionType { get; }
        public int ColorSeed { get; }
        public PetColorRarity ColorRarity { get; }
        public DateTime? AcquiredAtUtc { get; }

        public bool HasSelectedLineage =>
            !string.IsNullOrWhiteSpace(LineageId);

        public static PetOrigin Unselected { get; } =
            new PetOrigin(
                string.Empty,
                PetAcquisitionType.None,
                0,
                PetColorRarity.Common,
                null,
                validateLineage: false);

        public PetOrigin(
            string lineageId,
            PetAcquisitionType acquisitionType,
            int colorSeed,
            PetColorRarity colorRarity,
            DateTime acquiredAtUtc)
            : this(
                lineageId,
                acquisitionType,
                colorSeed,
                colorRarity,
                acquiredAtUtc,
                validateLineage: true)
        {
        }

        private PetOrigin(
            string lineageId,
            PetAcquisitionType acquisitionType,
            int colorSeed,
            PetColorRarity colorRarity,
            DateTime? acquiredAtUtc,
            bool validateLineage)
        {
            if (!validateLineage)
            {
                LineageId = string.Empty;
                AcquisitionType = PetAcquisitionType.None;
                ColorSeed = 0;
                ColorRarity = PetColorRarity.Common;
                AcquiredAtUtc = null;
                return;
            }

            PetLineageCatalog.GetRequired(lineageId);

            if (acquisitionType == PetAcquisitionType.None)
            {
                throw new ArgumentException(
                    "Selected lineage requires an acquisition type.",
                    nameof(acquisitionType));
            }

            if (!Enum.IsDefined(
                    typeof(PetAcquisitionType),
                    acquisitionType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(acquisitionType));
            }

            if (!Enum.IsDefined(typeof(PetColorRarity), colorRarity))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(colorRarity));
            }

            if (!acquiredAtUtc.HasValue ||
                acquiredAtUtc.Value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "Acquisition time must use UTC.",
                    nameof(acquiredAtUtc));
            }

            LineageId = lineageId.Trim();
            AcquisitionType = acquisitionType;
            ColorSeed = colorSeed < 0 ? 0 : colorSeed;
            ColorRarity = colorRarity;
            AcquiredAtUtc = acquiredAtUtc;
        }
    }
}
