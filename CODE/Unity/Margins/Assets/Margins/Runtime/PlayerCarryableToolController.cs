using System;
using UnityEngine;

namespace Margins
{
    public sealed class PlayerCarryableToolController : MonoBehaviour
    {
        [SerializeField] private Transform holdPoint;
        [SerializeField] private Transform playerBody;
        [SerializeField] private StockingController stocking;
        [SerializeField] private DeliveryBoxComponent deliveryBox;
        [SerializeField] private CarryableToolComponent cleaningKit;
        [SerializeField, Min(0.25f)] private float setDownDistance = 1.2f;

        public CarryableToolComponent HeldTool { get; private set; }
        public bool HasHeldTool => HeldTool != null;
        public string HeldToolName => HeldTool?.DisplayName;

        public void ResetTransientStateAfterRestore()
        {
            HeldTool = null;
            cleaningKit?.ResetKitAfterRestore();
        }

        public bool TryReturnHeldTool(out string error)
        {
            CarryableToolComponent storage = HeldTool?.StorageTool;
            if (storage == null || storage.IsCarried)
            {
                error = "There is no placed bucket for this mop.";
                return false;
            }
            if (Vector3.Distance(playerBody.position, storage.transform.position) > 3f)
            {
                error = "Move closer to the bucket, then return the mop.";
                return false;
            }
            HeldTool.ReturnToStorage();
            HeldTool = null;
            error = null;
            return true;
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (holdPoint == null || playerBody == null || stocking == null ||
                setDownDistance <= 0f)
            {
                error =
                    "Player tool carrying requires explicit hold-point, player, and stocking references.";
                return false;
            }

            error = null;
            return true;
        }

        public bool HasCapability(string capabilityId)
        {
            return HeldTool != null &&
                   FirstStoreIdentifier.IsValid(capabilityId) &&
                   string.Equals(
                       HeldTool.CapabilityId,
                       capabilityId,
                       StringComparison.Ordinal);
        }

        public bool TryPickUp(CarryableToolComponent tool, out string error)
        {
            if (!TryValidateConfiguration(out error) || tool == null ||
                !tool.TryValidateConfiguration(out error))
            {
                error ??= "That tool cannot be carried right now.";
                return false;
            }

            if (HeldTool != null)
            {
                error = HeldTool.StorageTool != null
                    ? "Return the mop to its bucket first."
                    : $"Set down {HeldTool.DisplayName} first.";
                return false;
            }

            if (deliveryBox != null && deliveryBox.IsCarried &&
                deliveryBox.transform.IsChildOf(playerBody))
            {
                error = "Set down the delivery box first.";
                return false;
            }
            if (tool.StorageTool != null &&
                (tool.StorageTool.IsCarried || !tool.StorageTool.HasBeenPlaced))
            {
                error = "Carry and place the bucket before taking the mop.";
                return false;
            }
            if (tool.StoredTool != null &&
                tool.StoredTool.transform.parent != tool.transform)
            {
                error = "Return the mop to the bucket first.";
                return false;
            }

            if (stocking.HasHeldUnit)
            {
                error = stocking.PlayerHasHeldUnit
                    ? "Put down or stock the product in your hands first."
                    : "A team member is moving stock. Try again in a moment.";
                return false;
            }

            HeldTool = tool;
            tool.AttachTo(holdPoint);
            error = null;
            return true;
        }

        public bool TrySetDownHeldTool(out string error)
        {
            if (!TryValidateConfiguration(out error) || HeldTool == null)
            {
                error ??= "No tool is being carried.";
                return false;
            }

            if (HeldTool.StorageTool != null)
                return TryReturnHeldTool(out error);

            Vector3 forward = Vector3.ProjectOnPlane(playerBody.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();
            Vector3 position = playerBody.position + forward * setDownDistance;
            position.y = 0f;
            Quaternion rotation = Quaternion.Euler(0f, playerBody.eulerAngles.y, 0f);
            CarryableToolComponent released = HeldTool;
            HeldTool = null;
            released.DetachAt(position, rotation);
            error = null;
            return true;
        }
    }
}
