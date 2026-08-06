using UnityEngine;

namespace Margins
{
    public sealed class CustomerCheckoutItemWorldInteractionTarget :
        MonoBehaviour,
        IFirstStoreWorldInteractionTarget
    {
        private StoreCustomerFlowController customerFlow;
        private string physicalUnitId;

        public string StableTargetId =>
            $"target-customer-item-{physicalUnitId}";
        public FirstStoreWorldInteractionPriority Priority =>
            FirstStoreWorldInteractionPriority.Checkout;
        public bool IsAvailable =>
            isActiveAndEnabled && customerFlow != null &&
            customerFlow.CanScanCustomerItem(physicalUnitId);
        public FirstStoreWorldInteractionPrompt Prompt =>
            new(
                "E",
                $"Scan {ProductName}",
                $"item {customerFlow.ActiveCheckoutScannedCount + 1}/" +
                customerFlow.ActiveCheckoutItemCount);

        internal void Initialize(
            StoreCustomerFlowController flow,
            string unitId)
        {
            customerFlow = flow;
            physicalUnitId = unitId;
        }

        public bool TryPrimary(out string error)
        {
            if (!IsAvailable)
            {
                error = "This physical item is not ready to scan.";
                return false;
            }
            return customerFlow.TryScanCustomerItem(physicalUnitId, out error);
        }

        public bool TryCancel(out string error)
        {
            if (customerFlow == null)
            {
                error = "No customer checkout is active.";
                return false;
            }
            return customerFlow.TryCorrectLastScan(out error);
        }

        private string ProductName
        {
            get
            {
                return customerFlow != null &&
                       customerFlow.PhysicalUnits.TryGetUnit(
                           physicalUnitId,
                           out ProductItem item,
                           out _) &&
                       !string.IsNullOrWhiteSpace(item.Definition?.DisplayName)
                    ? item.Definition.DisplayName
                    : "item";
            }
        }
    }
}
