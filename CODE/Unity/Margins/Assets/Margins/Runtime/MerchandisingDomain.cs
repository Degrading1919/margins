using System;
using System.Collections.Generic;
using System.Linq;

namespace Margins
{
    [Serializable]
    public sealed class MerchandisePriceSnapshot
    {
        public string productId;
        public int referencePriceCents;
        public int salePriceCents;
    }

    [Serializable]
    public sealed class ShelfMerchandiseAssignmentSnapshot
    {
        public string shelfFixtureId;
        public string inventoryLocationId;
        public string assignedProductId;
        public string customDisplayLabel;
    }

    [Serializable]
    public sealed class MerchandiseSaleLineSnapshot
    {
        public string productId;
        public int unitPriceCents;
        public int quantityUnits;

        public long GrossSalesCents =>
            checked((long)unitPriceCents * quantityUnits);
    }

    public readonly struct MerchandiseOffer
    {
        public MerchandiseOffer(
            string productId,
            string shelfFixtureId,
            string inventoryLocationId,
            int referencePriceCents,
            int salePriceCents,
            string customDisplayLabel)
        {
            ProductId = productId;
            ShelfFixtureId = shelfFixtureId;
            InventoryLocationId = inventoryLocationId;
            ReferencePriceCents = referencePriceCents;
            SalePriceCents = salePriceCents;
            CustomDisplayLabel = customDisplayLabel ?? string.Empty;
        }

        public string ProductId { get; }
        public string ShelfFixtureId { get; }
        public string InventoryLocationId { get; }
        public int ReferencePriceCents { get; }
        public int SalePriceCents { get; }
        public string CustomDisplayLabel { get; }
    }

    /// <summary>
    /// Validation and deterministic price response for the small persistent
    /// merchandising model. Product definitions remain catalog identity;
    /// mutable retail prices and shelf assignments live on each business.
    /// </summary>
    public static class MerchandisingRules
    {
        public const int MaximumSalePriceCents = 999_999;
        public const int MaximumCustomDisplayLabelLength = 32;
        public const int BasisPoints = 10_000;

        public static bool TryValidate(
            IReadOnlyList<MerchandisePriceSnapshot> prices,
            IReadOnlyList<ShelfMerchandiseAssignmentSnapshot> assignments,
            out string error)
        {
            if (prices == null || prices.Count == 0 || assignments == null)
            {
                error = "Merchandising requires product prices and a shelf-assignment collection.";
                return false;
            }

            HashSet<string> productIds = new(StringComparer.Ordinal);
            foreach (MerchandisePriceSnapshot price in prices)
            {
                if (price == null ||
                    !FirstStoreIdentifier.IsValid(price.productId) ||
                    price.referencePriceCents <= 0 ||
                    price.salePriceCents <= 0 ||
                    price.referencePriceCents > MaximumSalePriceCents ||
                    price.salePriceCents > MaximumSalePriceCents ||
                    !productIds.Add(price.productId))
                {
                    error = "Merchandising contains an invalid or duplicate product price.";
                    return false;
                }
            }

            HashSet<string> fixtureIds = new(StringComparer.Ordinal);
            HashSet<string> locationIds = new(StringComparer.Ordinal);
            HashSet<string> assignedProductIds = new(StringComparer.Ordinal);
            foreach (ShelfMerchandiseAssignmentSnapshot assignment in assignments)
            {
                string label = assignment?.customDisplayLabel ?? string.Empty;
                if (assignment == null ||
                    !FirstStoreIdentifier.IsValid(assignment.shelfFixtureId) ||
                    !FirstStoreIdentifier.IsValid(assignment.inventoryLocationId) ||
                    !fixtureIds.Add(assignment.shelfFixtureId) ||
                    !locationIds.Add(assignment.inventoryLocationId) ||
                    label.Length > MaximumCustomDisplayLabelLength ||
                    label.Any(char.IsControl))
                {
                    error = "Merchandising contains an invalid or duplicate shelf assignment.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(assignment.assignedProductId))
                {
                    continue;
                }

                if (!productIds.Contains(assignment.assignedProductId) ||
                    !assignedProductIds.Add(assignment.assignedProductId))
                {
                    error =
                        $"Product '{assignment.assignedProductId}' is unavailable or assigned to more than one shelf.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public static bool TryGetOfferForProduct(
            PortfolioLocationSnapshot location,
            string productId,
            out MerchandiseOffer offer)
        {
            offer = default;
            if (location?.merchandisePrices == null ||
                location.shelfMerchandiseAssignments == null ||
                !FirstStoreIdentifier.IsValid(productId))
            {
                return false;
            }

            MerchandisePriceSnapshot price = location.merchandisePrices
                .FirstOrDefault(value => value != null && string.Equals(
                    value.productId,
                    productId,
                    StringComparison.Ordinal));
            ShelfMerchandiseAssignmentSnapshot assignment =
                location.shelfMerchandiseAssignments.FirstOrDefault(value =>
                    value != null && string.Equals(
                        value.assignedProductId,
                        productId,
                        StringComparison.Ordinal));
            if (price == null || assignment == null)
            {
                return false;
            }

            offer = new MerchandiseOffer(
                price.productId,
                assignment.shelfFixtureId,
                assignment.inventoryLocationId,
                price.referencePriceCents,
                price.salePriceCents,
                assignment.customDisplayLabel);
            return true;
        }

        public static bool TryGetOfferForShelf(
            PortfolioLocationSnapshot location,
            string shelfFixtureId,
            out MerchandiseOffer offer)
        {
            offer = default;
            if (location?.shelfMerchandiseAssignments == null ||
                !FirstStoreIdentifier.IsValid(shelfFixtureId))
            {
                return false;
            }

            ShelfMerchandiseAssignmentSnapshot assignment =
                location.shelfMerchandiseAssignments.FirstOrDefault(value =>
                    value != null && string.Equals(
                        value.shelfFixtureId,
                        shelfFixtureId,
                        StringComparison.Ordinal));
            return assignment != null &&
                   !string.IsNullOrWhiteSpace(assignment.assignedProductId) &&
                   TryGetOfferForProduct(
                       location,
                       assignment.assignedProductId,
                       out offer);
        }

        public static int CalculatePurchaseAcceptanceBasisPoints(
            int salePriceCents,
            int referencePriceCents)
        {
            if (salePriceCents <= 0 || referencePriceCents <= 0)
            {
                return 0;
            }

            long ratio = checked(
                (long)salePriceCents * BasisPoints / referencePriceCents);
            if (ratio <= 7_500)
            {
                return 10_000;
            }
            if (ratio <= 10_000)
            {
                return InterpolateDown(
                    ratio,
                    7_500,
                    10_000,
                    10_000,
                    9_500);
            }
            if (ratio <= 12_500)
            {
                return InterpolateDown(
                    ratio,
                    10_000,
                    12_500,
                    9_500,
                    7_000);
            }
            if (ratio <= 15_000)
            {
                return InterpolateDown(ratio, 12_500, 15_000, 7_000, 3_500);
            }
            if (ratio <= 20_000)
            {
                return InterpolateDown(ratio, 15_000, 20_000, 3_500, 500);
            }
            if (ratio <= 22_500)
            {
                return InterpolateDown(ratio, 20_000, 22_500, 500, 0);
            }
            return 0;
        }

        public static bool WillPurchase(
            string customerId,
            string productId,
            int salePriceCents,
            int referencePriceCents)
        {
            int acceptance = CalculatePurchaseAcceptanceBasisPoints(
                salePriceCents,
                referencePriceCents);
            return acceptance >= BasisPoints ||
                   StableRollBasisPoints(customerId, productId) < acceptance;
        }

        public static int ApplyDemandResponse(
            int potentialDemandUnits,
            int salePriceCents,
            int referencePriceCents)
        {
            if (potentialDemandUnits <= 0)
            {
                return 0;
            }

            int acceptance = CalculatePurchaseAcceptanceBasisPoints(
                salePriceCents,
                referencePriceCents);
            return checked((int)Math.Min(
                int.MaxValue,
                ((long)potentialDemandUnits * acceptance + BasisPoints / 2) /
                BasisPoints));
        }

        public static List<MerchandiseSaleLineSnapshot> AggregateCompletedSales(
            IReadOnlyList<CheckoutTransactionSummary> transactions)
        {
            SortedDictionary<(string ProductId, int UnitPriceCents), int> totals =
                new(Comparer<(string ProductId, int UnitPriceCents)>.Create(
                    (left, right) =>
                    {
                        int product = StringComparer.Ordinal.Compare(
                            left.ProductId,
                            right.ProductId);
                        return product != 0
                            ? product
                            : left.UnitPriceCents.CompareTo(right.UnitPriceCents);
                    }));
            if (transactions != null)
            {
                foreach (CheckoutTransactionSummary transaction in transactions)
                {
                    if (transaction?.lines == null)
                    {
                        continue;
                    }

                    foreach (CheckoutLineSnapshot line in transaction.lines)
                    {
                        var key = (line.productId, line.unitPriceCents);
                        totals.TryGetValue(key, out int existing);
                        totals[key] = checked(existing + line.quantityUnits);
                    }
                }
            }

            return totals.Select(value => new MerchandiseSaleLineSnapshot
            {
                productId = value.Key.ProductId,
                unitPriceCents = value.Key.UnitPriceCents,
                quantityUnits = value.Value
            }).ToList();
        }

        public static bool TryValidateSalesBreakdown(
            IReadOnlyList<MerchandiseSaleLineSnapshot> lines,
            int expectedUnits,
            long expectedGrossSalesCents,
            out string error)
        {
            if (lines == null || (expectedUnits > 0 && lines.Count == 0))
            {
                error = "An exact merchandise sales breakdown is missing.";
                return false;
            }

            int units = 0;
            long gross = 0;
            HashSet<string> keys = new(StringComparer.Ordinal);
            try
            {
                foreach (MerchandiseSaleLineSnapshot line in lines)
                {
                    string key = $"{line?.productId}\n{line?.unitPriceCents}";
                    if (line == null ||
                        !FirstStoreIdentifier.IsValid(line.productId) ||
                        line.unitPriceCents <= 0 ||
                        line.unitPriceCents > MaximumSalePriceCents ||
                        line.quantityUnits <= 0 ||
                        !keys.Add(key))
                    {
                        error = "A merchandise sales line is invalid or duplicated.";
                        return false;
                    }
                    units = checked(units + line.quantityUnits);
                    gross = checked(gross + line.GrossSalesCents);
                }
            }
            catch (OverflowException)
            {
                error = "Merchandise sales breakdown overflowed integer-cent storage.";
                return false;
            }

            if (units != expectedUnits || gross != expectedGrossSalesCents)
            {
                error = "Merchandise sales lines do not reconcile to the report totals.";
                return false;
            }

            error = null;
            return true;
        }

        public static List<MerchandisePriceSnapshot> CreateConvenienceStorePrices(
            PortfolioPricingPolicy legacyPreset)
        {
            return new List<MerchandisePriceSnapshot>
            {
                CreatePrice("prod-cola-can-355ml", 149, legacyPreset),
                CreatePrice("prod-potato-chips-small", 199, legacyPreset)
            };
        }

        public static List<ShelfMerchandiseAssignmentSnapshot>
            CreateConvenienceStoreAssignments(string locationId)
        {
            bool firstStore = string.Equals(
                locationId,
                PortfolioProgressionRules.FirstLocationId,
                StringComparison.Ordinal);
            string prefix = firstStore ? string.Empty : $"{locationId}-";
            return new List<ShelfMerchandiseAssignmentSnapshot>
            {
                new()
                {
                    shelfFixtureId = firstStore
                        ? "fixture-shelf-cola-validation"
                        : $"{prefix}fixture-merchandise-01",
                    inventoryLocationId = firstStore
                        ? "loc-shelf-cola"
                        : $"{prefix}loc-merchandise-01",
                    assignedProductId = "prod-cola-can-355ml"
                },
                new()
                {
                    shelfFixtureId = firstStore
                        ? "fixture-shelf-chips-validation"
                        : $"{prefix}fixture-merchandise-02",
                    inventoryLocationId = firstStore
                        ? "loc-shelf-chips"
                        : $"{prefix}loc-merchandise-02",
                    assignedProductId = "prod-potato-chips-small"
                }
            };
        }

        private static MerchandisePriceSnapshot CreatePrice(
            string productId,
            int referencePriceCents,
            PortfolioPricingPolicy preset)
        {
            return new MerchandisePriceSnapshot
            {
                productId = productId,
                referencePriceCents = referencePriceCents,
                salePriceCents = CalculatePresetSalePrice(
                    referencePriceCents,
                    preset)
            };
        }

        public static int CalculatePresetSalePrice(
            int referencePriceCents,
            PortfolioPricingPolicy preset)
        {
            return preset switch
            {
                PortfolioPricingPolicy.Value =>
                    Math.Max(1, referencePriceCents * 85 / 100),
                PortfolioPricingPolicy.Premium =>
                    Math.Max(1, referencePriceCents * 140 / 100),
                _ => referencePriceCents
            };
        }

        private static int InterpolateDown(
            long value,
            long start,
            long end,
            int startAcceptance,
            int endAcceptance)
        {
            long distance = value - start;
            long range = end - start;
            long reduction =
                (long)(startAcceptance - endAcceptance) * distance / range;
            return Math.Max(0, startAcceptance - (int)reduction);
        }

        private static int StableRollBasisPoints(
            string customerId,
            string productId)
        {
            unchecked
            {
                uint hash = 2166136261;
                AddStable(ref hash, customerId);
                hash ^= (byte)':';
                hash *= 16777619;
                AddStable(ref hash, productId);
                return (int)(hash % BasisPoints);
            }
        }

        private static void AddStable(ref uint hash, string value)
        {
            value ??= string.Empty;
            for (int index = 0; index < value.Length; index++)
            {
                hash ^= value[index];
                hash *= 16777619;
            }
        }
    }
}
