using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Margins
{
    /// <summary>
    /// Unity adapter for the first store's persistent portfolio merchandising
    /// state. It owns no prices itself when a portfolio controller is present.
    /// </summary>
    public sealed class FirstStoreMerchandisingComponent : MonoBehaviour
    {
        [SerializeField] private PortfolioProgressionController portfolioProgression;
        [SerializeField] private StockingController stocking;
        [SerializeField] private CheckoutStationComponent checkout;
        [SerializeField] private StoreCustomerFlowController customerFlow;
        [SerializeField] private string locationId =
            PortfolioProgressionRules.FirstLocationId;
        [SerializeField] private bool allowStandaloneAuthorityForTests;

        private PortfolioLocationSnapshot standaloneLocation;

        public event Action Changed;

        public string LocationId => locationId;
        public bool UsesPersistentPortfolio => portfolioProgression != null;
        public PortfolioProgressionController PortfolioProgression =>
            portfolioProgression;

        public bool TryBindDetailedLocation(
            string detailedLocationId,
            out string error)
        {
            if (!FirstStoreIdentifier.IsValid(detailedLocationId))
            {
                error = "A valid portfolio location is required for detailed merchandising.";
                return false;
            }

            string previous = locationId;
            locationId = detailedLocationId;
            standaloneLocation = null;
            if (!TryValidateConfiguration(out error))
            {
                locationId = previous;
                return false;
            }

            Changed?.Invoke();
            error = null;
            return true;
        }

        public IReadOnlyList<ProductDefinition> ProductCatalog
        {
            get
            {
                List<ProductDefinition> products = new();
                if (checkout != null)
                {
                    foreach (CheckoutPriceConfiguration mapping in
                             checkout.AuthoredPriceMappings)
                    {
                        if (mapping?.ProductDefinition != null)
                        {
                            products.Add(mapping.ProductDefinition);
                        }
                    }
                }
                products.Sort((left, right) => string.CompareOrdinal(
                    left.StableProductId,
                    right.StableProductId));
                return products;
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (stocking == null || checkout == null ||
                !FirstStoreIdentifier.IsValid(locationId) ||
                (portfolioProgression == null && !allowStandaloneAuthorityForTests) ||
                (portfolioProgression != null && customerFlow == null))
            {
                error =
                    "Merchandising requires explicit stocking, checkout, customer-flow, location, and persistent portfolio references.";
                return false;
            }

            if (stocking.Merchandising != this || checkout.Merchandising != this)
            {
                error =
                    "Stocking and checkout must consume this merchandising authority.";
                return false;
            }

            if (!TryGetLocation(out PortfolioLocationSnapshot location, out error) ||
                !MerchandisingRules.TryValidate(
                    location.merchandisePrices,
                    location.shelfMerchandiseAssignments,
                    out error))
            {
                return false;
            }

            foreach (MerchandisePriceSnapshot price in location.merchandisePrices)
            {
                if (!checkout.TryGetAuthoredProductEconomy(
                        price.productId,
                        out ProductDefinition checkoutProduct,
                        out int referencePrice,
                        out _,
                        out _) ||
                    checkoutProduct == null ||
                    !stocking.TryGetAuthoredProduct(
                        price.productId,
                        out ProductDefinition stockingProduct) ||
                    stockingProduct != checkoutProduct ||
                    price.referencePriceCents != referencePrice)
                {
                    error =
                        $"Merchandise product '{price.productId}' does not match the authored catalog and reference price.";
                    return false;
                }
            }

            foreach (ShelfMerchandiseAssignmentSnapshot assignment in
                     location.shelfMerchandiseAssignments)
            {
                if (!TryMapPersistentAssignment(
                        location,
                        assignment,
                        out StockingProductConfiguration physical) ||
                    (!string.IsNullOrWhiteSpace(assignment.assignedProductId) &&
                     !stocking.IsProductCompatibleWithShelf(
                         assignment.assignedProductId,
                         physical.ShelfFixture.StableFixtureId,
                         out error)))
                {
                    error ??=
                        $"Shelf '{assignment.shelfFixtureId}' has no deterministic authored detailed-store slot.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        internal bool TryValidateStockingConfiguration(
            StockingController candidate,
            out string error)
        {
            if (candidate != stocking)
            {
                error = "Stocking references a different merchandising authority.";
                return false;
            }
            if (!TryGetLocation(out PortfolioLocationSnapshot location, out error))
            {
                return false;
            }

            foreach (ShelfMerchandiseAssignmentSnapshot assignment in
                     location.shelfMerchandiseAssignments)
            {
                if (!TryMapPersistentAssignment(
                        location,
                        assignment,
                        out _))
                {
                    error =
                        $"Merchandising shelf '{assignment.shelfFixtureId}' has no deterministic physical stocking destination.";
                    return false;
                }
            }
            error = null;
            return true;
        }

        internal bool TryValidateCheckoutConfiguration(
            CheckoutStationComponent candidate,
            out string error)
        {
            error = null;
            if (candidate != checkout ||
                !TryGetLocation(out PortfolioLocationSnapshot location, out error))
            {
                error ??= "Checkout references a different merchandising authority.";
                return false;
            }

            foreach (MerchandisePriceSnapshot price in location.merchandisePrices)
            {
                if (!candidate.TryGetAuthoredProductEconomy(
                        price.productId,
                        out _,
                        out int referencePrice,
                        out _,
                        out _) ||
                    referencePrice != price.referencePriceCents)
                {
                    error =
                        $"Checkout has no catalog/reference-price entry for '{price.productId}'.";
                    return false;
                }
            }
            error = null;
            return true;
        }

        public bool TryGetOfferForProduct(
            string productId,
            out MerchandiseOffer offer)
        {
            offer = default;
            if (!TryGetLocation(out PortfolioLocationSnapshot location, out _) ||
                !MerchandisingRules.TryGetOfferForProduct(
                    location,
                    productId,
                    out MerchandiseOffer persistent) ||
                !TryGetPersistentAssignment(
                    location,
                    persistent.ShelfFixtureId,
                    out ShelfMerchandiseAssignmentSnapshot assignment) ||
                !TryMapPersistentAssignment(
                    location,
                    assignment,
                    out StockingProductConfiguration physical))
            {
                return false;
            }

            offer = new MerchandiseOffer(
                persistent.ProductId,
                physical.ShelfFixture.StableFixtureId,
                physical.ShelfLocationId,
                persistent.ReferencePriceCents,
                persistent.SalePriceCents,
                persistent.CustomDisplayLabel);
            return true;
        }

        public bool TryGetOfferForShelf(
            string shelfFixtureId,
            out MerchandiseOffer offer)
        {
            offer = default;
            if (!TryGetLocation(out PortfolioLocationSnapshot location, out _) ||
                !TryMapAuthoredShelf(
                    location,
                    shelfFixtureId,
                    out ShelfMerchandiseAssignmentSnapshot assignment,
                    out StockingProductConfiguration physical) ||
                string.IsNullOrWhiteSpace(assignment.assignedProductId) ||
                !MerchandisingRules.TryGetOfferForProduct(
                    location,
                    assignment.assignedProductId,
                    out MerchandiseOffer persistent))
            {
                return false;
            }

            offer = new MerchandiseOffer(
                persistent.ProductId,
                physical.ShelfFixture.StableFixtureId,
                physical.ShelfLocationId,
                persistent.ReferencePriceCents,
                persistent.SalePriceCents,
                persistent.CustomDisplayLabel);
            return true;
        }

        public bool TryGetShelfAssignment(
            string shelfFixtureId,
            out string assignedProductId,
            out string customDisplayLabel)
        {
            assignedProductId = null;
            customDisplayLabel = null;
            if (!TryGetLocation(out PortfolioLocationSnapshot location, out _))
            {
                return false;
            }

            if (!TryMapAuthoredShelf(
                    location,
                    shelfFixtureId,
                    out ShelfMerchandiseAssignmentSnapshot assignment,
                    out _))
            {
                return false;
            }
            assignedProductId = assignment.assignedProductId;
            customDisplayLabel = assignment.customDisplayLabel;
            return true;
        }

        public bool TryGetProductPrice(
            string productId,
            out int salePriceCents,
            out int referencePriceCents)
        {
            salePriceCents = 0;
            referencePriceCents = 0;
            if (!TryGetLocation(out PortfolioLocationSnapshot location, out _))
            {
                return false;
            }
            MerchandisePriceSnapshot price = location.merchandisePrices
                .FirstOrDefault(value => string.Equals(
                    value.productId,
                    productId,
                    StringComparison.Ordinal));
            if (price == null)
            {
                return false;
            }
            salePriceCents = price.salePriceCents;
            referencePriceCents = price.referencePriceCents;
            return true;
        }

        public bool TryUpdateShelfOffer(
            string shelfFixtureId,
            string assignedProductId,
            int salePriceCents,
            string customDisplayLabel,
            out string error)
        {
            if (!TryValidateConfiguration(out error) ||
                !TryGetShelfAssignment(
                    shelfFixtureId,
                    out string currentProductId,
                    out _))
            {
                error ??= "That shelf is not a sellable merchandise area.";
                return false;
            }

            assignedProductId = string.IsNullOrWhiteSpace(assignedProductId)
                ? null
                : assignedProductId.Trim();
            bool assignmentChanged = !string.Equals(
                currentProductId,
                assignedProductId,
                StringComparison.Ordinal);
            bool priceChanged = assignedProductId != null &&
                                TryGetProductPrice(
                                    assignedProductId,
                                    out int currentPrice,
                                    out _) &&
                                currentPrice != salePriceCents;

            if (!stocking.TryGetAuthoredShelf(
                    shelfFixtureId,
                    out ShelfFixture shelf,
                    out string shelfLocationId,
                    out _))
            {
                error = "That shelf has no physical stocking destination.";
                return false;
            }

            if (!TryGetLocation(out PortfolioLocationSnapshot location, out error) ||
                !TryMapAuthoredShelf(
                    location,
                    shelfFixtureId,
                    out ShelfMerchandiseAssignmentSnapshot persistentAssignment,
                    out _))
            {
                error ??= "That shelf has no persistent portfolio assignment.";
                return false;
            }

            bool hasCustomerReservation =
                customerFlow?.HasReservationAtShelfLocation(shelfLocationId) == true;

            if (assignmentChanged)
            {
                if (stocking.GetShelfInventoryQuantity(shelfFixtureId) > 0 ||
                    shelf.HasOccupiedSnapPoints ||
                    hasCustomerReservation)
                {
                    error =
                        "Remove every physical item and wait for customer reservations to clear before changing this shelf's product.";
                    return false;
                }

                if (stocking.HasHeldUnit)
                {
                    error =
                        "Finish stocking or set down the carried product before changing a shelf assignment.";
                    return false;
                }

                if (assignedProductId != null &&
                    !stocking.IsProductCompatibleWithShelf(
                        assignedProductId,
                        shelfFixtureId,
                        out error))
                {
                    return false;
                }
            }

            if (priceChanged && hasCustomerReservation)
            {
                error =
                    "Wait for customers holding this shelf's product to finish or leave before changing its price.";
                return false;
            }

            if ((assignmentChanged || priceChanged) &&
                checkout.HasActiveIncompleteSession)
            {
                error =
                    "Complete or cancel the active checkout before changing merchandise or price.";
                return false;
            }

            if (portfolioProgression != null)
            {
                if (!portfolioProgression.Progression.TryUpdateShelfOffer(
                        locationId,
                        persistentAssignment.shelfFixtureId,
                        assignedProductId,
                        salePriceCents,
                        customDisplayLabel,
                        out error))
                {
                    return false;
                }
            }
            else if (!TryUpdateStandalone(
                         shelfFixtureId,
                         assignedProductId,
                         salePriceCents,
                         customDisplayLabel,
                         out error))
            {
                return false;
            }

            Changed?.Invoke();
            error = null;
            return true;
        }

        public bool TrySetSalePrice(
            string productId,
            int salePriceCents,
            out string error)
        {
            if (!TryGetOfferForProduct(productId, out MerchandiseOffer offer))
            {
                error = "That product is not currently assigned to a shelf.";
                return false;
            }
            return TryUpdateShelfOffer(
                offer.ShelfFixtureId,
                productId,
                salePriceCents,
                offer.CustomDisplayLabel,
                out error);
        }

        public bool TryApplyPricePreset(
            PortfolioPricingPolicy preset,
            out string error)
        {
            error = null;
            if (portfolioProgression == null ||
                !Enum.IsDefined(typeof(PortfolioPricingPolicy), preset) ||
                !TryValidateConfiguration(out error) ||
                !TryGetLocation(out PortfolioLocationSnapshot location, out error))
            {
                error ??=
                    "Price presets require the persistent first-store merchandising authority.";
                return false;
            }

            HashSet<string> changedProducts = new(StringComparer.Ordinal);
            foreach (MerchandisePriceSnapshot price in location.merchandisePrices)
            {
                if (price.salePriceCents !=
                    MerchandisingRules.CalculatePresetSalePrice(
                        price.referencePriceCents,
                        preset))
                {
                    changedProducts.Add(price.productId);
                }
            }

            if (changedProducts.Count > 0 && checkout.HasActiveIncompleteSession)
            {
                error =
                    "Complete or cancel the active checkout before applying a price preset.";
                return false;
            }

            foreach (ShelfMerchandiseAssignmentSnapshot assignment in
                     location.shelfMerchandiseAssignments)
            {
                if (changedProducts.Contains(assignment.assignedProductId) &&
                    TryMapPersistentAssignment(
                        location,
                        assignment,
                        out StockingProductConfiguration physical) &&
                    customerFlow.HasReservationAtShelfLocation(
                        physical.ShelfLocationId))
                {
                    error =
                        "Wait for customers holding shelf products to finish or leave before applying a price preset.";
                    return false;
                }
            }

            if (!portfolioProgression.Progression.TrySetPricingPolicy(
                    locationId,
                    preset,
                    out error))
            {
                return false;
            }

            Changed?.Invoke();
            error = null;
            return true;
        }

        private bool TryGetLocation(
            out PortfolioLocationSnapshot location,
            out string error)
        {
            location = null;
            if (portfolioProgression != null)
            {
                if (!portfolioProgression.IsInitialized)
                {
                    error = "Persistent portfolio merchandising is not initialized.";
                    return false;
                }
                location = portfolioProgression.Progression.Locations.FirstOrDefault(value =>
                    string.Equals(
                        value.locationId,
                        locationId,
                        StringComparison.Ordinal));
                if (location == null)
                {
                    error = $"Portfolio location '{locationId}' is unavailable.";
                    return false;
                }
                error = null;
                return true;
            }

            error = "Persistent portfolio merchandising is required.";
            if (!allowStandaloneAuthorityForTests ||
                !TryEnsureStandaloneLocation(out error))
            {
                return false;
            }
            location = standaloneLocation;
            error = null;
            return true;
        }

        private bool TryMapPersistentAssignment(
            PortfolioLocationSnapshot location,
            ShelfMerchandiseAssignmentSnapshot assignment,
            out StockingProductConfiguration physical)
        {
            physical = null;
            if (location?.shelfMerchandiseAssignments == null ||
                assignment == null || stocking == null)
            {
                return false;
            }

            List<StockingProductConfiguration> authored = AuthoredShelfSlots();
            physical = authored.FirstOrDefault(value =>
                string.Equals(
                    value.ShelfFixture.StableFixtureId,
                    assignment.shelfFixtureId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    value.ShelfLocationId,
                    assignment.inventoryLocationId,
                    StringComparison.Ordinal));
            if (physical != null)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(assignment.assignedProductId))
            {
                List<StockingProductConfiguration> productMatches = authored
                    .Where(value => value.ProductDefinition != null &&
                                    string.Equals(
                                        value.ProductDefinition.StableProductId,
                                        assignment.assignedProductId,
                                        StringComparison.Ordinal))
                    .ToList();
                if (productMatches.Count == 1)
                {
                    physical = productMatches[0];
                    return true;
                }
            }

            List<ShelfMerchandiseAssignmentSnapshot> persistent = location
                .shelfMerchandiseAssignments
                .Where(value => value != null)
                .OrderBy(value => value.shelfFixtureId, StringComparer.Ordinal)
                .ToList();
            int index = persistent.FindIndex(value => ReferenceEquals(
                value,
                assignment));
            if (index < 0)
            {
                index = persistent.FindIndex(value => string.Equals(
                    value.shelfFixtureId,
                    assignment.shelfFixtureId,
                    StringComparison.Ordinal));
            }
            if (index < 0 || index >= authored.Count)
            {
                return false;
            }

            physical = authored[index];
            return true;
        }

        private bool TryMapAuthoredShelf(
            PortfolioLocationSnapshot location,
            string shelfFixtureId,
            out ShelfMerchandiseAssignmentSnapshot assignment,
            out StockingProductConfiguration physical)
        {
            assignment = null;
            physical = null;
            if (location?.shelfMerchandiseAssignments == null ||
                !FirstStoreIdentifier.IsValid(shelfFixtureId))
            {
                return false;
            }

            foreach (ShelfMerchandiseAssignmentSnapshot candidate in
                     location.shelfMerchandiseAssignments)
            {
                if (TryMapPersistentAssignment(
                        location,
                        candidate,
                        out StockingProductConfiguration mapped) &&
                    string.Equals(
                        mapped.ShelfFixture.StableFixtureId,
                        shelfFixtureId,
                        StringComparison.Ordinal))
                {
                    assignment = candidate;
                    physical = mapped;
                    return true;
                }
            }
            return false;
        }

        private static bool TryGetPersistentAssignment(
            PortfolioLocationSnapshot location,
            string persistentShelfFixtureId,
            out ShelfMerchandiseAssignmentSnapshot assignment)
        {
            assignment = location?.shelfMerchandiseAssignments?
                .FirstOrDefault(value => value != null && string.Equals(
                    value.shelfFixtureId,
                    persistentShelfFixtureId,
                    StringComparison.Ordinal));
            return assignment != null;
        }

        private List<StockingProductConfiguration> AuthoredShelfSlots()
        {
            return stocking.AuthoredProductMappings
                .Where(value => value?.ShelfFixture != null)
                .GroupBy(
                    value => value.ShelfFixture.StableFixtureId,
                    StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(
                    value => value.ShelfFixture.StableFixtureId,
                    StringComparer.Ordinal)
                .ToList();
        }

        private bool TryEnsureStandaloneLocation(out string error)
        {
            if (standaloneLocation != null)
            {
                error = null;
                return true;
            }
            if (stocking == null || checkout == null)
            {
                error = "Standalone merchandising test authority is missing adapters.";
                return false;
            }

            PortfolioLocationSnapshot location = new()
            {
                locationId = locationId,
                merchandisePrices = checkout.AuthoredPriceMappings
                    .Where(value => value?.ProductDefinition != null)
                    .Select(value => new MerchandisePriceSnapshot
                    {
                        productId = value.ProductDefinition.StableProductId,
                        referencePriceCents = value.UnitPriceCents,
                        salePriceCents = value.UnitPriceCents
                    })
                    .ToList(),
                shelfMerchandiseAssignments = stocking.AuthoredProductMappings
                    .Where(value =>
                        value?.ProductDefinition != null &&
                        value.ShelfFixture != null)
                    .Select(value => new ShelfMerchandiseAssignmentSnapshot
                    {
                        shelfFixtureId = value.ShelfFixture.StableFixtureId,
                        inventoryLocationId = value.ShelfLocationId,
                        assignedProductId = value.ProductDefinition.StableProductId
                    })
                    .ToList()
            };
            if (!MerchandisingRules.TryValidate(
                    location.merchandisePrices,
                    location.shelfMerchandiseAssignments,
                    out error))
            {
                return false;
            }
            standaloneLocation = location;
            error = null;
            return true;
        }

        private bool TryUpdateStandalone(
            string shelfFixtureId,
            string assignedProductId,
            int salePriceCents,
            string customDisplayLabel,
            out string error)
        {
            ShelfMerchandiseAssignmentSnapshot assignment = standaloneLocation
                .shelfMerchandiseAssignments.First(value => string.Equals(
                    value.shelfFixtureId,
                    shelfFixtureId,
                    StringComparison.Ordinal));
            MerchandisePriceSnapshot price = assignedProductId == null
                ? null
                : standaloneLocation.merchandisePrices.FirstOrDefault(value =>
                    string.Equals(
                        value.productId,
                        assignedProductId,
                        StringComparison.Ordinal));
            string previousProduct = assignment.assignedProductId;
            string previousLabel = assignment.customDisplayLabel;
            int previousPrice = price?.salePriceCents ?? 0;
            assignment.assignedProductId = assignedProductId;
            assignment.customDisplayLabel = string.IsNullOrWhiteSpace(customDisplayLabel)
                ? null
                : customDisplayLabel.Trim();
            if (price != null)
            {
                price.salePriceCents = salePriceCents;
            }
            if (MerchandisingRules.TryValidate(
                    standaloneLocation.merchandisePrices,
                    standaloneLocation.shelfMerchandiseAssignments,
                    out error))
            {
                return true;
            }

            assignment.assignedProductId = previousProduct;
            assignment.customDisplayLabel = previousLabel;
            if (price != null)
            {
                price.salePriceCents = previousPrice;
            }
            return false;
        }
    }
}
