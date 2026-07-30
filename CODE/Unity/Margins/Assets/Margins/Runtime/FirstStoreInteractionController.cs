using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Margins
{
    public sealed class FirstStoreInteractionController : MonoBehaviour
    {
        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private StockingController stocking;
        [SerializeField] private FirstStoreFixturePlacementModeController fixturePlacementMode;
        [SerializeField, Min(0.1f)] private float pickupDistance = 3f;
        [SerializeField] private LayerMask pickupLayers = ~0;

        private readonly List<FirstStoreWorldInteractionCandidate> candidates = new();
        private IFirstStoreWorldInteractionTarget focusedTarget;
        private FirstStoreWorldInteractionPrompt currentPrompt;

        public event Action<FirstStoreInteractionFeedback> InteractionResolved;

        public ProductItem HeldProduct => stocking?.HeldPhysicalUnit;
        public bool IsWorldInteractionEnabled =>
            firstPersonController != null &&
            firstPersonController.IsGameplayInputActive;
        public string FocusedTargetId => focusedTarget?.StableTargetId;
        public FirstStoreWorldInteractionPrompt CurrentPrompt => currentPrompt;
        public string CurrentPromptText => currentPrompt?.FormattedText ?? string.Empty;
        public string LastFeedback { get; private set; }
        public int FeedbackRevision { get; private set; }

        private void Start()
        {
            if (!TryValidateConfiguration(out string error))
            {
                Debug.LogError(error, this);
            }
        }

        private void OnDisable()
        {
            CancelActiveFixturePlacement();
            ClearFocus();
        }

        private void Update()
        {
            if (!IsWorldInteractionEnabled)
            {
                CancelActiveFixturePlacement();
                ClearFocus();
                return;
            }

            RefreshFocus();

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                TryPrimaryInteraction(out _);
            }
            if (keyboard != null && keyboard.qKey.wasPressedThisFrame)
            {
                TryCancelInteraction(out _);
            }
            if (keyboard != null && keyboard.backspaceKey.wasPressedThisFrame)
            {
                TryRemoveFocusedFixture(out _);
            }

            if (Mouse.current == null)
            {
                return;
            }

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > 0f)
            {
                TryRotateContext(1);
            }
            else if (scroll < 0f)
            {
                TryRotateContext(-1);
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (firstPersonController == null || viewCamera == null || stocking == null ||
                fixturePlacementMode == null)
            {
                error =
                    "First-store interaction requires explicit player, camera, stocking, and fixture-placement references.";
                return false;
            }

            if (pickupDistance <= 0f)
            {
                error = "First-store interaction distance must be positive.";
                return false;
            }

            error = null;
            return true;
        }

        public bool RefreshFocus()
        {
            if (!IsWorldInteractionEnabled || viewCamera == null)
            {
                ClearFocus();
                return false;
            }

            candidates.Clear();
            Ray ray = new(viewCamera.transform.position, viewCamera.transform.forward);
            if (fixturePlacementMode != null && fixturePlacementMode.IsActive)
            {
                HeldProduct?.ClearPlacementPreview();
                fixturePlacementMode.TryRefreshPreview(ray, out _);
                focusedTarget = fixturePlacementMode;
                currentPrompt = fixturePlacementMode.Prompt;
                return true;
            }

            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                pickupDistance,
                pickupLayers,
                QueryTriggerInteraction.Ignore);

            for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
            {
                RaycastHit hit = hits[hitIndex];
                IFirstStoreWorldInteractionTarget explicitTarget =
                    FindExplicitTarget(hit.collider);
                if (explicitTarget != null)
                {
                    candidates.Add(new FirstStoreWorldInteractionCandidate(
                        explicitTarget,
                        hit.distance));
                    continue;
                }

                ProductItem product = hit.collider.GetComponentInParent<ProductItem>();
                if (product != null)
                {
                    candidates.Add(new FirstStoreWorldInteractionCandidate(
                        new LooseProductTarget(stocking, product),
                        hit.distance));
                }
            }

            IFirstStoreWorldInteractionTarget selected =
                FirstStoreWorldInteractionTargetResolver.Resolve(candidates);
            if (!ReferenceEquals(selected, focusedTarget) &&
                !string.Equals(
                    selected?.StableTargetId,
                    focusedTarget?.StableTargetId,
                    StringComparison.Ordinal))
            {
                HeldProduct?.ClearPlacementPreview();
            }

            focusedTarget = selected;
            currentPrompt = focusedTarget?.Prompt;
            if (focusedTarget == null)
            {
                LastFeedback = null;
            }
            return focusedTarget != null;
        }

        public bool TryPrimaryInteraction(out string error)
        {
            if (!IsWorldInteractionEnabled)
            {
                error = "World interaction is disabled while the development HUD owns input.";
                LastFeedback = error;
                return false;
            }

            if (focusedTarget == null && !RefreshFocus())
            {
                error = "No world interaction target is focused.";
                LastFeedback = error;
                return false;
            }

            string targetId = focusedTarget.StableTargetId;
            string action = focusedTarget.Prompt?.Action ?? "Interact";
            bool success = focusedTarget.TryPrimary(out error);
            RecordInteraction(success, targetId, action, error);
            RefreshFocus();
            return success;
        }

        public bool TryCancelInteraction(out string error)
        {
            if (!IsWorldInteractionEnabled)
            {
                error = "World interaction is disabled while the development HUD owns input.";
                LastFeedback = error;
                return false;
            }

            if (focusedTarget == null && !RefreshFocus())
            {
                error = "No context action can be cancelled.";
                LastFeedback = error;
                return false;
            }

            string targetId = focusedTarget.StableTargetId;
            string action = focusedTarget.Prompt?.Action ?? "Cancel";
            bool success = focusedTarget.TryCancel(out error);
            RecordInteraction(success, targetId, action, error);
            RefreshFocus();
            return success;
        }

        public bool TryPickUpTargetedUnit(
            out ProductItem selectedUnit,
            out string error)
        {
            selectedUnit = null;
            if (!IsWorldInteractionEnabled)
            {
                error = "World interaction is disabled while the development HUD owns input.";
                return false;
            }

            Ray ray = new(viewCamera.transform.position, viewCamera.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                pickupDistance,
                pickupLayers,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, static (left, right) =>
                left.distance.CompareTo(right.distance));

            ProductItem targetedUnit = null;
            for (int index = 0; index < hits.Length; index++)
            {
                targetedUnit = hits[index].collider.GetComponentInParent<ProductItem>();
                if (targetedUnit != null)
                {
                    break;
                }
            }

            if (targetedUnit == null)
            {
                error = "No loose physical product unit is targeted.";
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
                error = "World interaction is disabled while the development HUD owns input.";
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
            bool changed =
                held != null && held.AdjustQuarterTurns(direction > 0 ? 1 : -1);
            if (changed)
            {
                RefreshFocus();
            }
            return changed;
        }

        public bool TryRemoveFocusedFixture(out string error)
        {
            if (!IsWorldInteractionEnabled)
            {
                error = "World interaction is disabled while the development HUD owns input.";
                LastFeedback = error;
                return false;
            }

            if (fixturePlacementMode != null && fixturePlacementMode.IsActive)
            {
                error = "Confirm or cancel the active fixture placement before removing a fixture.";
                LastFeedback = error;
                return false;
            }

            if (focusedTarget == null && !RefreshFocus())
            {
                error = "No placed fixture is focused for removal.";
                LastFeedback = error;
                return false;
            }

            if (focusedTarget is not IFirstStoreRemovableWorldInteractionTarget removable)
            {
                error = "The focused target is not a removable fixture.";
                LastFeedback = error;
                return false;
            }

            string targetId = focusedTarget.StableTargetId;
            bool success = removable.TryRemove(out error);
            RecordInteraction(success, targetId, "Remove fixture", error);
            RefreshFocus();
            return success;
        }

        private bool TryRotateContext(int direction)
        {
            if (fixturePlacementMode != null && fixturePlacementMode.IsActive)
            {
                bool changed = fixturePlacementMode.AdjustQuarterTurns(
                    direction,
                    out string error);
                RecordInteraction(
                    changed,
                    fixturePlacementMode.StableTargetId,
                    "Rotate fixture",
                    error);
                RefreshFocus();
                return changed;
            }

            bool rotated = TryRotateHeldUnit(direction);
            if (rotated)
            {
                RecordInteraction(
                    true,
                    HeldProduct?.PhysicalUnitId,
                    "Rotate product",
                    null);
            }
            return rotated;
        }

        private void CancelActiveFixturePlacement()
        {
            if (fixturePlacementMode != null && fixturePlacementMode.IsActive)
            {
                fixturePlacementMode.TryCancel(out _);
            }
        }

        public void ResetTransientStateAfterRestore()
        {
            fixturePlacementMode?.ResetTransientStateAfterRestore();
            ClearFocus();
        }

        private void ClearFocus()
        {
            HeldProduct?.ClearPlacementPreview();
            candidates.Clear();
            focusedTarget = null;
            currentPrompt = null;
            LastFeedback = null;
        }

        private void RecordInteraction(
            bool succeeded,
            string targetId,
            string action,
            string error)
        {
            LastFeedback = succeeded ? null : error;
            FeedbackRevision++;
            string message = succeeded
                ? action
                : string.IsNullOrWhiteSpace(error)
                    ? "That action is unavailable."
                    : error;
            InteractionResolved?.Invoke(new FirstStoreInteractionFeedback(
                succeeded,
                targetId,
                action,
                message));
        }

        private static IFirstStoreWorldInteractionTarget FindExplicitTarget(
            Collider collider)
        {
            if (collider == null)
            {
                return null;
            }

            MonoBehaviour[] behaviours =
                collider.GetComponentsInParent<MonoBehaviour>(true);
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IFirstStoreWorldInteractionTarget target)
                {
                    return target;
                }
            }
            return null;
        }

        private sealed class LooseProductTarget : IFirstStoreWorldInteractionTarget
        {
            private readonly StockingController stocking;
            private readonly ProductItem product;

            public LooseProductTarget(
                StockingController stocking,
                ProductItem product)
            {
                this.stocking = stocking;
                this.product = product;
            }

            public string StableTargetId => product?.PhysicalUnitId;
            public FirstStoreWorldInteractionPriority Priority =>
                FirstStoreWorldInteractionPriority.LooseProduct;
            public bool IsAvailable =>
                stocking != null &&
                product != null &&
                !product.IsHeld &&
                !product.IsSnapped;
            public FirstStoreWorldInteractionPrompt Prompt =>
                new(
                    "E",
                    $"Pick up {DisplayName}");

            public bool TryPrimary(out string error)
            {
                return stocking.TryPickUpLooseUnit(product, out _, out error);
            }

            public bool TryCancel(out string error)
            {
                error = "Loose-product pickup has no cancel action.";
                return false;
            }

            private string DisplayName =>
                string.IsNullOrWhiteSpace(product?.Definition?.DisplayName)
                    ? "product"
                    : product.Definition.DisplayName;
        }
    }
}
