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
        [SerializeField] private PlayerCarryableToolController toolCarrier;
        [SerializeField] private FirstStoreFixturePlacementModeController fixturePlacementMode;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string inputActionMapName = "Player";
        [SerializeField] private string interactActionName = "Interact";
        [SerializeField] private string cancelActionName = "Cancel";
        [SerializeField] private string buildModeActionName = "BuildMode";
        [SerializeField] private string rotatePlacementActionName = "RotatePlacement";
        [SerializeField, Min(0.1f)] private float pickupDistance = 3f;
        [SerializeField] private LayerMask pickupLayers = ~0;

        private readonly List<FirstStoreWorldInteractionCandidate> candidates = new();
        private IFirstStoreWorldInteractionTarget focusedTarget;
        private FirstStoreWorldInteractionPrompt currentPrompt;
        private Transform focusedWorldTransform;
        private Vector3 focusedWorldPoint;
        private Vector3 focusedWorldNormal;
        private bool hasFocusedWorldPoint;
        private InputAction interactAction;
        private InputAction cancelAction;
        private InputAction buildModeAction;
        private InputAction rotatePlacementAction;
        private bool ownsFallbackActions;

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
        public Transform FocusedWorldTransform => focusedWorldTransform;
        public Vector3 FocusedWorldPoint => focusedWorldPoint;
        public Vector3 FocusedWorldNormal => focusedWorldNormal;
        public bool HasFocusedWorldPoint => hasFocusedWorldPoint;

        private void OnEnable()
        {
            if (!TryResolveInputActions(out string error))
            {
                Debug.LogError(error, this);
            }
        }

        private void Start()
        {
            if (!TryValidateConfiguration(out string error))
            {
                Debug.LogError(error, this);
            }
        }

        private void OnDisable()
        {
            fixturePlacementMode?.TrySetBuildMode(false, out _);
            SetInputActionsEnabled(false);
            ClearFocus();
        }

        private void OnDestroy()
        {
            if (!ownsFallbackActions)
            {
                return;
            }

            interactAction?.Dispose();
            cancelAction?.Dispose();
            buildModeAction?.Dispose();
            rotatePlacementAction?.Dispose();
        }

        private void Update()
        {
            if (!IsWorldInteractionEnabled)
            {
                fixturePlacementMode?.TrySetBuildMode(false, out _);
                ClearFocus();
                return;
            }

            if (buildModeAction != null && buildModeAction.WasPressedThisFrame())
            {
                TryToggleBuildMode(out _);
            }

            RefreshFocus();

            if (interactAction != null && interactAction.WasPressedThisFrame())
            {
                TryPrimaryInteraction(out _);
            }
            if (cancelAction != null && cancelAction.WasPressedThisFrame())
            {
                TryCancelInteraction(out _);
            }

            float scroll = rotatePlacementAction?.ReadValue<float>() ?? 0f;
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
                toolCarrier == null || fixturePlacementMode == null)
            {
                error =
                    "First-store interaction requires explicit player, camera, stocking, tool-carrying, and fixture-placement references.";
                return false;
            }

            if (pickupDistance <= 0f)
            {
                error = "First-store interaction distance must be positive.";
                return false;
            }

            if (!TryResolveInputActions(out error))
            {
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
                focusedWorldTransform = fixturePlacementMode.ActiveFixture?.transform;
                focusedWorldPoint = focusedWorldTransform != null
                    ? focusedWorldTransform.position
                    : ray.origin + ray.direction * Mathf.Min(2f, pickupDistance);
                focusedWorldNormal = -ray.direction;
                hasFocusedWorldPoint = true;
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
                    bool isFixtureTarget =
                        explicitTarget.Priority == FirstStoreWorldInteractionPriority.Fixture;
                    if (fixturePlacementMode.IsBuildModeActive != isFixtureTarget)
                    {
                        continue;
                    }

                    candidates.Add(new FirstStoreWorldInteractionCandidate(
                        explicitTarget,
                        hit.distance));
                    continue;
                }

                if (fixturePlacementMode.IsBuildModeActive)
                {
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
            ResolveFocusedWorldPoint(hits);
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
                error = "Return to the store before interacting.";
                LastFeedback = error;
                return false;
            }

            if (focusedTarget == null && !RefreshFocus())
            {
                error = "No world interaction target is focused.";
                LastFeedback = error;
                return false;
            }

            if (toolCarrier != null && toolCarrier.HasHeldTool &&
                focusedTarget.Priority != FirstStoreWorldInteractionPriority.Cleaning &&
                focusedTarget.Priority != FirstStoreWorldInteractionPriority.Operating &&
                focusedTarget.Priority != FirstStoreWorldInteractionPriority.Tool)
            {
                error = $"Put down {toolCarrier.HeldToolName} before using that object.";
                RecordInteraction(
                    false,
                    focusedTarget.StableTargetId,
                    focusedTarget.Prompt?.Action ?? "Interact",
                    error);
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
                error = "Return to the store before interacting.";
                LastFeedback = error;
                return false;
            }

            if (fixturePlacementMode != null && fixturePlacementMode.IsActive)
            {
                string placementTargetId = fixturePlacementMode.StableTargetId;
                bool shouldPlace =
                    fixturePlacementMode.HasPreview &&
                    fixturePlacementMode.PreviewResult != null &&
                    fixturePlacementMode.PreviewResult.IsSuccess;
                bool resolved = shouldPlace
                    ? fixturePlacementMode.TryConfirm(out error)
                    : fixturePlacementMode.TryCancel(out error);
                RecordInteraction(
                    resolved,
                    placementTargetId,
                    shouldPlace ? "Place fixture" : "Cancel fixture move",
                    error);
                RefreshFocus();
                return resolved;
            }

            if (toolCarrier != null && toolCarrier.HasHeldTool)
            {
                string toolId = toolCarrier.HeldTool?.StableToolId;
                bool released = toolCarrier.TrySetDownHeldTool(out error);
                RecordInteraction(
                    released,
                    toolId,
                    "Put down tool",
                    error);
                RefreshFocus();
                return released;
            }

            if (stocking != null && stocking.PlayerHasHeldUnit)
            {
                string productId = stocking.HeldPhysicalUnit?.PhysicalUnitId;
                bool released = stocking.TrySetDownPlayerHeldUnit(out _, out error);
                RecordInteraction(
                    released,
                    productId,
                    "Put down product",
                    error);
                RefreshFocus();
                return released;
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
                error = "Return to the store before interacting.";
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
                error = "Return to the store before interacting.";
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
                error = "Return to the store before interacting.";
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

        public bool TryToggleBuildMode(out string error)
        {
            if (!IsWorldInteractionEnabled || fixturePlacementMode == null)
            {
                error = "Return to the store before changing Build Mode.";
                LastFeedback = error;
                return false;
            }

            if (!fixturePlacementMode.IsBuildModeActive &&
                ((stocking != null && stocking.HasHeldUnit) ||
                 (toolCarrier != null && toolCarrier.HasHeldTool)))
            {
                error = "Put down the carried product or tool before entering Build Mode.";
                RecordInteraction(
                    false,
                    fixturePlacementMode.StableTargetId,
                    "Enter Build Mode",
                    error);
                return false;
            }

            bool wasActive = fixturePlacementMode.IsBuildModeActive;
            bool changed = fixturePlacementMode.TryToggleBuildMode(out error);
            RecordInteraction(
                changed,
                fixturePlacementMode.StableTargetId,
                wasActive ? "Exit Build Mode" : "Enter Build Mode",
                error);
            RefreshFocus();
            return changed;
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

        private bool TryResolveInputActions(out string error)
        {
            if (interactAction != null && cancelAction != null &&
                buildModeAction != null && rotatePlacementAction != null)
            {
                SetInputActionsEnabled(true);
                error = null;
                return true;
            }

            if (inputActions != null)
            {
                InputActionMap actionMap = inputActions.FindActionMap(
                    inputActionMapName,
                    false);
                interactAction = actionMap?.FindAction(interactActionName, false);
                cancelAction = actionMap?.FindAction(cancelActionName, false);
                buildModeAction = actionMap?.FindAction(buildModeActionName, false);
                rotatePlacementAction = actionMap?.FindAction(
                    rotatePlacementActionName,
                    false);
                ownsFallbackActions = false;
            }
            else
            {
                interactAction = new InputAction(
                    interactActionName,
                    InputActionType.Button,
                    "<Keyboard>/e");
                cancelAction = new InputAction(
                    cancelActionName,
                    InputActionType.Button,
                    "<Keyboard>/q");
                buildModeAction = new InputAction(
                    buildModeActionName,
                    InputActionType.Button,
                    "<Keyboard>/b");
                rotatePlacementAction = new InputAction(
                    rotatePlacementActionName,
                    InputActionType.Value,
                    "<Mouse>/scroll/y");
                ownsFallbackActions = true;
            }

            if (interactAction == null || cancelAction == null ||
                buildModeAction == null || rotatePlacementAction == null)
            {
                error =
                    $"Input action map '{inputActionMapName}' must define '{interactActionName}', " +
                    $"'{cancelActionName}', '{buildModeActionName}', and '{rotatePlacementActionName}'.";
                return false;
            }

            SetInputActionsEnabled(true);
            error = null;
            return true;
        }

        private void SetInputActionsEnabled(bool enabled)
        {
            InputAction[] actions =
            {
                interactAction,
                cancelAction,
                buildModeAction,
                rotatePlacementAction
            };
            foreach (InputAction action in actions)
            {
                if (action == null)
                {
                    continue;
                }

                if (enabled)
                {
                    action.Enable();
                }
                else
                {
                    action.Disable();
                }
            }
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
            focusedWorldTransform = null;
            focusedWorldPoint = default;
            focusedWorldNormal = default;
            hasFocusedWorldPoint = false;
            LastFeedback = null;
        }

        private void ResolveFocusedWorldPoint(RaycastHit[] hits)
        {
            focusedWorldTransform = null;
            focusedWorldPoint = default;
            focusedWorldNormal = default;
            hasFocusedWorldPoint = false;
            if (focusedTarget == null || hits == null)
            {
                return;
            }

            for (int index = 0; index < hits.Length; index++)
            {
                RaycastHit hit = hits[index];
                IFirstStoreWorldInteractionTarget explicitTarget =
                    FindExplicitTarget(hit.collider);
                if (MatchesFocusedTarget(explicitTarget))
                {
                    focusedWorldTransform =
                        explicitTarget is Component component
                            ? component.transform
                            : hit.collider.transform;
                    focusedWorldPoint = hit.point;
                    focusedWorldNormal = hit.normal;
                    hasFocusedWorldPoint = true;
                    return;
                }

                ProductItem product = hit.collider.GetComponentInParent<ProductItem>();
                if (product != null &&
                    string.Equals(
                        product.PhysicalUnitId,
                        focusedTarget.StableTargetId,
                        StringComparison.Ordinal))
                {
                    focusedWorldTransform = product.transform;
                    focusedWorldPoint = hit.point;
                    focusedWorldNormal = hit.normal;
                    hasFocusedWorldPoint = true;
                    return;
                }
            }
        }

        private bool MatchesFocusedTarget(
            IFirstStoreWorldInteractionTarget candidate)
        {
            return candidate != null &&
                   (ReferenceEquals(candidate, focusedTarget) ||
                    string.Equals(
                        candidate.StableTargetId,
                        focusedTarget.StableTargetId,
                        StringComparison.Ordinal));
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

        private IFirstStoreWorldInteractionTarget FindExplicitTarget(
            Collider collider)
        {
            if (collider == null)
            {
                return null;
            }

            MonoBehaviour[] behaviours =
                collider.GetComponentsInParent<MonoBehaviour>(true);
            IFirstStoreWorldInteractionTarget selected = null;
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is not IFirstStoreWorldInteractionTarget target ||
                    !target.IsAvailable)
                {
                    continue;
                }

                bool isFixtureTarget =
                    target.Priority == FirstStoreWorldInteractionPriority.Fixture;
                if ((fixturePlacementMode?.IsBuildModeActive ?? false) !=
                    isFixtureTarget)
                {
                    continue;
                }

                if (selected == null || target.Priority < selected.Priority)
                {
                    selected = target;
                }
            }
            return selected;
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
