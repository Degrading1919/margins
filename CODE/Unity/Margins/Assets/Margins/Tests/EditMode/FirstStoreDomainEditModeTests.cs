// Draft implementation — Unity verification pending
using System.Collections.Generic;
using NUnit.Framework;

namespace Margins.Tests
{
    public sealed class FirstStoreDomainEditModeTests
    {
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
            Assert.That(
                CheckoutSession.TryCreate(
                    inventory,
                    "loc-shelf",
                    "transaction-001",
                    out CheckoutSession checkout,
                    out string error),
                Is.True,
                error);

            Assert.That(checkout.TryScan("prod-cola", 149, 2, out _), Is.True);
            Assert.That(checkout.TryScan("prod-chips", 299, 1, out _), Is.True);
            Assert.That(checkout.SubtotalCents, Is.EqualTo(597));
            Assert.That(checkout.TryRemove("prod-cola", 1, out _), Is.True);
            Assert.That(checkout.SubtotalCents, Is.EqualTo(448));

            Assert.That(
                checkout.TryComplete(
                    out CheckoutTransactionSummary summary,
                    out CheckoutFailure failure),
                Is.True);
            Assert.That(failure, Is.EqualTo(CheckoutFailure.None));
            Assert.That(summary.subtotalCents, Is.EqualTo(448));
            Assert.That(summary.unitsSold, Is.EqualTo(2));
            Assert.That(inventory.GetQuantity("loc-shelf", "prod-cola"), Is.EqualTo(2));
            Assert.That(inventory.GetQuantity("loc-shelf", "prod-chips"), Is.EqualTo(1));
        }

        [Test]
        public void CheckoutCompletionIsIdempotent()
        {
            FirstStoreInventory inventory = CreateCheckoutInventory();
            Assert.That(
                CheckoutSession.TryCreate(
                    inventory,
                    "loc-shelf",
                    "transaction-002",
                    out CheckoutSession checkout,
                    out string error),
                Is.True,
                error);
            Assert.That(checkout.TryScan("prod-cola", 149, 1, out _), Is.True);

            Assert.That(
                checkout.TryComplete(
                    out CheckoutTransactionSummary first,
                    out CheckoutFailure firstFailure),
                Is.True);
            int stockAfterFirstCompletion =
                inventory.GetQuantity("loc-shelf", "prod-cola");
            Assert.That(
                checkout.TryComplete(
                    out CheckoutTransactionSummary second,
                    out CheckoutFailure secondFailure),
                Is.True);

            Assert.That(firstFailure, Is.EqualTo(CheckoutFailure.None));
            Assert.That(secondFailure, Is.EqualTo(CheckoutFailure.AlreadyCompleted));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(second, Is.Not.SameAs(first));
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
                    "loc-shelf",
                    "transaction-003",
                    out CheckoutSession checkout,
                    out string error),
                Is.True,
                error);

            Assert.That(
                checkout.TryScan(
                    "prod-missing",
                    149,
                    1,
                    out CheckoutFailure invalidProduct),
                Is.False);
            Assert.That(invalidProduct, Is.EqualTo(CheckoutFailure.InvalidProduct));
            Assert.That(
                checkout.TryScan(
                    "prod-cola",
                    149,
                    4,
                    out CheckoutFailure insufficientStock),
                Is.False);
            Assert.That(insufficientStock, Is.EqualTo(CheckoutFailure.InsufficientStock));
            Assert.That(checkout.Lines, Is.Empty);
            Assert.That(inventory.GetQuantity("loc-shelf", "prod-cola"), Is.EqualTo(3));
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
                    null,
                    out StoreOperatingFailure directOpenFailure),
                Is.False);
            Assert.That(directOpenFailure, Is.EqualTo(StoreOperatingFailure.InvalidTransition));
            Assert.That(session.State, Is.EqualTo(StoreOperatingState.Closed));

            Assert.That(
                session.TryTransition(
                    StoreOperatingState.Preparing,
                    null,
                    out _),
                Is.True);
            Assert.That(
                session.TryTransition(
                    StoreOperatingState.Open,
                    null,
                    out _),
                Is.True);
            Assert.That(
                session.TryTransition(
                    StoreOperatingState.ClosedWithResultPending,
                    null,
                    out StoreOperatingFailure skipClosingFailure),
                Is.False);
            Assert.That(skipClosingFailure, Is.EqualTo(StoreOperatingFailure.InvalidTransition));
            Assert.That(session.State, Is.EqualTo(StoreOperatingState.Open));
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
                restored.CheckoutSummary,
                restored.StoreOperating,
                restored.CleaningTask);

            Assert.That(after, Is.EqualTo(before));
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
        public void SaveRestoreMappingDoesNotDuplicateAnyInventoryUnit()
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

            FirstStoreSnapshot mappedAgain = FirstStoreSnapshotMapper.Create(
                restored.FixtureLayout,
                restored.Inventory,
                restored.DeliveryContainers,
                restored.CheckoutSummary,
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

            Assert.That(
                CheckoutSession.TryCreate(
                    inventory,
                    "loc-shelf",
                    "transaction-complete-001",
                    out CheckoutSession checkout,
                    out string checkoutError),
                Is.True,
                checkoutError);
            Assert.That(checkout.TryScan("prod-cola", 149, 1, out _), Is.True);
            Assert.That(
                checkout.TryComplete(
                    out CheckoutTransactionSummary summary,
                    out _),
                Is.True);

            Assert.That(
                StoreOperatingSession.TryCreate(
                    "session-opening-001",
                    out StoreOperatingSession store,
                    out _),
                Is.True);
            Assert.That(store.TryTransition(StoreOperatingState.Preparing, null, out _), Is.True);
            Assert.That(store.TryTransition(StoreOperatingState.Open, null, out _), Is.True);
            Assert.That(store.TryTransition(StoreOperatingState.Closing, null, out _), Is.True);
            Assert.That(
                store.TryTransition(
                    StoreOperatingState.ClosedWithResultPending,
                    new StoreSessionTotals(149, 1, 1, 90),
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
                summary,
                store,
                new CleaningTaskSnapshot("task-floor-spill-01", 4, 4));
        }
    }
}
