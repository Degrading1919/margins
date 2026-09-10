using System;
using UnityEngine;

namespace Margins
{
    public interface ICarryableToolCapability
    {
        string CapabilityId { get; }
    }

    /// <summary>
    /// A physical reusable tool. The tool exposes one bounded capability while
    /// PlayerCarryableToolController owns which tool is currently carried.
    /// </summary>
    public sealed class CarryableToolComponent :
        MonoBehaviour,
        ICarryableToolCapability,
        IFirstStoreWorldInteractionTarget
    {
        [SerializeField] private string stableToolId;
        [SerializeField] private string capabilityId;
        [SerializeField] private string displayName = "tool";
        [SerializeField] private PlayerCarryableToolController carrier;
        [SerializeField] private Vector3 carriedLocalPosition;
        [SerializeField] private Vector3 carriedLocalEulerAngles;
        [SerializeField] private CarryableToolComponent storageTool;
        [SerializeField] private CarryableToolComponent storedTool;

        private Transform restingParent;
        private Collider[] toolColliders;
        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;
        public CarryableToolComponent StorageTool => storageTool;
        public CarryableToolComponent StoredTool => storedTool;
        public Transform KitRoot => storageTool != null ? storageTool.transform : transform;
        public bool IsKitCarried => IsCarried || storageTool?.IsCarried == true || storedTool?.IsCarried == true;
        public bool HasBeenPlaced { get; private set; }

        public string StableToolId => stableToolId;
        public string CapabilityId => capabilityId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? "tool"
            : displayName;
        public bool IsCarried => carrier != null && carrier.HeldTool == this;
        public string StableTargetId => stableToolId;
        public FirstStoreWorldInteractionPriority Priority =>
            FirstStoreWorldInteractionPriority.Tool;
        public bool IsAvailable =>
            FirstStoreIdentifier.IsValid(stableToolId) &&
            FirstStoreIdentifier.IsValid(capabilityId) &&
            carrier != null;
        public FirstStoreWorldInteractionPrompt Prompt => storageTool != null
            ? IsCarried
                ? new FirstStoreWorldInteractionPrompt("Q", "Return mop to bucket")
                : !storageTool.HasBeenPlaced
                    ? storageTool.Prompt
                    : new FirstStoreWorldInteractionPrompt("E", "Take mop from bucket")
            : storedTool?.IsCarried == true
                ? new FirstStoreWorldInteractionPrompt("E", "Return mop to bucket")
                : new FirstStoreWorldInteractionPrompt("E", IsCarried
                    ? $"Place {DisplayName}" : $"Pick up {DisplayName}");

        private void Awake()
        {
            restingParent = transform.parent;
            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;
            toolColliders = GetComponentsInChildren<Collider>(true);
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (!FirstStoreIdentifier.IsValid(stableToolId) ||
                !FirstStoreIdentifier.IsValid(capabilityId) || carrier == null)
            {
                error =
                    $"Carryable tool '{name}' requires stable tool/capability ids and an explicit carrier.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryPrimary(out string error)
        {
            if (!TryValidateConfiguration(out error))
            {
                return false;
            }

            if (storageTool != null && !storageTool.HasBeenPlaced)
                return storageTool.TryPrimary(out error);
            if (storedTool?.IsCarried == true)
                return carrier.TryReturnHeldTool(out error);
            return IsCarried
                ? carrier.TrySetDownHeldTool(out error)
                : carrier.TryPickUp(this, out error);
        }

        public bool TryCancel(out string error)
        {
            if (!IsCarried)
            {
                error = $"The {DisplayName} is not being carried.";
                return false;
            }

            return carrier.TrySetDownHeldTool(out error);
        }

        internal void AttachTo(Transform holdPoint)
        {
            restingParent = transform.parent;
            transform.SetParent(holdPoint, false);
            transform.localPosition = carriedLocalPosition;
            transform.localRotation = Quaternion.Euler(carriedLocalEulerAngles);
            SetColliderState(false);
        }

        internal void DetachAt(Vector3 worldPosition, Quaternion worldRotation)
        {
            transform.SetParent(restingParent, true);
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            HasBeenPlaced = true;
            SetColliderState(true);
        }

        internal void ReturnToStorage()
        {
            transform.SetParent(storageTool.transform, false);
            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
            SetColliderState(true);
        }

        internal void ResetKitAfterRestore()
        {
            transform.SetParent(restingParent, false);
            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
            HasBeenPlaced = false;
            SetColliderState(true);
            if (storedTool != null) storedTool.ReturnToStorage();
        }

        private void SetColliderState(bool enabled)
        {
            toolColliders ??= GetComponentsInChildren<Collider>(true);
            foreach (Collider toolCollider in toolColliders)
            {
                if (toolCollider != null)
                {
                    toolCollider.enabled = enabled;
                }
            }
        }
    }
}
