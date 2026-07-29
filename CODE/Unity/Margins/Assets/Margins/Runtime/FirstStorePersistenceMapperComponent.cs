using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    public sealed class FirstStorePersistenceMapperComponent : MonoBehaviour
    {
        [SerializeField] private FixturePlacementController fixturePlacement;
        [SerializeField] private FirstStoreInventoryComponent inventoryComponent;
        [SerializeField] private DeliveryBoxComponent[] deliveryBoxes;
        [SerializeField] private PhysicalProductUnitRegistry physicalUnits;
        [SerializeField] private CheckoutStationComponent checkout;
        [SerializeField] private StoreOperatingController storeOperating;
        [SerializeField] private CleaningTaskComponent cleaningTask;

        public bool TryValidateConfiguration(out string error)
        {
            if (fixturePlacement == null ||
                inventoryComponent == null ||
                physicalUnits == null ||
                checkout == null ||
                storeOperating == null ||
                cleaningTask == null)
            {
                error =
                    "First-store persistence requires explicit fixture, inventory, physical-unit, checkout, operating, and cleaning references.";
                return false;
            }

            if (!fixturePlacement.IsInitialized ||
                !inventoryComponent.IsInitialized ||
                !storeOperating.IsInitialized ||
                checkout.TransactionLedger == null)
            {
                error = "First-store persistence references are not initialized.";
                return false;
            }

            if (checkout.InventoryComponent != inventoryComponent ||
                checkout.PhysicalUnits != physicalUnits ||
                storeOperating.Stocking.PhysicalUnits != physicalUnits)
            {
                error =
                    "First-store persistence checkout and physical units do not use the configured inventory path.";
                return false;
            }

            if (storeOperating.FixturePlacement != fixturePlacement ||
                storeOperating.Checkout != checkout ||
                storeOperating.CleaningTask != cleaningTask)
            {
                error =
                    "First-store persistence references do not match the operating controller.";
                return false;
            }

            if (deliveryBoxes == null)
            {
                error = "First-store persistence delivery-box array is missing.";
                return false;
            }

            HashSet<string> containerIds = new(StringComparer.Ordinal);
            foreach (DeliveryBoxComponent deliveryBox in deliveryBoxes)
            {
                if (deliveryBox == null || !deliveryBox.IsInitialized)
                {
                    error =
                        "First-store persistence contains a missing or uninitialized delivery box.";
                    return false;
                }

                if (deliveryBox.InventoryComponent != inventoryComponent ||
                    deliveryBox.PhysicalUnits != physicalUnits)
                {
                    error =
                        $"Delivery box '{deliveryBox.StableContainerId}' does not use the configured inventory and physical units.";
                    return false;
                }

                if (!containerIds.Add(deliveryBox.StableContainerId))
                {
                    error =
                        $"First-store persistence contains duplicate delivery id '{deliveryBox.StableContainerId}'.";
                    return false;
                }
            }

            if (!physicalUnits.TryValidateConfiguration(out error) ||
                !checkout.TryValidateConfiguration(out error) ||
                !cleaningTask.TryValidateConfiguration(out error))
            {
                return false;
            }

            error = null;
            return true;
        }

        public bool TryCapture(
            out FirstStoreSnapshot snapshot,
            out string error)
        {
            snapshot = null;
            if (!TryValidateConfiguration(out error))
            {
                return false;
            }

            List<DeliveryContainer> containers = new();
            foreach (DeliveryBoxComponent deliveryBox in deliveryBoxes)
            {
                containers.Add(deliveryBox.Container);
            }

            if (!physicalUnits.TryCapture(
                    inventoryComponent.Inventory,
                    out List<PhysicalProductUnitSnapshot> physicalSnapshots,
                    out int nextPhysicalUnitOrdinal,
                    out error))
            {
                return false;
            }

            try
            {
                snapshot = FirstStoreSnapshotMapper.Create(
                    fixturePlacement.Layout,
                    inventoryComponent.Inventory,
                    containers,
                    checkout.TransactionLedger,
                    storeOperating.Session,
                    cleaningTask.CreateSnapshot(),
                    checkout.ProductUnitCostsCents);
                snapshot.physicalProductUnits = physicalSnapshots;
                snapshot.nextPhysicalUnitOrdinal = nextPhysicalUnitOrdinal;
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = $"First-store snapshot capture failed: {exception.Message}";
                return false;
            }
        }

        public bool TryRestore(
            FirstStoreSnapshot snapshot,
            out string error)
        {
            if (!TryValidateConfiguration(out error))
            {
                return false;
            }

            if (!FirstStoreSnapshotMapper.TryRestore(
                    snapshot,
                    checkout.ProductUnitCostsCents,
                    out RestoredFirstStoreState restored,
                    out error))
            {
                return false;
            }

            if (!inventoryComponent.CanApplyRestoredInventory(
                    restored.Inventory,
                    out error) ||
                !fixturePlacement.CanApplyRestoredLayout(
                    restored.FixtureLayout,
                    out error) ||
                !checkout.CanApplyLedger(
                    restored.TransactionLedger,
                    out error) ||
                !storeOperating.CanApplySnapshot(
                    restored.StoreOperating.CreateSnapshot(),
                    restored.TransactionLedger,
                    out error) ||
                !cleaningTask.CanApplySnapshot(
                    restored.CleaningTask,
                    out error) ||
                !physicalUnits.CanApplySnapshot(
                    restored.Inventory,
                    snapshot.physicalProductUnits,
                    snapshot.nextPhysicalUnitOrdinal,
                    storeOperating.Stocking,
                    out error))
            {
                return false;
            }

            Dictionary<string, DeliveryContainer> restoredContainers =
                new(StringComparer.Ordinal);
            foreach (DeliveryContainer container in restored.DeliveryContainers)
            {
                restoredContainers.Add(container.ContainerId, container);
            }

            if (restoredContainers.Count != deliveryBoxes.Length)
            {
                error = "Restored delivery-container count does not match inspector references.";
                return false;
            }

            foreach (DeliveryBoxComponent deliveryBox in deliveryBoxes)
            {
                if (!restoredContainers.TryGetValue(
                        deliveryBox.StableContainerId,
                        out DeliveryContainer restoredContainer) ||
                    !deliveryBox.CanApplyRestoredContainer(
                        restoredContainer,
                        out error))
                {
                    error ??=
                        $"No restored delivery container matches '{deliveryBox.StableContainerId}'.";
                    return false;
                }
            }

            if (!inventoryComponent.TryApplyRestoredInventory(
                    restored.Inventory,
                    out error) ||
                !fixturePlacement.TryApplyRestoredLayout(
                    restored.FixtureLayout,
                    out error))
            {
                return false;
            }

            foreach (DeliveryBoxComponent deliveryBox in deliveryBoxes)
            {
                if (!deliveryBox.TryApplyRestoredContainer(
                        restoredContainers[deliveryBox.StableContainerId],
                        out error))
                {
                    return false;
                }
            }

            if (!checkout.TryApplyLedger(restored.TransactionLedger, out error) ||
                !storeOperating.TryApplySnapshot(
                    restored.StoreOperating.CreateSnapshot(),
                    restored.TransactionLedger,
                    out error) ||
                !cleaningTask.TryApplySnapshot(
                    restored.CleaningTask,
                    out error) ||
                !physicalUnits.TryApplySnapshot(
                    restored.Inventory,
                    snapshot.physicalProductUnits,
                    snapshot.nextPhysicalUnitOrdinal,
                    storeOperating.Stocking,
                    out error))
            {
                return false;
            }

            error = null;
            return true;
        }
    }
}
