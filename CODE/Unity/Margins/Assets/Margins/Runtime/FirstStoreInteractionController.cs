using UnityEngine;
using UnityEngine.InputSystem;

namespace Margins
{
    public sealed class FirstStoreInteractionController : MonoBehaviour
    {
        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private StockingController stocking;
        [SerializeField, Min(0.1f)] private float pickupDistance = 3f;
        [SerializeField] private LayerMask pickupLayers = ~0;

        public ProductItem HeldProduct => stocking?.HeldPhysicalUnit;
        public bool IsWorldInteractionEnabled =>
            firstPersonController != null &&
            firstPersonController.IsGameplayInputActive;

        private void Start()
        {
            if (!TryValidateConfiguration(out string error))
            {
                Debug.LogError(error, this);
            }
        }

        private void Update()
        {
            if (!IsWorldInteractionEnabled)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                if (HeldProduct == null)
                {
                    TryPickUpTargetedUnit(out _, out _);
                }
                else
                {
                    TryStockHeldUnit(out _);
                }
            }

            ProductItem held = HeldProduct;
            if (held == null || Mouse.current == null)
            {
                return;
            }

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > 0f)
            {
                TryRotateHeldUnit(1);
            }
            else if (scroll < 0f)
            {
                TryRotateHeldUnit(-1);
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (firstPersonController == null || viewCamera == null || stocking == null)
            {
                error =
                    "First-store interaction requires explicit player, camera, and stocking references.";
                return false;
            }

            if (pickupDistance <= 0f)
            {
                error = "First-store interaction pickup distance must be positive.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryPickUpTargetedUnit(
            out ProductItem selectedUnit,
            out string error)
        {
            selectedUnit = null;
            if (!IsWorldInteractionEnabled)
            {
                error = "World interaction is disabled while the validation HUD is active.";
                return false;
            }

            Ray ray = new(viewCamera.transform.position, viewCamera.transform.forward);
            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    pickupDistance,
                    pickupLayers,
                    QueryTriggerInteraction.Ignore))
            {
                error = "No loose physical product unit is targeted.";
                return false;
            }

            ProductItem targetedUnit =
                hit.collider.GetComponentInParent<ProductItem>();
            if (targetedUnit == null)
            {
                error = "The targeted collider is not a physical product unit.";
                return false;
            }

            return stocking.TryPickUpLooseUnit(
                targetedUnit,
                out selectedUnit,
                out error);
        }

        public bool TryStockHeldUnit(out string error)
        {
            if (!IsWorldInteractionEnabled)
            {
                error = "World interaction is disabled while the validation HUD is active.";
                return false;
            }

            ProductItem held = HeldProduct;
            if (held == null)
            {
                error = "No physical product unit is held.";
                return false;
            }

            return stocking.TryStockHeldUnit(held.QuarterTurns, out error);
        }

        public bool TryRotateHeldUnit(int direction)
        {
            if (!IsWorldInteractionEnabled || direction == 0)
            {
                return false;
            }

            ProductItem held = HeldProduct;
            return held != null && held.AdjustQuarterTurns(direction > 0 ? 1 : -1);
        }
    }
}
