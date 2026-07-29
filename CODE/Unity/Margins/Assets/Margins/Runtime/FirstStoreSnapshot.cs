// Draft implementation — Unity verification pending
using System;
using System.Collections.Generic;

namespace Margins
{
    [Serializable]
    public sealed class CleaningTaskSnapshot : IEquatable<CleaningTaskSnapshot>
    {
        public string taskId;
        public int requiredProgressUnits;
        public int completedProgressUnits;

        public CleaningTaskSnapshot(
            string taskId,
            int requiredProgressUnits,
            int completedProgressUnits)
        {
            this.taskId = taskId;
            this.requiredProgressUnits = requiredProgressUnits;
            this.completedProgressUnits = completedProgressUnits;
        }

        public bool IsComplete =>
            requiredProgressUnits > 0 &&
            completedProgressUnits >= requiredProgressUnits;

        public bool IsValid =>
            FirstStoreIdentifier.IsValid(taskId) &&
            requiredProgressUnits > 0 &&
            completedProgressUnits >= 0 &&
            completedProgressUnits <= requiredProgressUnits;

        public bool Equals(CleaningTaskSnapshot other)
        {
            return other != null &&
                   string.Equals(taskId, other.taskId, StringComparison.Ordinal) &&
                   requiredProgressUnits == other.requiredProgressUnits &&
                   completedProgressUnits == other.completedProgressUnits;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CleaningTaskSnapshot);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                taskId,
                requiredProgressUnits,
                completedProgressUnits);
        }
    }

    [Serializable]
    public sealed class FirstStoreSnapshot : IEquatable<FirstStoreSnapshot>
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int fixtureGridWidth;
        public int fixtureGridDepth;
        public List<FixturePlacementSnapshot> fixturePlacements = new();
        public FirstStoreInventorySnapshot inventory;
        public List<DeliveryContainerSnapshot> deliveryContainers = new();
        public CompletedTransactionLedgerSnapshot transactionLedger;
        public StoreOperatingSnapshot storeOperating;
        public CleaningTaskSnapshot cleaningTask;
        public int nextPhysicalUnitOrdinal = 1;
        public List<PhysicalProductUnitSnapshot> physicalProductUnits = new();

        public bool Equals(FirstStoreSnapshot other)
        {
            if (other == null ||
                version != other.version ||
                fixtureGridWidth != other.fixtureGridWidth ||
                fixtureGridDepth != other.fixtureGridDepth ||
                !FirstStoreEquality.AreEqual(inventory, other.inventory) ||
                !FirstStoreEquality.AreEqual(transactionLedger, other.transactionLedger) ||
                !FirstStoreEquality.AreEqual(storeOperating, other.storeOperating) ||
                !FirstStoreEquality.AreEqual(cleaningTask, other.cleaningTask) ||
                fixturePlacements == null ||
                other.fixturePlacements == null ||
                deliveryContainers == null ||
                other.deliveryContainers == null ||
                physicalProductUnits == null ||
                other.physicalProductUnits == null ||
                nextPhysicalUnitOrdinal != other.nextPhysicalUnitOrdinal ||
                fixturePlacements.Count != other.fixturePlacements.Count ||
                deliveryContainers.Count != other.deliveryContainers.Count ||
                physicalProductUnits.Count != other.physicalProductUnits.Count)
            {
                return false;
            }

            for (int index = 0; index < fixturePlacements.Count; index++)
            {
                if (!fixturePlacements[index].Equals(other.fixturePlacements[index]))
                {
                    return false;
                }
            }

            for (int index = 0; index < deliveryContainers.Count; index++)
            {
                if (!deliveryContainers[index].Equals(other.deliveryContainers[index]))
                {
                    return false;
                }
            }

            for (int index = 0; index < physicalProductUnits.Count; index++)
            {
                if (!physicalProductUnits[index].Equals(
                        other.physicalProductUnits[index]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FirstStoreSnapshot);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(version);
            hash.Add(fixtureGridWidth);
            hash.Add(fixtureGridDepth);
            hash.Add(inventory);
            hash.Add(transactionLedger);
            hash.Add(storeOperating);
            hash.Add(cleaningTask);
            hash.Add(nextPhysicalUnitOrdinal);
            if (fixturePlacements != null)
            {
                foreach (FixturePlacementSnapshot placement in fixturePlacements)
                {
                    hash.Add(placement);
                }
            }

            if (deliveryContainers != null)
            {
                foreach (DeliveryContainerSnapshot container in deliveryContainers)
                {
                    hash.Add(container);
                }
            }
            if (physicalProductUnits != null)
            {
                foreach (PhysicalProductUnitSnapshot physicalUnit in physicalProductUnits)
                {
                    hash.Add(physicalUnit);
                }
            }
            return hash.ToHashCode();
        }
    }

    public sealed class RestoredFirstStoreState
    {
        public FixtureLayout FixtureLayout { get; }
        public FirstStoreInventory Inventory { get; }
        public IReadOnlyList<DeliveryContainer> DeliveryContainers { get; }
        public CompletedTransactionLedger TransactionLedger { get; }
        public StoreOperatingSession StoreOperating { get; }
        public CleaningTaskSnapshot CleaningTask { get; }

        public RestoredFirstStoreState(
            FixtureLayout fixtureLayout,
            FirstStoreInventory inventory,
            IReadOnlyList<DeliveryContainer> deliveryContainers,
            CompletedTransactionLedger transactionLedger,
            StoreOperatingSession storeOperating,
            CleaningTaskSnapshot cleaningTask)
        {
            FixtureLayout = fixtureLayout;
            Inventory = inventory;
            DeliveryContainers = deliveryContainers;
            TransactionLedger = transactionLedger;
            StoreOperating = storeOperating;
            CleaningTask = cleaningTask;
        }
    }

    public static class FirstStoreSnapshotMapper
    {
        public static FirstStoreSnapshot Create(
            FixtureLayout fixtureLayout,
            FirstStoreInventory inventory,
            IReadOnlyList<DeliveryContainer> deliveryContainers,
            CompletedTransactionLedger transactionLedger,
            StoreOperatingSession storeOperating,
            CleaningTaskSnapshot cleaningTask,
            IReadOnlyDictionary<string, int> productUnitCostsCents)
        {
            if (fixtureLayout == null)
            {
                throw new ArgumentNullException(nameof(fixtureLayout));
            }

            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            if (deliveryContainers == null)
            {
                throw new ArgumentNullException(nameof(deliveryContainers));
            }

            if (storeOperating == null)
            {
                throw new ArgumentNullException(nameof(storeOperating));
            }

            if (transactionLedger == null)
            {
                throw new ArgumentNullException(nameof(transactionLedger));
            }

            if (productUnitCostsCents == null)
            {
                throw new ArgumentNullException(nameof(productUnitCostsCents));
            }

            foreach (CheckoutTransactionSummary transaction in transactionLedger.Transactions)
            {
                foreach (CheckoutLineSnapshot line in transaction.lines)
                {
                    if (!inventory.IsKnownProduct(line.productId))
                    {
                        throw new ArgumentException(
                            $"Transaction ledger references unknown product '{line.productId}'.",
                            nameof(transactionLedger));
                    }
                }
            }

            if (!StoreOperatingSession.TryRestore(
                    storeOperating.CreateSnapshot(),
                    transactionLedger,
                    productUnitCostsCents,
                    out _,
                    out string operatingError))
            {
                throw new ArgumentException(
                    operatingError,
                    nameof(storeOperating));
            }

            if (cleaningTask != null && !cleaningTask.IsValid)
            {
                throw new ArgumentException(
                    "Cleaning task snapshot is invalid.",
                    nameof(cleaningTask));
            }

            FirstStoreSnapshot snapshot = new()
            {
                fixtureGridWidth = fixtureLayout.Width,
                fixtureGridDepth = fixtureLayout.Depth,
                fixturePlacements = fixtureLayout.CreateSnapshot(),
                inventory = inventory.CreateSnapshot(),
                transactionLedger = transactionLedger.CreateSnapshot(),
                storeOperating = storeOperating.CreateSnapshot(),
                cleaningTask = CloneCleaningTask(cleaningTask)
            };

            HashSet<string> containerIds = new(StringComparer.Ordinal);
            foreach (DeliveryContainer deliveryContainer in deliveryContainers)
            {
                if (deliveryContainer == null)
                {
                    throw new ArgumentException(
                        "Delivery container list contains null.",
                        nameof(deliveryContainers));
                }

                if (!containerIds.Add(deliveryContainer.ContainerId))
                {
                    throw new ArgumentException(
                        $"Delivery container list contains duplicate id '{deliveryContainer.ContainerId}'.",
                        nameof(deliveryContainers));
                }
                snapshot.deliveryContainers.Add(deliveryContainer.CreateSnapshot());
            }

            snapshot.deliveryContainers.Sort((left, right) =>
                string.CompareOrdinal(left.containerId, right.containerId));
            return snapshot;
        }

        public static bool TryRestore(
            FirstStoreSnapshot snapshot,
            IReadOnlyDictionary<string, int> productUnitCostsCents,
            out RestoredFirstStoreState state,
            out string error)
        {
            state = null;
            if (snapshot == null)
            {
                error = "First-store snapshot is missing.";
                return false;
            }

            if (snapshot.version != FirstStoreSnapshot.CurrentVersion)
            {
                error =
                    $"Unsupported first-store snapshot version {snapshot.version}; expected {FirstStoreSnapshot.CurrentVersion}.";
                return false;
            }

            if (!FixtureLayout.TryRestore(
                    snapshot.fixtureGridWidth,
                    snapshot.fixtureGridDepth,
                    snapshot.fixturePlacements,
                    out FixtureLayout fixtureLayout,
                    out error))
            {
                return false;
            }

            if (!FirstStoreInventory.TryRestore(
                    snapshot.inventory,
                    out FirstStoreInventory inventory,
                    out error))
            {
                return false;
            }

            if (!CompletedTransactionLedger.TryRestore(
                    snapshot.transactionLedger,
                    out CompletedTransactionLedger transactionLedger,
                    out error))
            {
                return false;
            }

            foreach (CheckoutTransactionSummary transaction in transactionLedger.Transactions)
            {
                foreach (CheckoutLineSnapshot line in transaction.lines)
                {
                    if (!inventory.IsKnownProduct(line.productId))
                    {
                        error =
                            $"Transaction ledger references unknown product '{line.productId}'.";
                        return false;
                    }
                }
            }

            if (snapshot.deliveryContainers == null)
            {
                error = "Delivery container snapshot list is missing.";
                return false;
            }

            List<DeliveryContainerSnapshot> orderedContainerSnapshots =
                new(snapshot.deliveryContainers);
            orderedContainerSnapshots.Sort((left, right) =>
            {
                if (left == null)
                {
                    return right == null ? 0 : -1;
                }

                if (right == null)
                {
                    return 1;
                }

                return string.CompareOrdinal(left.containerId, right.containerId);
            });

            HashSet<string> containerIds = new(StringComparer.Ordinal);
            List<DeliveryContainer> deliveryContainers = new();
            foreach (DeliveryContainerSnapshot containerSnapshot in orderedContainerSnapshots)
            {
                if (containerSnapshot == null ||
                    !containerIds.Add(containerSnapshot.containerId))
                {
                    error = "Delivery container snapshot contains a null or duplicate identifier.";
                    return false;
                }

                if (!DeliveryContainer.TryRestore(
                        inventory,
                        containerSnapshot,
                        out DeliveryContainer container,
                        out error))
                {
                    return false;
                }

                deliveryContainers.Add(container);
            }

            if (!StoreOperatingSession.TryRestore(
                    snapshot.storeOperating,
                    transactionLedger,
                    productUnitCostsCents,
                    out StoreOperatingSession storeOperating,
                    out error))
            {
                return false;
            }

            if (snapshot.cleaningTask != null && !snapshot.cleaningTask.IsValid)
            {
                error = "Cleaning task snapshot is invalid.";
                return false;
            }

            state = new RestoredFirstStoreState(
                fixtureLayout,
                inventory,
                deliveryContainers,
                transactionLedger,
                storeOperating,
                CloneCleaningTask(snapshot.cleaningTask));
            error = null;
            return true;
        }

        private static CleaningTaskSnapshot CloneCleaningTask(
            CleaningTaskSnapshot source)
        {
            return source == null
                ? null
                : new CleaningTaskSnapshot(
                    source.taskId,
                    source.requiredProgressUnits,
                    source.completedProgressUnits);
        }
    }
}
