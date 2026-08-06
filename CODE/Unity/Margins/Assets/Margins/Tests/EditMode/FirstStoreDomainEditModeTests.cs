// Draft implementation — Unity verification pending
using System.Collections.Generic;
using NUnit.Framework;

namespace Margins.Tests
{
    public sealed class FirstStoreDomainEditModeTests
    {
        [Test]
        public void PreviewPlaceRespectsRotatedFootprintAndMatchesCommitValidation()
        {
            FixtureLayout layout = new(4, 4);
            GridPosition position = new(2, 1);
            GridFootprint footprint = new(3, 1);

            FixturePlacementResult preview = layout.PreviewPlace(
                "fixture-preview-rotated",
                position,
                footprint,
                1);
            Assert.That(layout.Count, Is.Zero);
            Assert.That(layout.TryGetOccupant(position, out _), Is.False);
            FixturePlacementResult committed = layout.TryPlace(
                "fixture-preview-rotated",
                position,
                footprint,
                1);

            Assert.That(preview.IsSuccess, Is.True);
            Assert.That(layout.Count, Is.EqualTo(1));
            AssertEquivalentPlacementResult(preview, committed);
            Assert.That(
                layout.TryGetPlacement(
                    "fixture-preview-rotated",
                    out FixturePlacementSnapshot placement),
                Is.True);
            Assert.That(placement.RotatedFootprint, Is.EqualTo(new GridFootprint(1, 3)));
            Assert.That(layout.TryGetOccupant(new GridPosition(2, 3), out _), Is.True);
            Assert.That(layout.TryGetOccupant(new GridPosition(3, 1), out _), Is.False);
        }

        [Test]
        public void PreviewPlaceOutOfBoundsMatchesCommitFirstBlockedCell()
        {
            FixtureLayout layout = new(4, 4);

            FixturePlacementResult preview = layout.PreviewPlace(
                "fixture-preview-edge",
                new GridPosition(3, 3),
                new GridFootprint(2, 2),
                0);
            FixturePlacementResult committed = layout.TryPlace(
                "fixture-preview-edge",
                new GridPosition(3, 3),
                new GridFootprint(2, 2),
                0);

            Assert.That(preview.Failure, Is.EqualTo(FixturePlacementFailure.OutOfBounds));
            Assert.That(preview.BlockedCell, Is.EqualTo(new GridPosition(4, 3)));
            AssertEquivalentPlacementResult(preview, committed);
            Assert.That(layout.Count, Is.Zero);
        }

        [Test]
        public void PreviewPlaceOverlapMatchesCommitConflictWithoutMutatingLayout()
        {
            FixtureLayout layout = new(6, 6);
            Assert.That(
                layout.TryPlace(
                    "fixture-preview-alpha",
                    new GridPosition(1, 1),
                    new GridFootprint(2, 2),
                    0).IsSuccess,
                Is.True);
            List<FixturePlacementSnapshot> before = layout.CreateSnapshot();

            FixturePlacementResult preview = layout.PreviewPlace(
                "fixture-preview-beta",
                new GridPosition(2, 1),
                new GridFootprint(1, 1),
                0);

            Assert.That(preview.Failure, Is.EqualTo(FixturePlacementFailure.Occupied));
            Assert.That(preview.BlockedCell, Is.EqualTo(new GridPosition(2, 1)));
            Assert.That(preview.ConflictingFixtureInstanceId, Is.EqualTo("fixture-preview-alpha"));
            Assert.That(layout.CreateSnapshot(), Is.EqualTo(before));
            Assert.That(layout.TryGetOccupant(new GridPosition(2, 1), out string occupant), Is.True);
            Assert.That(occupant, Is.EqualTo("fixture-preview-alpha"));

            FixturePlacementResult committed = layout.TryPlace(
                "fixture-preview-beta",
                new GridPosition(2, 1),
                new GridFootprint(1, 1),
                0);
            AssertEquivalentPlacementResult(preview, committed);
        }

        [Test]
        public void PreviewMoveLeavesExactPriorSnapshotAndOccupancyUnchanged()
        {
            FixtureLayout layout = new(6, 6);
            Assert.That(
                layout.TryPlace(
                    "fixture-preview-movable",
                    new GridPosition(1, 1),
                    new GridFootprint(2, 1),
                    0).IsSuccess,
                Is.True);
            Assert.That(
                layout.TryPlace(
                    "fixture-preview-other",
                    new GridPosition(4, 1),
                    new GridFootprint(1, 1),
                    0).IsSuccess,
                Is.True);
            List<FixturePlacementSnapshot> before = layout.CreateSnapshot();

            FixturePlacementResult preview = layout.PreviewMove(
                "fixture-preview-movable",
                new GridPosition(1, 3),
                1);

            Assert.That(preview.IsSuccess, Is.True);
            Assert.That(layout.CreateSnapshot(), Is.EqualTo(before));
            Assert.That(layout.TryGetOccupant(new GridPosition(1, 1), out string originalOccupant), Is.True);
            Assert.That(originalOccupant, Is.EqualTo("fixture-preview-movable"));
            Assert.That(layout.TryGetOccupant(new GridPosition(1, 3), out _), Is.False);

            FixturePlacementResult committed = layout.TryMove(
                "fixture-preview-movable",
                new GridPosition(1, 3),
                1);
            AssertEquivalentPlacementResult(preview, committed);
        }

        [Test]
        public void PreviewInvalidDuplicateAndMissingCasesMatchCommitBehavior()
        {
            FixtureLayout layout = new(4, 4);

            AssertEquivalentPlacementResult(
                layout.PreviewPlace(
                    "Bad Fixture Id",
                    new GridPosition(0, 0),
                    new GridFootprint(1, 1),
                    0),
                layout.TryPlace(
                    "Bad Fixture Id",
                    new GridPosition(0, 0),
                    new GridFootprint(1, 1),
                    0));
            AssertEquivalentPlacementResult(
                layout.PreviewPlace(
                    "fixture-preview-invalid-footprint",
                    new GridPosition(0, 0),
                    new GridFootprint(0, 1),
                    0),
                layout.TryPlace(
                    "fixture-preview-invalid-footprint",
                    new GridPosition(0, 0),
                    new GridFootprint(0, 1),
                    0));

            Assert.That(
                layout.TryPlace(
                    "fixture-preview-duplicate",
                    new GridPosition(0, 0),
                    new GridFootprint(1, 1),
                    0).IsSuccess,
                Is.True);
            AssertEquivalentPlacementResult(
                layout.PreviewPlace(
                    "fixture-preview-duplicate",
                    new GridPosition(1, 0),
                    new GridFootprint(1, 1),
                    0),
                layout.TryPlace(
                    "fixture-preview-duplicate",
                    new GridPosition(1, 0),
                    new GridFootprint(1, 1),
                    0));
            AssertEquivalentPlacementResult(
                layout.PreviewMove(
                    "fixture-preview-missing",
                    new GridPosition(1, 0),
                    0),
                layout.TryMove(
                    "fixture-preview-missing",
                    new GridPosition(1, 0),
                    0));
        }

        [Test]
        public void OverlappingFixturePlacementIsRejectedWithoutReplacingOccupant()
        {
            FixtureLayout layout = new(8, 8);
            Assert.That(
                layout.TryPlace(
                    "fixture-alpha",
                    new GridPosition(1, 1),
                    new GridFootprint(2, 2),
                    0).IsSuccess,
                Is.True);

            FixturePlacementResult result = layout.TryPlace(
                "fixture-beta",
                new GridPosition(2, 2),
                new GridFootprint(2, 1),
                0);

            Assert.That(result.Failure, Is.EqualTo(FixturePlacementFailure.Occupied));
            Assert.That(result.ConflictingFixtureInstanceId, Is.EqualTo("fixture-alpha"));
            Assert.That(result.BlockedCell, Is.EqualTo(new GridPosition(2, 2)));
            Assert.That(layout.Count, Is.EqualTo(1));
            Assert.That(
                layout.TryGetOccupant(new GridPosition(2, 2), out string occupant),
                Is.True);
            Assert.That(occupant, Is.EqualTo("fixture-alpha"));
        }

        [Test]
        public void OutOfBoundsFixturePlacementIsRejectedAtFirstDeterministicCell()
        {
            FixtureLayout layout = new(4, 4);

            FixturePlacementResult result = layout.TryPlace(
                "fixture-edge",
                new GridPosition(3, 3),
                new GridFootprint(2, 2),
                0);

            Assert.That(result.Failure, Is.EqualTo(FixturePlacementFailure.OutOfBounds));
            Assert.That(result.BlockedCell, Is.EqualTo(new GridPosition(4, 3)));
            Assert.That(layout.Count, Is.Zero);
        }

        [Test]
        public void QuarterTurnDeterministicallyRotatesFootprintAndMovePreservesIt()
        {
            FixtureLayout layout = new(8, 8);

            Assert.That(
                layout.TryPlace(
                    "fixture-rotated",
                    new GridPosition(1, 1),
                    new GridFootprint(3, 1),
                    -3).IsSuccess,
                Is.True);
            Assert.That(
                layout.TryGetPlacement(
                    "fixture-rotated",
                    out FixturePlacementSnapshot placed),
                Is.True);
            Assert.That(placed.quarterTurns, Is.EqualTo(1));
            Assert.That(placed.RotatedFootprint, Is.EqualTo(new GridFootprint(1, 3)));
            Assert.That(
                layout.TryGetOccupant(new GridPosition(1, 3), out _),
                Is.True);
            Assert.That(
                layout.TryGetOccupant(new GridPosition(2, 1), out _),
                Is.False);

            Assert.That(
                layout.TryMove(
                    "fixture-rotated",
                    new GridPosition(4, 2),
                    2).IsSuccess,
                Is.True);
            Assert.That(
                layout.TryGetPlacement(
                    "fixture-rotated",
                    out FixturePlacementSnapshot moved),
                Is.True);
            Assert.That(moved.RotatedFootprint, Is.EqualTo(new GridFootprint(3, 1)));
            Assert.That(
                layout.TryGetOccupant(new GridPosition(1, 3), out _),
                Is.False);
        }

        [Test]
        public void RejectedMovePreservesPriorPlacementAndRemoveFreesCells()
        {
            FixtureLayout layout = new(6, 6);
            Assert.That(
                layout.TryPlace(
                    "fixture-alpha",
                    new GridPosition(0, 0),
                    new GridFootprint(2, 1),
                    0).IsSuccess,
                Is.True);
            Assert.That(
                layout.TryPlace(
                    "fixture-beta",
                    new GridPosition(3, 0),
                    new GridFootprint(1, 1),
                    0).IsSuccess,
                Is.True);

            FixturePlacementResult rejected = layout.TryMove(
                "fixture-alpha",
                new GridPosition(3, 0),
                0);
            Assert.That(rejected.Failure, Is.EqualTo(FixturePlacementFailure.Occupied));
            Assert.That(
                layout.TryGetOccupant(new GridPosition(0, 0), out string originalOccupant),
                Is.True);
            Assert.That(originalOccupant, Is.EqualTo("fixture-alpha"));

            Assert.That(layout.TryRemove("fixture-alpha").IsSuccess, Is.True);
            Assert.That(layout.TryGetOccupant(new GridPosition(0, 0), out _), Is.False);
            Assert.That(layout.Count, Is.EqualTo(1));
        }

        [Test]
        public void InventoryTransfersConserveEveryUnit()
        {
            FirstStoreInventory inventory = CreateInventory();
            int before = inventory.GetTotalQuantity("prod-cola");

            Assert.That(
                inventory.TryTransfer(
                    "prod-cola",
                    "loc-box",
                    "loc-loose",
                    4).IsSuccess,
                Is.True);
            Assert.That(
                inventory.TryTransfer(
                    "prod-cola",
                    "loc-loose",
                    "loc-held",
                    1).IsSuccess,
                Is.True);
            Assert.That(
                inventory.TryTransfer(
                    "prod-cola",
                    "loc-held",
                    "loc-shelf",
                    1).IsSuccess,
                Is.True);

            Assert.That(inventory.GetTotalQuantity("prod-cola"), Is.EqualTo(before));
            Assert.That(inventory.GetQuantity("loc-box", "prod-cola"), Is.EqualTo(6));
            Assert.That(inventory.GetQuantity("loc-loose", "prod-cola"), Is.EqualTo(3));
            Assert.That(inventory.GetQuantity("loc-shelf", "prod-cola"), Is.EqualTo(1));
        }

        [Test]
        public void InvalidAndOverdrawnTransfersLeaveInventoryUnchanged()
        {
            FirstStoreInventory inventory = CreateInventory();
            FirstStoreInventorySnapshot before = inventory.CreateSnapshot();

            InventoryTransferResult invalid = inventory.TryTransfer(
                "prod-cola",
                "loc-box",
                "loc-loose",
                0);
            InventoryTransferResult overdrawn = inventory.TryTransfer(
                "prod-cola",
                "loc-box",
                "loc-loose",
                11);
            InventoryTransferResult unknown = inventory.TryTransfer(
                "prod-missing",
                "loc-box",
                "loc-loose",
                1);

            Assert.That(invalid.Failure, Is.EqualTo(InventoryTransferFailure.InvalidQuantity));
            Assert.That(overdrawn.Failure, Is.EqualTo(InventoryTransferFailure.InsufficientQuantity));
            Assert.That(unknown.Failure, Is.EqualTo(InventoryTransferFailure.UnknownProduct));
            Assert.That(inventory.CreateSnapshot(), Is.EqualTo(before));
        }

        [Test]
        public void BoxToLooseToHeldToShelfTransitionsAreExplicit()
        {
            FirstStoreInventory inventory = CreateInventory();
            Assert.That(
                DeliveryContainer.TryCreate(
                    inventory,
                    "container-starter",
                    "loc-box",
                    false,
                    out DeliveryContainer container,
                    out string error),
                Is.True,
                error);

            Assert.That(
                container.TryRemoveTo(
                    "prod-cola",
                    "loc-loose",
                    2,
                    out DeliveryContainerFailure sealedFailure,
                    out _),
                Is.False);
            Assert.That(sealedFailure, Is.EqualTo(DeliveryContainerFailure.Sealed));
            Assert.That(container.TryOpen(), Is.EqualTo(DeliveryContainerOpenResult.Opened));
            Assert.That(
                container.TryRemoveTo(
                    "prod-cola",
                    "loc-loose",
                    2,
                    out DeliveryContainerFailure openedFailure,
                    out _),
                Is.True);
            Assert.That(openedFailure, Is.EqualTo(DeliveryContainerFailure.None));
            Assert.That(
                inventory.TryTransfer(
                    "prod-cola",
                    "loc-loose",
                    "loc-held",
                    1).IsSuccess,
                Is.True);
            Assert.That(
                inventory.TryTransfer(
                    "prod-cola",
                    "loc-held",
                    "loc-shelf",
                    1).IsSuccess,
                Is.True);

            Assert.That(container.IsOpen, Is.True);
            Assert.That(inventory.GetQuantity("loc-box", "prod-cola"), Is.EqualTo(8));
            Assert.That(inventory.GetQuantity("loc-loose", "prod-cola"), Is.EqualTo(1));
            Assert.That(inventory.GetQuantity("loc-shelf", "prod-cola"), Is.EqualTo(1));
            Assert.That(inventory.GetTotalQuantity("prod-cola"), Is.EqualTo(10));
        }

        [Test]
        public void ShelfLocationRejectsASecondProductWithoutMutation()
        {
            FirstStoreInventory inventory = CreateInventory();
            Assert.That(
                inventory.TrySeedQuantity(
                    "loc-shelf",
                    "prod-cola",
                    1,
                    out string shelfError),
                Is.True,
                shelfError);
            Assert.That(
                inventory.TrySeedQuantity(
                    "loc-loose",
                    "prod-chips",
                    2,
                    out string looseError),
                Is.True,
                looseError);
            FirstStoreInventorySnapshot before = inventory.CreateSnapshot();

            InventoryTransferResult result = inventory.TryTransfer(
                "prod-chips",
                "loc-loose",
                "loc-shelf",
                1);

            Assert.That(
                result.Failure,
                Is.EqualTo(InventoryTransferFailure.DestinationOccupiedByOtherProduct));
            Assert.That(inventory.CreateSnapshot(), Is.EqualTo(before));
        }

        [Test]
        public void CheckoutSubtotalAndCorrectionUseIntegerCents()
        {
            FirstStoreInventory inventory = CreateCheckoutInventory();
            CompletedTransactionLedger ledger = new(8);
            Assert.That(
                CheckoutSession.TryCreate(
                    inventory,
                    CreateShelfMappings(),
                    "transaction-001",
                    out CheckoutSession checkout,
                    out string error),
                Is.True,
                error);

            Assert.That(checkout.TryScan("prod-cola", 149, 60, 2, out _), Is.True);
            Assert.That(checkout.TryScan("prod-chips", 299, 100, 1, out _), Is.True);
            Assert.That(checkout.SubtotalCents, Is.EqualTo(597));
            Assert.That(checkout.TryRemove("prod-cola", 1, out _), Is.True);
            Assert.That(checkout.SubtotalCents, Is.EqualTo(448));

            Assert.That(
                checkout.TryComplete(
                    ledger,
                    out CheckoutTransactionSummary summary,
                    out CheckoutFailure failure),
                Is.True);
            Assert.That(failure, Is.EqualTo(CheckoutFailure.None));
            Assert.That(summary.subtotalCents, Is.EqualTo(448));
            Assert.That(summary.unitsSold, Is.EqualTo(2));
            Assert.That(ledger.GrossSalesCents, Is.EqualTo(448));
            Assert.That(ledger.TransactionCount, Is.EqualTo(1));
            Assert.That(inventory.GetQuantity("loc-shelf", "prod-cola"), Is.EqualTo(2));
            Assert.That(inventory.GetQuantity("loc-shelf", "prod-chips"), Is.EqualTo(1));
        }

        [Test]
        public void CheckoutCompletionIsIdempotent()
        {
            FirstStoreInventory inventory = CreateCheckoutInventory();
            CompletedTransactionLedger ledger = new(8);
            Assert.That(
                CheckoutSession.TryCreate(
                    inventory,
                    CreateShelfMappings(),
                    "transaction-002",
                    out CheckoutSession checkout,
                    out string error),
                Is.True,
                error);
            Assert.That(checkout.TryScan("prod-cola", 149, 60, 1, out _), Is.True);

            Assert.That(
                checkout.TryComplete(
                    ledger,
                    out CheckoutTransactionSummary first,
                    out CheckoutFailure firstFailure),
                Is.True);
            int stockAfterFirstCompletion =
                inventory.GetQuantity("loc-shelf", "prod-cola");
            Assert.That(
                checkout.TryComplete(
                    ledger,
                    out CheckoutTransactionSummary second,
                    out CheckoutFailure secondFailure),
                Is.True);

            Assert.That(firstFailure, Is.EqualTo(CheckoutFailure.None));
            Assert.That(secondFailure, Is.EqualTo(CheckoutFailure.AlreadyCompleted));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(ledger.TransactionCount, Is.EqualTo(1));
            Assert.That(ledger.GrossSalesCents, Is.EqualTo(149));
            Assert.That(
                inventory.GetQuantity("loc-shelf", "prod-cola"),
                Is.EqualTo(stockAfterFirstCompletion));
        }

        [Test]
        public void CheckoutRejectsInvalidProductAndInsufficientStock()
        {
            FirstStoreInventory inventory = CreateCheckoutInventory();
            Assert.That(
                CheckoutSession.TryCreate(
                    inventory,
                    CreateShelfMappings(),
                    "transaction-003",
                    out CheckoutSession checkout,
                    out string error),
                Is.True,
                error);

            Assert.That(
                checkout.TryScan(
                    "prod-missing",
                    149,
                    60,
                    1,
                    out CheckoutFailure invalidProduct),
                Is.False);
            Assert.That(invalidProduct, Is.EqualTo(CheckoutFailure.InvalidProduct));
            Assert.That(
                checkout.TryScan(
                    "prod-cola",
                    149,
                    60,
                    4,
                    out CheckoutFailure insufficientStock),
                Is.False);
            Assert.That(insufficientStock, Is.EqualTo(CheckoutFailure.InsufficientStock));
            Assert.That(checkout.Lines, Is.Empty);
            Assert.That(inventory.GetQuantity("loc-shelf", "prod-cola"), Is.EqualTo(3));
        }

        [Test]
        public void CheckoutConsumesEachProductFromItsMappedShelf()
        {
            FirstStoreInventory inventory = new();
            Assert.That(inventory.TryRegisterProduct("prod-cola", out _), Is.True);
            Assert.That(inventory.TryRegisterProduct("prod-chips", out _), Is.True);
            Assert.That(
                inventory.TryRegisterLocation(
                    "loc-shelf-cola",
                    InventoryLocationKind.Shelf,
                    4,
                    true,
                    out _),
                Is.True);
            Assert.That(
                inventory.TryRegisterLocation(
                    "loc-shelf-chips",
                    InventoryLocationKind.Shelf,
                    4,
                    true,
                    out _),
                Is.True);
            Assert.That(
                inventory.TrySeedQuantity("loc-shelf-cola", "prod-cola", 2, out _),
                Is.True);
            Assert.That(
                inventory.TrySeedQuantity("loc-shelf-chips", "prod-chips", 2, out _),
                Is.True);

            Dictionary<string, string> mappings = new()
            {
                ["prod-cola"] = "loc-shelf-cola",
                ["prod-chips"] = "loc-shelf-chips"
            };
            Assert.That(
                CheckoutSession.TryCreate(
                    inventory,
                    mappings,
                    "transaction-mapped-shelves-001",
                    out CheckoutSession checkout,
                    out string error),
                Is.True,
                error);
            Assert.That(checkout.TryScan("prod-cola", 149, 60, 1, out _), Is.True);
            Assert.That(checkout.TryScan("prod-chips", 299, 100, 1, out _), Is.True);
            Assert.That(
                checkout.TryComplete(new CompletedTransactionLedger(8), out _, out _),
                Is.True);

            Assert.That(inventory.GetQuantity("loc-shelf-cola", "prod-cola"), Is.EqualTo(1));
            Assert.That(inventory.GetQuantity("loc-shelf-chips", "prod-chips"), Is.EqualTo(1));
        }

        [Test]
        public void CheckoutLineExposureCannotMutateActiveState()
        {
            FirstStoreInventory inventory = CreateCheckoutInventory();
            Assert.That(
                CheckoutSession.TryCreate(
                    inventory,
                    CreateShelfMappings(),
                    "transaction-immutable-001",
                    out CheckoutSession checkout,
                    out string error),
                Is.True,
                error);
            Assert.That(checkout.TryScan("prod-cola", 149, 60, 2, out _), Is.True);

            CheckoutLineSnapshot exposed = checkout.Lines[0];
            exposed.productId = "prod-chips";
            exposed.unitPriceCents = 1;
            exposed.unitCostCents = 1;
            exposed.quantityUnits = 99;

            Assert.That(checkout.Lines[0].productId, Is.EqualTo("prod-cola"));
            Assert.That(checkout.Lines[0].unitPriceCents, Is.EqualTo(149));
            Assert.That(checkout.Lines[0].unitCostCents, Is.EqualTo(60));
            Assert.That(checkout.Lines[0].quantityUnits, Is.EqualTo(2));
            Assert.That(checkout.SubtotalCents, Is.EqualTo(298));
        }

        [Test]
        public void CompletedLedgerRetainsTwoTransactionsInDeterministicOrder()
        {
            CompletedTransactionLedger ledger = CreateResultLedger();

            Assert.That(ledger.TransactionCount, Is.EqualTo(2));
            Assert.That(ledger.GrossSalesCents, Is.EqualTo(550));
            Assert.That(ledger.UnitsSold, Is.EqualTo(3));
            Assert.That(
                ledger.Transactions[0].transactionId,
                Is.EqualTo("transaction-result-001"));
            Assert.That(
                ledger.Transactions[1].transactionId,
                Is.EqualTo("transaction-result-002"));

            CheckoutTransactionSummary exposed = ledger.Transactions[0];
            exposed.subtotalCents = 0;
            exposed.lines[0].quantityUnits = 99;
            Assert.That(ledger.GrossSalesCents, Is.EqualTo(550));
            Assert.That(ledger.Transactions[0].subtotalCents, Is.EqualTo(250));
        }

        [Test]
        public void DuplicateTransactionIdIsRejectedBeforeSecondConsumption()
        {
            FirstStoreInventory inventory = CreateCheckoutInventory();
            CompletedTransactionLedger ledger = new(8);
            Assert.That(
                CheckoutSession.TryCreate(
                    inventory,
                    CreateShelfMappings(),
                    "transaction-duplicate-001",
                    out CheckoutSession first,
                    out _),
                Is.True);
            Assert.That(first.TryScan("prod-cola", 149, 60, 1, out _), Is.True);
            Assert.That(first.TryComplete(ledger, out _, out _), Is.True);

            Assert.That(
                CheckoutSession.TryCreate(
                    inventory,
                    CreateShelfMappings(),
                    "transaction-duplicate-001",
                    out CheckoutSession duplicate,
                    out _),
                Is.True);
            Assert.That(duplicate.TryScan("prod-cola", 149, 60, 1, out _), Is.True);
            int stockBeforeDuplicate =
                inventory.GetQuantity("loc-shelf", "prod-cola");

            Assert.That(
                duplicate.TryComplete(
                    ledger,
                    out _,
                    out CheckoutFailure failure),
                Is.False);
            Assert.That(failure, Is.EqualTo(CheckoutFailure.DuplicateTransactionId));
            Assert.That(ledger.TransactionCount, Is.EqualTo(1));
            Assert.That(ledger.GrossSalesCents, Is.EqualTo(149));
            Assert.That(
                inventory.GetQuantity("loc-shelf", "prod-cola"),
                Is.EqualTo(stockBeforeDuplicate));
        }

        [Test]
        public void ResultCalculationDerivesCostOfGoodsSoldFromCapturedLineCosts()
        {
            Assert.That(
                StoreSessionTotals.TryCreateFromLedger(
                    CreateResultLedger(),
                    205,
                    out StoreSessionTotals totals,
                    out string error),
                Is.True,
                error);

            Assert.That(totals.grossSalesCents, Is.EqualTo(550));
            Assert.That(totals.costOfGoodsSoldCents, Is.EqualTo(220));
            Assert.That(totals.unitsSold, Is.EqualTo(3));
            Assert.That(totals.transactionCount, Is.EqualTo(2));
        }

        [Test]
        public void ResultCalculationDerivesContributionAfterCostOfGoods()
        {
            Assert.That(
                StoreSessionTotals.TryCreateFromLedger(
                    CreateResultLedger(),
                    205,
                    out StoreSessionTotals totals,
                    out string error),
                Is.True,
                error);

            Assert.That(totals.includedOperatingExpensesCents, Is.EqualTo(205));
            Assert.That(totals.contributionAfterCostOfGoodsCents, Is.EqualTo(125));
        }

        [Test]
        public void CompletedTransactionPreservesSaleTimeUnitCost()
        {
            FirstStoreInventory inventory = CreateCheckoutInventory();
            CompletedTransactionLedger ledger = new(8);
            Assert.That(
                CheckoutSession.TryCreate(
                    inventory,
                    CreateShelfMappings(),
                    "transaction-historical-cost-001",
                    out CheckoutSession checkout,
                    out string error),
                Is.True,
                error);
            Assert.That(
                checkout.TryScan("prod-cola", 149, 60, 2, out _),
                Is.True);
            Assert.That(checkout.TryComplete(ledger, out _, out _), Is.True);

            CheckoutLineSnapshot restoredLine = ledger.Transactions[0].lines[0];
            Assert.That(restoredLine.unitCostCents, Is.EqualTo(60));
            Assert.That(restoredLine.LineCostCents, Is.EqualTo(120));
            Assert.That(
                StoreSessionTotals.TryCreateFromLedger(
                    ledger,
                    0,
                    out StoreSessionTotals totals,
                    out error),
                Is.True,
                error);
            Assert.That(totals.costOfGoodsSoldCents, Is.EqualTo(120));
        }

        [Test]
        public void InvalidOperatingStateTransitionsDoNotMutateState()
        {
            Assert.That(
                StoreOperatingSession.TryCreate(
                    "session-opening-001",
                    out StoreOperatingSession session,
                    out _),
                Is.True);

            Assert.That(
                session.TryTransition(
                    StoreOperatingState.Open,
                    out StoreOperatingFailure directOpenFailure),
                Is.False);
            Assert.That(directOpenFailure, Is.EqualTo(StoreOperatingFailure.InvalidTransition));
            Assert.That(session.State, Is.EqualTo(StoreOperatingState.Closed));

            Assert.That(
                session.TryTransition(
                    StoreOperatingState.Preparing,
                    out _),
                Is.True);
            Assert.That(
                session.TryTransition(
                    StoreOperatingState.Open,
                    out _),
                Is.True);
            Assert.That(
                session.TryTransition(
                    StoreOperatingState.ClosedWithResultPending,
                    out StoreOperatingFailure skipClosingFailure),
                Is.False);
            Assert.That(skipClosingFailure, Is.EqualTo(StoreOperatingFailure.InvalidTransition));
            Assert.That(session.State, Is.EqualTo(StoreOperatingState.Open));
        }

        [Test]
        public void OperatingRestoreNormalizesJsonZeroTotalsPlaceholderWithoutAcceptingContradiction()
        {
            StoreOperatingSnapshot serializedPreparing = new(
                "session-json-placeholder-001",
                StoreOperatingState.Preparing,
                false,
                new StoreSessionTotals(0, 0, 0, 0, 0, 0));
            CompletedTransactionLedger ledger = new(8);

            Assert.That(
                StoreOperatingSession.TryRestore(
                    serializedPreparing,
                    ledger,
                    out StoreOperatingSession restored,
                    out string error),
                Is.True,
                error);
            Assert.That(restored.HasResult, Is.False);
            Assert.That(restored.Totals, Is.Null);

            serializedPreparing.totals.grossSalesCents = 1;
            serializedPreparing.totals.contributionAfterCostOfGoodsCents = 1;
            Assert.That(
                StoreOperatingSession.TryRestore(
                    serializedPreparing,
                    ledger,
                    out _,
                    out error),
                Is.False);
            StringAssert.Contains("disagree", error);
        }

        [Test]
        public void FirstStoreSnapshotRoundTripPreservesEquality()
        {
            FirstStoreSnapshot before = CreateCompleteSnapshot(out _);

            Assert.That(
                FirstStoreSnapshotMapper.TryRestore(
                    before,
                    out RestoredFirstStoreState restored,
                    out string error),
                Is.True,
                error);
            FirstStoreSnapshot after = FirstStoreSnapshotMapper.Create(
                restored.FixtureLayout,
                restored.Inventory,
                restored.DeliveryContainers,
                restored.TransactionLedger,
                restored.StoreOperating,
                restored.CleaningTask);

            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void RestoreRejectsTotalsThatContradictCompletedTransactions()
        {
            FirstStoreSnapshot snapshot = CreateCompleteSnapshot(out _);
            snapshot.storeOperating.totals.grossSalesCents += 1;
            snapshot.storeOperating.totals.contributionAfterCostOfGoodsCents += 1;

            Assert.That(
                FirstStoreSnapshotMapper.TryRestore(
                    snapshot,
                    out _,
                    out string error),
                Is.False);
            StringAssert.Contains("contradict", error);
        }

        [Test]
        public void RestoreUsesHistoricalLineCostsWithoutConfiguredCostInput()
        {
            FirstStoreSnapshot snapshot = CreateCompleteSnapshot(out _);
            Assert.That(snapshot.storeOperating.totals.costOfGoodsSoldCents, Is.EqualTo(160));

            Assert.That(
                FirstStoreSnapshotMapper.TryRestore(
                    snapshot,
                    out RestoredFirstStoreState restored,
                    out string error),
                Is.True,
                error);

            Assert.That(
                restored.StoreOperating.Totals.costOfGoodsSoldCents,
                Is.EqualTo(160));
            Assert.That(
                restored.StoreOperating.Totals.contributionAfterCostOfGoodsCents,
                Is.EqualTo(198));
        }

        [Test]
        public void RestoreRejectsHistoricalLineCostThatContradictsStoredResult()
        {
            FirstStoreSnapshot snapshot = CreateCompleteSnapshot(out _);
            snapshot.transactionLedger.transactions[0].lines[0].unitCostCents += 1;

            Assert.That(
                FirstStoreSnapshotMapper.TryRestore(
                    snapshot,
                    out _,
                    out string error),
                Is.False);
            StringAssert.Contains("contradict", error);
        }

        [Test]
        public void MalformedAndDuplicateStableIdentifiersAreRejected()
        {
            FixtureLayout layout = new(4, 4);
            FixturePlacementResult invalidPlacement = layout.TryPlace(
                "Bad Fixture Id",
                GridPositionDefault(),
                new GridFootprint(1, 1),
                0);
            Assert.That(
                invalidPlacement.Failure,
                Is.EqualTo(FixturePlacementFailure.InvalidIdentifier));

            FirstStoreInventorySnapshot duplicateProducts = new()
            {
                productIds = new List<string> { "prod-cola", "prod-cola" },
                locations = new List<InventoryLocationSnapshot>()
            };
            Assert.That(
                FirstStoreInventory.TryRestore(
                    duplicateProducts,
                    out _,
                    out string error),
                Is.False);
            StringAssert.Contains("Duplicate product id", error);
        }

        [Test]
        public void RepeatedRestoreDoesNotDuplicateRevenueOrInventoryConsumption()
        {
            FirstStoreSnapshot snapshot = CreateCompleteSnapshot(
                out Dictionary<string, int> totalsBefore);

            Assert.That(
                FirstStoreSnapshotMapper.TryRestore(
                    snapshot,
                    out RestoredFirstStoreState restored,
                    out string error),
                Is.True,
                error);

            foreach (KeyValuePair<string, int> expected in totalsBefore)
            {
                Assert.That(
                    restored.Inventory.GetTotalQuantity(expected.Key),
                    Is.EqualTo(expected.Value),
                    expected.Key);
            }
            Assert.That(restored.TransactionLedger.TransactionCount, Is.EqualTo(2));
            Assert.That(restored.TransactionLedger.GrossSalesCents, Is.EqualTo(448));
            Assert.That(restored.TransactionLedger.UnitsSold, Is.EqualTo(2));

            FirstStoreSnapshot mappedAgain = FirstStoreSnapshotMapper.Create(
                restored.FixtureLayout,
                restored.Inventory,
                restored.DeliveryContainers,
                restored.TransactionLedger,
                restored.StoreOperating,
                restored.CleaningTask);
            Assert.That(
                FirstStoreSnapshotMapper.TryRestore(
                    mappedAgain,
                    out RestoredFirstStoreState restoredAgain,
                    out error),
                Is.True,
                error);
            foreach (KeyValuePair<string, int> expected in totalsBefore)
            {
                Assert.That(
                    restoredAgain.Inventory.GetTotalQuantity(expected.Key),
                    Is.EqualTo(expected.Value),
                    expected.Key);
            }
            Assert.That(restoredAgain.TransactionLedger.TransactionCount, Is.EqualTo(2));
            Assert.That(restoredAgain.TransactionLedger.GrossSalesCents, Is.EqualTo(448));
            Assert.That(restoredAgain.TransactionLedger.UnitsSold, Is.EqualTo(2));
        }

        private static void AssertEquivalentPlacementResult(
            FixturePlacementResult preview,
            FixturePlacementResult committed)
        {
            Assert.That(committed.Failure, Is.EqualTo(preview.Failure));
            Assert.That(committed.FixtureInstanceId, Is.EqualTo(preview.FixtureInstanceId));
            Assert.That(committed.BlockedCell, Is.EqualTo(preview.BlockedCell));
            Assert.That(
                committed.ConflictingFixtureInstanceId,
                Is.EqualTo(preview.ConflictingFixtureInstanceId));
        }

        private static GridPosition GridPositionDefault()
        {
            return new GridPosition(0, 0);
        }

        private static FirstStoreInventory CreateInventory()
        {
            FirstStoreInventory inventory = new();
            Assert.That(inventory.TryRegisterProduct("prod-cola", out _), Is.True);
            Assert.That(inventory.TryRegisterProduct("prod-chips", out _), Is.True);
            Assert.That(
                inventory.TryRegisterLocation(
                    "loc-box",
                    InventoryLocationKind.DeliveryContainer,
                    24,
                    false,
                    out _),
                Is.True);
            Assert.That(
                inventory.TryRegisterLocation(
                    "loc-loose",
                    InventoryLocationKind.Loose,
                    24,
                    false,
                    out _),
                Is.True);
            Assert.That(
                inventory.TryRegisterLocation(
                    "loc-held",
                    InventoryLocationKind.Held,
                    1,
                    true,
                    out _),
                Is.True);
            Assert.That(
                inventory.TryRegisterLocation(
                    "loc-shelf",
                    InventoryLocationKind.Shelf,
                    8,
                    true,
                    out _),
                Is.True);
            Assert.That(inventory.TrySeedQuantity("loc-box", "prod-cola", 10, out _), Is.True);
            return inventory;
        }

        private static FirstStoreInventory CreateCheckoutInventory()
        {
            FirstStoreInventory inventory = new();
            Assert.That(inventory.TryRegisterProduct("prod-cola", out _), Is.True);
            Assert.That(inventory.TryRegisterProduct("prod-chips", out _), Is.True);
            Assert.That(
                inventory.TryRegisterLocation(
                    "loc-shelf",
                    InventoryLocationKind.Shelf,
                    10,
                    false,
                    out _),
                Is.True);
            Assert.That(inventory.TrySeedQuantity("loc-shelf", "prod-cola", 3, out _), Is.True);
            Assert.That(inventory.TrySeedQuantity("loc-shelf", "prod-chips", 2, out _), Is.True);
            return inventory;
        }

        private static Dictionary<string, string> CreateShelfMappings()
        {
            return new Dictionary<string, string>
            {
                ["prod-cola"] = "loc-shelf",
                ["prod-chips"] = "loc-shelf"
            };
        }

        private static CompletedTransactionLedger CreateResultLedger()
        {
            CompletedTransactionLedger ledger = new(8);
            Assert.That(
                ledger.TryAdd(
                    CreateCompletedSummary(
                        "transaction-result-002",
                        ("prod-cola", 150, 60, 2)),
                    out CompletedTransactionLedgerFailure secondFailure),
                Is.True,
                secondFailure.ToString());
            Assert.That(
                ledger.TryAdd(
                    CreateCompletedSummary(
                        "transaction-result-001",
                        ("prod-chips", 250, 100, 1)),
                    out CompletedTransactionLedgerFailure firstFailure),
                Is.True,
                firstFailure.ToString());
            return ledger;
        }

        private static CheckoutTransactionSummary CreateCompletedSummary(
            string transactionId,
            params (string productId, int unitPriceCents, int unitCostCents, int quantityUnits)[] lines)
        {
            CheckoutTransactionSummary summary = new(transactionId)
            {
                isCompleted = true
            };
            foreach ((string productId, int unitPriceCents, int unitCostCents, int quantityUnits) line in lines)
            {
                CheckoutLineSnapshot snapshot = new(
                    line.productId,
                    line.unitPriceCents,
                    line.unitCostCents,
                    line.quantityUnits);
                summary.lines.Add(snapshot);
                summary.subtotalCents += snapshot.LineTotalCents;
                summary.unitsSold += snapshot.quantityUnits;
            }
            summary.lines.Sort(
                (left, right) => string.CompareOrdinal(left.productId, right.productId));
            return summary;
        }

        private static FirstStoreSnapshot CreateCompleteSnapshot(
            out Dictionary<string, int> totalsBefore)
        {
            FixtureLayout layout = new(10, 8);
            Assert.That(
                layout.TryPlace(
                    "fixture-checkout-01",
                    new GridPosition(1, 1),
                    new GridFootprint(3, 2),
                    0).IsSuccess,
                Is.True);
            Assert.That(
                layout.TryPlace(
                    "fixture-shelf-01",
                    new GridPosition(5, 2),
                    new GridFootprint(2, 1),
                    1).IsSuccess,
                Is.True);

            FirstStoreInventory inventory = new();
            Assert.That(inventory.TryRegisterProduct("prod-cola", out _), Is.True);
            Assert.That(inventory.TryRegisterProduct("prod-chips", out _), Is.True);
            Assert.That(
                inventory.TryRegisterLocation(
                    "loc-box",
                    InventoryLocationKind.DeliveryContainer,
                    24,
                    false,
                    out _),
                Is.True);
            Assert.That(
                inventory.TryRegisterLocation(
                    "loc-loose",
                    InventoryLocationKind.Loose,
                    24,
                    false,
                    out _),
                Is.True);
            Assert.That(
                inventory.TryRegisterLocation(
                    "loc-held",
                    InventoryLocationKind.Held,
                    1,
                    true,
                    out _),
                Is.True);
            Assert.That(
                inventory.TryRegisterLocation(
                    "loc-shelf",
                    InventoryLocationKind.Shelf,
                    12,
                    false,
                    out _),
                Is.True);
            Assert.That(inventory.TrySeedQuantity("loc-box", "prod-cola", 6, out _), Is.True);
            Assert.That(inventory.TrySeedQuantity("loc-loose", "prod-chips", 3, out _), Is.True);
            Assert.That(inventory.TrySeedQuantity("loc-shelf", "prod-cola", 2, out _), Is.True);
            Assert.That(inventory.TrySeedQuantity("loc-shelf", "prod-chips", 2, out _), Is.True);

            Assert.That(
                DeliveryContainer.TryCreate(
                    inventory,
                    "container-starter",
                    "loc-box",
                    true,
                    out DeliveryContainer container,
                    out string containerError),
                Is.True,
                containerError);

            CompletedTransactionLedger ledger = new(8);
            Assert.That(
                CheckoutSession.TryCreate(
                    inventory,
                    CreateShelfMappings(),
                    "transaction-complete-001",
                    out CheckoutSession checkout,
                    out string checkoutError),
                Is.True,
                checkoutError);
            Assert.That(checkout.TryScan("prod-cola", 149, 60, 1, out _), Is.True);
            Assert.That(
                checkout.TryComplete(
                    ledger,
                    out _,
                    out _),
                Is.True);
            Assert.That(
                CheckoutSession.TryCreate(
                    inventory,
                    CreateShelfMappings(),
                    "transaction-complete-002",
                    out CheckoutSession secondCheckout,
                    out checkoutError),
                Is.True,
                checkoutError);
            Assert.That(secondCheckout.TryScan("prod-chips", 299, 100, 1, out _), Is.True);
            Assert.That(secondCheckout.TryComplete(ledger, out _, out _), Is.True);

            Assert.That(
                StoreOperatingSession.TryCreate(
                    "session-opening-001",
                    out StoreOperatingSession store,
                    out _),
                Is.True);
            Assert.That(store.TryTransition(StoreOperatingState.Preparing, out _), Is.True);
            Assert.That(store.TryTransition(StoreOperatingState.Open, out _), Is.True);
            Assert.That(store.TryTransition(StoreOperatingState.Closing, out _), Is.True);
            Assert.That(
                store.TryFinalizeClosing(
                    ledger,
                    90,
                    out _),
                Is.True);

            totalsBefore = new Dictionary<string, int>
            {
                ["prod-cola"] = inventory.GetTotalQuantity("prod-cola"),
                ["prod-chips"] = inventory.GetTotalQuantity("prod-chips")
            };

            return FirstStoreSnapshotMapper.Create(
                layout,
                inventory,
                new[] { container },
                ledger,
                store,
                new CleaningTaskSnapshot("task-floor-spill-01", 4, 4));
        }
    }
}
