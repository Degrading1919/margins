using System;
using System.Collections.Generic;
using System.Linq;

namespace Margins
{
    public enum PortfolioPropertyTenure
    {
        Leased = 0,
        Owned = 1
    }

    [Serializable]
    public sealed class PortfolioLocationModificationSnapshot
    {
        public string modificationId;
        public string modificationTypeId;
        public string targetStableId;
        public float localPositionX;
        public float localPositionY;
        public float localPositionZ;
        public float localYawDegrees;
        public string payload;
        public int appliedDay;
    }

    [Serializable]
    public sealed class PersistentGeneratedLayoutSnapshot
    {
        public string generatorVersion;
        public int seed;
        public CommercialBuildingArchetype archetype;
        public OrthogonalFootprintKind footprintKind;
        public int widthFeet;
        public int depthFeet;
        public bool cornerExposure;
        public string businessRecipeId;
        public string selectedUnitId;
        public string canonicalSignature;
        public int layoutRevision;
        public List<PortfolioLocationModificationSnapshot> modifications = new();
    }

    [Serializable]
    public sealed class PortfolioImprovementSnapshot
    {
        public string improvementId;
        public string improvementTypeId;
        public string commercialUnitId;
        public int installedDay;
        public long costCents;
        public int condition;
    }

    [Serializable]
    public sealed class PortfolioCommercialUnitSnapshot
    {
        public string commercialUnitId;
        public string propertyId;
        public string displayName;
        public string occupyingLocationId;
        public PersistentGeneratedLayoutSnapshot generatedLayout;
        public List<PortfolioImprovementSnapshot> improvements = new();
    }

    [Serializable]
    public sealed class PortfolioCommercialPropertySnapshot
    {
        public string propertyId;
        public string displayName;
        public string districtName;
        public PortfolioPropertyTenure tenure;
        public long acquisitionCostCents;
        public int acquiredDay;
        public List<PortfolioCommercialUnitSnapshot> commercialUnits = new();
    }

    [Serializable]
    public sealed class PortfolioBrandSnapshot
    {
        public string brandId;
        public string displayName;
        public string businessTypeId;
    }

    [Serializable]
    public sealed class PortfolioStartupProfileSnapshot
    {
        public string businessName;
        public string primaryColorHex;
        public string secondaryColorHex;
        public string logoSelectionId;
        public int seed;
        public string difficultyPurposeId;
    }

    [Serializable]
    public sealed class NewBusinessSetupData
    {
        public string businessName;
        public string primaryColorHex;
        public string secondaryColorHex;
        public string logoSelectionId;
        public int seed;
        public string difficultyPurposeId;

        public static NewBusinessSetupData CreateDefault()
        {
            PortfolioStartupProfileSnapshot profile =
                PortfolioStartupProfileRules.CreateDefault();
            return new NewBusinessSetupData
            {
                businessName = profile.businessName,
                primaryColorHex = profile.primaryColorHex,
                secondaryColorHex = profile.secondaryColorHex,
                logoSelectionId = profile.logoSelectionId,
                seed = profile.seed,
                difficultyPurposeId = profile.difficultyPurposeId
            };
        }
    }

    /// <summary>
    /// Validates the player-authored startup identity. Difficulty values record
    /// only the four purposes approved by FD-006; they intentionally do not
    /// apply gameplay modifiers in this remediation wave.
    /// </summary>
    public static class PortfolioStartupProfileRules
    {
        public const string DefaultBusinessName = "Mile 7 Market";
        public const string DefaultPrimaryColorHex = "#1F897A";
        public const string DefaultSecondaryColorHex = "#DE5B22";
        public const int DefaultSeed = 1907;
        public const string DefaultLogoSelectionId = "placeholder-mark-a";
        public const string DefaultDifficultyPurposeId =
            "purpose-challenging-recoverable";

        public static readonly string[] LogoSelectionIds =
        {
            "placeholder-mark-a",
            "placeholder-mark-b",
            "placeholder-mark-c"
        };

        public static readonly string[] DifficultyPurposeIds =
        {
            "purpose-forgiving-growth",
            "purpose-challenging-recoverable",
            "purpose-harsher-simulation",
            "purpose-sandbox-experimentation"
        };

        public static PortfolioStartupProfileSnapshot CreateDefault()
        {
            return new PortfolioStartupProfileSnapshot
            {
                businessName = DefaultBusinessName,
                primaryColorHex = DefaultPrimaryColorHex,
                secondaryColorHex = DefaultSecondaryColorHex,
                logoSelectionId = DefaultLogoSelectionId,
                seed = DefaultSeed,
                difficultyPurposeId = DefaultDifficultyPurposeId
            };
        }

        public static bool TryNormalize(
            NewBusinessSetupData source,
            out PortfolioStartupProfileSnapshot profile,
            out string error)
        {
            profile = null;
            if (source == null)
            {
                error = "New Business setup is missing.";
                return false;
            }

            PortfolioStartupProfileSnapshot candidate =
                new PortfolioStartupProfileSnapshot
                {
                    businessName = source.businessName?.Trim(),
                    primaryColorHex = NormalizeHex(source.primaryColorHex),
                    secondaryColorHex = NormalizeHex(source.secondaryColorHex),
                    logoSelectionId = source.logoSelectionId?.Trim(),
                    seed = source.seed,
                    difficultyPurposeId = source.difficultyPurposeId?.Trim()
                };
            if (!TryValidate(candidate, out error))
            {
                return false;
            }

            profile = candidate;
            error = null;
            return true;
        }

        public static bool TryValidate(
            PortfolioStartupProfileSnapshot profile,
            out string error)
        {
            if (profile == null ||
                string.IsNullOrWhiteSpace(profile.businessName) ||
                profile.businessName.Trim().Length < 2 ||
                profile.businessName.Trim().Length > 32)
            {
                error = "Business name must contain 2 to 32 characters.";
                return false;
            }
            if (!IsHexColor(profile.primaryColorHex) ||
                !IsHexColor(profile.secondaryColorHex))
            {
                error = "Business colors must use six-digit hex values such as #1F897A.";
                return false;
            }
            if (!Contains(LogoSelectionIds, profile.logoSelectionId))
            {
                error = "Choose one of the available logos.";
                return false;
            }
            if (!Contains(DifficultyPurposeIds, profile.difficultyPurposeId))
            {
                error = "The starting business profile is unavailable.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool TryApply(
            PortfolioProgressionSnapshot snapshot,
            NewBusinessSetupData setup,
            out string error)
        {
            error = null;
            if (snapshot?.company == null || snapshot.locations == null ||
                !TryNormalize(setup, out PortfolioStartupProfileSnapshot profile,
                    out error))
            {
                error ??= "New Business company state is unavailable.";
                return false;
            }

            snapshot.company.startupProfile = Clone(profile);
            snapshot.company.displayName = profile.businessName;
            PortfolioBrandSnapshot firstBrand = snapshot.company.brands?
                .FirstOrDefault(brand => string.Equals(
                    brand?.brandId,
                    PortfolioPropertyRules.ConvenienceBrandId,
                    StringComparison.Ordinal));
            if (firstBrand != null)
            {
                firstBrand.displayName = profile.businessName;
            }

            PortfolioLocationSnapshot firstLocation = snapshot.locations
                .FirstOrDefault(location => string.Equals(
                    location?.locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal));
            if (firstLocation == null)
            {
                error = "New Business first-location state is missing.";
                return false;
            }
            firstLocation.displayName = profile.businessName;
            error = null;
            return true;
        }

        public static PortfolioStartupProfileSnapshot Clone(
            PortfolioStartupProfileSnapshot source)
        {
            return source == null
                ? null
                : new PortfolioStartupProfileSnapshot
                {
                    businessName = source.businessName,
                    primaryColorHex = source.primaryColorHex,
                    secondaryColorHex = source.secondaryColorHex,
                    logoSelectionId = source.logoSelectionId,
                    seed = source.seed,
                    difficultyPurposeId = source.difficultyPurposeId
                };
        }

        private static bool Contains(string[] accepted, string value)
        {
            return accepted.Any(item => string.Equals(
                item,
                value,
                StringComparison.Ordinal));
        }

        private static string NormalizeHex(string value)
        {
            string accepted = value?.Trim() ?? string.Empty;
            if (!accepted.StartsWith("#", StringComparison.Ordinal))
            {
                accepted = $"#{accepted}";
            }
            return accepted.ToUpperInvariant();
        }

        private static bool IsHexColor(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Length == 7 &&
                   value[0] == '#' &&
                   value.Skip(1).All(Uri.IsHexDigit);
        }
    }

    [Serializable]
    public sealed class PortfolioCompanySnapshot
    {
        public string companyId;
        public string displayName;
        public string activeDetailedLocationId;
        public PortfolioStartupProfileSnapshot startupProfile;
        public List<PortfolioBrandSnapshot> brands = new();
        public List<PortfolioCommercialPropertySnapshot> properties = new();
    }

    public sealed class PortfolioPropertyDefinition
    {
        public PortfolioPropertyDefinition(
            string propertyId,
            string commercialUnitId,
            string locationId,
            string displayName,
            string districtName,
            long acquisitionCostCents,
            int seed,
            CommercialBuildingArchetype archetype,
            OrthogonalFootprintKind footprintKind,
            int widthFeet,
            int depthFeet,
            bool cornerExposure)
        {
            if (!StableIdentifier.IsValid(propertyId) ||
                !StableIdentifier.IsValid(commercialUnitId) ||
                !StableIdentifier.IsValid(locationId) ||
                string.IsNullOrWhiteSpace(displayName) ||
                string.IsNullOrWhiteSpace(districtName) ||
                acquisitionCostCents <= 0 || widthFeet < 12 || depthFeet < 12)
            {
                throw new ArgumentException(
                    "Property definitions require stable identity, positive provisional acquisition cost, and valid authored dimensions.");
            }

            PropertyId = propertyId;
            CommercialUnitId = commercialUnitId;
            LocationId = locationId;
            DisplayName = displayName;
            DistrictName = districtName;
            AcquisitionCostCents = acquisitionCostCents;
            Seed = seed;
            Archetype = archetype;
            FootprintKind = footprintKind;
            WidthFeet = widthFeet;
            DepthFeet = depthFeet;
            CornerExposure = cornerExposure;
        }

        public string PropertyId { get; }
        public string CommercialUnitId { get; }
        public string LocationId { get; }
        public string DisplayName { get; }
        public string DistrictName { get; }
        public long AcquisitionCostCents { get; }
        public int Seed { get; }
        public CommercialBuildingArchetype Archetype { get; }
        public OrthogonalFootprintKind FootprintKind { get; }
        public int WidthFeet { get; }
        public int DepthFeet { get; }
        public bool CornerExposure { get; }
    }

    /// <summary>
    /// Stable holding-company and property identity plus provisional acquisition
    /// configuration. Financing, valuation, zoning, taxes, and tenant simulation
    /// intentionally remain outside this foundation.
    /// </summary>
    public static class PortfolioPropertyRules
    {
        public const string PlayerCompanyId = "company-margins-holdings";
        public const string PlayerCompanyName = "Margins Holdings";
        public const string ConvenienceBrandId = "brand-mile-7-market";
        public const string ConvenienceBrandName = "Mile 7 Market";
        public const string ConvenienceBusinessTypeId =
            "business-convenience-retail";
        public const string ConvenienceRecipeId =
            "graybox-convenience-store";

        private static readonly PortfolioPropertyDefinition[] Definitions =
        {
            new(
                "property-mile-7-market",
                "unit-mile-7-market-01",
                PortfolioProgressionRules.FirstLocationId,
                "Mile 7 Market Property",
                "Cedar Junction",
                750_000,
                1907,
                CommercialBuildingArchetype.StandaloneSmallCommercial,
                OrthogonalFootprintKind.Rectangle,
                48,
                50,
                true),
            new(
                "property-riverbend-strip",
                "unit-riverbend-strip-01",
                "location-riverbend-market",
                "Riverbend Strip Center",
                "Riverbend",
                3_250_000,
                2,
                CommercialBuildingArchetype.StripCenterInlineRetail,
                OrthogonalFootprintKind.Rectangle,
                84,
                50,
                false),
            new(
                "property-downtown-main-street",
                "unit-downtown-main-street-01",
                "location-downtown-market",
                "Exchange Main Street Building",
                "Downtown Exchange",
                4_750_000,
                3141,
                CommercialBuildingArchetype.OlderMainStreetMixedUse,
                OrthogonalFootprintKind.LShape,
                60,
                60,
                true)
        };

        public static IReadOnlyList<PortfolioPropertyDefinition>
            PropertyDefinitions => Definitions;

        public static bool TryGetDefinitionForLocation(
            string locationId,
            out PortfolioPropertyDefinition definition)
        {
            definition = Definitions.FirstOrDefault(value => string.Equals(
                value.LocationId,
                locationId,
                StringComparison.Ordinal));
            return definition != null;
        }

        public static bool TryGetDefinitionForProperty(
            string propertyId,
            out PortfolioPropertyDefinition definition)
        {
            definition = Definitions.FirstOrDefault(value => string.Equals(
                value.PropertyId,
                propertyId,
                StringComparison.Ordinal));
            return definition != null;
        }

        public static PortfolioCompanySnapshot CreateForLocations(
            IReadOnlyList<PortfolioLocationSnapshot> locations)
        {
            if (locations == null)
            {
                throw new ArgumentNullException(nameof(locations));
            }

            PortfolioCompanySnapshot company = new()
            {
                companyId = PlayerCompanyId,
                displayName = PlayerCompanyName,
                startupProfile = PortfolioStartupProfileRules.CreateDefault()
            };
            company.brands.Add(new PortfolioBrandSnapshot
            {
                brandId = ConvenienceBrandId,
                displayName = ConvenienceBrandName,
                businessTypeId = ConvenienceBusinessTypeId
            });
            foreach (PortfolioLocationSnapshot location in locations
                         .OrderBy(value => value.locationId, StringComparer.Ordinal))
            {
                if (!TryGetDefinitionForLocation(
                        location.locationId,
                        out PortfolioPropertyDefinition definition))
                {
                    throw new ArgumentException(
                        $"Location '{location.locationId}' has no approved property identity.",
                        nameof(locations));
                }

                company.properties.Add(CreateLeasedProperty(definition));
            }
            return company;
        }

        public static PortfolioCommercialPropertySnapshot CreateLeasedProperty(
            PortfolioPropertyDefinition definition)
        {
            PortfolioCommercialPropertySnapshot property = new()
            {
                propertyId = definition.PropertyId,
                displayName = definition.DisplayName,
                districtName = definition.DistrictName,
                tenure = PortfolioPropertyTenure.Leased,
                acquisitionCostCents = definition.AcquisitionCostCents,
                acquiredDay = 0
            };
            property.commercialUnits.Add(new PortfolioCommercialUnitSnapshot
            {
                commercialUnitId = definition.CommercialUnitId,
                propertyId = definition.PropertyId,
                displayName = $"{definition.DisplayName} Commercial Unit",
                occupyingLocationId = definition.LocationId,
                generatedLayout = new PersistentGeneratedLayoutSnapshot
                {
                    generatorVersion =
                        ProceduralGenerationResult.CurrentGeneratorVersion,
                    seed = definition.Seed,
                    archetype = definition.Archetype,
                    footprintKind = definition.FootprintKind,
                    widthFeet = definition.WidthFeet,
                    depthFeet = definition.DepthFeet,
                    cornerExposure = definition.CornerExposure,
                    businessRecipeId = ConvenienceRecipeId,
                    selectedUnitId = "unit-01",
                    canonicalSignature = null,
                    layoutRevision = 0
                }
            });
            return property;
        }

        public static bool TryValidate(
            PortfolioCompanySnapshot company,
            IReadOnlyList<PortfolioLocationSnapshot> locations,
            int currentDay,
            out string error)
        {
            error = null;
            if (company == null ||
                !string.Equals(
                    company.companyId,
                    PlayerCompanyId,
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(company.displayName) ||
                !PortfolioStartupProfileRules.TryValidate(
                    company.startupProfile,
                    out error) ||
                company.brands == null || company.brands.Count == 0 ||
                company.properties == null || locations == null ||
                currentDay < 1)
            {
                error = "Player company, brand, property, or location state is missing or contradictory.";
                return false;
            }

            Dictionary<string, PortfolioBrandSnapshot> brandsById =
                new(StringComparer.Ordinal);
            foreach (PortfolioBrandSnapshot brand in company.brands)
            {
                if (brand == null ||
                    !StableIdentifier.IsValid(brand.brandId) ||
                    !StableIdentifier.IsValid(brand.businessTypeId) ||
                    string.IsNullOrWhiteSpace(brand.displayName) ||
                    !brandsById.TryAdd(brand.brandId, brand))
                {
                    error = "Portfolio brands require unique stable identities, business types, and display names.";
                    return false;
                }
            }

            HashSet<string> locationIds = new(
                locations.Select(value => value.locationId),
                StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(company.activeDetailedLocationId) &&
                !locationIds.Contains(company.activeDetailedLocationId))
            {
                error = "The active detailed location is not part of the portfolio.";
                return false;
            }

            Dictionary<string, PortfolioCommercialPropertySnapshot>
                propertiesById = new(StringComparer.Ordinal);
            Dictionary<string, PortfolioCommercialUnitSnapshot> unitsById =
                new(StringComparer.Ordinal);
            HashSet<string> occupiedLocations = new(StringComparer.Ordinal);
            foreach (PortfolioCommercialPropertySnapshot property in company.properties)
            {
                bool hasDefinition = TryGetDefinitionForProperty(
                    property?.propertyId,
                    out PortfolioPropertyDefinition definition);
                if (property == null ||
                    !StableIdentifier.IsValid(property.propertyId) ||
                    !propertiesById.TryAdd(property.propertyId, property) ||
                    string.IsNullOrWhiteSpace(property.displayName) ||
                    string.IsNullOrWhiteSpace(property.districtName) ||
                    property.acquisitionCostCents <= 0 ||
                    (hasDefinition &&
                     (!string.Equals(
                          property.displayName,
                          definition.DisplayName,
                          StringComparison.Ordinal) ||
                      !string.Equals(
                          property.districtName,
                          definition.DistrictName,
                          StringComparison.Ordinal) ||
                      property.acquisitionCostCents !=
                      definition.AcquisitionCostCents)) ||
                    !Enum.IsDefined(
                        typeof(PortfolioPropertyTenure),
                        property.tenure) ||
                    property.acquiredDay < 0 ||
                    property.acquiredDay > currentDay ||
                    (property.tenure == PortfolioPropertyTenure.Leased &&
                     property.acquiredDay != 0) ||
                    (property.tenure == PortfolioPropertyTenure.Owned &&
                     property.acquiredDay == 0) ||
                    property.commercialUnits == null)
                {
                    error = "Commercial property identity, tenure, acquisition, or unit state is invalid.";
                    return false;
                }

                foreach (PortfolioCommercialUnitSnapshot unit in
                         property.commercialUnits)
                {
                    bool occupied = !string.IsNullOrWhiteSpace(
                        unit?.occupyingLocationId);
                    PortfolioPropertyDefinition unitDefinition =
                        hasDefinition && unit != null && string.Equals(
                            unit.commercialUnitId,
                            definition.CommercialUnitId,
                            StringComparison.Ordinal)
                            ? definition
                            : null;
                    if (unit == null ||
                        !StableIdentifier.IsValid(unit.commercialUnitId) ||
                        !unitsById.TryAdd(unit.commercialUnitId, unit) ||
                        !string.Equals(
                            unit.propertyId,
                            property.propertyId,
                            StringComparison.Ordinal) ||
                        string.IsNullOrWhiteSpace(unit.displayName) ||
                        (occupied &&
                         (!locationIds.Contains(unit.occupyingLocationId) ||
                          !occupiedLocations.Add(unit.occupyingLocationId) ||
                          unit.generatedLayout == null)) ||
                        (unit.generatedLayout != null &&
                         !TryValidateGeneratedLayout(
                             unit.generatedLayout,
                             unitDefinition,
                             currentDay,
                             out error)) ||
                        !TryValidateImprovements(
                            unit.improvements,
                            unit.commercialUnitId,
                            currentDay,
                            out error))
                    {
                        error ??= "Commercial unit identity, occupancy, layout, or improvement state is invalid.";
                        return false;
                    }
                }
            }

            foreach (PortfolioLocationSnapshot location in locations)
            {
                if (!brandsById.TryGetValue(
                        location.brandId,
                        out PortfolioBrandSnapshot brand) ||
                    !string.Equals(
                        brand.businessTypeId,
                        location.businessTypeId,
                        StringComparison.Ordinal) ||
                    !propertiesById.TryGetValue(
                        location.propertyId,
                        out PortfolioCommercialPropertySnapshot property) ||
                    !unitsById.TryGetValue(
                        location.commercialUnitId,
                        out PortfolioCommercialUnitSnapshot unit) ||
                    !string.Equals(
                        unit.propertyId,
                        property.propertyId,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        unit.occupyingLocationId,
                        location.locationId,
                        StringComparison.Ordinal))
                {
                    error =
                        $"Business location '{location.locationId}' does not resolve to its brand, property, and occupied commercial unit by stable ID.";
                    return false;
                }
                if (string.Equals(
                        location.locationId,
                        PortfolioProgressionRules.FirstLocationId,
                        StringComparison.Ordinal) &&
                    (!string.Equals(
                         location.displayName,
                         company.startupProfile.businessName,
                         StringComparison.Ordinal) ||
                     !string.Equals(
                         brand.displayName,
                         company.startupProfile.businessName,
                         StringComparison.Ordinal)))
                {
                    error =
                        "The first store and its brand must match the saved New Business identity.";
                    return false;
                }
            }

            if (!occupiedLocations.SetEquals(locationIds))
            {
                error = "Every business location must occupy exactly one persistent commercial unit; other units may remain vacant.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryValidateGeneratedLayout(
            PersistentGeneratedLayoutSnapshot layout,
            PortfolioPropertyDefinition definition,
            int currentDay,
            out string error)
        {
            if (layout == null ||
                !string.Equals(
                    layout.generatorVersion,
                    ProceduralGenerationResult.CurrentGeneratorVersion,
                    StringComparison.Ordinal) ||
                !Enum.IsDefined(
                    typeof(CommercialBuildingArchetype),
                    layout.archetype) ||
                !Enum.IsDefined(
                    typeof(OrthogonalFootprintKind),
                    layout.footprintKind) ||
                layout.widthFeet < 12 || layout.depthFeet < 12 ||
                !StableIdentifier.IsValid(layout.businessRecipeId) ||
                !StableIdentifier.IsValid(layout.selectedUnitId) ||
                (definition != null &&
                 (layout.seed != definition.Seed ||
                  layout.archetype != definition.Archetype ||
                  layout.footprintKind != definition.FootprintKind ||
                  layout.widthFeet != definition.WidthFeet ||
                  layout.depthFeet != definition.DepthFeet ||
                  layout.cornerExposure != definition.CornerExposure ||
                  !string.Equals(
                      layout.businessRecipeId,
                      ConvenienceRecipeId,
                      StringComparison.Ordinal) ||
                  !string.Equals(
                      layout.selectedUnitId,
                      "unit-01",
                      StringComparison.Ordinal))) ||
                layout.layoutRevision < 0 || layout.modifications == null ||
                (!string.IsNullOrWhiteSpace(layout.canonicalSignature) &&
                 (layout.canonicalSignature.Length != 8 ||
                  layout.canonicalSignature.Any(value => !Uri.IsHexDigit(value)))))
            {
                error = definition == null
                    ? "Persistent generated-layout identity is invalid."
                    : "Persistent generated-layout identity contradicts its approved property definition.";
                return false;
            }

            HashSet<string> modificationIds = new(StringComparer.Ordinal);
            foreach (PortfolioLocationModificationSnapshot modification in
                     layout.modifications)
            {
                if (modification == null ||
                    !StableIdentifier.IsValid(modification.modificationId) ||
                    !StableIdentifier.IsValid(modification.modificationTypeId) ||
                    !StableIdentifier.IsValid(modification.targetStableId) ||
                    !modificationIds.Add(modification.modificationId) ||
                    !IsFinite(modification.localPositionX) ||
                    !IsFinite(modification.localPositionY) ||
                    !IsFinite(modification.localPositionZ) ||
                    !IsFinite(modification.localYawDegrees) ||
                    modification.appliedDay < 1 ||
                    modification.appliedDay > currentDay)
                {
                    error = "Persistent location modification contains invalid identity, transform, or timing.";
                    return false;
                }
            }

            if (layout.layoutRevision != layout.modifications.Count)
            {
                error =
                    "Persistent layout revision does not reconcile to recorded player modifications.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryValidateImprovements(
            IReadOnlyList<PortfolioImprovementSnapshot> improvements,
            string unitId,
            int currentDay,
            out string error)
        {
            if (improvements == null)
            {
                error = "Commercial-unit improvement collection is missing.";
                return false;
            }

            HashSet<string> ids = new(StringComparer.Ordinal);
            foreach (PortfolioImprovementSnapshot improvement in improvements)
            {
                if (improvement == null ||
                    !StableIdentifier.IsValid(improvement.improvementId) ||
                    !StableIdentifier.IsValid(improvement.improvementTypeId) ||
                    !ids.Add(improvement.improvementId) ||
                    !string.Equals(
                        improvement.commercialUnitId,
                        unitId,
                        StringComparison.Ordinal) ||
                    improvement.installedDay < 1 ||
                    improvement.installedDay > currentDay ||
                    improvement.costCents < 0 ||
                    improvement.condition < 0 || improvement.condition > 100)
                {
                    error = "Commercial-unit improvement state is invalid.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public static PortfolioCompanySnapshot Clone(
            PortfolioCompanySnapshot source)
        {
            if (source == null)
            {
                return null;
            }

            return new PortfolioCompanySnapshot
            {
                companyId = source.companyId,
                displayName = source.displayName,
                activeDetailedLocationId = source.activeDetailedLocationId,
                startupProfile = PortfolioStartupProfileRules.Clone(
                    source.startupProfile),
                brands = source.brands?
                    .Select(CloneBrand)
                    .ToList() ?? new List<PortfolioBrandSnapshot>(),
                properties = source.properties?
                    .Select(CloneProperty)
                    .ToList() ?? new List<PortfolioCommercialPropertySnapshot>()
            };
        }

        private static PortfolioBrandSnapshot CloneBrand(
            PortfolioBrandSnapshot source)
        {
            return source == null
                ? null
                : new PortfolioBrandSnapshot
                {
                    brandId = source.brandId,
                    displayName = source.displayName,
                    businessTypeId = source.businessTypeId
                };
        }

        private static PortfolioCommercialPropertySnapshot CloneProperty(
            PortfolioCommercialPropertySnapshot source)
        {
            return source == null
                ? null
                : new PortfolioCommercialPropertySnapshot
                {
                    propertyId = source.propertyId,
                    displayName = source.displayName,
                    districtName = source.districtName,
                    tenure = source.tenure,
                    acquisitionCostCents = source.acquisitionCostCents,
                    acquiredDay = source.acquiredDay,
                    commercialUnits = source.commercialUnits?
                        .Select(CloneUnit)
                        .ToList() ?? new List<PortfolioCommercialUnitSnapshot>()
                };
        }

        private static PortfolioCommercialUnitSnapshot CloneUnit(
            PortfolioCommercialUnitSnapshot source)
        {
            return source == null
                ? null
                : new PortfolioCommercialUnitSnapshot
                {
                    commercialUnitId = source.commercialUnitId,
                    propertyId = source.propertyId,
                    displayName = source.displayName,
                    occupyingLocationId = source.occupyingLocationId,
                    generatedLayout = CloneLayout(source.generatedLayout),
                    improvements = source.improvements?
                        .Select(CloneImprovement)
                        .ToList() ?? new List<PortfolioImprovementSnapshot>()
                };
        }

        private static PersistentGeneratedLayoutSnapshot CloneLayout(
            PersistentGeneratedLayoutSnapshot source)
        {
            return source == null
                ? null
                : new PersistentGeneratedLayoutSnapshot
                {
                    generatorVersion = source.generatorVersion,
                    seed = source.seed,
                    archetype = source.archetype,
                    footprintKind = source.footprintKind,
                    widthFeet = source.widthFeet,
                    depthFeet = source.depthFeet,
                    cornerExposure = source.cornerExposure,
                    businessRecipeId = source.businessRecipeId,
                    selectedUnitId = source.selectedUnitId,
                    canonicalSignature = source.canonicalSignature,
                    layoutRevision = source.layoutRevision,
                    modifications = source.modifications?
                        .Select(CloneModification)
                        .ToList() ??
                        new List<PortfolioLocationModificationSnapshot>()
                };
        }

        private static PortfolioLocationModificationSnapshot CloneModification(
            PortfolioLocationModificationSnapshot source)
        {
            return source == null
                ? null
                : new PortfolioLocationModificationSnapshot
                {
                    modificationId = source.modificationId,
                    modificationTypeId = source.modificationTypeId,
                    targetStableId = source.targetStableId,
                    localPositionX = source.localPositionX,
                    localPositionY = source.localPositionY,
                    localPositionZ = source.localPositionZ,
                    localYawDegrees = source.localYawDegrees,
                    payload = source.payload,
                    appliedDay = source.appliedDay
                };
        }

        private static PortfolioImprovementSnapshot CloneImprovement(
            PortfolioImprovementSnapshot source)
        {
            return source == null
                ? null
                : new PortfolioImprovementSnapshot
                {
                    improvementId = source.improvementId,
                    improvementTypeId = source.improvementTypeId,
                    commercialUnitId = source.commercialUnitId,
                    installedDay = source.installedDay,
                    costCents = source.costCents,
                    condition = source.condition
                };
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
