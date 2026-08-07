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

        private Transform restingParent;
        private Collider[] toolColliders;

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
        public FirstStoreWorldInteractionPrompt Prompt => IsCarried
            ? new FirstStoreWorldInteractionPrompt("Q", $"Put down {DisplayName}")
            : new FirstStoreWorldInteractionPrompt("E", $"Pick up {DisplayName}");

        private void Awake()
        {
            restingParent = transform.parent;
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
            SetColliderState(true);
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
